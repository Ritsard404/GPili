using GPili.Utils.State;

namespace GPili.Presentation.Contents;

public partial class FooterView : ContentView, IDisposable
{
    private readonly PeriodicTimer _timer;

    public FooterView()
	{
		InitializeComponent();

        SysVer.Text = "System Version " + DeviceInfo.Current.Version.ToString();
        PosName.Text = "POS: " + CashierState.CashierEmail ?? "Unknown POS";

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
        while (await _timer.WaitForNextTickAsync())
        {
            DateTime now = DateTime.Now;

            Date.Text = "Date: " + now.ToString("dd/MM/yyyy(ddd) hh:mm:ss");
        }
    }

    public async void Dispose() => _timer?.Dispose();
}