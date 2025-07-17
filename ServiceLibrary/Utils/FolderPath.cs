using System;
using System.IO;
using Microsoft.Maui.Devices;

namespace ServiceLibrary.Utils
{
    public static class FolderPath
    {
        // Base GPili root on WinUI or Android external storage
        public static string Root
        {
            get
            {
                string rootPath;

                if (DeviceInfo.Platform == DevicePlatform.Android)
                {
#if ANDROID
                    rootPath = Path.Combine(Android.OS.Environment.ExternalStorageDirectory.AbsolutePath, "GPili");
#else
                    rootPath = "/storage/emulated/0/GPili";
#endif
                }
                else if (DeviceInfo.Platform == DevicePlatform.WinUI)
                {
                    rootPath = @"C:\GPili";
                }
                else
                {
                    throw new NotSupportedException($"Platform {DeviceInfo.Platform} is not supported for GPili storage.");
                }

                if (!Directory.Exists(rootPath))
                    Directory.CreateDirectory(rootPath);

                return rootPath;
            }
        }

        public static class SalesReport
        {
            private static string Base => Path.Combine(Root, "Reports");

            public static string Invoices => CreateIfMissing(Path.Combine(Base, "Invoices"));
            public static string SearchedInvoices => CreateIfMissing(Path.Combine(Base, "SearchedInvoices"));
            public static string DailySalesReports => CreateIfMissing(Path.Combine(Base, "DailySalesReports"));
            public static string XInvoiceReports => CreateIfMissing(Path.Combine(Base, "XInvoiceReports"));
            public static string ZInvoiceReports => CreateIfMissing(Path.Combine(Base, "ZInvoiceReports"));
            public static string CashTracks => CreateIfMissing(Path.Combine(Base, "CashTracks"));
            public static string TransactionLogs => CreateIfMissing(Path.Combine(Base, "TransactionLogs"));
        }

        public static class Logs
        {
            private static string Base => Path.Combine(Root, "Logs");

            public static string TransactionLogs => CreateIfMissing(Path.Combine(Base, "TransactionLogs"));
            public static string AuditTrail => CreateIfMissing(Path.Combine(Base, "AuditTrail"));
            public static string ProductBarcodes => CreateIfMissing(Path.Combine(Base, "ProductBarcodes"));
        }

        public static class Database
        {
            public static string Test => CreateIfMissing(Path.Combine(Root, "Database"));
            public const string Password = "Ritsard200303";  // leave as const if truly constant
            public static string TestPush => CreateIfMissing(Path.Combine(Root, "TestPush"));
        }

        public static class JournalLink
        {
            public const string Ebisx = "https://ebisx.com/";
        }

        public static class ImagePath
        {
            public static string Image => CreateIfMissing(Path.Combine(Root, "Images"));
        }

        public static class PDF
        {
            private static string Base => Path.Combine(Root, "Reports");

            public static string Barcodes => CreateIfMissing(Path.Combine(Base, "Barcodes"));
            public static string AuditTrail => CreateIfMissing(Path.Combine(Base, "AuditTrail"));
            public static string TransactionLists => CreateIfMissing(Path.Combine(Base, "TransactionLists"));
            public static string SalesHistory => CreateIfMissing(Path.Combine(Base, "SalesHistory"));
            public static string SalesBook => CreateIfMissing(Path.Combine(Base, "SalesBook"));
            public static string VoidedLists => CreateIfMissing(Path.Combine(Base, "VoidedLists"));

            public static string GetPath(string folderName)
                => CreateIfMissing(Path.Combine(Base, folderName));
        }

        /// <summary>
        /// Ensures the directory exists, then returns the path.
        /// </summary>
        private static string CreateIfMissing(string path)
        {
            if (!Directory.Exists(path))
                Directory.CreateDirectory(path);
            return path;
        }
    }
}
