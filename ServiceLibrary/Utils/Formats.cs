namespace ServiceLibrary.Utils
{
    internal static class Formats
    {
        public static string Invoice(long id)
        {
            return id.ToString("D12");
        }

        public static string Date(DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        public static decimal StoreDecimalValue(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
