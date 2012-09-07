using System;
using System.Drawing;
using System.Drawing.Imaging;

namespace ItemCollage
{
    public sealed class LockData : IDisposable
    {
        public BitmapData Data { get; private set; }
        public int Height { get { return Data.Height; } }
        public int Width { get { return Data.Width; } }

        private Bitmap bitmap;
        private int bytes;

        public LockData(Bitmap bmp) : this(bmp, new Rectangle(0, 0, bmp.Width, bmp.Height)) { }

        public LockData(Bitmap bmp, Rectangle rect, ImageLockMode mode = ImageLockMode.ReadOnly)
        {
            Data = bmp.LockBits(rect, mode, bmp.PixelFormat);

            bitmap = bmp;
            bytes = bmp.BytesPerPixel();
        }

        public void Dispose()
        {
            bitmap.UnlockBits(Data);
        }

        unsafe public byte* this[int x]
        {
            get { return (byte*)Data.Scan0 + bytes * x; }
            set
            {
                var dest = this[x];
                for (var b = 0; b < bytes; b++)
                    dest[b] = value[b];
            }
        }

        unsafe public byte* this[int x, int y]
        {
            get { return (byte*)Row(y) + bytes * x; }
            set
            {
                var dest = this[x, y];
                for (var b = 0; b < bytes; b++)
                    dest[b] = value[b];
            }
        }

        public bool IsBlackAt(int x)
        {
            return Data.Scan0.IsBlackAt(x, bytes);
        }

        public bool IsBlackAt(int x, int y)
        {
            return Row(y).IsBlackAt(x, bytes);
        }

        unsafe public IntPtr Row(int y)
        {
            return (IntPtr)((byte*)Data.Scan0 + y * Data.Stride);
        }
    }
}