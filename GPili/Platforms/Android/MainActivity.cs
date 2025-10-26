using Android.App;
using Android.Content.PM;
using Android.OS;
using Android;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using System.Collections.Generic;
using System.Linq;

namespace GPili
{
    [Activity(Theme = "@style/Maui.SplashTheme", ScreenOrientation = ScreenOrientation.Landscape, MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
    public class MainActivity : MauiAppCompatActivity
    {

        public void RequestBluetoothAndLocationPermissions()
        {
            var permissions = new List<string>
            {
                Manifest.Permission.AccessFineLocation,
                Manifest.Permission.AccessCoarseLocation
            };

            if (Build.VERSION.SdkInt >= BuildVersionCodes.S)
            {
                permissions.Add(Manifest.Permission.BluetoothScan);
                permissions.Add(Manifest.Permission.BluetoothConnect);
                permissions.Add(Manifest.Permission.BluetoothAdvertise);
            }

            var notGranted = permissions.Where(p => ContextCompat.CheckSelfPermission(this, p) != Permission.Granted).ToList();
            if (notGranted.Any())
            {
                ActivityCompat.RequestPermissions(this, notGranted.ToArray(), 1001);
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            if (requestCode == 1001)
            {
                if (grantResults.All(r => r == Permission.Granted))
                {
                    // Permissions granted, proceed with printing
                }
                else
                {
                    // Inform the user that permissions are required
                    Shell.Current.DisplayAlert("Required","Allow the bluetooth and location to print","Ok");
                }
            }
        }
    }
}
