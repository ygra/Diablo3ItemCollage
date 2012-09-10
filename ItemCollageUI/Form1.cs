using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;

namespace ItemCollage
{
    public partial class Form1 : Form
    {
        const string UpdateUrl =
            "https://raw.github.com/ygra/Diablo3ItemCollage/master/version";
        const string ChangelogUrl =
            "https://raw.github.com/ygra/Diablo3ItemCollage/master/CHANGELOG";
        const string DownloadUrl =
            "https://github.com/ygra/Diablo3ItemCollage/downloads";

        IDictionary<GlobalHotkey, Action> hotkeys;
        BindingList<Item> items = new BindingList<Item>();
        ImageTooltip tooltip = new ImageTooltip();

        public Form1()
        {
            InitializeComponent();

            // Initialise hotkeys
            hotkeys = new Dictionary<GlobalHotkey, Action> {
                { new GlobalHotkey(Constants.NOMOD, Keys.F1, this), () => GrabItem() },
                { new GlobalHotkey(Constants.NOMOD, Keys.F2, this), CreateCollage },
#if DEBUG
                { new GlobalHotkey(Constants.NOMOD, Keys.F3, this), () => GrabItem(true) },
#endif
            };

            // register hotkeys
            foreach (var hk in hotkeys)
                hk.Key.Register();

            UpdateLabel();

            var screenSize = Screen.FromControl(this).Bounds;
            Width = (int)(0.1 * screenSize.Width * screenSize.Width / screenSize.Height);

            CheckForUpdates();

            // Data binding
            itemListBox1.DataSource = items;
            itemListBox1.ItemClick += delegate(object sender,
                ItemListBox.ItemClickEventArgs e)
            {
                items.RemoveAt(e.Index);
                UpdateLabel();
            };

            items.ListChanged += delegate
            {
                button1.Enabled = items.Count > 0;
            };
        }

        private void GrabItem(bool saveScreenshot = false)
        {
            Stopwatch sw = new Stopwatch();

            var cursorPos = Cursor.Position;

            sw.Start();
            this.Opacity = 0;
            var screen = TakeScreenshot(ref cursorPos);
            this.Opacity = 1;

            var ie = new ItemExtractor(screen, cursorPos);
            Item item = null;
            try
            {
                item = ie.ExtractItem();
            }
            catch { }

            sw.Stop();
            Debug.Print("Time for extraction: " + sw.Elapsed.ToString());

            if (item == null)
            {
                label1.Text = "No item found";
            }
            else
            {
                Clipboard.SetImage(item.Image);
                items.Add(item);
                itemListBox1.SelectedIndex = itemListBox1.Items.Count - 1;
                Debug.Print("{0} items in ListBox", itemListBox1.Items.Count);
                UpdateLabel();
            }

            if (saveScreenshot)
            {
                // save picture for future testing
                string baseName;
                if (item == null)
                    baseName = string.Format("itemat-{0:yyyy-MM-dd-HH-mm-ss}-P{1}-{2}",
                        DateTime.UtcNow, cursorPos.X, cursorPos.Y);
                else
                    baseName = string.Format(
                        "itemat-{0:yyyy-MM-dd-HH-mm-ss}-P{1}-{2}-R{3}-{4}-{5}-{6}",
                        DateTime.UtcNow, cursorPos.X, cursorPos.Y, ie.ItemFrame.X,
                        ie.ItemFrame.Y, ie.ItemFrame.Width, ie.ItemFrame.Height);

                var picFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                var testFolder = Path.Combine(picFolder, "ExtractTest");
                if (!Directory.Exists(testFolder)) Directory.CreateDirectory(testFolder);

                var file = Path.Combine(testFolder, baseName + ".in.png");
                screen.Save(file);

                if (item != null)
                {
                    var outfile = Path.Combine(testFolder, baseName + ".out.png");
                    item.Image.Save(outfile);

                    var titlefile = Path.Combine(testFolder, baseName + ".title.png");
                    item.Title.Save(titlefile);
                }
            }

        }

