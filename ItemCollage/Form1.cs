using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;

namespace ItemCollage
{
    public partial class Form1 : Form
    {
        GlobalHotkey F1;
        List<Image> items = new List<Image>();

        public Form1()
        {
            InitializeComponent();
            F1 = new GlobalHotkey(Constants.NOMOD, Keys.F1, this);
            F1.Register();
            UpdateLabel();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            HandleF1();
        }

        private Rectangle FindFrame(Bitmap bmp, Point p)
        {
            try
            {
                var extentUp = p.Y -
                    Enumerable.Range(0, p.Y)
                        .TakeWhile(y => bmp.GetPixel(p.X, p.Y - y).ToArgb() == Color.Black.ToArgb())
                        .Last();
                var extentDown =
                    Enumerable.Range(p.Y, bmp.Height - p.Y)
                        .TakeWhile(y => bmp.GetPixel(p.X, y).ToArgb() == Color.Black.ToArgb())
                        .Last();
                var extentLeft = p.X -
                    Enumerable.Range(0, p.X)
                        .TakeWhile(x => bmp.GetPixel(p.X - x, extentUp).ToArgb() == Color.Black.ToArgb() && bmp.GetPixel(p.X - x, extentDown).ToArgb() == Color.Black.ToArgb())
                        .Last();
                var extentRight =
                    Enumerable.Range(p.X, bmp.Width)
                        .TakeWhile(x => bmp.GetPixel(x, extentUp).ToArgb() == Color.Black.ToArgb() && bmp.GetPixel(x, extentDown).ToArgb() == Color.Black.ToArgb())
                        .Last();
                return new Rectangle(extentLeft, extentUp, extentRight - extentLeft, extentDown - extentUp);
            }
            catch
            {
                return new Rectangle();
            }
        }

        private IEnumerable<int> Range(int start, int end, int step = 1)
        {
            int i;
            for (i = start; i < end; i += step)
            {
                yield return i;
            }
            if (i < end) yield return end;
        }

        private void HandleF1()
        {
            try
            {
                var screen = TakeScreenshot();
                //pictureBox1.Image = screen;
                var cursorPos = Cursor.Position;

                Rectangle bounds;

                if (cursorPos.X < screen.Width / 2)
                {
                    var blockSize = new Size(3, 8);
                    var searchRect = new Rectangle(cursorPos.X - 20, cursorPos.Y - 210, 180, 350);
                    // find block of black pixels
                    var blocks = from y in Range(searchRect.Top, searchRect.Bottom, 8)
                                 from x in Range(searchRect.Left, searchRect.Right)
                                 where (from yy in Range(0, blockSize.Height, 3)
                                        from xx in Range(0, blockSize.Width)
                                        select screen.GetPixel(x + xx, y + yy)).All(c => c.ToArgb() == Color.Black.ToArgb())
                                 select new Point(x, y);
                    var leftEdge = (from b in blocks orderby b.X select b).First();
                    var topEdge =
                        Enumerable.Range(0, leftEdge.Y)
                            .Reverse()
                            .TakeWhile(y => screen.GetPixel(leftEdge.X, y).ToArgb() == Color.Black.ToArgb())
                            .Last();
                    var topLeftCorner = new Point(leftEdge.X, topEdge);
                    var width =
                        Enumerable.Range(topLeftCorner.X, 650)
                            .TakeWhile(x => screen.GetPixel(x, topLeftCorner.Y).ToArgb() == Color.Black.ToArgb())
                            .Count();
                    var height =
                        Enumerable.Range(topLeftCorner.Y, screen.Height - topLeftCorner.Y)
                            .TakeWhile(y => screen.GetPixel(topLeftCorner.X, y).ToArgb() == Color.Black.ToArgb())
                            .Count();
                    bounds = new Rectangle(topLeftCorner, new Size(width, height));
                }
                else
                {
                    var blockSize = new Size(2, 30);
                    var searchRect = new Rectangle(cursorPos.X - 160, cursorPos.Y - 210, 160, 210);
                    // find block of black pixels
                    var blocks = from y in Range(searchRect.Top, searchRect.Bottom, 4)
                                 from x in Range(searchRect.Left, searchRect.Right)
                                 where (from yy in Range(0, blockSize.Height, 5)
                                        from xx in Range(0, blockSize.Width)
                                        select screen.GetPixel(x - xx, y - yy)).All(c => c.ToArgb() == Color.Black.ToArgb())
                                 select new Point(x, y);
                    var rightEdge = (from b in blocks orderby b.X select b).Last();
                    var topEdge =
                        Enumerable.Range(0, rightEdge.Y)
                            .Reverse()
                            .TakeWhile(y => screen.GetPixel(rightEdge.X, y).ToArgb() == Color.Black.ToArgb())
                            .Last();
                    var topRightCorner = new Point(rightEdge.X, topEdge);
                    var width =
                        Enumerable.Range(0, topRightCorner.X)
                            .TakeWhile(x => screen.GetPixel(topRightCorner.X - x, topRightCorner.Y).ToArgb() == Color.Black.ToArgb())
                            .Count();
                    var height =
                        Enumerable.Range(topRightCorner.Y, screen.Height - topRightCorner.Y)
                            .TakeWhile(y => screen.GetPixel(topRightCorner.X, y).ToArgb() == Color.Black.ToArgb())
                            .Count();
                    bounds = new Rectangle(topRightCorner.X - width + 1, topRightCorner.Y, width, height);
                }

                if (bounds.Width < 100) return;

                Bitmap item = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
                Graphics g = Graphics.FromImage(item);
                g.DrawImage(screen, new Rectangle(0, 0, bounds.Width, bounds.Height), bounds, GraphicsUnit.Pixel);
                pictureBox1.Image = item;
                items.Add(item);
                UpdateLabel();
            }
            catch { }
        }

