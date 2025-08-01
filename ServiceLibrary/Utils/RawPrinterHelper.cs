#if WINDOWS
using System.Runtime.InteropServices;
using System.Text;

namespace ServiceLibrary.Utils
{
    public static class RawPrinterHelper
    {
        static RawPrinterHelper()
        {
            // Register the code page encoding provider
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        private class CustomEncoderFallback : EncoderFallback
        {
            public override int MaxCharCount => 1;

            public override EncoderFallbackBuffer CreateFallbackBuffer()
            {
                return new CustomEncoderFallbackBuffer();
            }
        }

        private class CustomEncoderFallbackBuffer : EncoderFallbackBuffer
        {
            private char _fallbackChar = 'P'; // Replace ₱ with 'P'
            private bool _hasFallback;

            public override int Remaining => _hasFallback ? 1 : 0;

            public override bool Fallback(char charUnknown, int index)
            {
                _hasFallback = true;
                return true;
            }

            public override bool Fallback(char charUnknownHigh, char charUnknownLow, int index)
            {
                _hasFallback = true;
                return true;
            }

            public override char GetNextChar()
            {
                if (!_hasFallback)
                    return '\0';

                _hasFallback = false;
                return _fallbackChar;
            }

            public override bool MovePrevious()
            {
                return false;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private class DOCINFO
        {
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDocName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pOutputFile;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string pDataType;
        }

        // Import necessary Win32 functions from winspool.drv
        [DllImport("winspool.Drv", EntryPoint = "OpenPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool OpenPrinter(string pPrinterName, out IntPtr phPrinter, IntPtr pDefaults);

        [DllImport("winspool.Drv", EntryPoint = "ClosePrinter", SetLastError = true)]
        private static extern bool ClosePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartDocPrinterW", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool StartDocPrinter(IntPtr hPrinter, int level, [In] DOCINFO pDocInfo);

        [DllImport("winspool.Drv", EntryPoint = "EndDocPrinter", SetLastError = true)]
        private static extern bool EndDocPrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "StartPagePrinter", SetLastError = true)]
        private static extern bool StartPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "EndPagePrinter", SetLastError = true)]
        private static extern bool EndPagePrinter(IntPtr hPrinter);

        [DllImport("winspool.Drv", EntryPoint = "WritePrinter", SetLastError = true)]
        private static extern bool WritePrinter(IntPtr hPrinter, IntPtr pBytes, int dwCount, out int dwWritten);

        /// <summary>
        /// Send raw text to a Windows‐installed printer.
        /// </summary>
        public static bool PrintText(string printerName, string text)
        {
            try
            {
                // Create encoding with custom fallback
                var encoding = Encoding.GetEncoding(437,
                    new CustomEncoderFallback(),
                    DecoderFallback.ExceptionFallback);

                byte[] bytes = encoding.GetBytes(text);
                return PrintRawBytes(printerName, bytes);
            }
            catch (Exception)
            {
                // If all else fails, try ASCII
                byte[] bytes = Encoding.ASCII.GetBytes(text);
                return PrintRawBytes(printerName, bytes);
            }
        }
        public static bool PrintRawBytes(string printerName, byte[] bytes)
        {
            IntPtr hPrinter = IntPtr.Zero;
            var docInfo = new DOCINFO()
            {
                pDocName = "Raw Thermal Job",
                pDataType = "RAW",
                pOutputFile = null
            };

            try
            {
                // 1) Open the printer by name
                if (!OpenPrinter(printerName, out hPrinter, IntPtr.Zero))
                    return false;

                // 2) Start a new document
                if (!StartDocPrinter(hPrinter, 1, docInfo))
                {
                    ClosePrinter(hPrinter);
                    return false;
                }

                // 3) Start a new page
                if (!StartPagePrinter(hPrinter))
                {
                    EndDocPrinter(hPrinter);
                    ClosePrinter(hPrinter);
                    return false;
                }

                // 4) Allocate unmanaged memory, copy bytes, and write
                IntPtr pUnmanagedBytes = Marshal.AllocCoTaskMem(bytes.Length);
                Marshal.Copy(bytes, 0, pUnmanagedBytes, bytes.Length);

                bool success = WritePrinter(hPrinter, pUnmanagedBytes, bytes.Length, out int written);
                Marshal.FreeCoTaskMem(pUnmanagedBytes);

                // 5) End page, end doc, close printer
                EndPagePrinter(hPrinter);
                EndDocPrinter(hPrinter);
                ClosePrinter(hPrinter);

                return success && (written == bytes.Length);
            }
            catch
            {
                if (hPrinter != IntPtr.Zero)
                {
                    EndPagePrinter(hPrinter);
                    EndDocPrinter(hPrinter);
                    ClosePrinter(hPrinter);
                }
                return false;
            }
        }
    }
}
#endif

#if ANDROID
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Java.Nio;
using Java.Util;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ServiceLibrary.Utils
{
    public static class RawPrinterHelper
    {
        private static readonly object _logLock = new object();
        private static string _logFilePath;

        static RawPrinterHelper()
        {
            // Initialize log file path - use the app's internal files directory
            var documentsPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDocuments).AbsolutePath;
            if (!Directory.Exists(documentsPath))
                Directory.CreateDirectory(documentsPath);

            _logFilePath = Path.Combine(documentsPath, "bluetooth_print_log.txt");
        }

        //public static string GetLogFilePath()
        //{
        //    return _logFilePath;
        //}

        //public static string ReadLogFile()
        //{
        //    try
        //    {
        //        if (File.Exists(_logFilePath))
        //        {
        //            return File.ReadAllText(_logFilePath);
        //        }
        //        return "Log file not found.";
        //    }
        //    catch (Exception ex)
        //    {
        //        return $"Error reading log file: {ex.Message}";
        //    }
        //}

        //public static void ClearLogFile()
        //{
        //    try
        //    {
        //        if (File.Exists(_logFilePath))
        //        {
        //            File.Delete(_logFilePath);
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        System.Diagnostics.Debug.WriteLine($"Error clearing log file: {ex.Message}");
        //    }
        //}

        private static void LogMessage(string message)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            var logEntry = $"[{timestamp}] {message}";

            lock (_logLock)
            {
                try
                {
                    // Append to file
                    File.AppendAllText(_logFilePath, logEntry + System.Environment.NewLine);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error writing to log file: {ex.Message}");
                }
            }

            // Also write to debug console for development
            System.Diagnostics.Debug.WriteLine(logEntry);
        }

