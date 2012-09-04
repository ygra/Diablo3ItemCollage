using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Imaging;

namespace ItemCollage
{
    public static class Extensions
    {
        /* IEnumerable */
        private static IEnumerable<KeyValuePair<TVal, TMapped>> MapSortBy<TVal, TMapped>(
            this IEnumerable<TVal> source, Func<TVal, TMapped> selector)
        {
            return source.Select(o => new KeyValuePair<TVal, TMapped>(o, selector(o)))
                .OrderBy(a => a.Value);
        }

        public static TVal MaxBy<TVal, TMapped>(this IEnumerable<TVal> source,
            Func<TVal, TMapped> selector)
        {
            return source.MapSortBy(selector).LastOrDefault().Key;
        }

        public static TVal MinBy<TVal, TMapped>(this IEnumerable<TVal> source,
        Func<TVal, TMapped> selector)
        {
            return source.MapSortBy(selector).FirstOrDefault().Key;
        }

        public class FuncEqualityComparer<TSource, TResult> : EqualityComparer<TSource>
        {
            Func<TSource, TResult> func;
            public FuncEqualityComparer(Func<TSource, TResult> func)
            {
                this.func = func;
            }

            public override bool Equals(TSource x, TSource y)
            {
                return func(x).Equals(func(y));
            }
            public override int GetHashCode(TSource obj)
            {
                return func(obj).GetHashCode();
            }
        }

        public static IEnumerable<TSource> Distinct<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> func)
        {
            return source.Distinct(new FuncEqualityComparer<TSource, TResult>(func));
        }

        /* Bitmap */
        public static int BytesPerPixel(this Bitmap b)
        {
            return Image.GetPixelFormatSize(b.PixelFormat) / 8;
        }

        public static bool IsBlackAt(this Bitmap b, int x, int y)
        {
            if (x < 0 || y < 0 || x >= b.Width || y >= b.Height)
                return false;

            Color c = b.GetPixel(x, y);
            return c.R == 0 && c.G == 0 && c.B == 0;
        }

        public static bool IsColumnBlack(this Bitmap b, int x)
        {
            return b.IsColumnBlack(x, 0, b.Height - 1);
        }

        public static bool IsColumnBlack(this Bitmap b, int x, int ystart)
        {
            return b.IsColumnBlack(x, ystart, b.Height - 1);
        }

        public static bool IsColumnBlack(this Bitmap b, int x, int ystart,
            int yend)
        {
            return Helper.Range(ystart, yend).All(y => b.IsBlackAt(x, y));
        }

        public static bool IsRowBlack(this Bitmap b, int y)
        {
            return b.IsRowBlack(y, 0, b.Width - 1);
        }

        public static bool IsRowBlack(this Bitmap b, int y, int xstart)
        {
            return b.IsRowBlack(y, xstart, b.Width - 1);
        }

        public static bool IsRowBlack(this Bitmap b, int y, int xstart,
            int xend)
        {
            return Helper.Range(xstart, xend).All(x => b.IsBlackAt(x, y));
        }

        public static bool IsColumnNonBlack(this Bitmap b, int x)
        {
            return b.IsColumnNonBlack(x, 0, b.Height - 1);
        }

        public static bool IsColumnNonBlack(this Bitmap b, int x, int ystart)
        {
            return b.IsColumnNonBlack(x, ystart, b.Height - 1);
        }

        public static bool IsColumnNonBlack(this Bitmap b, int x, int ystart,
            int yend)
        {
            return Helper.Range(ystart, yend).All(y => !b.IsBlackAt(x, y));
        }

        public static bool IsRowNonBlack(this Bitmap b, int y)
        {
            return b.IsRowNonBlack(y, 0, b.Width - 1);
        }

        public static bool IsRowNonBlack(this Bitmap b, int y, int xstart)
        {
            return b.IsRowNonBlack(y, xstart, b.Width - 1);
        }

        public static bool IsRowNonBlack(this Bitmap b, int y, int xstart,
            int xend)
        {
            return Helper.Range(xstart, xend).All(x => !b.IsBlackAt(x, y));
        }

        /* string */
        public static int ToInt(this string s)
        {
            int x;
            if (int.TryParse(s, out x)) return x;

            return 0;
        }

        /* byte[] */
        public static bool IsBlackAt(this byte[] b, int x, int bytes)
        {
            return Enumerable.Range(0, bytes).All(dx =>
                b[bytes * x + dx] == 0);
        }

        /* IntPtr */
        unsafe public static bool IsBlackAt(this IntPtr b, int x, int bytes)
        {
            return Enumerable.Range(0, bytes).All(dx =>
                ((byte*)b)[bytes * x + dx] == 0);
        }
    }
}
