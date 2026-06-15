namespace WinTextCapture;

internal sealed class ToastForm : Form
{
    private readonly System.Windows.Forms.Timer _timer = new();

    public ToastForm(string message, Theme theme)
    {
        FormBorderStyle = FormBorderStyle.None;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.Manual;
        TopMost = true;
        Font = new Font("Segoe UI", 9f);
        BackColor = theme.Surface;
        ForeColor = theme.Text;
        Padding = new Padding(14, 10, 14, 10);
        AutoSize = true;
        AutoSizeMode = AutoSizeMode.GrowAndShrink;

        var label = new Label
        {
            AutoSize = true,
            MaximumSize = new Size(360, 0),
            Text = message,
            ForeColor = theme.Text,
            BackColor = theme.Surface,
        };
        Controls.Add(label);

        _timer.Interval = 2200;
        _timer.Tick += (s, e) => Close();
    }

    protected override bool ShowWithoutActivation => true;

    protected override CreateParams CreateParams
    {
        get
        {
            const int WS_EX_NOACTIVATE = 0x08000000;
            var cp = base.CreateParams;
            cp.ExStyle |= WS_EX_NOACTIVATE;
            return cp;
        }
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        Theming.DarkTitleBar(this, BackColor.R < 128);
        var area = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(area.Right - Width - 18, area.Bottom - Height - 18);
        _timer.Start();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        using var border = new Pen(Color.FromArgb(90, ForeColor));
        e.Graphics.DrawRectangle(border, 0, 0, Width - 1, Height - 1);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _timer.Dispose();
        base.Dispose(disposing);
    }
}
