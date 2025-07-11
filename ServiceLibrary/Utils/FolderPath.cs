﻿namespace ServiceLibrary.Utils
{
    public static class FolderPath
    {
        public static class SalesReport
        {
            public const string Invoices = "C:\\Reports\\Invoices";
            public const string SearchedInvoices = "C:\\Reports\\SearchedInvoices";
            public const string DailySalesReports = "C:\\Reports\\DailySalesReports";
            public const string XInvoiceReports = "C:\\Reports\\XInvoiceReports";
            public const string ZInvoiceReports = "C:\\Reports\\ZInvoiceReports";
            public const string CashTracks = "C:\\Reports\\CashTracks";
            public const string TransactionLogs = "C:\\Reports\\TransactionLogs";
        }
        public static class Logs
        {
            public const string TransactionLogs = "C:\\Logs\\TransactionLogs";
            public const string AuditTrail = "C:\\Logs\\AuditTrail";
            public const string ProductBarcodes = "C:\\Logs\\ProductBarcodes";
        }
        public static class Database
        {
            public const string Test = "C:\\Database";
            public const string Password = "Ritsard200303";
        }
        public static class JournalLink
        {
            public const string Ebisx = "https://ebisx.com/";
        }
        public static class ImagePath
        {
            public const string Image = "C:\\GPIli\\Images";
        }
        public static class PDF
        {
            public const string Barcodes = "C:\\Reports\\Barcodes";
            public const string AuditTrail = "C:\\Reports\\AuditTrail";
            public const string TransactionLists = "C:\\Reports\\TransactionLists";
            public const string SalesHistory = "C:\\Reports\\SalesHistory";
            public const string SalesBook = "C:\\Reports\\SalesBook";
            public const string VoidedLists = "C:\\Reports\\VoidedLists";
            public static string GetPath(string folderName)
            {
                return $"C:\\Reports\\{folderName}";
            }
        }
    }
}
