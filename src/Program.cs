using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Text.Json;

namespace KeyboardSwitcher;

internal static class Program {
    [STAThread]
    private static int Main(string[] args) {
        ApplicationConfiguration.Initialize();
        if (args.Length >= 2 && args[0] == "--self-test") return SelfTest(args[1]);
        string user = WindowsIdentity.GetCurrent().User?.Value ?? Environment.UserName;
        using var mutex = new Mutex(true, @"Local\ThinkPadKeyboardSwitcher-" + user, out bool first);
        using var showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, @"Local\ThinkPadKeyboardSwitcherShow-" + user);
        try {
            if (args.Contains("--enable-startup")) StartupRegistration.Set(true);
            if (args.Contains("--disable-startup")) StartupRegistration.Set(false);
            if (!first) { if (!args.Contains("--tray")) showEvent.Set(); return 0; }
            int diagnosticIndex = Array.IndexOf(args, "--diagnose-f1");
            if (diagnosticIndex >= 0 && diagnosticIndex + 1 >= args.Length)
                throw new ArgumentException("--diagnose-f1 requires an output JSON path");
            string? diagnosticPath = diagnosticIndex < 0 ? null : Path.GetFullPath(args[diagnosticIndex + 1]);
            Application.Run(new TrayApplication(showEvent, args.Contains("--tray"), diagnosticPath));
            return 0;
        } catch (Exception error) {
            MessageBox.Show(error.Message, "ThinkPad 键盘切换器", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return 1;
        } finally { if (first) mutex.ReleaseMutex(); }
    }
    private static int SelfTest(string output) {
        try {
            if (Marshal.SizeOf<Native.Input>() != (Environment.Is64BitProcess ? 40 : 28)) throw new Exception("INPUT layout mismatch");
            using (var hook = new KeyboardHook()) hook.Configure(false, true).GetAwaiter().GetResult();
            using var icon = StatusIcons.Create("F", Color.SeaGreen);
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(output))!);
            File.WriteAllText(output, JsonSerializer.Serialize(new { status = "PASS", inputBytes = Marshal.SizeOf<Native.Input>(), hookInstallAndRemove = true, iconWidth = icon.Width, emittedKeys = 0 }));
            return 0;
        } catch (Exception e) { File.WriteAllText(output, JsonSerializer.Serialize(new { status = "FAIL", error = e.Message })); return 1; }
    }
}
