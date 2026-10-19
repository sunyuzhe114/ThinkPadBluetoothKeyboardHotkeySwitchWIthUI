namespace KeyboardSwitcher;

internal sealed class MainForm : Form {
    private readonly Label state = new(), detail = new();
    private readonly RadioButton function = new(), original = new();
    private readonly CheckBox startup = new(), compatibility = new();
    private readonly Button restart = new();
    private bool rendering;
    private FormWindowState restoreState = FormWindowState.Normal;
    internal bool AllowClose { get; set; }
    internal event Action<bool>? ModeChanged, StartupChanged, CompatibilityChanged;
    internal event Action? RestartRequested, ExitRequested;

    internal MainForm() {
        Text = "ThinkPad 蓝牙键盘切换器";
        Font = new Font("Microsoft YaHei UI", 10F);
        AutoScaleDimensions = new SizeF(96F, 96F);
        AutoScaleMode = AutoScaleMode.Dpi;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(570, 605);
        MinimumSize = SizeFromClientSize(new Size(520, 540));
        BackColor = Color.FromArgb(247, 249, 248);
        var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(24, 20, 24, 16), ColumnCount = 1, RowCount = 9 };
        layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int row = 0; row < 9; row++)
            layout.RowStyles.Add(row == 3 ? new RowStyle(SizeType.Percent, 100) : new RowStyle(SizeType.AutoSize));
        Controls.Add(layout);
        var title = new Label { Text = "ThinkPad 键盘", Font = new Font(Font.FontFamily, 19, FontStyle.Bold), Dock = DockStyle.Fill, AutoSize = true, Margin = new Padding(3, 3, 3, 8) };
        layout.Controls.Add(title, 0, 0);
        state.Dock = DockStyle.Fill; state.AutoSize = true; state.Font = new Font(Font.FontFamily, 11, FontStyle.Bold); state.Name = "modeStatus";
        layout.Controls.Add(state, 0, 1);
        var modes = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = true, Padding = new Padding(0, 6, 0, 6) };
        function.Text = "F 键优先"; original.Text = "原始热键";
        foreach (var radio in new[] { function, original }) { radio.AutoSize = true; radio.Margin = new Padding(0, 0, 32, 0); modes.Controls.Add(radio); }
        function.CheckedChanged += (_, _) => { if (!rendering && function.Checked) ModeChanged?.Invoke(true); };
        original.CheckedChanged += (_, _) => { if (!rendering && original.Checked) ModeChanged?.Invoke(false); };
        layout.Controls.Add(modes, 0, 2);
        var list = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false, BorderStyle = BorderStyle.FixedSingle, HeaderStyle = ColumnHeaderStyle.Nonclickable, BackColor = Color.White, TabStop = false };
        list.Columns.Add("功能键", 100); list.Columns.Add("对应热键", 360);
        string[] labels = ["静音", "降低音量", "提高音量", "上一曲", "播放 / 暂停", "下一曲", "浏览器后退", "浏览器主页", "菜单", "Shift + Win + F21（兼容）", "Ctrl + Alt + Tab（兼容）"];
        for (int i = 0; i < labels.Length; i++) { var item = new ListViewItem("F" + (i + 1)); item.SubItems.Add(labels[i]); list.Items.Add(item); }
        bool columnsQueued = false;
        void FitColumns() {
            int available = Math.Max(0, list.ClientSize.Width - SystemInformation.VerticalScrollBarWidth - 2);
            int first = Math.Min(available, Math.Clamp(available / 5, (int)(90 * DeviceDpi / 96f), (int)(150 * DeviceDpi / 96f)));
            list.Columns[0].Width = first; list.Columns[1].Width = available - first;
        }
        void QueueColumns() {
            if (columnsQueued || !list.IsHandleCreated || list.IsDisposed) return;
            columnsQueued = true;
            // Native list-view sizing and DPI updates can finish after the form's resize event.
            list.BeginInvoke(() => { columnsQueued = false; if (!list.IsDisposed) FitColumns(); });
        }
        list.ClientSizeChanged += (_, _) => QueueColumns();
        list.HandleCreated += (_, _) => QueueColumns();
        layout.Layout += (_, _) => QueueColumns();
        layout.Controls.Add(list, 0, 3);
        compatibility.Text = "兼容 F10 / F11 组合键"; compatibility.AutoSize = true; compatibility.Dock = DockStyle.Fill; compatibility.Margin = new Padding(3, 8, 3, 4);
        compatibility.CheckedChanged += (_, _) => { if (!rendering) CompatibilityChanged?.Invoke(compatibility.Checked); };
        layout.Controls.Add(compatibility, 0, 4);
        startup.Text = "登录后自动启动"; startup.AutoSize = true; startup.Dock = DockStyle.Fill; startup.Margin = new Padding(3, 4, 3, 8);
        startup.CheckedChanged += (_, _) => { if (!rendering) StartupChanged?.Invoke(startup.Checked); };
        layout.Controls.Add(startup, 0, 5);
        layout.Controls.Add(new Label { Text = "作用范围：所有键盘    |    F12：保持原样", Dock = DockStyle.Fill, ForeColor = Color.FromArgb(92, 103, 99), AutoSize = true }, 0, 6);
        detail.Dock = DockStyle.Fill; detail.AutoSize = true; detail.Name = "operationStatus"; detail.Margin = new Padding(3, 8, 3, 8);
        layout.Controls.Add(detail, 0, 7);
        var actions = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, FlowDirection = FlowDirection.RightToLeft, WrapContents = true, Padding = new Padding(0, 6, 0, 0) };
        var exit = new Button { Text = "退出", AutoSize = true, MinimumSize = new Size(70, 32) };
        var hide = new Button { Text = "收起到托盘", AutoSize = true, MinimumSize = new Size(105, 32) };
        restart.Text = "重启监听"; restart.AutoSize = true; restart.MinimumSize = new Size(90, 32);
        exit.Click += (_, _) => ExitRequested?.Invoke(); hide.Click += (_, _) => Hide(); restart.Click += (_, _) => RestartRequested?.Invoke();
        actions.Controls.AddRange([exit, hide, restart]); layout.Controls.Add(actions, 0, 8);
        FormClosing += (_, e) => { if (!AllowClose && e.CloseReason == CloseReason.UserClosing) { e.Cancel = true; Hide(); } };
        Resize += (_, _) => {
            if (WindowState == FormWindowState.Minimized) { Hide(); WindowState = restoreState; }
            else restoreState = WindowState;
        };
    }
    internal void UpdateState(AppSettings settings, bool autoStart, bool busy, string? error, Icon icon) {
        rendering = true;
        try {
            function.Checked = settings.FunctionMode; original.Checked = !settings.FunctionMode;
            compatibility.Checked = settings.Compatibility; startup.Checked = autoStart;
            function.Enabled = original.Enabled = compatibility.Enabled = restart.Enabled = !busy;
            state.Text = error is not null ? "状态异常" : busy ? "正在切换…" : settings.FunctionMode ? "F 键优先 · 映射已启用" : "原始热键 · 不改写按键";
            state.ForeColor = error is not null ? Color.Firebrick : settings.FunctionMode ? Color.FromArgb(27, 122, 85) : Color.FromArgb(40, 91, 178);
            detail.Text = error ?? "键盘监听已安装 · 设置保存在当前用户账户";
            detail.ForeColor = error is not null ? Color.Firebrick : Color.FromArgb(92, 103, 99);
            Icon = icon;
        } finally { rendering = false; }
    }
    internal void Reveal() { Show(); if (WindowState == FormWindowState.Minimized) WindowState = restoreState; Activate(); }
}
