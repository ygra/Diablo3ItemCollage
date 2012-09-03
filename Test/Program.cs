using ItemCollage;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Test
{
    class ConsoleOutput
    {
        public string Text { get; set; }
        public ConsoleColor? Color { get; set; }
    }

    class ConsoleOutputList : List<ConsoleOutput>
    {
        public void Add(string text, ConsoleColor? color = null)
        {
            this.Add(new ConsoleOutput
            {
                Text = text,
                Color = color
            });
        }

        public void AddTime(double time)
        {
            ConsoleColor? color = null;
            if (time > 0.2) color = ConsoleColor.Red;
            else if (time > 0.1) color = ConsoleColor.Yellow;

            this.Add(string.Format("{0:F3}s", time), color);
        }

        public void Print()
        {
            foreach (var output in this)
            {
                if (output.Color.HasValue) Console.ForegroundColor = output.Color.Value;
                else Console.ResetColor();
                Console.Write(output.Text);
            }
            Console.WriteLine();

            this.Clear();
        }
    }

    class Program
    {
        static Regex cursorPattern = new Regex(@"P(?<x>\d+)-(?<y>\d+)");
        static Regex itemPattern = new Regex(
            @"R(?<x>\d+)-(?<y>\d+)-(?<width>\d+)-(?<height>\d+)");
        static string folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "ExtractTest");

        static ConcurrentBag<double> extractTimes = new ConcurrentBag<double>();
        static ConcurrentBag<double> extractTitleTimes = new ConcurrentBag<double>();

        static int test = 1;
        static int success = 0;
        static int fail = 0;
        static int numTests = 0;

        static bool verbose = true;

        static object lockobj = new object();

        static void Main(string[] args)
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.Fail("Test directory does not exist");
                Environment.ExitCode = 3;
                return;
            }

            if (args.Contains("-q") || args.Contains("--quiet"))
                verbose = false;

            var folder = new DirectoryInfo(folderPath);
            var files = folder.GetFiles("*.in.png");
            numTests = files.Count();

            var sw = new Stopwatch();
            sw.Start();
            Parallel.ForEach(files, TestFile);
            sw.Stop();

            Console.WriteLine();
            if (verbose)
            {
                Console.ResetColor();
                Console.WriteLine("{0} of {1} tests succeeded.", success, numTests);
                Console.WriteLine("Time taken: {0} total", sw.Elapsed);
                Console.WriteLine("Extraction: {0:F3}s - Titles: {1:F3}s", extractTimes.Sum(),
                    extractTitleTimes.Sum());
                Console.WriteLine("Slow extractions: {0} / {1} - {2} / {3}",
                    extractTimes.Where(t => t > 0.1 && t <= 0.2).Count(),
                    extractTimes.Where(t => t > 0.2).Count(),
                    extractTitleTimes.Where(t => t > 0.1 && t <= 0.2).Count(),
                    extractTitleTimes.Where(t => t > 0.2).Count());
            }

            if (fail > 0) Environment.ExitCode = 1;
        }

        private static void TestFile(FileInfo input)
        {
            var infile = input.FullName;
            var outfile = infile.Replace(".in.png", ".out.png");
            var titlefile = infile.Replace(".in.png", ".title.png");

            Console.ResetColor();
            var output = new ConsoleOutputList();
            output.Add(string.Format("Test {0}/{1}... ",
                Interlocked.Increment(ref test), numTests));

            string reason = CompareItemExtraction(infile, outfile, titlefile, output);
            if (reason == "")
            {
                output.Add("passed!", ConsoleColor.Green);
                Interlocked.Increment(ref success);
            }
            else
            {
                output.Add(string.Format("failed ({0})\n{1}", reason, input.Name),
                    ConsoleColor.Red);
                Interlocked.Increment(ref fail);
            }

            if (verbose)
            {
                PrintOutput(output);
            }
            else
            {
                lock (lockobj)
                {
                    Console.CursorLeft = 0;
                    Console.Write("{0} - {1}", success, fail);
                }
            }
        }

        private static void PrintOutput(ConsoleOutputList outlist)
        {
            lock (lockobj)
            {
                outlist.Print();
            }
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
                        var resultRow = (IntPtr)(resultStart + y * resultData.Stride);
                        var expectedRow = (IntPtr)(expectedStart + y * expectedData.Stride);
                        if (memcmp(resultRow, expectedRow, resultData.Width * 3) != 0)
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
            string titlefile, ConsoleOutputList output, bool imageCompare = false)
        {
            var match = cursorPattern.Match(infile);
            if (!match.Success)
            {
                return "failed to extract cursor position!";
            }

            var bmp = new Bitmap(infile);
            var cursorPos = new Point(match.Groups["x"].Value.ToInt(),
                                      match.Groups["y"].Value.ToInt());

            var sw = new Stopwatch();
            var ie = new ItemExtractor(bmp, cursorPos);
            Bitmap item = null;
            var success = false;

            sw.Start();
            try
            {
                if (imageCompare)
                {
                    item = (Bitmap)ie.ExtractItem();
                    success = (item != null);
                }
                else
                {
                    success = ie.FindItem();
                }
            }
            catch { }
            sw.Stop();

            var itemTime = sw.Elapsed.TotalSeconds;
            extractTimes.Add(itemTime);
            output.Add("(");
            output.AddTime(itemTime);

            var result = "";
            if (imageCompare)
            {
                result = CompareImageResult(item, outfile, "item");
            }
            else
            {
                var itemMatch = itemPattern.Match(infile);
                if (!itemMatch.Success)
                {
                    if (success) result = "Unexpectedly found item";
                }
                else
                {
                    if (success)
                    {
                        var expected = new Rectangle(
                            itemMatch.Groups["x"].Value.ToInt(),
                            itemMatch.Groups["y"].Value.ToInt(),
                            itemMatch.Groups["width"].Value.ToInt(),
                            itemMatch.Groups["height"].Value.ToInt());
                        var found = ie.ItemFrame;

                        if (found != expected) result = string.Format(
                             "Found item at {0}, expected {1}", found, expected);
                    }
                    else
                    {
                        result = "No item found";
                    }
                }
            }

            if (result == "")
            {
                Bitmap title = null;
                if (success)
                {
                    var titleWatch = new Stopwatch();
                    titleWatch.Start();

                    if (imageCompare)
                    {
                        title = ItemExtractor.ExtractItemName(item, true);
                    }
                    else
                    {
                        title = ie.ExtractItemName(true);
                    }

                    titleWatch.Stop();

                    var titleTime = titleWatch.Elapsed.TotalSeconds;
                    extractTitleTimes.Add(titleTime);
                    output.Add(" - ");
                    output.AddTime(titleTime);

                }

                result = CompareImageResult(title, titlefile, "title");
            }

            output.Add(") ");

            return result;
        }


        [DllImport("msvcrt.dll", CallingConvention = CallingConvention.Cdecl)]
        static extern int memcmp(IntPtr b1, IntPtr b2, long count);
    }
}
