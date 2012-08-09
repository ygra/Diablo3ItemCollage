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
        IDictionary<GlobalHotkey, Action> hotkeys;
        List<Image> items = new List<Image>();

        public Form1()
        {
            InitializeComponent();

            // Initialise hotkeys
            hotkeys = new Dictionary<GlobalHotkey, Action> {
                { new GlobalHotkey(Constants.NOMOD, Keys.F1, this), GrabItem }
            };

            // register hotkeys
            foreach (var hk in hotkeys)
                hk.Key.Register();

            UpdateLabel();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            GrabItem();
        }

        private void GrabItem()
        {
            Stopwatch sw = new Stopwatch();

            var cursorPos = Cursor.Position;

            sw.Start();
            this.Opacity = 0;
            var screen = TakeScreenshot(ref cursorPos);
            this.Opacity = 1;

#if DEBUG
            sw.Stop();
            // save picture for future testing
            var baseName = string.Format("itemat-{0:yyyy-MM-dd-HH-mm-ss}-P{1}-{2}",
                DateTime.UtcNow, cursorPos.X, cursorPos.Y);
            var fileName = baseName + ".in.png";
            var picFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            var testFolder = Path.Combine(picFolder, "ExtractTest");
            if (!Directory.Exists(testFolder)) Directory.CreateDirectory(testFolder);
            var file = Path.Combine(testFolder, fileName);
            screen.Save(file);
            sw.Start();
#endif

            var ie = new ItemExtractor(screen, cursorPos);
            var item = ie.ExtractItem();
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

#if DEBUG
            var outfile = Path.Combine(testFolder, baseName + ".out.png");
            item.Save(outfile);
#endif

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
            {
                var key = (Keys)((int)m.LParam >> 16);
                foreach (var hotkey in hotkeys.Where(hk => hk.Key.Key == key))
                    hotkey.Value();
            }
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
            foreach (var hk in hotkeys)
                hk.Key.Unregister();
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
                    var col = Helper.Range(0, numCols - 1).MinBy(i => colLengths[i]);
                    colLengths[col] += item.Height;
                }

                Bitmap b = new Bitmap(numCols * w, colLengths.Max(), PixelFormat.Format16bppRgb555);
                Graphics g = Graphics.FromImage(b);
                colLengths = new int[numCols];

                int itemIndex = 1;

                foreach (var item in items)
                {
                    var col = Helper.Range(0, numCols - 1).MinBy(i => colLengths[i]);
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