        private void UpdateLabel()
        {
            label1.Text = string.Format("{0} item{1}",
                items.Count == 0 ? "No" : items.Count.ToString(),
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

            client.DownloadStringAsync(new Uri(UpdateUrl));
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
                ShowUpdateAvailable(local, remote);
            }
        }

        private void ShowUpdateAvailable(Version oldVersion, Version newVersion)
        {
            var client = new WebClient();
            client.Encoding = Encoding.UTF8;
            client.DownloadStringCompleted +=
                delegate(object s, DownloadStringCompletedEventArgs e)
                {
                    if (e.Error != null)
                        return;

                    var changelog = GetChangelog(e.Result, oldVersion, newVersion);

                    var res = MessageBox.Show(string.Format(
                        "New version {0} available, download now?\r\n\r\nChanges:\r\n" + changelog, newVersion),
                        "Diablo3ItemCollage", MessageBoxButtons.YesNo);

                    if (res == DialogResult.Yes)
                        Process.Start(DownloadUrl);
                };

            client.DownloadStringAsync(new Uri(ChangelogUrl));
        }

        private string GetChangelog(string completeChangelog, Version oldVersion, Version newVersion)
        {
            var lines = Regex.Split(completeChangelog, "\r?\n");

            var versionLine = new Regex(@"^\d{4}-\d{2}-\d{2}\tv(?<version>[\d.]+)");
            var log = new List<string>();
            var capture = false;
            foreach (var line in lines)
            {
                // did we hit a line that starts the changelog for a specific version?
                Match m = versionLine.Match(line);
                if (m.Success)
                {
                    var lineVersion = new Version(m.Groups["version"].Value);
                    // capture only newer versions
                    if (lineVersion <= newVersion) capture = true;
                    // and leave the older ones out
                    if (lineVersion <= oldVersion) capture = false;
                }

                // grab line only if it doesn't start with a . which marks minor changes
                if (capture && !line.StartsWith("."))
                {
                    // make the asterisk into a nice bullet point
                    var lineToAdd = Regex.Replace(line, @"^\*", "     •");
                    // remove the date from the version line
                    lineToAdd = Regex.Replace(lineToAdd, @"^[\d-]+\t", "");

                    log.Add(lineToAdd);
                }
            }

            return string.Join("\r\n", log.ToArray());
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

            UpdateLabel();
        }

        private void CreateCollage()
        {
            if (items.Count == 0) return;

            var columns = (int)Math.Min(Math.Ceiling(Math.Sqrt(items.Count)), 4);
            var collage = new Collage(items, columns);

            Bitmap b = collage.CreateCollage();

            var picFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            var fileName = string.Format("items-{0:yyyyMMdd-HHmmss}.png", DateTime.Now);
            var file = Path.Combine(picFolder, fileName);
            b.Save(file);
            Clipboard.SetImage(b);

            items.Clear();
            label1.Text = "Collage saved";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            CreateCollage();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.TopMost = true;
        }

        private void itemListBox1_MouseMove(object sender, MouseEventArgs e)
        {
            var itemIndex = (short)itemListBox1.IndexFromPoint(e.X, e.Y);
            if (itemIndex == ListBox.NoMatches) return;

            var item = items[itemIndex];
            if (tooltip.Image == item.Image && tooltip.Visible) return;

            tooltip.Image = item.Image;
            var location = PointToScreen(itemListBox1.Location);
            location.Offset(itemListBox1.Width, 0);

            var position = new Rectangle(location, tooltip.Size);
            var bounds = Screen.FromPoint(e.Location).Bounds;
            if (position.Right > bounds.Right)
                location.Offset(-(itemListBox1.Width + tooltip.Width), 0);
            if (position.Bottom > bounds.Bottom)
                location.Y = bounds.Bottom - tooltip.Height;

            if (!tooltip.Visible)
                Helper.ShowInactiveTopmost(tooltip);

            // this has to happen after Show, as otherwise it's ignored until
            // the tooltip gets redrawn
            tooltip.Location = location;
        }

        private void itemListBox1_MouseLeave(object sender, EventArgs e)
        {
            tooltip.Hide();
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            items.Clear();
        }

    }
}
