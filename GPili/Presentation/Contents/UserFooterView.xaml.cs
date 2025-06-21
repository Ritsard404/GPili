using GPili.Utils.State;

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
        PosName.Text = "POS1";

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

    public void Dispose() => _timer?.Dispose();
}