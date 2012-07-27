using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace ItemCollage
{
    public partial class Form2 : Form
    {
        public Form2(Image img)
        {
            InitializeComponent();
            pictureBox1.Image = img;
            //this.Size = pictureBox1.Size;
            //this.StartPosition = FormStartPosition.Manual;
            //var w = Screen.PrimaryScreen.WorkingArea;
            //this.Left = w.Width - this.Width;
            //this.Top = w.Height - this.Height;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Opacity < .05)
            {
                timer1.Enabled = false;
                this.Close();
            }
            Opacity -= .05;
        }

        private void Form2_Shown(object sender, EventArgs e)
        {
            timer1.Enabled = true;
        }
    }
}
