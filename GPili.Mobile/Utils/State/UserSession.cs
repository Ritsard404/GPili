namespace GPili.Mobile.Utils.State
{
    public static class UserSession
    {
        public static void Save(UserInfo user)
        {
            Preferences.Set("UserEmail", user.Email ?? "");
            Preferences.Set("UserRole", user.Role ?? "");
            Preferences.Set("UserName", user.Name ?? "");
        }

        public static UserInfo? Load()
        {
            var email = Preferences.Get("UserEmail", null);
            var role = Preferences.Get("UserRole", null);
            var name = Preferences.Get("UserName", null);

            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(role))
                return null;

            return new UserInfo { Email = email, Role = role, Name = name };
        }

        public static void Clear()
        {
            Preferences.Remove("UserEmail");
            Preferences.Remove("UserRole");
            Preferences.Remove("UserName");
        }
    }
}
