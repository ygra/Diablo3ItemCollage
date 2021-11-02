using System.Windows.Forms;
using System.Drawing;

namespace ItemCollage
{
    public class ImageTooltip : Form
    {
        Image image = null;

        public ImageTooltip()
            : base()
        {
            FormBorderStyle = FormBorderStyle.None;
            TopMost = true;
            ShowInTaskbar = false;
        }

        protected override bool ShowWithoutActivation => true;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (image != null)
            {
                e.Graphics.DrawImageUnscaled(image, 0, 0);
            }
        }

        public Image Image
        {
            get => image;
            set
            {
                if (image == value) return;

                image = value;
                this.Size = image != null ? image.Size : new Size();
                this.Invalidate();
            }
        }
    }
}
