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
                Toast.Make("Please enter the eligible person's name for the discount.", ToastDuration.Short).Show();
                return;
            }

            if (!IsPwdScDisc)
            {
                if (DiscountAmount > 0 && DiscountPercent > 0)
                {
                    Toast.Make("Please provide either Discount Amount or Discount Percent, not both.", ToastDuration.Short).Show();
                    return;
                }

                if (DiscountPercent > 0 && DiscountAmount == 0)
                {
                    if (DiscountPercent < 0 || DiscountPercent > 100)
                    {
                        Toast.Make("Discount percent must be between 0 and 100.", ToastDuration.Short).Show();
                        return;
                    }
                }

                if (DiscountPercent == 0 && DiscountAmount > 0)
                {
                    if (DiscountAmount < 0)
                    {
                        Toast.Make("Discount amount must not be negative.", ToastDuration.Short).Show();
                        return;
                    }
                }

            }
            else
            {
                if (!IsSeniorChecked && !IsPwdChecked)
                {
                    Toast.Make("Please select either 'PWD' or 'Senior' as the discount type.", ToastDuration.Short).Show();
                    return;
                }

                if (string.IsNullOrWhiteSpace(OscaIdNum))
                {
                    Toast.Make("Please provide a valid OSCA ID.", ToastDuration.Short).Show();
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
