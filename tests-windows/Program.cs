using KeyboardSwitcher;

internal static class Program {
    private static int count;
    private static void Check(bool value, string name) {
        if (!value) throw new InvalidOperationException(name);
        count++; Console.WriteLine("PASS " + name);
    }

    [STAThread]
    private static void Main() {
        ApplicationConfiguration.Initialize();
        using var form = new MainForm { AllowClose = true, ShowInTaskbar = false };
        _ = form.Handle;
        var layout = form.Controls.OfType<TableLayoutPanel>().Single();
        var list = (ListView)layout.GetControlFromPosition(0, 3)!;
        var actions = (FlowLayoutPanel)layout.GetControlFromPosition(0, 8)!;
        var detail = (Label)layout.GetControlFromPosition(0, 7)!;
        void CreateHandles(Control parent) {
            _ = parent.Handle;
            foreach (Control child in parent.Controls) CreateHandles(child);
        }
        CreateHandles(form);

        void Arrange(int width, int height, string? error = null) {
            form.ClientSize = new Size(width, height);
            form.UpdateState(new(true, true), true, false, error, SystemIcons.Application);
            form.PerformLayout(); layout.PerformLayout(); actions.PerformLayout();
            Application.DoEvents();
        }

        Arrange(570, 605);
        int initialHeight = list.Height;
        Arrange(1200, 800);
        Check(list.Height > initialHeight + 100, "mapping list grows with window height");

        foreach (var size in new[] { new Size(520, 540), new Size(570, 605), new Size(800, 650), new Size(1280, 800), new Size(1920, 1080) }) {
            Arrange(size.Width, size.Height);
            string name = $"{form.ClientSize.Width}x{form.ClientSize.Height}";
            Check(list.Width > 0 && list.Height >= 100, name + " mapping list usable");
            Check(list.Columns.Cast<ColumnHeader>().Sum(c => c.Width) <= list.ClientSize.Width, name + " columns fit");
            Check(actions.Bottom <= layout.ClientSize.Height - layout.Padding.Bottom, name + " footer visible");
            Check(actions.Top >= detail.Bottom, name + " status and footer do not overlap");
            foreach (Control button in actions.Controls)
                Check(actions.ClientRectangle.Contains(button.Bounds), name + " button fits: " + button.Text);
        }

        Arrange(570, 605, string.Join(" ", Enumerable.Repeat("A longer status message that must wrap without covering any buttons.", 4)));
        Check(detail.Height >= detail.GetPreferredSize(new Size(detail.Width, 0)).Height, "long status fits its row");
        Check(actions.Top >= detail.Bottom, "long status does not cover actions");
        Check(actions.Bottom <= layout.ClientSize.Height - layout.Padding.Bottom, "long status keeps footer visible");
        Arrange(1920, 1080);
        Arrange(570, 605);
        Check(list.Columns.Cast<ColumnHeader>().Sum(c => c.Width) <= list.ClientSize.Width, "native columns fit after shrinking a large window");
        Console.WriteLine($"{count} Windows layout assertions passed (DPI {form.DeviceDpi})");
    }
}
