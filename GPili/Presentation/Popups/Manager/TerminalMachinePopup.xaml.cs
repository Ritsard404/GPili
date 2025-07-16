namespace GPili.Presentation.Popups.Manager;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class TerminalMachinePopup : Popup
{
    public TerminalMachinePopup(TerminalMachineViewModel vm)
    {
        PopupState.PopupInfo.OpenPopup("Terminal", "Terminal machine");
        InitializeComponent();
        vm.Popup = this;
        _ = vm.LoadTerminalInfos();
        BindingContext = vm;
        Closed += (_, _) => PopupState.PopupInfo.ClosePopup();
    }
}