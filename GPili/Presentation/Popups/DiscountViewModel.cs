using ServiceLibrary.Services.DTO.Order;
using ServiceLibrary.Utils;
using UtilsDiscountPercent = ServiceLibrary.Utils.DiscountPercent;

namespace GPili.Presentation.Popups
{
    public partial class DiscountViewModel : ObservableObject
    {
        public DiscountView Popup;

        [ObservableProperty]
        private DiscountDTO? _discount;

        [ObservableProperty]
        private bool _isPwdScDisc = true;

        [ObservableProperty]
        private bool _isPwdChecked = true;

        [ObservableProperty]
        private bool _isSeniorChecked = false;

        // Match DTO property names for easier mapping
        [ObservableProperty]
        private string? _eligibleDiscName;

        [ObservableProperty]
        private string? _oscaIdNum;

        [ObservableProperty]
        private int _discountPercent = 0;

        [ObservableProperty]
        private decimal _discountAmount = 0m;

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.5;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.5;

        [RelayCommand]
        private void SelectPwdSc()
        {
            IsPwdScDisc = true;
            DiscountAmount = 0m;
            DiscountPercent = 0;
            EligibleDiscName = null;
            OscaIdNum = null;
            IsPwdChecked = true;
            IsSeniorChecked = false;
        }

        [RelayCommand]
        private void SelectOthers()
        {
            IsPwdScDisc = false;
            DiscountAmount = 0m;
            DiscountPercent = 0;
            EligibleDiscName = null;
            OscaIdNum = null;
            IsPwdChecked = false;
            IsSeniorChecked = false;
        }

        partial void OnIsPwdCheckedChanged(bool value)
        {
            if (value)
                IsSeniorChecked = false;
        }

        partial void OnIsSeniorCheckedChanged(bool value)
        {
            if (value)
                IsPwdChecked = false;
        }

        [RelayCommand]
        private void Submit()
        {
            if (string.IsNullOrWhiteSpace(EligibleDiscName))
            {
                Snackbar.Make("Please enter the eligible person's name for the discount.", duration: TimeSpan.FromSeconds(1)).Show();
                return;
            }

            

            if (!IsPwdScDisc)
            {
                if (DiscountAmount > 0 && DiscountPercent > 0)
                {
                    Snackbar.Make("Please provide either Discount Amount or Discount Percent, not both.", duration: TimeSpan.FromSeconds(1)).Show();
                    return;
                }   

                if (DiscountPercent > 0 && DiscountAmount == 0)
                {
                    if (DiscountPercent < 0 || DiscountPercent > 100)
                    {
                        Snackbar.Make("Discount percent must be between 0 and 100.", duration: TimeSpan.FromSeconds(1)).Show();
                        return;
                    }
                }

                if (DiscountPercent == 0 && DiscountAmount > 0)
                {
                    if (DiscountAmount < 0)
                    {
                        Snackbar.Make("Discount amount must not be negative.", duration: TimeSpan.FromSeconds(1)).Show();
                        return;
                    }
                }

            }
            else
            {
                if (!IsSeniorChecked && !IsPwdChecked)
                {
                    Snackbar.Make("Please select either 'PWD' or 'Senior' as the discount type.", 
                        duration: TimeSpan.FromSeconds(1)).Show();
                    return;
                }

                if (string.IsNullOrWhiteSpace(OscaIdNum))
                {
                    Snackbar.Make("Please provide a valid OSCA ID.",
                        duration: TimeSpan.FromSeconds(1)).Show();
                    return;
                }

                DiscountPercent = UtilsDiscountPercent.PwdSc;
            }

            Discount = new DiscountDTO
            {
                EligibleDiscName = EligibleDiscName,
                OSCAIdNum = OscaIdNum,
                DiscountType = IsPwdScDisc
                    ? (IsPwdChecked ? DiscountType.Pwd : IsSeniorChecked ? DiscountType.SeniorCitizen : null)
                    : DiscountType.Others,
                DiscountPercent = DiscountPercent,
                DiscountAmount = DiscountAmount
            };

            // Add logic to close popup or notify view
            Popup.CloseWithResult(Discount);
        }
    }
}
