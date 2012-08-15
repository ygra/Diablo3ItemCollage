using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Imaging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace ItemCollage
{
    public class ItemListBox : ListBox
    {
        const int titleCount = 50;

        IDictionary<Bitmap, Bitmap> titles;
        Queue<Bitmap> titleQueue;

        int? hotTrackedIndex = null;
        double scalingFactor = 1;
        int oldSelectedIndex = -1;

        public ItemListBox()
            : base()
        {
            this.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.DoubleBuffered = true;
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
            return title;
        }

        private int GetItemWidth()
        {
            return ClientRectangle.Width - 4;
        }

        public new int GetItemHeight(int index)
        {
            int height;
            var item = (Bitmap)this.Items[index];

            if (index == SelectedIndex)
            {
                // show full item if selected
                height = (int)(item.Height * scalingFactor);
            }
            else
            {
                // show only the title
                var title = GetTitle(item);
                height = (int)(title.Height * scalingFactor);
            }

            // leave a little more space
            height += 4;
            return height;
        }

        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;

            e.ItemHeight = GetItemHeight(e.Index);

            base.OnMeasureItem(e);
        }

        private Bitmap GetBrighter(Bitmap bmp)
        {
            var b = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format24bppRgb);

            for (int y = 0; y < bmp.Height; y++)
                for (int x = 0; x < bmp.Width; x++)
                {
                    var c = bmp.GetPixel(x, y);
                    var c2 = Color.FromArgb((int)Math.Min(255, c.R * 1.5),
                        (int)Math.Min(255, c.G * 1.5), (int)Math.Min(255, c.B * 1.5));
                    b.SetPixel(x, y, c2);
                }

            return b;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            var index = IndexFromPoint(e.Location);
            var oldIndex = hotTrackedIndex;

            hotTrackedIndex = (index == NoMatches) ? (int?)null : index;

            if (oldIndex != index)
            {
                if (oldIndex != null && oldIndex >= 0 && oldIndex < Items.Count)
                    Invalidate(GetItemRectangle((int)oldIndex));
                if (index >= 0 && index < Items.Count)
                    Invalidate(GetItemRectangle(index));
            }
        }

        protected override void OnSelectedIndexChanged(EventArgs e)
        {
            base.OnSelectedIndexChanged(e);
            UpdateItemHeight(SelectedIndex);
            UpdateItemHeight(oldSelectedIndex);
            Refresh();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            hotTrackedIndex = null;
            Refresh();
        }

        private double GetScalingFactor(int titleWidth)
        {
            var itemWidth = GetItemWidth();
            if (titleWidth < itemWidth) return 1;
            return titleWidth / itemWidth;
        }

        public void UpdateScalingFactor()
        {
            scalingFactor =
                Items.Cast<Bitmap>()
                    .Select(b => GetScalingFactor(b.Width)).DefaultIfEmpty(1)
                    .Min();
        }

        private void UpdateItemHeight()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                SendMessage(Handle, LB_SETITEMHEIGHT, i, GetItemHeight(i));
            }
            Refresh();
        }

        private void UpdateItemHeight(int index)
        {
            if (index < 0 || index >= Items.Count) return;
            SendMessage(Handle, LB_SETITEMHEIGHT, index, GetItemHeight(index));
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnResize(e);
            UpdateScalingFactor();
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            var item = (Bitmap)this.Items[e.Index];

            var image = e.Index == SelectedIndex ? item : GetTitle(item);

            if (e.Index == hotTrackedIndex)
                image = GetBrighter(image);

            var drawWidth = image.Width * scalingFactor;
            var x = (drawWidth >= GetItemWidth()) ? 2 : (int)(GetItemWidth() / 2 - drawWidth / 2);

            var destRect = e.Bounds;
            destRect.Inflate(-2 * x, -4);

            e.Graphics.FillRectangle(Brushes.Black, e.Bounds);
            e.Graphics.DrawImage(image, destRect, new Rectangle(new Point(), image.Size), GraphicsUnit.Pixel);
        }

        public const int LB_SETITEMHEIGHT = 0x01a0;
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern IntPtr SendMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);
    }

    class ItemList : BindingList<Bitmap>
    {

    }
}
