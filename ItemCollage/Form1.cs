using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Threading.Tasks;

namespace ItemCollage
{
    public partial class Form1 : Form
    {
        readonly IDictionary<GlobalHotkey, Action> hotkeys;
        readonly BindingList<Item> items = new BindingList<Item>();
        readonly ImageTooltip tooltip = new ImageTooltip();
        readonly Options options = new Options();

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
            {
                hk.Key.Register();
            }

            UpdateLabel();

            var screenSize = Screen.FromControl(this).Bounds;
            Width = (int)(0.1 * screenSize.Width * screenSize.Width / screenSize.Height);

            // Data binding
            itemListBox1.DataSource = items;
            itemListBox1.ItemClick += (sender, e) =>
            {
                items.RemoveAt(e.Index);
                UpdateLabel();
            };

            items.ListChanged += delegate
            {
                button1.Enabled = items.Count > 0;
            };

            // Data binding, options
            var immediately = DataSourceUpdateMode.OnPropertyChanged;

            chkCopyCollages.DataBindings.Add(nameof(CheckBox.Checked), options,
                nameof(Options.CollageToClipboard), false, immediately);
            chkCopyItems.DataBindings.Add(nameof(CheckBox.Checked), options,
                nameof(Options.ItemToClipboard), false, immediately);
            chkUpdates.DataBindings.Add(nameof(CheckBox.Checked), options,
                nameof(Options.CheckForUpdates), false, immediately);
            chkTopMost.DataBindings.Add(nameof(CheckBox.Checked), options,
                nameof(Options.TopMost), false, immediately);

            this.DataBindings.Add(nameof(Form.TopMost), options,
                nameof(Options.TopMost), false, immediately);
        }

        private void GrabItem(bool saveScreenshot = false)
        {
            var cursorPos = Cursor.Position;

            var sw = Stopwatch.StartNew();
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
                // Status("No item found");
            }
            else
            {
                if (options.ItemToClipboard)
                {
                    Clipboard.SetImage(item.Image);
                }

                items.Add(item);
                itemListBox1.SelectedIndex = itemListBox1.Items.Count - 1;
                Debug.Print($"{itemListBox1.Items.Count} items in ListBox");
                UpdateLabel();
            }

            if (saveScreenshot)
            {
                // save picture for future testing
                string baseName;
                if (item == null)
                {
                    baseName = $"itemat-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}-P{cursorPos.X}-{cursorPos.Y}";
                }
                else
                {
                    baseName = $"itemat-{DateTime.UtcNow:yyyy-MM-dd-HH-mm-ss}-P{cursorPos.X}-{cursorPos.Y}-R{ie.ItemFrame.X}-{ie.ItemFrame.Y}-{ie.ItemFrame.Width}-{ie.ItemFrame.Height}";
                }

                var picFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
                var testFolder = Path.Combine(picFolder, "ExtractTest");
                if (!Directory.Exists(testFolder))
                {
                    Directory.CreateDirectory(testFolder);
                }

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
            // Status(string.Format("{0} item{1}",
            //     items.Count == 0 ? "No" : items.Count.ToString(),
            //     items.Count != 1 ? "s" : ""));
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Constants.WM_HOTKEY_MSG_ID)
            {
                var key = (Keys)((int)m.LParam >> 16);
                foreach (var hotkey in hotkeys.Where(hk => hk.Key.Key == key))
                {
                    hotkey.Value();
                }
            }
            base.WndProc(ref m);
        }

        private static Bitmap TakeScreenshot(ref Point p)
        {
            var bounds = Screen.FromPoint(p).Bounds;
            p.Offset(-bounds.X, -bounds.Y);
            Bitmap bmp = new Bitmap(bounds.Width, bounds.Height, PixelFormat.Format24bppRgb);
            var g = Graphics.FromImage(bmp);
            g.CopyFromScreen(bounds.X, bounds.Y, 0, 0, new Size(bounds.Width, bounds.Height));
            return bmp;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnClosing(e);
            options.Save();
            foreach (var hk in hotkeys)
            {
                hk.Key.Unregister();
            }
        }

        private void CreateCollage()
        {
            if (items.Count == 0)
            {
                return;
            }

            var columns = (int)Math.Min(Math.Ceiling(Math.Sqrt(items.Count)), 4);
            var collage = new Collage(items, columns);

            var b = collage.CreateCollage();

            var picFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            var fileName = $"items-{DateTime.Now:yyyyMMdd-HHmmss}.png";
            var file = Path.Combine(picFolder, fileName);
            b.Save(file);

            if (options.CollageToClipboard)
                Clipboard.SetImage(b);

            items.Clear();
            // Status("Collage saved");
        }

        private void CollageButtonClicked(object sender, EventArgs e)
        {
            CreateCollage();
        }

        protected async override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            this.TopMost = options.TopMost;
            await CheckForUpdates();
        }

        private void ListMouseOver(object sender, MouseEventArgs e)
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
            {
                location.Offset(-(itemListBox1.Width + tooltip.Width), 0);
            }

            if (position.Bottom > bounds.Bottom)
            {
                location.Y = bounds.Bottom - tooltip.Height;
            }

            if (!tooltip.Visible)
            {
                Helper.ShowInactiveTopmost(tooltip);
            }

            // this has to happen after Show, as otherwise it's ignored until
            // the tooltip gets redrawn
            tooltip.Location = location;
        }

        private void ListMouseLeave(object sender, EventArgs e) => tooltip.Hide();

        private void ClearLinkClicked(object sender, LinkLabelLinkClickedEventArgs e) => items.Clear();

        private void ToggleOptions()
        {
            pnlOptions.Visible = !pnlOptions.Visible;
            itemListBox1.Visible = !itemListBox1.Visible;
            lblHelp.Text = pnlOptions.Visible ?
                "Options are saved automatically." :
                "Grab items with F1, click to remove.";

            if (!pnlOptions.Visible)
            {
                options.Save();
            }
        }

        private void OptionsButtonClicked(object sender, EventArgs e) => ToggleOptions();

        private async Task CheckForUpdates()
        {
            if (!options.CheckForUpdates)
            {
                return;
            }

            try
            {
                var (updateAvailable, newVersion, changelog) = await UpdateCheck.IsUpdateAvailable();
                if (updateAvailable)
                {
                    var res = MessageBox.Show(
                        $"New version {newVersion} available, download now?\r\n\r\nChanges:\r\n{changelog}",
                        "Diablo3ItemCollage", MessageBoxButtons.YesNo);

                    if (res == DialogResult.Yes)
                    {
                        UpdateCheck.OpenDownloadUrl();
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show($"Checking for updates failed:\n{e.Message}");
            }
        }
    }
}
