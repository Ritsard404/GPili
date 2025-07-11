﻿namespace ServiceLibrary.Utils
{
    public static class Formats
    {
        public static string InvoiceFormat(this long id)
        {
            return id.ToString("D12");
        }

        public static string DateFormat(this DateTime date)
        {
            return date.ToString("yyyy-MM-dd");
        }

        public static string DateTimeFormat(this DateTime date)
        {
            return date.ToString("yyyy-MM-dd hh:mm:ss tt");
        }

        public static decimal StoreDecimalValueFormat(this decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }

        public static string StoreDecimalStringValueFormat(this decimal? value)
        {
            return Math.Round(value ?? 0, 2, MidpointRounding.AwayFromZero).ToString();
        }

        public static string StoreDecimalStringValueFormat(this decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero).ToString();
        }

        public static string PesoFormat(this decimal value)
        {
            return $"₱{value:N2}";
        }
        public static string PesoFormat(this decimal? value)
        {
            return $"₱{(value ?? 0):N2}";
        }

        public static string Capitalize(this string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return value!;

            var lower = value.ToLowerInvariant();
            return char.ToUpperInvariant(lower[0]) + lower.Substring(1);
        }
    }
}
