namespace GPili.Mobile.Presentation.Contents.Cashier;

public partial class TitleViewControl : ContentView
{
    public TitleViewControl()
    {
        InitializeComponent();
    }
    protected override void OnParentSet()
    {
        base.OnParentSet();
        TrainLabel.IsVisible = POSInfo.Terminal.IsTrainMode;
    }
}