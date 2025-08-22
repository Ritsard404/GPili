using GPili.Mobile.Presentation.Features.Manager.Report;
using GPili.Mobile.Presentation.Popups.Manager;
using ServiceLibrary.Utils;
using Shiny;
using System.ComponentModel.DataAnnotations;
using static Java.Text.Normalizer;

namespace GPili.Mobile.Presentation.Features.Manager.Data
{
    public partial class DataViewModel(IInventory _inventory, IAuditLog _auditLog,
        IPopupService _popupService, IDatabaseService _databaseService,
        IGPiliTerminalMachine _terminalMachine, IAuth _auth,
        IEPayment _ePayment) : ObservableObject
    {
        [ObservableProperty] private DataPageType _currenDataPage;
        [ObservableProperty]
        private bool _isLoading = false;

        [ObservableProperty]
        private TerminalConfiguration? _terminalConfig;

        [ObservableProperty]
        private List<SaleType> _saleTypes = new();
        [RelayCommand]
        private async Task SaveSettings()
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

            var info = new PosTerminalInfo
            {
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
                PrinterName = TerminalConfig.PrinterName,
                IsRetailType = TerminalConfig.IsRetailType
            };

            var (isSuccess, message) = await _terminalMachine.SetPosTerminalInfo(info);

            if (isSuccess)
            {
                await Toast.Make(message).Show();

                POSInfo.Terminal = await _terminalMachine.GetTerminalInfo();
            }
        }

        public async Task InitializeData()
        {
            IsLoading = true;
            try
            {
                switch (CurrenDataPage)
                {
                    case DataPageType.Settings:
                        if (await _terminalMachine.GetTerminalInfo() is { } posInfo)
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
                                PrinterName = posInfo.PrinterName,
                                IsRetailType = posInfo.IsRetailType
                            };
                        }
                        break;

                    case DataPageType.Products:
                        var products = await _inventory.GetProducts();
                        var categories = await _inventory.GetCategories();
                        break;

                    case DataPageType.Categories:
                        var catList = await _inventory.GetCategories();
                        break;

                    case DataPageType.Users:
                        var users = await _auth.Users();
                        break;

                    case DataPageType.SaleTypes:
                        SaleTypes = await _ePayment.SaleTypes();
                        break;
                }
            }
            finally
            {
                IsLoading = false;
            }
        }


        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(ModeText))]
        [NotifyPropertyChangedFor(nameof(ModeButtonColor))]
        private bool _isTrainingMode = POSInfo.Terminal.IsTrainMode;

        public string ModeText => IsTrainingMode ? "Training Mode" : "Live Mode";
        public Color ModeButtonColor => IsTrainingMode ? Colors.Orange : Colors.Green;

        [RelayCommand]
        private async Task ChangeMode()
        {
            IsLoading = true;
            if (App.UserInfo != null && App.UserInfo.Role != RoleType.Cashier)
            {
                var result = await _terminalMachine.ChangeMode(App.UserInfo.Email!);

                IsTrainingMode = result;
                POSInfo.Terminal = await _terminalMachine.GetTerminalInfo();
            }

            IsLoading = false;
        }
    }
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

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(PosTypeName))]
        private bool _isRetailType;
        public string PosTypeName => IsRetailType ? "Retail POS" : "Restaurant POS";

        /// <summary>
        /// Call this method to validate all properties.
        /// </summary>
        public void ValidateAll()
        {
            ValidateAllProperties();
        }
    }
    public enum DataPageType
    {
        Settings,
        Products,
        Categories,
        Users,
        SaleTypes
    }
}
