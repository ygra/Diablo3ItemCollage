using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ItemCollage
{
    public class ItemExtractor
    {
        private Bitmap bmp;
        private Point cursorPos;

        public ItemExtractor(Bitmap bitmap, Point cursorPosition)
        {
            this.bmp = bitmap;
            this.cursorPos = cursorPosition;
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

            return new Rectangle(extentLeft, extentUp,
                                 extentRight - extentLeft,
                                 extentDown - extentUp);
        }

        private Point FindOuter(Bitmap bmp, int x, int y, int step = 1, int searchWidth = 20)
        {
            // TODO: error handling
            var delta = step < 0 ? -1 : 1;
            var target = Helper.Range(1, searchWidth, Math.Abs(step))
                .FirstOrDefault(dx => bmp.IsBlackAt(x + delta * dx, y));
            target = x + delta * target;

            // if possible, move slightly to the left or right to get to the
            // middle of the frame
            if (bmp.IsBlackAt(target + delta, y))
                target += delta;

            return new Point(target, y);
        }


        public Image ExtractItem()
        {
            var searchSize = new Size(400, 400);
            var searchRect = new Rectangle(cursorPos.X - searchSize.Width / 2,
                                           cursorPos.Y - searchSize.Height / 2,
                                           searchSize.Width, searchSize.Height);

            // first, we have to find the inner item box
            var black = new List<Point>();
            foreach (var y in Helper.Range(searchRect.Top, searchRect.Bottom, 5))
            {
                foreach (var x in Helper.Range(searchRect.Left, searchRect.Right, 20))
                {
                    if (Helper.Range(-5, 5).All(dx =>
                        Helper.Range(-5, 5).All(dy => bmp.IsBlackAt(x + dx, y + dy))))
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
    }
}
