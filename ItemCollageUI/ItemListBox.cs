using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace ItemCollage
{
    public class ItemListBox : ListBox
    {
        const int titleCount = 50;
        const int xMargin = 4;
        const int yMargin = 2;

        IDictionary<Bitmap, Bitmap> titles;
        Queue<Bitmap> titleQueue;

        double scalingFactor = 1;

        public class ItemClickEventArgs : EventArgs
        {
            public int Index { get; set; }
        }

        public event EventHandler<ItemClickEventArgs> ItemClick;

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

        private int GetListWidth()
        {
            return ClientRectangle.Width - xMargin;
        }

        protected override void OnMeasureItem(MeasureItemEventArgs e)
        {
            base.OnMeasureItem(e);
            if (e.Index < 0 || e.Index >= Items.Count) return;

            UpdateScalingFactor();

            var item = (Bitmap)this.Items[e.Index];

            var title = GetTitle(item);
            e.ItemHeight = (int)(title.Height * scalingFactor) + 2 * yMargin;

            // account for the separator
            if (e.Index > 0)
                e.ItemHeight += 3;
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            var itemIndex = (short)IndexFromPoint(e.X, e.Y);
            if (itemIndex == NoMatches) return;

            if (ItemClick != null) ItemClick(this, new ItemClickEventArgs
            {
                Index = itemIndex
            });
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
        }

        private double GetScalingFactor(int titleWidth)
        {
            var listWidth = GetListWidth();
            if (listWidth >= titleWidth) return 1;
            return (double)listWidth / titleWidth;
        }

        public void UpdateScalingFactor()
        {
            scalingFactor =
                Items.Cast<Bitmap>()
                    .Select(b => GetScalingFactor(GetTitle(b).Width))
                    .DefaultIfEmpty(1)
                    .Min();
        }

        protected override void OnClientSizeChanged(EventArgs e)
        {
            base.OnClientSizeChanged(e);
            UpdateScalingFactor();
            // re-attach the data source, this is necessary to re-measure all items
            var items = DataSource;
            DataSource = null;
            DataSource = items;
        }

        protected override void OnDrawItem(DrawItemEventArgs e)
        {
            if (e.Index < 0 || e.Index >= Items.Count) return;

            var item = (Bitmap)this.Items[e.Index];

            var image = GetTitle(item);

            var drawWidth = (int)(image.Width * scalingFactor);
            var drawHeight = (int)(image.Height * scalingFactor);

            var destRect = new Rectangle(e.Bounds.Location,
                new Size(drawWidth, drawHeight));

            // we need to manually add xMargin to the offset again, as it's
            // already subtracted from the actual list width in GetListWidth
            destRect.Offset((GetListWidth() - drawWidth + xMargin) / 2, yMargin);

            // background
            e.Graphics.FillRectangle(Brushes.Black, e.Bounds);

            // separator
            if (e.Index > 0)
            {
                e.Graphics.DrawLine(Pens.DimGray,
                    e.Bounds.Left + 5, e.Bounds.Top + 1,
                    e.Bounds.Right - 5, e.Bounds.Top + 1);

                destRect.Offset(0, 3);
            }

            // title
            e.Graphics.DrawImage(image, destRect,
                new Rectangle(new Point(), image.Size), GraphicsUnit.Pixel);
        }
    }
}