        private void UpdateLabel()
        {
            label1.Text = string.Format("{0} item(s) copied.", items.Count);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Constants.WM_HOTKEY_MSG_ID)
                HandleF1();
            base.WndProc(ref m);
        }

        private Bitmap TakeScreenshot()
        {
            var bounds = Screen.PrimaryScreen.Bounds;
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
            var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, new Size(bounds.Width, bounds.Height));
            return bmp;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            F1.Unregister();
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            if (items.Count == 0) return;
            items.RemoveAt(items.Count - 1);
            if (items.Count > 0)
                pictureBox1.Image = items[items.Count - 1];
            else pictureBox1.Image = null;

            UpdateLabel();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (items.Count == 0) return;

                //int numCols = items.Count > 12 ? 4 : 3;
                int numCols = (int)Math.Min(Math.Ceiling(Math.Sqrt(items.Count)), 4); // Ventero
                int w = items[0].Width;
                var colLengths = new int[numCols];

                foreach (var item in items)
                {
                    var col = Range(0, numCols).Select(i => new { i = i, h = colLengths[i] }).OrderBy(k => k.h).First().i;
                    colLengths[col] += item.Height;
                }

                Bitmap b = new Bitmap(numCols * w, colLengths.Max(), PixelFormat.Format16bppRgb555);
                Graphics g = Graphics.FromImage(b);
                //g.FillRectangle(Brushes.Black, 0, 0, b.Width, b.Height);
                colLengths = new int[numCols];

                int itemIndex = 1;

                foreach (var item in items)
                {
                    var col = Range(0, numCols).Select(i => new { i = i, h = colLengths[i] }).OrderBy(k => k.h).First().i;
                    g.DrawImageUnscaledAndClipped(item, new Rectangle(w * col, colLengths[col], w, item.Height));
                    g.DrawString(itemIndex.ToString(), new Font("Arial", 20, FontStyle.Bold), Brushes.White, col * w + 10, colLengths[col] + 10);
                    colLengths[col] += item.Height;
                    itemIndex++;
                }

                var picFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                var fileName = string.Format("items-{0:yyyy-MM-dd-HH-mm-ss}.png", DateTime.UtcNow);
                var file = Path.Combine(picFolder, fileName);
                b.Save(file);
                items.Clear();
                UpdateLabel();
            }
            catch { }
        }
    }
}
