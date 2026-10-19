using System.ComponentModel;
using System.Runtime.InteropServices;

namespace KeyboardSwitcher;

internal sealed class KeyboardHook : IDisposable {
    private readonly MappingEngine engine = new();
    private readonly Native.HookProc callback;
    private readonly Thread thread;
    private readonly ManualResetEventSlim ready = new();
    private Control? dispatcher;
    private IntPtr hook;
    private Exception? startError;
    private bool disposed;
    private readonly F1Diagnostics? diagnostics;
    public event Action<string>? Fault;

    public KeyboardHook(F1Diagnostics? diagnostics = null) {
        this.diagnostics = diagnostics;
        callback = Handle;
        thread = new Thread(Loop) { IsBackground = true, Name = "Keyboard hook message loop" };
        thread.SetApartmentState(ApartmentState.STA); thread.Start();
        if (!ready.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("键盘监听线程启动超时");
        if (startError is not null) throw new InvalidOperationException("键盘监听启动失败", startError);
    }
    private void Loop() {
        try {
            using var control = new Control(); dispatcher = control; _ = control.Handle;
            foreach (int key in new[] { 0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0x5B, 0x5C })
                if ((Native.GetAsyncKeyState(key) & 0x8000) != 0) engine.SeedModifier(key);
            hook = Native.SetWindowsHookEx(Native.KeyboardHook, callback, Native.GetModuleHandle(null), 0);
            if (hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            ready.Set(); Application.Run();
        } catch (Exception error) { startError = error; ready.Set(); }
        finally {
            Native.Emit(engine.ReleaseAll());
            if (hook != IntPtr.Zero) Native.UnhookWindowsHookEx(hook);
            hook = IntPtr.Zero;
        }
    }
    public Task Configure(bool enabled, bool compatibility) {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        if (disposed || dispatcher is null || !thread.IsAlive) return Task.FromException(new InvalidOperationException("键盘监听线程已停止"));
        dispatcher.BeginInvoke(() => {
            try {
                bool success = Native.Emit(engine.Configure(enabled, compatibility));
                diagnostics?.Configure(enabled, compatibility, success);
                if (!success) throw new Win32Exception("无法释放映射按键，可能受到权限或系统限制");
                done.SetResult();
            } catch (Exception error) { done.SetException(error); }
        });
        return done.Task;
    }
    private IntPtr Handle(int code, IntPtr message, IntPtr pointer) {
        if (code < 0) return Native.CallNextHookEx(hook, code, message, pointer);
        int type = message.ToInt32();
        if (type is not (0x100 or 0x101 or 0x104 or 0x105)) return Native.CallNextHookEx(hook, code, message, pointer);
        var data = Marshal.PtrToStructure<Native.HookData>(pointer);
        bool down = type is 0x100 or 0x104;
        var input = new KeyEvent((int)data.Key, down, (data.Flags & 0x10) != 0,
            data.Extra == Native.Marker, (data.Flags & 0x02) != 0);
        if (MappingEngine.ShouldIgnore(input)) {
            diagnostics?.Key(data.Key, data.Scan, data.Flags, data.Extra == Native.Marker, down, "injected-bypass");
            return Native.CallNextHookEx(hook, code, message, pointer);
        }
        try {
            var result = engine.Process(input);
            if (result.Suppress) {
                bool success = Native.Emit(result.Output);
                diagnostics?.Key(data.Key, data.Scan, data.Flags, false, down, "mapped", result.Output, success);
                if (success) return (IntPtr)1;
                Fail("按键输出被系统阻止，已停止映射。管理员窗口可能受权限限制。");
            } else diagnostics?.Key(data.Key, data.Scan, data.Flags, false, down, "pass");
        } catch (Exception) { Fail("键盘映射异常，已停止映射。"); }
        return Native.CallNextHookEx(hook, code, message, pointer);
    }
    private void Fail(string message) {
        Native.Emit(engine.Configure(false, false));
        ThreadPool.QueueUserWorkItem(_ => Fault?.Invoke(message));
    }
    public void Dispose() {
        if (disposed) return; disposed = true;
        if (dispatcher is not null && thread.IsAlive) {
            try { dispatcher.BeginInvoke(() => Application.ExitThread()); } catch (InvalidOperationException) { }
            thread.Join(3000);
        }
        ready.Dispose();
    }
}
