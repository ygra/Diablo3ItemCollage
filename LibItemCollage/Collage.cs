using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

namespace ItemCollage
{
    class CollageItem
    {
        public Bitmap Item { get; set; }
        public int Column { get; set; }
        public Point Pos { get; set; }
        public int Index { get; set; }
    }

    public class Collage
    {
        private int columns;
        private int itemWidth;

        private Size size;
        private List<CollageItem> collageItems;
        private Font indexFont = new Font("Arial", 20, FontStyle.Bold);

        public Collage(IEnumerable<Image> items, int columns)
        {
            this.columns = columns;

            GenerateLayout(items);
        }

        private void GenerateLayout(IEnumerable<Image> items)
        {
            collageItems = new List<CollageItem>();

            var itemList = items.ToList();
            itemWidth = itemList[0].Width;

            var itemIndex = 1;
            var colLengths = new int[columns];
            foreach (var item in itemList)
            {
                var col = Helper.Range(0, columns - 1).MinBy(i => colLengths[i]);

                collageItems.Add(new CollageItem
                {
                    Item = (Bitmap)item,
                    Column = col,
                    Pos = new Point(col * itemWidth, colLengths[col]),
                    Index = itemIndex++
                });

                colLengths[col] += item.Height;
            }

            size = new Size(columns * itemWidth, colLengths.Max());
        }

        public Bitmap CreateCollage()
        {
            if (collageItems == null || collageItems.Count() == 0) return null;

            Bitmap bmp = new Bitmap(size.Width, size.Height, PixelFormat.Format16bppRgb555);
            using (Graphics g = Graphics.FromImage(bmp))
            {
                foreach (var item in collageItems)
                {
                    g.DrawImageUnscaledAndClipped(item.Item, new Rectangle(item.Pos, item.Item.Size));

                    // don't draw numbers for just a single item
                    if (collageItems.Count > 1)
                        g.DrawString(item.Index.ToString(), indexFont, Brushes.White,
                            item.Pos.X + 10, item.Pos.Y + 10);
                }
            }

            return bmp;
        }
    }
}
