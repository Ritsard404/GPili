namespace GPili.Presentation.Contents;

public partial class UserFooterView : ContentView, IDisposable
{
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();

    public UserFooterView()
    {
        InitializeComponent();

        User.Text = ("User: " + CashierState.Info.CashierName) ?? "Unknown User";
        PosName.Text = $"POS: {POSInfo.Terminal.PosName}{(POSInfo.Terminal.IsTrainMode ? " (Training)" : "")}";

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
                var isConnected = Connectivity.NetworkAccess == NetworkAccess.Internet;
                if (Date != null)
                {
                    // Use MainThread to ensure UI update is on the correct thread
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Date.Text = "Date: " + now.ToString("dd/MM/yyyy(ddd) hh:mm:ss");
                        NetworkStatus.Text = isConnected ? "Online" : "Offline";
                        NetworkStatus.TextColor = isConnected ? Colors.Green : Colors.Red;
                    });
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Timer was cancelled, safe to ignore
        }
        catch (Exception ex)
        {
            // Optionally log or handle unexpected exceptions
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _timer?.Dispose();
        _cts.Dispose();
    }
}