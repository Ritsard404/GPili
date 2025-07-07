namespace GPili.Presentation.Contents;

public partial class UserFooterView : ContentView, IDisposable
{
    IDispatcherTimer? _timer;

    public UserFooterView()
    {
        InitializeComponent();

    }

    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent != null)
            StartTimer();
        else
            StopTimer();
    }

    void StartTimer()
    {
        if (_timer is not null)
            return;

        _timer = Dispatcher.CreateTimer();
        _timer.Interval = TimeSpan.FromSeconds(1);
        _timer.Tick += OnTick;
        _timer.Start();
    }

    void StopTimer()
    {
        if (_timer is null)
            return;

        _timer.Stop();
        _timer.Tick -= OnTick;
        _timer = null;
    }

    void OnTick(object? sender, EventArgs e)
    {
        var now = DateTime.Now;
        var isConnected = Connectivity.NetworkAccess == NetworkAccess.Internet;

        Date.Text = $"Date: {now:dd/MM/yyyy(ddd) hh:mm:ss tt}";

        User.Text = ("User: " + CashierState.Info.CashierName) ?? "Unknown User";

        PosName.Text = $"POS: {POSInfo.Terminal.PosName}" +
                              (POSInfo.Terminal.IsTrainMode ? " (Training)" : "");
        NetworkStatus.Text = isConnected ? "Online" : "Offline";
        NetworkStatus.TextColor = isConnected ? Colors.Green : Colors.Red;

    }

    public void Dispose()
    {
        StopTimer();
    }
}