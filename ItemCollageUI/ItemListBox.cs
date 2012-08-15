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

        double scalingFactor = 1;

        ImageTooltip tooltip;

        public ItemListBox()
            : base()
        {
            this.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.DoubleBuffered = true;
            this.titles = new Dictionary<Bitmap, Bitmap>();
            this.titleQueue = new Queue<Bitmap>(titleCount + 1);
            tooltip = new ImageTooltip();
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

        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            base.OnMeasureItem(e);
            if (e.Index < 0 || e.Index >= Items.Count) return;

            var item = (Bitmap)this.Items[e.Index];

            var title = GetTitle(item);
            e.ItemHeight = (int)(title.Height * scalingFactor) + 4;
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
            var itemIndex = IndexFromPoint(e.X, e.Y);
            if (itemIndex == NoMatches) return;
            
            var item = Items[itemIndex] as Bitmap;
            tooltip.Image = item;
            var location = PointToScreen(this.Location);
            location.Offset(this.Width, 0);
            tooltip.Location = location;
            tooltip.Show();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            tooltip.Hide();
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

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnResize(e);
            UpdateScalingFactor();
            var items = DataSource;
            DataSource = null;
            DataSource = items;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;

            var item = (Bitmap)this.Items[e.Index];

            var image = GetTitle(item);

            var drawWidth = image.Width * scalingFactor;
            var x = (drawWidth >= GetItemWidth()) ? 2 : (int)(GetItemWidth() / 2 - drawWidth / 2);

            var destRect = new Rectangle(e.Bounds.Location,
                new Size((int)drawWidth, (int)(image.Height * scalingFactor)));
            destRect.Offset(GetItemWidth() / 2 - destRect.Width / 2, 2);

            e.Graphics.FillRectangle(Brushes.Black, e.Bounds);
            e.Graphics.DrawImage(image, destRect, new Rectangle(new Point(), image.Size), GraphicsUnit.Pixel);
        }
    }
}
