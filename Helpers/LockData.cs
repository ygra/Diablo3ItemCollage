using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

namespace ItemCollage
{
    public sealed class LockData : IDisposable
    {
        public BitmapData Data { get; private set; }
        public int Height => Data.Height;
        public int Width => Data.Width;

        private readonly Bitmap bitmap;
        private readonly int bytes;

        public LockData(Bitmap bmp) : this(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height)) { }

        public LockData(Bitmap bmp, Rectangle rect, ImageLockMode mode = ImageLockMode.ReadOnly)
        {
            Data = bmp.LockBits(rect, mode, bmp.PixelFormat);

            bitmap = bmp;
            bytes = bmp.BytesPerPixel();
        }

        public void Dispose() => bitmap.UnlockBits(Data);

        unsafe public byte* this[int x]
        {
            get => (byte*)Data.Scan0 + bytes * x;
            set
            {
                var dest = this[x];
                for (var b = 0; b < bytes; b++)
                    dest[b] = value[b];
            }
        }

        unsafe public byte* this[int x, int y]
        {
            get => (byte*)Row(y) + bytes * x;
            set
            {
                var dest = this[x, y];
                for (var b = 0; b < bytes; b++)
                    dest[b] = value[b];
            }
        }

        unsafe public void SetBlack(int x, int y)
        {
            var dest = this[x, y];
            for (var b = 0; b < bytes; b++) dest[b] = 0;
        }

        public bool IsBlackAt(int x) => Data.Scan0.IsBlackAt(x, bytes);

        public bool IsBlackAt(int x, int y) => Row(y).IsBlackAt(x, bytes);

        unsafe public IntPtr Row(int y) => (IntPtr)((byte*)Data.Scan0 + y * Data.Stride);

        public bool IsRowBlack(int y, int xstart = 0) => IsRowBlack(y, xstart, bitmap.Width - 1);

        public bool IsRowBlack(int y, int xstart, int xend)
        {
            var row = Row(y);
            return Helper.Range(xstart, xend).All(x => row.IsBlackAt(x, bytes));
        }

        public bool IsColumnBlack(int x, int ystart = 0) => IsColumnBlack(x, ystart, Height - 1);

        public bool IsColumnBlack(int x, int ystart, int yend, int maxSkip = 0)
        {
            var skip = 0;
            return Helper.Range(ystart, yend).All(y => Row(y).IsBlackAt(x, bytes) ||
                skip++ < maxSkip);
        }

        public bool IsRowNonBlack(int y, int xstart = 0) => IsRowNonBlack(y, xstart, Width - 1);

        public bool IsRowNonBlack(int y, int xstart, int xend, int maxSkip = 0)
        {
            var skip = 0;
            var row = Row(y);
            return Helper.Range(xstart, xend).All(x => !row.IsBlackAt(x, bytes) ||
                skip++ < maxSkip);
        }

        public bool IsColumnNonBlack(int x, int ystart = 0) => IsColumnNonBlack(x, ystart, Height - 1);

        public bool IsColumnNonBlack(int x, int ystart, int yend) => Helper.Range(ystart, yend).All(y => !Row(y).IsBlackAt(x, bytes));
    }
}