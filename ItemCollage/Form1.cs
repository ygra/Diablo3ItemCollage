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
using System.Diagnostics;

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

        private Rectangle FindFrame(Bitmap bmp, Point p, bool twoDim = true)
        {
            var extentUp = p.Y;
            var extentDown = p.Y;
            var skip = 0;
            const int MAX_SKIP = 3;

            if (!bmp.IsBlackAt(p.X, p.Y)) return new Rectangle();

            if (twoDim)
            {
                extentUp = p.Y -
                    Enumerable.Range(0, p.Y)
                        .TakeWhile(y => bmp.IsBlackAt(p.X, p.Y - y) ||
                            ++skip < MAX_SKIP)
                        .Last(y => bmp.IsBlackAt(p.X, p.Y - y));

                extentDown =
                    Enumerable.Range(p.Y, bmp.Height - p.Y)
                        .TakeWhile(y => bmp.IsBlackAt(p.X, y) ||
                            ++skip < MAX_SKIP)
                        .Last(y => bmp.IsBlackAt(p.X, y));
            }

            var extentLeft = p.X -
                Enumerable.Range(0, p.X)
                    .TakeWhile(x => bmp.IsBlackAt(p.X - x, extentDown))
                    .Last();

            var extentRight =
                Enumerable.Range(p.X, bmp.Width)
                    .TakeWhile(x => bmp.IsBlackAt(x, extentDown))
                    .Last();

            skip = 0;
            if (twoDim && !Range(extentUp, extentDown).All(
                    y => bmp.IsBlackAt(extentRight, y) &&
                         bmp.IsBlackAt(extentLeft, y) ||
                         ++skip < MAX_SKIP))
                return new Rectangle();

            return new Rectangle(extentLeft, extentUp,
                                 extentRight - extentLeft,
                                 extentDown - extentUp);
        }

        private Point FindOuter(Bitmap bmp, int x, int y, int step = 1, int searchWidth = 20)
        {
            var delta = step < 0 ? -1 : 1;
            var target = Range(1, searchWidth, Math.Abs(step))
                .FirstOrDefault(dx => bmp.IsBlackAt(x + delta * dx, y));
            target = x + delta * target;

            // if possible, move slightly to the left or right to get to the
            // middle of the frame
            while (bmp.IsBlackAt(target + delta, y))
                target += delta;

            return new Point(target, y);
        }

        private IEnumerable<int> Range(int start, int end, int step = 1)
        {
            if (start > end && step > 0 ||
                start < end && step < 0 ||
                step == 0)
                throw new ArgumentException("Impossible range");

            int steps = (end - start) / step;
            int i, s;
            for (i = start, s = 0; s <= steps; i += step, s++)
            {
                yield return i;
            }
        }

        Image ExtractItem(Bitmap bmp, Point cursorPosition)
        {
            var searchSize = new Size(400, 400);
            var searchRect = new Rectangle(cursorPosition.X - searchSize.Width / 2,
                                           cursorPosition.Y - searchSize.Height / 2,
                                           searchSize.Width, searchSize.Height);

            // first, we have to find the inner item box
            var black = new List<Point>();
            foreach(var y in Range(searchRect.Top, searchRect.Bottom, 5))
            {
                foreach (var x in Range(searchRect.Left, searchRect.Right, 20))
                {
                    if (Range(-5, 5).All(dx =>
                        Range(-5, 5).All(dy => bmp.IsBlackAt(x + dx, y + dy))))
                    {
                        black.Add(new Point(x, y));
                        break;
                    }
                }
            }

            var frames = black.Select(p => FindFrame(bmp, p, false)).
                OrderBy(f => f.Width);

            // reject all frames that aren't full width
            var fullWidth = frames.LastOrDefault().Width;
            var fullFrames = frames.Where(f => f.Width == fullWidth);

            // then, its left and right border
            var left = fullFrames.OrderBy(f => f.Left).FirstOrDefault();
            var right = fullFrames.OrderBy(f => f.Right).LastOrDefault();
            if (left.Width == 0 && right.Width == 0) return null;

            // and from there find the outer frame
            var leftTarget = FindOuter(bmp, left.Left, left.Top, -1);
            var leftFrame = FindFrame(bmp, leftTarget, true);

            var rightTarget = FindOuter(bmp, right.Right, right.Top);
            var rightFrame = FindFrame(bmp, rightTarget, true);

            var itemFrame = rightFrame;
            if (leftFrame.Width > rightFrame.Width ||
                leftFrame.Width == rightFrame.Width &&
                leftFrame.Height > rightFrame.Height)
            {
                itemFrame = leftFrame;
            }

            if (itemFrame.Width < 100 || itemFrame.Height < 50)
                return null;

            Bitmap item = new Bitmap(itemFrame.Width, itemFrame.Height,
                PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(item);
            g.DrawImage(bmp, 0, 0, itemFrame, GraphicsUnit.Pixel);
            g.Dispose();

            return item;
        }

        private void HandleF1()
        {
            Stopwatch sw = new Stopwatch();

            var cursorPos = Cursor.Position;

            sw.Start();
            this.Opacity = 0;
            var screen = TakeScreenshot(ref cursorPos);
            this.Opacity = 1;

            var item = ExtractItem(screen, cursorPos);
            sw.Stop();
            Debug.Print("Time for extraction: " + sw.Elapsed.ToString());

            if (item == null)
            {
                label1.Text = "No item found";
                return;
            }

            items.Add(item);
            pictureBox1.Image = item;
            Clipboard.SetImage(item);

            UpdateLabel();
        }

        private void UpdateLabel()
        {
            label1.Text = string.Format("{0} item{1} copied.", items.Count,
                items.Count != 1 ? "s" : "");
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Constants.WM_HOTKEY_MSG_ID)
                HandleF1();
            base.WndProc(ref m);
        }

        private Bitmap TakeScreenshot(ref Point p)
        {
            var bounds = Screen.FromPoint(p).Bounds;
            p.Offset(-bounds.X, -bounds.Y);
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
            else
                pictureBox1.Image = null;

            UpdateLabel();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                if (items.Count == 0) return;

                int numCols = (int)Math.Min(Math.Ceiling(Math.Sqrt(items.Count)), 4);
                int w = items[0].Width;
                var colLengths = new int[numCols];

                foreach (var item in items)
                {
                    var col = Range(0, numCols - 1).MinBy(i => colLengths[i]);
                    colLengths[col] += item.Height;
                }

                Bitmap b = new Bitmap(numCols * w, colLengths.Max(), PixelFormat.Format16bppRgb555);
                Graphics g = Graphics.FromImage(b);
                colLengths = new int[numCols];

                int itemIndex = 1;

                foreach (var item in items)
                {
                    var col = Range(0, numCols - 1).MinBy(i => colLengths[i]);
                    g.DrawImageUnscaledAndClipped(item, new Rectangle(w * col, colLengths[col], w, item.Height));
                    g.DrawString(itemIndex.ToString(), new Font("Arial", 20, FontStyle.Bold), Brushes.White, col * w + 10, colLengths[col] + 10);
                    colLengths[col] += item.Height;
                    itemIndex++;
                }

                var picFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                var fileName = string.Format("items-{0:yyyy-MM-dd-HH-mm-ss}.png", DateTime.UtcNow);
                var file = Path.Combine(picFolder, fileName);
                b.Save(file);
                Clipboard.SetImage(b);
                items.Clear();
                pictureBox1.Image = null;
                label1.Text = "Collage saved";
            }
            catch { }
        }
    }
}