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
    struct ConsoleOutput
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
            if (time > 0.2)
            {
                color = ConsoleColor.Red;
            }
            else if (time > 0.1)
            {
                color = ConsoleColor.Yellow;
            }

            Add($"{time:F3}s", color);
        }

        public void Print()
        {
            foreach (var output in this)
            {
                if (output.Color.HasValue)
                {
                    Console.ForegroundColor = output.Color.Value;
                }
                else
                {
                    Console.ResetColor();
                }

                Console.Write(output.Text);
            }
            Console.WriteLine();

            Clear();
        }
    }

    class Program
    {
        static readonly Regex cursorPattern = new Regex(@"P(?<x>\d+)-(?<y>\d+)");
        static readonly Regex itemPattern = new Regex(
            @"R(?<x>\d+)-(?<y>\d+)-(?<width>\d+)-(?<height>\d+)");
        static readonly string folderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyPictures),
            "ExtractTest");

        static readonly ConcurrentBag<double> extractTimes = new ConcurrentBag<double>();
        static readonly ConcurrentBag<double> extractTitleTimes = new ConcurrentBag<double>();

        static int test = 0;
        static int success = 0;
        static int fail = 0;
        static int numTests = 0;

        static bool verbose = true;

        static readonly object lockobj = new object();

        static void Main(string[] args)
        {
            if (!Directory.Exists(folderPath))
            {
                Debug.Fail("Test directory does not exist");
                Environment.ExitCode = 3;
                return;
            }

            if (args.Contains("-q") || args.Contains("--quiet"))
            {
                verbose = false;
            }

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

            if (fail > 0)
            {
                Environment.ExitCode = 1;
            }
        }

        private static void TestFile(FileInfo input)
        {
            var infile = input.FullName;
            var outfile = infile.Replace(".in.png", ".out.png");
            var titlefile = infile.Replace(".in.png", ".title.png");

            Console.ResetColor();
            var output = new ConsoleOutputList
            {
                $"Test {Interlocked.Increment(ref test)}/{numTests}... "
            };

            string reason = CompareItemExtraction(infile, outfile, titlefile, output);
            if (reason == "")
            {
                output.Add("passed!", ConsoleColor.Green);
                Interlocked.Increment(ref success);
            }
            else
            {
                output.Add($"failed ({reason})\n{input.Name}", ConsoleColor.Red);
                Interlocked.Increment(ref fail);
            }

            if (verbose)
            {
                lock (lockobj)
                {
                    output.Print();
                }
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

        private static string CompareImageResult(Bitmap bmp, string file, string type)
        {
            var exist = File.Exists(file);
            if (bmp == null)
            {
                if (exist)
                {
                    return $"{type} not found";
                }
                else
                {
                    return "";
                }
            }

            if (!exist)
            {
                return $"unexpectedly found {type}";
            }

            var expected = new Bitmap(file);
            if (expected.Width != bmp.Width ||
                expected.Height != bmp.Height)
            {
                return $"{type} dimension does not match, expected {expected.Width}/{expected.Height}, got {bmp.Width}/{bmp.Height}";
            }

            unsafe
            {
                var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
                var bytes = bmp.BytesPerPixel();

                using (var expectedData = new LockData(expected, rect))
                using (var resultData = new LockData(bmp, rect))
                {
                    for (var y = 0; y < resultData.Data.Height; y++)
                    {
                        if (memcmp(resultData.Row(y), expectedData.Row(y),
                            resultData.Width * bytes) != 0)
                        {
                            return $"{type} bitmap data did not match";
                        }
                    }
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
            Item item = null;
            var success = false;

            sw.Start();
            try
            {
                success = ie.FindItem();
                item = ie.ExtractItem(false);
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
                result = CompareImageResult(success ? item.Image : null, outfile, "item");
            }
            else
            {
                var itemMatch = itemPattern.Match(infile);
                if (!itemMatch.Success)
                {
                    if (success) result = $"Unexpectedly found item at {ie.ItemFrame}";
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

                        if (found != expected) result = $"Found item at {found}, expected {expected}";
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

                    title = ItemExtractor.ExtractItemName(item.Image);

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
