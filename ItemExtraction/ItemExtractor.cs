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

        public ItemExtractor(Bitmap bitmap, Point cursorPos)
        {
            this.bmp = bitmap;
            this.cursorPos = cursorPos;
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
            if (twoDim && !Helper.Range(extentUp, extentDown).All(
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
            var target = Helper.Range(1, searchWidth, Math.Abs(step))
                .FirstOrDefault(dx => bmp.IsBlackAt(x + delta * dx, y));
            target = x + delta * target;

            // if possible, move slightly to the left or right to get to the
            // middle of the frame
            while (bmp.IsBlackAt(target + delta, y))
                target += delta;

            return new Point(target, y);
        }

        private List<Point> FindBlackSquares(Bitmap bmp, IEnumerable<int> horizontal,
            IEnumerable<int> vertical, int size = 5)
        {
            var black = new List<Point>();
            foreach (var y in vertical)
            {
                foreach (var x in horizontal)
                {
                    if (Helper.Range(-size, size).All(dx =>
                        Helper.Range(-size, size).All(dy => bmp.IsBlackAt(x + dx, y + dy))))
                    {
                        black.Add(new Point(x, y));
                        // since we move outwards from the cursor, we can safely
                        // break here without risking not to hit the actual item
                        // frame
                        break;
                    }
                }
            }
            return black;
        }

        public Image ExtractItem()
        {
            var searchSize = new Size(1200, 400);
            var searchRect = new Rectangle(cursorPos.X - searchSize.Width / 2,
                                           cursorPos.Y - searchSize.Height / 2,
                                           searchSize.Width, searchSize.Height);

            // first, we have to find the inner item box
            // we do this by moving outwards from the cursor, that way we can
            // be sure to hit the actual item first, instead of a potential
            // equipped item popup
            var vertical = Helper.Range(searchRect.Top, searchRect.Bottom, 5);
            var left = Helper.Range(cursorPos.X, searchRect.Left, -100);
            var right = Helper.Range(cursorPos.X, searchRect.Right, 100);

            var black = new List<Point>();
            black.AddRange(FindBlackSquares(bmp, left, vertical));
            black.AddRange(FindBlackSquares(bmp, right, vertical));

            // find all left and right border points
            var frames = black.Select(p => FindFrame(bmp, p, false))
                .Where(f => f.Width >= 150);
            var leftBorders = frames.Distinct(f => f.Left);
            var rightBorders = frames.Distinct(f => f.Right);

            // from those border points, move outwards to find the outer frame
            var outerPoints = rightBorders.Select(f => FindOuter(bmp, f.Right, f.Top))
                .Concat(leftBorders.Select(f => FindOuter(bmp, f.Left, f.Bottom, -1)));

            var outerFrames = outerPoints.Distinct()
                .Select(p => FindFrame(bmp, p, true));

            // the biggest frame we found is (hopefully) the item frame
            var itemFrame = outerFrames.OrderByDescending(f => f.Width)
                .ThenByDescending(f => f.Height).FirstOrDefault();

            if (itemFrame.Width < 100 || itemFrame.Height < 50)
                return null;

            Bitmap item = new Bitmap(itemFrame.Width, itemFrame.Height,
                PixelFormat.Format24bppRgb);
            Graphics g = Graphics.FromImage(item);
            var targetFrame = new Rectangle(0, 0, itemFrame.Width, itemFrame.Height);
            g.DrawImage(bmp, targetFrame, itemFrame, GraphicsUnit.Pixel);
            g.Dispose();

            return item;
        }
    }
}
