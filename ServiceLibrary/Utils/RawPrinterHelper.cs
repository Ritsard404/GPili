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

