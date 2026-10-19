namespace KeyboardSwitcher;

internal sealed class TrayApplication : ApplicationContext {
    private readonly MainForm form = new();
    private readonly NotifyIcon tray = new();
    private readonly ContextMenuStrip menu = new();
    private readonly ToolStripMenuItem function = new("F 键优先"), original = new("原始热键"), startup = new("登录后自动启动");
    private readonly Icon functionIcon = StatusIcons.Create("F", Color.FromArgb(26, 128, 85));
    private readonly Icon originalIcon = StatusIcons.Create("H", Color.FromArgb(45, 102, 190));
    private readonly Icon errorIcon = StatusIcons.Create("!", Color.Firebrick);
    private KeyboardHook? hook;
    private AppSettings settings = new();
    private string? error;
    private bool busy, closing;
    private readonly RegisteredWaitHandle wake;

    internal TrayApplication(EventWaitHandle showEvent, bool startHidden) {
        _ = form.Handle;
        try { settings = SettingsStore.Load(); } catch (Exception e) { settings = new(false, true); error = "设置读取失败：" + e.Message; }
        menu.Items.Add("打开控制窗口", null, (_, _) => form.Reveal());
        menu.Items.Add(new ToolStripSeparator()); menu.Items.Add(function); menu.Items.Add(original);
        menu.Items.Add(new ToolStripSeparator()); menu.Items.Add(startup);
        menu.Items.Add("重启监听", null, async (_, _) => await Restart());
        menu.Items.Add(new ToolStripSeparator()); menu.Items.Add("退出", null, (_, _) => Quit());
        menu.Opening += (_, _) => Render();
        function.Click += async (_, _) => await Change(settings with { FunctionMode = true });
        original.Click += async (_, _) => await Change(settings with { FunctionMode = false });
        startup.Click += (_, _) => SetStartup(!startup.Checked);
        form.ModeChanged += async value => await Change(settings with { FunctionMode = value });
        form.CompatibilityChanged += async value => await Change(settings with { Compatibility = value });
        form.StartupChanged += SetStartup;
        form.RestartRequested += async () => await Restart();
        form.ExitRequested += Quit;
        form.Activated += (_, _) => Render();
        tray.ContextMenuStrip = menu; tray.DoubleClick += (_, _) => form.Reveal();
        tray.Icon = originalIcon; tray.Text = "ThinkPad 键盘：正在启动"; tray.Visible = true;
        wake = ThreadPool.RegisterWaitForSingleObject(showEvent, (_, _) => OnUi(form.Reveal), null, Timeout.Infinite, false);
        if (!startHidden) form.Show();
        form.BeginInvoke(async () => await Restart());
    }
    private void OnUi(Action action) {
        if (closing || form.IsDisposed) return;
        try { form.BeginInvoke(action); } catch (InvalidOperationException) { }
    }
    private async Task Restart() {
        if (busy || closing) return;
        busy = true; error = null; Render();
        try {
            hook?.Dispose(); hook = new KeyboardHook();
            var current = hook;
            hook.Fault += message => OnUi(() => { if (ReferenceEquals(hook, current)) { error = message; Render(); } });
            await hook.Configure(settings.FunctionMode, settings.Compatibility);
        } catch (Exception e) { error = e.Message; }
        finally { busy = false; Render(); }
    }
    private async Task Change(AppSettings next) {
        if (busy || closing) return;
        if (hook is null) { await Restart(); if (hook is null) return; }
        busy = true; Render();
        try {
            await hook!.Configure(next.FunctionMode, next.Compatibility);
            settings = next; error = null; SettingsStore.Save(settings);
        } catch (Exception e) { error = e.Message; }
        finally { busy = false; Render(); }
    }
    private void SetStartup(bool value) {
        try { StartupRegistration.Set(value); }
        catch (Exception e) { MessageBox.Show(form, e.Message, "自动启动设置", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
        Render();
    }
    private void Render() {
        if (closing) return;
        bool registered = false;
        try { registered = StartupRegistration.Enabled; } catch (Exception e) { error = "启动项读取失败：" + e.Message; }
        var icon = error is not null ? errorIcon : settings.FunctionMode ? functionIcon : originalIcon;
        tray.Icon = icon;
        tray.Text = "ThinkPad 键盘：" + (error is not null ? "异常（双击查看）" : busy ? "切换中" : settings.FunctionMode ? "F 键优先" : "原始热键");
        function.Checked = settings.FunctionMode && error is null; original.Checked = !settings.FunctionMode && error is null;
        function.Enabled = original.Enabled = !busy; startup.Checked = registered;
        form.UpdateState(settings, registered, busy, error, icon);
    }
    private void Quit() { if (closing) return; closing = true; ExitThread(); }
    protected override void ExitThreadCore() {
        closing = true; wake.Unregister(null); tray.Visible = false;
        hook?.Dispose(); tray.Dispose(); menu.Dispose();
        form.AllowClose = true; form.Close(); form.Dispose();
        functionIcon.Dispose(); originalIcon.Dispose(); errorIcon.Dispose();
        base.ExitThreadCore();
    }
}
