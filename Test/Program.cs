using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace Test
{
    class Program
    {
        static Regex cursorPattern = new Regex(@"P(\d+)-(\d+)");
        static string folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "ExtractTest");

        static void Main(string[] args)
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.Fail("Test directory does not exist");
            }

            var folder = new DirectoryInfo(folderPath);
            var files = folder.GetFiles("*.in.png");
            var numTests = files.Count();
            int test = 0, success = 0;
            var sw = new Stopwatch();
            sw.Start();
            foreach (var input in files)
            {
                var infile = input.FullName;
                var outfile = infile.Replace(".in.png", ".out.png");

                Debug.Write(string.Format("Test {0}/{1}... ", ++test, numTests));

                string reason;
                if (CompareExtraction(infile, outfile, out reason))
                {
                    Debug.Print("passed!");
                    success++;
                }
                else
                {
                    Debug.Print("failed ({0})", reason);
                }
            }

            sw.Stop();
            Debug.Print("{0} of {1} tests succeeded. Time taken: {2}", success, numTests, sw.Elapsed);
        }

        private static bool CompareExtraction(string infile, string outfile, out string reason)
        {
            var match = cursorPattern.Match(infile);
            if (!match.Success)
            {
                reason = "failed to extract cursor position!";
                return false;
            }

            var bmp = new Bitmap(infile);
            var cursorPos = new Point(
                Convert.ToInt32(match.Groups[1].Captures[0].Value),
                Convert.ToInt32(match.Groups[2].Captures[0].Value));

            var ie = new ItemCollage.ItemExtractor(bmp, cursorPos);
            Bitmap result = (Bitmap)ie.ExtractItem();

            var exist = File.Exists(outfile);
            if (result != null && !exist)
            {
                reason = "Unexpectedly found item";
                return false;
            }
            else if (result == null && exist)
            {
                reason = "Item not found";
                return false;
            }
            else if (result == null && !exist)
            {
                reason = "";
                return true;
            }

            var expected = new Bitmap(outfile);
            if (expected.Width != result.Width ||
                expected.Height != result.Height)
            {
                reason = string.Format(
                    "Dimension does not match, expected {0}/{1}, got {2}/{3}",
                    expected.Width, expected.Height, result.Width, result.Height);
                return false;
            }

            unsafe {
                var rect = new Rectangle(0, 0, result.Width, result.Height);
                var resultData = result.LockBits(rect, ImageLockMode.ReadOnly,
                    result.PixelFormat);
                var expectedData = expected.LockBits(rect, ImageLockMode.ReadOnly,
                    expected.PixelFormat);

                try
                {
                    if (memcmp(resultData.Scan0, expectedData.Scan0,
                        resultData.Stride * resultData.Height) != 0)
                    {
                        reason = "Bitmap data did not match";
                        return false;
                    }
                }
                finally
                {
                    result.UnlockBits(resultData);
                    expected.UnlockBits(expectedData);
                }
            }

            reason = "";
            return true;
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(IntPtr b1, IntPtr b2, long count);

    }
}
