using GPili.Utils.State;
using static Microsoft.Maui.ApplicationModel.Permissions;

namespace GPili.Presentation.Contents;

public partial class FooterView : ContentView, IDisposable
{
    private readonly PeriodicTimer _timer;
    private readonly CancellationTokenSource _cts = new();

    public FooterView()
    {
        InitializeComponent();

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
                Dispatcher.Dispatch(() =>
                {
                    if (Date != null)
                        Date.Text = "Date: " + now.ToString("dd/MM/yyyy(ddd) hh:mm:ss tt");


                    PosName.Text = $"POS: {POSInfo.Terminal.PosName}{(POSInfo.Terminal.IsTrainMode ? " (Training)" : "")}";
                    NetworkStatus.Text = isConnected ? "Online" : "Offline";
                    NetworkStatus.TextColor = isConnected ? Colors.Green : Colors.Red;
                });
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