namespace GPili.Mobile.Utils
{
    public static class LocationAndFileMediaPermissions
    {
        public static async Task<bool> RequestAsync()
        {
            var permissions = new List<Permissions.BasePermission>
        {
            new Permissions.LocationWhenInUse()
        };

            if (OperatingSystem.IsAndroidVersionAtLeast(33)) 
            {
                permissions.Add(new Permissions.Media());
            }
            else
            {
                permissions.Add(new Permissions.StorageRead());
                permissions.Add(new Permissions.StorageWrite());
            }

            bool allGranted = true;

            foreach (var permission in permissions)
            {
                var status = await permission.CheckStatusAsync();
                if (status != PermissionStatus.Granted)
                {
                    status = await permission.RequestAsync();
                }

                if (status != PermissionStatus.Granted)
                    allGranted = false;
            }

            return allGranted;
        }
    }
}
