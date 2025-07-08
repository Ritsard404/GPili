using PdfSharp.Snippets;

namespace GPili.Presentation.Contents;

public partial class UserFooterView : ContentView, IDisposable
{
    IDispatcherTimer? _timer;
    bool isRunning = true;

    public UserFooterView()
    {
        InitializeComponent();


        Device.StartTimer(TimeSpan.FromSeconds(1), () =>
        {
            UpdateTime();
            return isRunning; // keep the timer running
        });

        UpdateTime(); // set initial time immediately


    }

    protected override void OnParentSet()
    {
        base.OnParentSet();

        if (Parent == null)
            isRunning = false;

        //if (Parent != null)
        //    StartTimer();
        //else
        //    StopTimer();
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

    void UpdateTime()
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
    //void UpdateTime()
    //{
    //    var now = DateTime.Now;
    //    textTime.Text = now.ToString("HH:mm:ss");//hh:mm tt
    //    textDisplayTime.Text = now.ToString("hh:mm tt");//
    //    textDate.Text = now.ToString("dddd, MMMM dd yyyy");
    //    txtDate.Text = now.ToString("yyyy-MM-dd");
    //}
}