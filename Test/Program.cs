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
        static double extractTitleTime = 0.0;

        static void Main(string[] args)
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.Fail("Test directory does not exist");
                return;
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
                var titlefile = infile.Replace(".in.png", ".title.png");

                Console.ResetColor();
                Console.Write(string.Format("Test {0}/{1}... ", ++test, numTests));

                string reason = CompareItemExtraction(infile, outfile, titlefile);
                if (reason == "")
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("passed!");
                    success++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("failed ({0})", reason);
                    Console.WriteLine(input.Name);
                }
            }

            sw.Stop();

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine("{0} of {1} tests succeeded.", success, numTests);
            Console.WriteLine("Time taken: {0} total", sw.Elapsed);
            Console.WriteLine("Extraction: {0:F3}s - Titles: {1:F3}s", extractTime,
                extractTitleTime);
        }

        private static string CompareImageResult(Bitmap bmp, string file, string type)
        {
            var exist = File.Exists(file);
            if (bmp == null)
            {
                if (exist)
                {
                    return string.Format("{0} not found", type);
                }
                else
                {
                    return "";
                }
            }

            if (!exist)
            {
                return string.Format("unexpectedly found {0}", type);
            }

            var expected = new Bitmap(file);
            if (expected.Width != bmp.Width ||
                expected.Height != bmp.Height)
            {
                return string.Format(
                    "{0} dimension does not match, expected {1}/{2}, got {3}/{4}",
                    type, expected.Width, expected.Height, bmp.Width, bmp.Height);
            }

            unsafe
            {
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var resultData = bmp.LockBits(rect, ImageLockMode.ReadOnly,
                    bmp.PixelFormat);
                var expectedData = expected.LockBits(rect, ImageLockMode.ReadOnly,
                    expected.PixelFormat);

                try
                {
                    byte* resultStart = (byte*)resultData.Scan0;
                    byte* expectedStart = (byte*)expectedData.Scan0;
                    for (var y = 0; y < resultData.Height; y++)
                    {
                        var resultRow = (IntPtr)(resultStart + y*resultData.Stride);
                        var expectedRow = (IntPtr)(expectedStart + y*expectedData.Stride);
                        if (memcmp(resultRow, expectedRow, resultData.Width*3) != 0)
                        {
                            return string.Format("{0} bitmap data did not match", type);
                        }
                    }
                }
                finally
                {
                    bmp.UnlockBits(resultData);
                    expected.UnlockBits(expectedData);
                }
            }

            return "";
        }

        private static string CompareItemExtraction(string infile, string outfile,
            string titlefile)
        {
            var match = cursorPattern.Match(infile);
            if (!match.Success)
            {
                return "failed to extract cursor position!";
            }

            var bmp = new Bitmap(infile);
            var cursorPos = new Point(
                Convert.ToInt32(match.Groups[1].Captures[0].Value),
                Convert.ToInt32(match.Groups[2].Captures[0].Value));

            var sw = new Stopwatch();
            sw.Start();
            var ie = new ItemCollage.ItemExtractor(bmp, cursorPos);
            Bitmap item = null;
            try
            {
                item = (Bitmap)ie.ExtractItem();
            }
            catch { }

            sw.Stop();

            var itemTime = sw.Elapsed.TotalSeconds;
            extractTime += itemTime;
            Console.Write("(");
            WriteTime(itemTime);

            var result = CompareImageResult(item, outfile, "item");
            if (result == "")
            {
                Bitmap title = null;
                if (item != null)
                {
                    sw.Reset();
                    sw.Start();
                    title = ItemCollage.ItemExtractor.ExtractItemName(item, true);
                    sw.Stop();

                    var titleTime = sw.Elapsed.TotalSeconds;
                    extractTitleTime += titleTime;
                    Console.Write(" - ");
                    WriteTime(titleTime);

                }

                result = CompareImageResult(title, titlefile, "title");
            }

            Console.Write(") ");
            return result;
        }

        private static void WriteTime(double time)
        {
            if (time > 0.1) Console.ForegroundColor = ConsoleColor.Yellow;
            else if (time > 0.2) Console.ForegroundColor = ConsoleColor.Red;

            Console.Write("{0:F3}s", time);
            Console.ResetColor();
        }

        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(IntPtr b1, IntPtr b2, long count);
    }
}
