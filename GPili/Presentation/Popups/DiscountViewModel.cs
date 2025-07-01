using ServiceLibrary.Services.DTO.Order;
using ServiceLibrary.Utils;

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
        private int _discountPercent;

        [ObservableProperty]
        private decimal _discountAmount;

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
            if(EligibleDiscName == null)
            {
                Toast.Make("Please enter the eligible person's name for the discount.").Show(); 
                return;
            }

            if (!IsPwdScDisc)
            {
                if (DiscountAmount != null && DiscountPercent != null)
                {
                    Toast.Make("Please provide either Discount Amount or Percent, not both.").Show();
                    return;
                }
            }
            else
            {
                if (!IsSeniorChecked && !IsPwdChecked)
                {
                    Toast.Make("Please select either 'PWD' or 'Senior' as the discount type.").Show(); 
                    return;
                }
                if (OscaIdNum == null)
                {
                    Toast.Make("Please provide a valid OSCA ID.").Show(); return;
                }

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
            Debug.WriteLine($"Discount Submitted:");
            Debug.WriteLine($"Name: {Discount.EligibleDiscName}");
            Debug.WriteLine($"OSCA ID: {Discount.OSCAIdNum}");
            Debug.WriteLine($"Type: {Discount.DiscountType}");
            Debug.WriteLine($"Percent: {Discount.DiscountPercent}");
            Debug.WriteLine($"Amount: {Discount.DiscountAmount}");

            // Add logic to close popup or notify view
            Popup.CloseWithResult(Discount);
        }
    }
}
