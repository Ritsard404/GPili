namespace GPili.Presentation.Contents;

public partial class UserFooterView : ContentView, IDisposable
{
    private readonly PeriodicTimer _timer;
    public UserFooterView()
    {
        InitializeComponent();

        SysVer.Text = "System Version " + DeviceInfo.Current.Version.ToString();
        Shift.Text = "Shift: " + DeviceInfo.Current.Platform.ToString();
        User.Text = ("User: " + CashierState.CashierName) ?? "Unknown User";
        PosName.Text = "POS: " + POSInfo.Terminal.PosName;

        _timer = new PeriodicTimer(TimeSpan.FromSeconds(1));
        UpdateDate();
    }
    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent == null)
        {
            Dispose();
        }
    }

    private async void UpdateDate()
    {
        try
        {
            while (await _timer.WaitForNextTickAsync(_cts.Token))
            {
                var now = DateTime.Now;
                Dispatcher.Dispatch(() =>
                {
                    if (Date != null)
                        Date.Text = "Date: " + now.ToString("dd/MM/yyyy(ddd) hh:mm:ss");
                });
            }
        }
        catch (OperationCanceledException)
        {
            // Timer was cancelled, safe to ignore
        }
    }
    private readonly CancellationTokenSource _cts = new(); 
    
    public void Dispose()
    {
        _cts.Cancel();
        _timer?.Dispose();
        _cts.Dispose();
    }
}