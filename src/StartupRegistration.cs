using Microsoft.Win32;

namespace KeyboardSwitcher;

internal static class StartupRegistration {
    internal const string KeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
    internal const string ValueName = "ThinkPadBluetoothKeyboardSwitcher";
    internal static string Command => $"\"{Environment.ProcessPath}\" --tray";
    internal static bool Enabled {
        get {
            using var key = Registry.CurrentUser.OpenSubKey(KeyPath);
            return string.Equals(key?.GetValue(ValueName) as string, Command, StringComparison.OrdinalIgnoreCase);
        }
    }
    internal static void Set(bool enabled) {
        using var key = Registry.CurrentUser.CreateSubKey(KeyPath, true);
        if (enabled) {
            if (Command.Length > 260) throw new InvalidOperationException("程序路径过长，无法注册登录启动项");
            key.SetValue(ValueName, Command, RegistryValueKind.String);
        } else {
            // Never remove a similarly named entry belonging to another installation.
            if (string.Equals(key.GetValue(ValueName) as string, Command, StringComparison.OrdinalIgnoreCase))
                key.DeleteValue(ValueName, false);
        }
        if (Enabled != enabled) throw new IOException("自动启动设置未能保存");
    }
}
