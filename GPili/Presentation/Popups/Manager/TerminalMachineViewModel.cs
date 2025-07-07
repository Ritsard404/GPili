using ServiceLibrary.Models;
using ServiceLibrary.Services.Interfaces;
using System.ComponentModel.DataAnnotations;

namespace GPili.Presentation.Popups.Manager
{
    public partial class TerminalMachineViewModel(IGPiliTerminalMachine _terminalMachine) : ObservableValidator
    {
        public TerminalMachinePopup Popup;

        public double PopupWidth => Shell.Current.CurrentPage.Width * 0.4;
        public double PopupHeight => Shell.Current.CurrentPage.Height * 0.8;


        [ObservableProperty]
        private TerminalConfiguration? _terminalConfig;

        [RelayCommand]
        private void ClosePopup()
        {
            Popup.Close();
        }
        [RelayCommand]
        private async Task Save()
        {
            if (TerminalConfig is null)
            {
                return;
            }

            TerminalConfig.ValidateAll();

            if (TerminalConfig.HasErrors)
            {
                return;
            }

            var info = new PosTerminalInfo {
                AccreditationNumber = TerminalConfig.AccreditationNumber,
                Address = TerminalConfig.Address,
                BranchCenter = TerminalConfig.BranchCenter,
                CostCenter = TerminalConfig.CostCenter,
                DateIssued = TerminalConfig.DateIssued,
                DbName = TerminalConfig.DbName,
                DiscountMax = TerminalConfig.DiscountMax,
                MinNumber = TerminalConfig.MinNumber,
                OperatedBy = TerminalConfig.OperatedBy,
                PtuNumber = TerminalConfig.PtuNumber,
                PosName = TerminalConfig.PosName,
                PosSerialNumber = TerminalConfig.PosSerialNumber,
                RegisteredName = TerminalConfig.RegisteredName,
                UseCenter = TerminalConfig.UseCenter,
                Vat = TerminalConfig.Vat,
                VatTinNumber = TerminalConfig.VatTinNumber,
                ValidUntil = TerminalConfig.ValidUntil,
                PrinterName = TerminalConfig.PrinterName
                };

            var (isSuccess, message) = await _terminalMachine.SetPosTerminalInfo(info);

            if (isSuccess)
            {
                await Snackbar.Make(message, 
                    duration: TimeSpan.FromSeconds(1)).Show();
                Popup.Close();
            }
        }

        public async Task LoadTerminalInfos()
        {
            var posInfo = await _terminalMachine.GetTerminalInfo();
            if (posInfo != null)
            {
                TerminalConfig = new TerminalConfiguration
                {
                    PosSerialNumber = posInfo.PosSerialNumber,
                    MinNumber = posInfo.MinNumber,
                    AccreditationNumber = posInfo.AccreditationNumber,
                    PtuNumber = posInfo.PtuNumber,
                    DateIssued = posInfo.DateIssued,
                    ValidUntil = posInfo.ValidUntil,
                    PosName = posInfo.PosName,
                    RegisteredName = posInfo.RegisteredName,
                    OperatedBy = posInfo.OperatedBy,
                    Address = posInfo.Address,
                    VatTinNumber = posInfo.VatTinNumber,
                    Vat = posInfo.Vat,
                    DiscountMax = posInfo.DiscountMax,
                    CostCenter = posInfo.CostCenter,
                    BranchCenter = posInfo.BranchCenter,
                    UseCenter = posInfo.UseCenter,
                    DbName = posInfo.DbName,
                    PrinterName = posInfo.PrinterName
                };
            }
            else
            {
                TerminalConfig = new TerminalConfiguration();

            }
        }
    }

        [NotifyDataErrorInfo]
        public partial class TerminalConfiguration : ObservableValidator
        {
            // POS machine details
            [Required(ErrorMessage = "POS Serial Number is required")]
            [ObservableProperty]
            private string _posSerialNumber = string.Empty;

            [Required(ErrorMessage = "MIN Number is required")]
            [ObservableProperty]
            private string _minNumber = string.Empty;

            [Required(ErrorMessage = "Accreditation Number is required")]
            [ObservableProperty]
            private string _accreditationNumber = string.Empty;

            [Required(ErrorMessage = "PTU Number is required")]
            [ObservableProperty]
            private string _ptuNumber = string.Empty;

            [Required(ErrorMessage = "Date Issued is required")]
            [ObservableProperty]
            private DateTime _dateIssued;

            [Required(ErrorMessage = "Valid Until date is required")]
            [ObservableProperty]
            private DateTime _validUntil;

            // Business details
            [Required(ErrorMessage = "POS Name is required")]
            [ObservableProperty]
            private string _posName = string.Empty;

            [Required(ErrorMessage = "Registered Name is required")]
            [ObservableProperty]
            private string _registeredName = string.Empty;

            [Required(ErrorMessage = "Operated By is required")]
            [ObservableProperty]
            private string _operatedBy = string.Empty;

            [Required(ErrorMessage = "Address is required")]
            [ObservableProperty]
            private string _address = string.Empty;

            [Required(ErrorMessage = "VAT TIN Number is required")]
            [ObservableProperty]
            private string _vatTinNumber = string.Empty;

            [Required(ErrorMessage = "VAT percentage is required")]
            [Range(0, int.MaxValue, ErrorMessage = "VAT must be a non-negative number")]
            [ObservableProperty]
            private int _vat;

            [Required(ErrorMessage = "Discount Max is required")]
            [Range(0, double.MaxValue, ErrorMessage = "Discount Max must be a non-negative value")]
            [ObservableProperty]
            private decimal _discountMax;

            // API Flags
            [Required(ErrorMessage = "Cost Center is required")]
            [ObservableProperty]
            private string _costCenter = string.Empty;

            [Required(ErrorMessage = "Branch Center is required")]
            [ObservableProperty]
            private string _branchCenter = string.Empty;

            [Required(ErrorMessage = "Use Center is required")]
            [ObservableProperty]
            private string _useCenter = string.Empty;

            [Required(ErrorMessage = "Database Name is required")]
            [ObservableProperty]
            private string _dbName = string.Empty;

            [Required(ErrorMessage = "Printer Name is required")]
            [ObservableProperty]
            private string _printerName = string.Empty;

            /// <summary>
            /// Call this method to validate all properties.
            /// </summary>
            public void ValidateAll()
            {
                ValidateAllProperties();
            }
        }
    }
