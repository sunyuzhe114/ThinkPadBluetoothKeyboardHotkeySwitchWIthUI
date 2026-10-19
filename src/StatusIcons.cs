using System.Drawing.Drawing2D;
using System.Drawing.Text;

namespace KeyboardSwitcher;

internal static class StatusIcons {
    internal static Icon Create(string text, Color color) {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = SmoothingMode.AntiAlias;
        graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
        graphics.Clear(Color.Transparent);
        using var brush = new SolidBrush(color);
        graphics.FillEllipse(brush, 1, 1, 30, 30);
        using var font = new Font("Segoe UI", 21, FontStyle.Bold, GraphicsUnit.Pixel);
        using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        graphics.DrawString(text, font, Brushes.White, new RectangleF(0, -1, 32, 33), format);
        IntPtr handle = bitmap.GetHicon();
        try { using var original = Icon.FromHandle(handle); return (Icon)original.Clone(); }
        finally { Native.DestroyIcon(handle); }
    }
}
