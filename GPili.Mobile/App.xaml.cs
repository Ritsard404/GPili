using GPili.Mobile.Utils.State;

namespace GPili.Mobile
{
    public partial class App : Application
    {
        public static UserInfo? UserInfo;
        public App()
        {
            InitializeComponent();

            UserAppTheme = AppTheme.Light;

            MainPage = new AppShell();
        }
    }
}