        // Synchronous wrapper for compatibility with your interface
        public static bool PrintText(string printerName, string text)
        {
            var bytes = Encoding.UTF8.GetBytes(text);
            return PrintRawBytes(printerName, bytes);
        }

        public static bool PrintRawBytes(string printerName, byte[] bytes)
        {
            try
            {

                // Check permissions before proceeding
                if (!CheckBluetoothPermissions())
                {
                    LogMessage("ERROR: Required Bluetooth permissions not granted");
                    return false;
                }

                return PrintRawBytesAsync(printerName, bytes).GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR in PrintRawBytes: {ex.Message}");
                return false;
            }
        }
        private static async Task<bool> PrintRawBytesAsync(string printerName, byte[] bytes)
        {
            var adapter = BluetoothAdapter.DefaultAdapter;
            if (adapter == null || !adapter.IsEnabled)
            {
                LogMessage("Bluetooth is not enabled or not supported.");
                return false;
            }

            var device = adapter.BondedDevices.FirstOrDefault(d => d.Name == printerName);
            if (device == null)
            {
                LogMessage("Bluetooth device not found.");
                return false;
            }

            var uuid = UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"); // SPP UUID
            using var socket = device.CreateRfcommSocketToServiceRecord(uuid);
            adapter.CancelDiscovery();

            try
            {
                // Offload the blocking connect + write to a background thread:
                await Task.Run(async () =>
                {
                    socket.Connect();                                 // blocking
                    await socket.OutputStream.WriteAsync(bytes, 0, bytes.Length);
                    // optional flush delay:
                    await Task.Delay(200);
                });

                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"[PrintRawBytesAsync] Error: {ex}");
                return false;
            }
            finally
            {
                if (socket.IsConnected)
                    socket.Close();
            }
        }



        private static bool CheckBluetoothPermissions()
        {
            try
            {
                var context = Android.App.Application.Context;
                
                // Check location permissions (required for Bluetooth scanning on all Android versions)
                var locationPermission = Android.Manifest.Permission.AccessFineLocation;
                var coarseLocationPermission = Android.Manifest.Permission.AccessCoarseLocation;
                
                if (context.CheckSelfPermission(locationPermission) != Android.Content.PM.Permission.Granted ||
                    context.CheckSelfPermission(coarseLocationPermission) != Android.Content.PM.Permission.Granted)
                {
                    LogMessage("ERROR: Location permissions not granted (required for Bluetooth scanning)");
                    return false;
                }

                // Check Bluetooth permissions for Android 12+ (API 31+)
                if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.S)
                {
                    var bluetoothScanPermission = Android.Manifest.Permission.BluetoothScan;
                    var bluetoothConnectPermission = Android.Manifest.Permission.BluetoothConnect;
                    
                    if (context.CheckSelfPermission(bluetoothScanPermission) != Android.Content.PM.Permission.Granted ||
                        context.CheckSelfPermission(bluetoothConnectPermission) != Android.Content.PM.Permission.Granted)
                    {
                        LogMessage("ERROR: Bluetooth permissions not granted (required for Android 12+)");
                        return false;
                    }
                }

                return true;
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR checking permissions: {ex.Message}");
                return false;
            }
        }
    }
}
#endif

