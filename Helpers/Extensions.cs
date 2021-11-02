using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;

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
            readonly Func<TSource, TResult> func;
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
            {
                return false;
            }

            Color c = b.GetPixel(x, y);
            return c.R == 0 && c.G == 0 && c.B == 0;
        }

        /* string */
        public static int ToInt(this string s)
        {
            return int.TryParse(s, out int x) ? x : 0;
        }

        /* IntPtr */
        unsafe public static bool IsBlackAt(this IntPtr b, int x, int bytes)
        {
            for (var dx = 0; dx < bytes; dx++)
            {
                if (((byte*)b)[bytes * x + dx] != 0)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
