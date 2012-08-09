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
        static double extractTime = 0.0;

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

                Console.ResetColor();
                Console.Write(string.Format("Test {0}/{1}... ", ++test, numTests));

                string reason;
                if (CompareExtraction(infile, outfile, out reason))
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("passed!");
                    success++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("failed ({0})", reason);
                }
            }

            sw.Stop();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("{0} of {1} tests succeeded.", success, numTests);
            Console.WriteLine("Time taken: {0} ({1}s in extraction)", sw.Elapsed, extractTime);
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

            var sw = new Stopwatch();
            sw.Start();
            var ie = new ItemCollage.ItemExtractor(bmp, cursorPos);
            Bitmap result = (Bitmap)ie.ExtractItem();
            sw.Stop();

            var time = sw.Elapsed.TotalSeconds;
            extractTime += time;
            if (time > 0.1) Console.ForegroundColor = ConsoleColor.Yellow;
            else if (time > 0.2) Console.ForegroundColor = ConsoleColor.Red;
            Console.Write("({0}) ", sw.Elapsed);
            Console.ResetColor();

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
