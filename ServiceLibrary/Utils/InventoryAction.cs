namespace ServiceLibrary.Utils
{
    public static class InventoryAction
    {
        public static class Actions
        {
            public const string In = nameof(In);
            public const string Out = nameof(Out);
            public const string Adjustment = nameof(Adjustment);
        }

        public static class References
        {
            public const string Invoice = nameof(Invoice);
            public const string Void = nameof(Void);
            public const string Return = nameof(Return);
        }
    }
}
