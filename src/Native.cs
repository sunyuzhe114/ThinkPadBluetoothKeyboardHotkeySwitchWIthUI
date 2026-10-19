using System.Runtime.InteropServices;

namespace KeyboardSwitcher;

internal static class Native {
    internal const int KeyboardHook = 13;
    internal static readonly UIntPtr Marker = (UIntPtr)0x54504B53;
    internal delegate IntPtr HookProc(int code, IntPtr message, IntPtr data);
    [StructLayout(LayoutKind.Sequential)] internal struct HookData { public uint Key, Scan, Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct KeyboardInput { public ushort Key, Scan; public uint Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Sequential)] internal struct MouseInput { public int X, Y; public uint Data, Flags, Time; public UIntPtr Extra; }
    [StructLayout(LayoutKind.Explicit)] internal struct InputUnion {
        [FieldOffset(0)] public KeyboardInput Keyboard;
        [FieldOffset(0)] public MouseInput Mouse;
    }
    [StructLayout(LayoutKind.Sequential)] internal struct Input { public uint Type; public InputUnion Data; }
    [DllImport("user32.dll", SetLastError = true)] internal static extern IntPtr SetWindowsHookEx(int type, HookProc callback, IntPtr module, uint thread);
    [DllImport("user32.dll", SetLastError = true)] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] internal static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr message, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)] internal static extern uint SendInput(uint count, Input[] inputs, int size);
    [DllImport("user32.dll")] internal static extern short GetAsyncKeyState(int key);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] internal static extern IntPtr GetModuleHandle(string? name);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool DestroyIcon(IntPtr icon);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] internal static extern uint RegisterWindowMessage(string message);
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] internal static extern bool PostMessage(IntPtr handle, uint message, IntPtr wParam, IntPtr lParam);

    internal static bool Emit(IReadOnlyList<Stroke> strokes) {
        if (strokes.Count == 0) return true;
        var inputs = strokes.Select(stroke => new Input {
            Type = 1, Data = new InputUnion { Keyboard = new KeyboardInput {
                Key = (ushort)stroke.Key, Extra = Marker,
                Flags = (stroke.Down ? 0u : 2u) | (Extended(stroke.Key) ? 1u : 0u)
            }}
        }).ToArray();
        return SendInput((uint)inputs.Length, inputs, Marshal.SizeOf<Input>()) == inputs.Length;
    }
    private static bool Extended(int key) => key is 0x5B or 0x5C or 0x5D or 0xA3 or 0xA5 || (key >= 0xA6 && key <= 0xB3);
}
