namespace WinTextCapture;

/// <summary>Hidden UI-thread owner for modal capture overlays.</summary>
internal sealed class BackgroundForm : Form
{
    private readonly Action<CaptureOutcome> _completed;
    private bool _capturing;

    public BackgroundForm(Action<CaptureOutcome> completed)
    {
        _completed = completed;
        Opacity = 0;
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        _ = Handle;
    }

    public void StartCapture(Settings settings)
    {
        if (!IsHandleCreated) return;

        if (_capturing)
        {
            _completed(CaptureOutcome.Busy);
            return;
        }

        BeginInvoke((MethodInvoker)(() =>
        {
            if (_capturing) return;
            _capturing = true;
            CaptureOutcome result = CaptureOutcome.Error;
            try
            {
                using var overlay = new OverlayForm(settings);
                result = overlay.ShowDialog(this) == DialogResult.OK ? overlay.Result : CaptureOutcome.Cancelled;
            }
            finally
            {
                _capturing = false;
                _completed(result);
            }
        }));
    }
}
