namespace GPili.Presentation.Features.Cashiering;
#if WINDOWS
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Input;
using Windows.System;
#endif
public partial class CashieringPage : ContentPage
{
    public CashieringPage()
    {
        InitializeComponent();


#if WINDOWS
        // Attach to Window content for full key coverage
        var window = (Microsoft.Maui.Controls.Application.Current?.Windows?.FirstOrDefault())?.Handler?.PlatformView as Microsoft.UI.Xaml.Window;
        if (window?.Content is FrameworkElement root)
        {
            root.KeyDown += Root_KeyDown;
        }
#endif
    }

#if WINDOWS
    private void Root_KeyDown(object sender, KeyRoutedEventArgs e)
    {
        if (e.Key == VirtualKey.F1)
        {
            ProductSelection?.FocusSearchEntry();
            e.Handled = true;
        }

        if (ProductSelection?.IsSearchEntryFocused() == true)
            return;

        if (e.Key == VirtualKey.C)
        {
            ProductSelection?.HandleKeypadAction(KeypadActions.CLR);
            e.Handled = true;
        }
        if (e.Key == VirtualKey.Q)
        {
            ProductSelection?.HandleKeypadAction(KeypadActions.QTY);
            e.Handled = true;
        }
        if (e.Key == VirtualKey.L)
        {
            ProductSelection?.HandleKeypadAction(KeypadActions.PLU);
            e.Handled = true;
        }
        if (e.Key == VirtualKey.P)
        {
            ProductSelection?.HandleKeypadAction(KeypadActions.PAY);
            e.Handled = true;
        }
        if (e.Key == VirtualKey.D)
        {
            ProductSelection?.HandleKeypadAction(KeypadActions.DISCOUNT);
            e.Handled = true;
        }
        if (e.Key == VirtualKey.E)
        {
            ProductSelection?.HandleKeypadAction("EXACT");
            e.Handled = true;
        }

        if (e.Key == VirtualKey.Enter)
        {
            ProductSelection?.HandleKeypadAction(KeypadActions.ENTER);
            e.Handled = true;
        }

        if (e.Key >= VirtualKey.Number0 && e.Key <= VirtualKey.Number9)
        {
            int number = e.Key - VirtualKey.Number0;
            ProductSelection?.HandleNumbersAction($"BTN{number}");
            e.Handled = true;
        }
        if (e.Key >= VirtualKey.NumberPad0 && e.Key <= VirtualKey.NumberPad9)
        {
            int number = e.Key - VirtualKey.NumberPad0;
            ProductSelection?.HandleNumbersAction($"BTN{number}");
            e.Handled = true;
        }
        if (e.Key == VirtualKey.Decimal)
        {
            ProductSelection?.HandleNumbersAction(KeypadActions.BTNDECIMAL);
            e.Handled = true;
        }
    }
#endif

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (BindingContext is CashieringViewModel vm)
            await vm.InitializeAsync();
    }
}