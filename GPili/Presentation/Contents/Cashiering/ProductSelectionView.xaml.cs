using GPili.Presentation.Features.Cashiering;

namespace GPili.Presentation.Contents.Cashiering;

public partial class ProductSelectionView : ContentView
{
    private CashieringViewModel _vm;
    public ProductSelectionView()
    {
        InitializeComponent();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        _vm = BindingContext as CashieringViewModel;
    }
    protected override void OnParentSet()
    {
        base.OnParentSet();

        SearchEntry.Focus();

    }
    public void FocusSearchEntry()
    {
        SearchEntry.Focus();
    }

    public bool IsSearchEntryFocused()
    {
        return SearchEntry.IsFocused;
    }

    public void HandleNumbersAction(string action)
    {
        if ((_vm.SelectedKeypadAction == KeypadActions.QTY || _vm.SelectedKeypadAction == KeypadActions.PAY) && !SearchEntry.IsFocused)
        {

            switch (action)
            {
                case KeypadActions.BTN0:
                    BTN0.SendClicked();
                    break;
                case KeypadActions.BTN1:
                    BTN1.SendClicked();
                    break;
                case KeypadActions.BTN2:
                    BTN2.SendClicked();
                    break;
                case KeypadActions.BTN3:
                    BTN3.SendClicked();
                    break;
                case KeypadActions.BTN4:
                    BTN4.SendClicked();
                    break;
                case KeypadActions.BTN5:
                    BTN5.SendClicked();
                    break;
                case KeypadActions.BTN6:
                    BTN6.SendClicked();
                    break;
                case KeypadActions.BTN7:
                    BTN7.SendClicked();
                    break;
                case KeypadActions.BTN8:
                    BTN8.SendClicked();
                    break;
                case KeypadActions.BTN9:
                    BTN9.SendClicked();
                    break;
                case KeypadActions.BTNDECIMAL:
                    BTNDECIMAL.SendClicked();
                    break;
            }
        }
    }


    public void HandleKeypadAction(string action)
    {
        switch (action)
        {
            case KeypadActions.QTY:
                QTY.SendClicked();
                break;

            case KeypadActions.CLR:
                CLR.SendClicked();
                break;

            case KeypadActions.PLU:
                PLU.SendClicked();
                break;

            case KeypadActions.PAY:
                PAY.SendClicked();
                break;

            case KeypadActions.DISCOUNT:
                DISCOUNT.SendClicked();
                break;

            case KeypadActions.VOID:
                VOID.SendClicked();
                break;

            case "EXACT":
                EXACT.SendClicked();
                break;

            case KeypadActions.ENTER:
                ENTER.SendClicked();
                break;
        }
    }
}