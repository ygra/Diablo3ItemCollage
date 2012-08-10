using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.ComponentModel;
using ItemCollage;

namespace ItemCollage
{
    public partial class Form1 : Form
    {
        const string UPDATE_URL =
            "https://raw.github.com/ygra/Diablo3ItemCollage/master/version";
        const string DOWNLOAD_URL =
            "https://github.com/ygra/Diablo3ItemCollage/downloads";

        IDictionary<GlobalHotkey, Action> hotkeys;
        BindingList<Image> items = new BindingList<Image>();

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

            CheckForUpdates();

            itemListBox1.DataSource = items;
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
            Debug.Print("{0} items in ListBox", itemListBox1.Items.Count);
            //pictureBox1.Image = item;
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

        private void CheckForUpdates()
        {
            var client = new WebClient();
            client.Encoding = Encoding.UTF8;
            client.DownloadStringCompleted +=
                delegate(object s, DownloadStringCompletedEventArgs e)
                {
                    if (e.Error != null)
                        CheckUpdateError(e.Error);
                    else
                        CompareVersions(e.Result);
                };

            client.DownloadStringAsync(new Uri(UPDATE_URL));
        }

        private void CheckUpdateError(Exception error)
        {
            MessageBox.Show("Checking for updates failed:\n" + error.Message);
        }

        private void CompareVersions(string data)
        {
            var local = Assembly.GetExecutingAssembly().GetName().Version;
            var remote = new Version(data);

            if (remote > local)
            {
                var res = MessageBox.Show(string.Format(
                    "New version {0} available, download now?", remote),
                    "Diablo3ItemCollage", MessageBoxButtons.YesNo);

                if (res == DialogResult.Yes)
                    Process.Start(DOWNLOAD_URL);
            }
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

            //if (items.Count > 0)
            //    pictureBox1.Image = items[items.Count - 1];
            //else
            //    pictureBox1.Image = null;

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
                //pictureBox1.Image = null;
                label1.Text = "Collage saved";
            }
            catch { }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.TopMost = true;
        }

    }
}

public class ItemListBox : ListBox
{
    const int titleCount = 50;

    IDictionary<Bitmap, Bitmap> titles;
    Queue<Bitmap> titleQueue;

    public ItemListBox() : base() {
        this.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
        this.titles = new Dictionary<Bitmap, Bitmap>();
        this.titleQueue = new Queue<Bitmap>(titleCount + 1);
    }

    private Bitmap GetTitle(Bitmap item)
    {
        if (titles.ContainsKey(item))
            return titles[item];

        var title = ItemExtractor.ExtractItemName(item, true);

        titleQueue.Enqueue(title);
        if (titleQueue.Count > titleCount)
            titleQueue.Dequeue();

        titles[item] = title;
        return item;
    }

    protected override void OnMeasureItem(MeasureItemEventArgs e)
    {
        base.OnMeasureItem(e);

        try
        {
            var item = (Bitmap)this.Items[e.Index];
            var title = GetTitle(item);
            e.ItemHeight = title.Height;
        }
        catch
        {
            e.ItemHeight = 10;
            return;
        }
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        base.OnDrawItem(e);

        try
        {
            var item = (Bitmap)this.Items[e.Index];
            var title = GetTitle(item);
            e.Graphics.FillRectangle(Brushes.Black, e.Bounds);
            e.Graphics.DrawImageUnscaled(title, e.Bounds);
        }
        catch
        {
            e.Graphics.FillRectangle(Brushes.Red, e.Bounds);
            return;
        }
    }
}
