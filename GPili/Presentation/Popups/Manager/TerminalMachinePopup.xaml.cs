namespace GPili.Presentation.Popups.Manager;

public partial class TerminalMachinePopup : Popup
{
    public TerminalMachinePopup()
    {
        PopupState.PopupInfo.OpenPopup("Terminal", "Terminal machine");
        InitializeComponent();
        var vm = IPlatformApplication.Current.Services.GetRequiredService<TerminalMachineViewModel>();
        _ = vm.LoadTerminalInfos();
        vm.Popup = this;
        BindingContext = vm;
        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();
    }
}