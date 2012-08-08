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
            foreach (var input in files)
            {
                var infile = input.FullName;
                var outfile = infile.Replace(".in.png", ".out.png");

                Debug.Write(string.Format("Test {0}/{1}... ", ++test, numTests));

                string reason;
                if (CompareExtraction(infile, outfile, false, out reason))
                {
                    Debug.Print("passed!");
                    success++;
                }
                else
                {
                    Debug.Print("failed ({0})", reason);
                }
            }

            Debug.Print("{0} of {1} tests succeeded", success, numTests);
        }

        private static bool CompareExtraction(string infile, string outfile,
            bool pixelComparison, out string reason)
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

            if (pixelComparison)
            {
                for (var x = 0; x < result.Width; x++)
                {
                    for (var y = 0; y < result.Height; y++)
                    {
                        if (result.GetPixel(x, y) != expected.GetPixel(x, y))
                        {
                            reason = string.Format("Images differ at {0}/{1}: {2}/{3}",
                                x, y, expected.GetPixel(x, y), result.GetPixel(x, y));
                            return false;
                        }
                    }
                }
            }
            else
            {
                var converter = new ImageConverter();
                var expectedBytes = (byte[])converter.ConvertTo(expected, typeof(byte[]));
                // TODO: for some reason we have to save this as file...
                var resfile = infile.Replace(".in.png", ".res.png");
                result.Save(resfile);
                var resultBytes = (byte[])converter.ConvertTo(new Bitmap(resfile), typeof(byte[]));

                var sha1 = new SHA1Managed();
                var expectedHash = BitConverter.ToString(sha1.ComputeHash(expectedBytes));
                var resultHash = BitConverter.ToString(sha1.ComputeHash(resultBytes));

                if (expectedHash != resultHash)
                {
                    reason = string.Format("Hash differs, expected {0}, got {1}",
                        expectedHash, resultHash);
                    return false;
                }
            }

            reason = "";
            return true;
        }
    }
}
