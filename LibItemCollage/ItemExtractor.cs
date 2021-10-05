﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace ItemCollage
{
    public class ItemExtractor
    {
        private Bitmap bmp;
        private Point cursorPos;

        private const int MaxSkip = 2;

        public Rectangle ItemFrame { get; private set; }

        public ItemExtractor(Bitmap bitmap, Point cursorPos)
        {
            this.bmp = bitmap;
            this.cursorPos = cursorPos;
        }

        private Rectangle FindBorder(Bitmap bmp, Point p)
        {
            int left, right;
            var rect = new Rectangle(0, p.Y, bmp.Width, 1);
            using (var data = new LockData(bmp, rect))
            {
                if (!data.IsBlackAt(p.X)) return new Rectangle();

                left = Helper.Range(p.X, 0, -1)
                    .TakeWhile(x => data.IsBlackAt(x))
                    .Last();

                right = Helper.Range(p.X, bmp.Width - 1)
                    .TakeWhile(x => data.IsBlackAt(x))
                    .Last();
            }

            return new Rectangle(left, p.Y, right - left + 1, 0);
        }

        private Rectangle SelectFrame(Bitmap bmp, Point p)
        {
            var skip = 0;

            var top = 0;
            var bottom = 0;

            var rect = new Rectangle(p.X, 0, 1, bmp.Height);
            using (var data = new LockData(bmp, rect))
            {
                if (!data.IsBlackAt(0, p.Y)) return new Rectangle();

                top = Helper.Range(p.Y, 0, -1)
                        .TakeWhile(y => data.IsBlackAt(0, y) ||
                            skip++ < MaxSkip)
                        .Last(y => data.IsBlackAt(0, y));

                skip = 0;
                bottom = Helper.Range(p.Y, bmp.Height - 1)
                        .TakeWhile(y => data.IsBlackAt(0, y) ||
                            skip++ < MaxSkip)
                        .Last(y => data.IsBlackAt(0, y));
            }

            var border = FindBorder(bmp, new Point(p.X, bottom));
            var left = border.Left;

            // verify the left border is indeed black
            var leftRect = new Rectangle(left, 0, 1, bmp.Height);
            using (var data = new LockData(bmp, leftRect))
            {
                if (!data.IsColumnBlack(0, top, bottom, MaxSkip))
                    return new Rectangle();
            }

            return new Rectangle(left, top, border.Width, bottom - top + 1);
        }

        private Point FindOuter(Bitmap bmp, int x, int y, int step = 1, int searchWidth = 20)
        {
            var delta = step < 0 ? -1 : 1;
            var max = delta > 0 ? bmp.Width - x : x;
            if (searchWidth > max) searchWidth = max;

            int target;
            var rect = new Rectangle(0, y, bmp.Width, 1);
            using (var data = new LockData(bmp, rect))
            {
                target = Helper.Range(1, searchWidth, Math.Abs(step))
                    .Select(dx => x + delta * dx)
                    .Where(dx => data.IsBlackAt(dx))
                    .DefaultIfEmpty(-1)
                    .First();

                // if possible, move slightly to the left or right to get to the
                // middle of the frame
                while (data.IsBlackAt(target + delta))
                    target += delta;
            }

            return new Point(target, y);
        }

        private List<Point> FindBlackSquares(Bitmap bmp, IEnumerable<int> horizontal,
            IEnumerable<int> vertical, int size = 5)
        {
            var black = new List<Point>();
            var w = bmp.Width;
            var h = bmp.Height;

            using (var data = new LockData(bmp))
            {
                foreach (var y in vertical)
                {
                    foreach (var x in horizontal)
                    {
                        if (Helper.Range(-size, size).All(dx =>
                            Helper.Range(-size, size).All(dy =>
                                x + dx < w && x + dx >= 0 &&
                                y + dy < h && y + dy >= 0 &&
                                data.IsBlackAt(x + dx, y + dy)
                            )))
                        {
                            black.Add(new Point(x, y));
                            // since we move outwards from the cursor, we can safely
                            // break here without risking not to hit the actual item
                            // frame
                            break;
                        }
                    }
                }
            }

            return black;
        }

        public bool FindItem()
        {
            int minWidth = 150, minHeight = 90;

            // first, we have to find the inner item box
            // we do this by moving outwards from the cursor, that way we can
            // be sure to hit the actual item first, instead of a potential
            // equipped item popup
            var vertical = Helper.Range(0, bmp.Height, 5);
            var left = Helper.Range(cursorPos.X, 0, -minWidth);
            var right = Helper.Range(cursorPos.X, bmp.Width, minWidth);

            var black = new List<Point>();
            black.AddRange(FindBlackSquares(bmp, left, vertical));
            black.AddRange(FindBlackSquares(bmp, right, vertical));

            // find all left and right border points
            var frames = black.Select(p => FindBorder(bmp, p))
                .Where(f => f.Width >= minWidth).ToList();
            var leftBorders = frames.Distinct(f => f.Left);
            var rightBorders = frames.Distinct(f => f.Right);

            // from those border points, move outwards to find the outer frame
            var outerPoints = rightBorders.Select(f => FindOuter(bmp, f.Right, f.Top))
                .Concat(leftBorders.Select(f => FindOuter(bmp, f.Left, f.Bottom, -1)));

            var outerFrames = outerPoints
                .Where(p => p.X >= 0)
                .Distinct()
                .Select(p => SelectFrame(bmp, p))
                .Where(f => f.Width >= minWidth && f.Height >= minHeight);

            // the frame closest to the cursor position is (hopefully) the
            // item frame. if the cursor is inside the item frame, we simply
            // take the biggest frame we can find
            var itemFrame = outerFrames.OrderBy(f =>
                cursorPos.X > f.Right ? cursorPos.X - f.Right :
                cursorPos.X < f.Left ? f.Left - cursorPos.X : 0)
                .ThenByDescending(f => f.Width)
                .ThenByDescending(f => f.Height)
                .FirstOrDefault();

            if (itemFrame.Width < minWidth || itemFrame.Height < minHeight)
            {
                this.ItemFrame = new Rectangle();
                return false;
            }

            this.ItemFrame = itemFrame;
            return true;
        }

        public Item ExtractItem(bool withTitle = true)
        {
            if (ItemFrame == new Rectangle() && !this.FindItem())
                return null;

            Bitmap image = new Bitmap(ItemFrame.Width, ItemFrame.Height,
                PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(image))
            {
                var targetFrame = new Rectangle(0, 0, ItemFrame.Width, ItemFrame.Height);
                g.DrawImage(bmp, targetFrame, ItemFrame, GraphicsUnit.Pixel);
            }

            var item = new Item();
            item.Image = image;

            if (withTitle)
            {
                var titleFrame = ExtractTitleFrame(image);
                var title = ExtractItemTitle(titleFrame);
                item.TitleFrame = titleFrame;
                item.Title = title;
            }

            return item;
        }

        public static Bitmap ExtractItemName(Bitmap item)
        {
            return ExtractItemTitle(ExtractTitleFrame(item));
        }

        public static Bitmap ExtractTitleFrame(Bitmap bmp)
        {
            int left, right, top, bottom;
            using (var data = new LockData(bmp))
            {
                var b = data.Height - 1;
                // first, remove the black border to the left and right
                left = Helper.Range(0, data.Width - 1).First(x =>
                    !data.IsColumnBlack(x, 0, b, MaxSkip));
                // for the right border, we can't check from top to bottom because
                // linked items have a non-black [X] at the top right, so we only
                // check the bottom half
                right = Helper.Range(data.Width - 2, 0, -1).First(x =>
                    !data.IsColumnBlack(x, data.Height / 2, b, MaxSkip)) + 1;

                // to separate the title from the actual item, simplify move down
                // from the first non-black row until everything is black again.
                // we don't check the full width to work around the [X] on linked
                // items again. additionally, we skip a few pixels to the left,
                // as there's sometimes some semi-black border left
                top = Helper.Range(0, b).First(y =>
                    data.IsRowNonBlack(y, left + 2, data.Width / 2)) - 1;

                // this is the first black row below the title, so the the title
                // height is given as bottom - top, not bottom - top + 1
                bottom = Helper.Range(top + 1, b).First(y =>
                    data.IsRowBlack(y, left, right));

                // remove any left-over semi-black border columns
                left = Helper.Range(left, data.Width - 1).First(x =>
                    data.IsColumnNonBlack(x, top + 1, bottom - 1));
                right = Helper.Range(right, 0, -1).First(x =>
                    data.IsColumnNonBlack(x, top + 1, bottom - 1)) + 1;
            }

            var width = right - left;
            var height = bottom - top;

            var targetFrame = new Rectangle(left, top, width, height);

            var title = new Bitmap(width, height, PixelFormat.Format24bppRgb);
            using (Graphics g = Graphics.FromImage(title))
            {
                g.DrawImage(bmp, new Rectangle(0, 0, width, height), targetFrame,
                    GraphicsUnit.Pixel);
            }

            return title;
        }

        unsafe private static Bitmap CopyName(Bitmap source, Bitmap mapper, Rectangle srcRect)
        {
            var w = srcRect.Width;
            var h = srcRect.Height;
            var name = new Bitmap(w, h, source.PixelFormat);

            var destRect = new Rectangle(0, 0, w, h);

            using (var dest = new LockData(name, destRect, ImageLockMode.ReadWrite))
            using (var src = new LockData(source, srcRect))
            using (var map = new LockData(mapper, srcRect))
            {
                for (var x = 0; x < w; x++)
                {
                    for (var y = 0; y < h; y++)
                    {
                        if (map.IsBlackAt(x, y))
                            continue;

                        // copy the matching and all neighboring pixels to get
                        // some kind of font anti-aliasing
                        var points = from dx in Helper.Range(-1, 1)
                                     from dy in Helper.Range(-1, 1)
                                     let fy = y + dy
                                     let fx = x + dx
                                     where fy >= 0 && fy < h && fx >= 0 && fx < w
                                     select new { dx, dy };

                        foreach (var d in points)
                        {
                            var dx = x + d.dx;
                            var dy = y + d.dy;
                            dest[dx, dy] = src[dx, dy];
                        }
                    }
                }
            }

            return name;
        }

        private static Bitmap ConvertToGrayscale(Bitmap bmp)
        {
            var width = bmp.Width;
            var height = bmp.Height;
            ColorMatrix grayscale = new ColorMatrix(new float[][]
            {
                new float[] {0.30f, 0.30f, 0.30f, 0, 0},
                new float[] {0.40f, 0.40f, 0.40f, 0, 0},
                new float[] {0.30f, 0.30f, 0.30f, 0, 0},
                new float[] {0, 0, 0, 1, 0},
                new float[] {-0.26f, -0.26f, -0.26f, 0, 1}
            });

            var attribs = new ImageAttributes();
            attribs.SetColorMatrix(grayscale);
            Bitmap img = new Bitmap(width, height, bmp.PixelFormat);
            using (Graphics g = Graphics.FromImage(img))
            {
                var target = new Rectangle(0, 0, width, height);
                g.DrawImage(bmp, target, 0, 0, width, height,
                    GraphicsUnit.Pixel, attribs);
            }

            return img;
        }

        // extracts the actual title from a title frame
        public static Bitmap ExtractItemTitle(Bitmap bmp)
        {
            // first, flood-fill the actual item frame to remove it
            using (var data = new LockData(bmp))
            {
                var w = data.Width;
                var h = data.Height;

                // we start at all border points to make sure we remove the
                // full border, and not just a part of it
                for (var x = 0; x < w; x++)
                {
                    TraverseFill(data, x, 0);
                    TraverseFill(data, x, h - 1);
                }

                for (var y = 0; y < h; y++)
                {
                    TraverseFill(data, 0, y);
                    TraverseFill(data, w - 1, y);
                }
            }

            // then, convert the image to grayscale and reduce the brightness
            // by 0.26 to get rid of the background color gradient
            var gray = ConvertToGrayscale(bmp);

            // we detect the name by simply looking for any non-black pixels
            // that are left after flood-filling the outer frame and removing
            // the name background
            // "inner" refers to the rectangle containing only the item name
            int innerTop, innerLeft, innerBottom, innerRight;
            using (var data = new LockData(gray))
            {
                var w = data.Width;
                var h = data.Height;

                innerTop = Helper.Range(0, h - 1).First(y => !data.IsRowBlack(y));
                innerBottom = Helper.Range(h - 1, 0, -1).First(y => !data.IsRowBlack(y));

                innerLeft = Helper.Range(0, w - 1).First(x =>
                    !data.IsColumnBlack(x, innerTop, innerBottom));
                innerRight = Helper.Range(w - 1, 0, -1).First(x =>
                    !data.IsColumnBlack(x, innerTop, innerBottom));
            }

            var nameFrame = new Rectangle(innerLeft, innerTop,
                innerRight - innerLeft + 1, innerBottom - innerTop + 1);

            // and copy the name with font anti-aliasing
            return CopyName(bmp, gray, nameFrame);
        }

        unsafe private static void TraverseFill(LockData data, int x, int y)
        {
            if (x < 0 || x >= data.Width) return;
            if (y < 0 || y >= data.Height) return;
            if (data.IsBlackAt(x, y)) return;

            data.SetBlack(x, y);

            TraverseFill(data, x - 1, y);
            TraverseFill(data, x + 1, y);
            TraverseFill(data, x, y - 1);
            TraverseFill(data, x, y + 1);
        }
    }
}
