namespace ItemCollage
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.pnlBottom = new System.Windows.Forms.Panel();
            this.button2 = new System.Windows.Forms.Button();
            this.linkLabel1 = new System.Windows.Forms.LinkLabel();
            this.button1 = new System.Windows.Forms.Button();
            this.lblHelp = new System.Windows.Forms.Label();
            this.pnlOptions = new System.Windows.Forms.Panel();
            this.grpOptions = new System.Windows.Forms.GroupBox();
            this.pnlInnerOptions = new System.Windows.Forms.Panel();
            this.chkTopMost = new System.Windows.Forms.CheckBox();
            this.chkCopyCollages = new System.Windows.Forms.CheckBox();
            this.chkUpdates = new System.Windows.Forms.CheckBox();
            this.chkCopyItems = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.itemListBox1 = new ItemCollage.ItemListBox();
            this.pnlBottom.SuspendLayout();
            this.pnlOptions.SuspendLayout();
            this.grpOptions.SuspendLayout();
            this.pnlInnerOptions.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlBottom
            // 
            this.pnlBottom.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(29)))), ((int)(((byte)(29)))), ((int)(((byte)(29)))));
            this.pnlBottom.Controls.Add(this.button2);
            this.pnlBottom.Controls.Add(this.linkLabel1);
            this.pnlBottom.Controls.Add(this.button1);
            this.pnlBottom.Controls.Add(this.lblHelp);
            this.pnlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
            this.pnlBottom.Location = new System.Drawing.Point(0, 290);
            this.pnlBottom.Name = "pnlBottom";
            this.pnlBottom.Size = new System.Drawing.Size(264, 47);
            this.pnlBottom.TabIndex = 3;
            // 
            // button2
            // 
            this.button2.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.button2.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.button2.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button2.Location = new System.Drawing.Point(3, 20);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(58, 23);
            this.button2.TabIndex = 6;
            this.button2.Text = "Options";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.OptionsButtonClicked);
            // 
            // linkLabel1
            // 
            this.linkLabel1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.linkLabel1.AutoSize = true;
            this.linkLabel1.LinkColor = System.Drawing.Color.FromArgb(((int)(((byte)(128)))), ((int)(((byte)(128)))), ((int)(((byte)(255)))));
            this.linkLabel1.Location = new System.Drawing.Point(211, 3);
            this.linkLabel1.Name = "linkLabel1";
            this.linkLabel1.Size = new System.Drawing.Size(50, 13);
            this.linkLabel1.TabIndex = 5;
            this.linkLabel1.TabStop = true;
            this.linkLabel1.Text = "Clear List";
            this.linkLabel1.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            this.linkLabel1.UseMnemonic = false;
            this.linkLabel1.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.ClearLinkClicked);
            // 
            // button1
            // 
            this.button1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.button1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(64)))), ((int)(((byte)(64)))), ((int)(((byte)(64)))));
            this.button1.Enabled = false;
            this.button1.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Location = new System.Drawing.Point(186, 20);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(75, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "Collage [F2]";
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.CollageButtonClicked);
            // 
            // lblHelp
            // 
            this.lblHelp.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblHelp.AutoSize = true;
            this.lblHelp.ForeColor = System.Drawing.Color.Silver;
            this.lblHelp.Location = new System.Drawing.Point(3, 3);
            this.lblHelp.Name = "lblHelp";
            this.lblHelp.Size = new System.Drawing.Size(175, 13);
            this.lblHelp.TabIndex = 3;
            this.lblHelp.Text = "Grab items with F1, click to remove.";
            this.lblHelp.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.lblHelp.UseMnemonic = false;
            // 
            // pnlOptions
            // 
            this.pnlOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlOptions.Controls.Add(this.grpOptions);
            this.pnlOptions.Location = new System.Drawing.Point(12, 12);
            this.pnlOptions.Name = "pnlOptions";
            this.pnlOptions.Size = new System.Drawing.Size(240, 272);
            this.pnlOptions.TabIndex = 5;
            this.pnlOptions.Visible = false;
            // 
            // grpOptions
            // 
            this.grpOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grpOptions.BackColor = System.Drawing.Color.Black;
            this.grpOptions.Controls.Add(this.pnlInnerOptions);
            this.grpOptions.ForeColor = System.Drawing.Color.White;
            this.grpOptions.Location = new System.Drawing.Point(3, 3);
            this.grpOptions.Name = "grpOptions";
            this.grpOptions.Padding = new System.Windows.Forms.Padding(6, 3, 6, 3);
            this.grpOptions.Size = new System.Drawing.Size(234, 266);
            this.grpOptions.TabIndex = 1;
            this.grpOptions.TabStop = false;
            this.grpOptions.Text = "Options";
            // 
            // pnlInnerOptions
            // 
            this.pnlInnerOptions.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.pnlInnerOptions.AutoScroll = true;
            this.pnlInnerOptions.Controls.Add(this.label1);
            this.pnlInnerOptions.Controls.Add(this.chkTopMost);
            this.pnlInnerOptions.Controls.Add(this.chkCopyCollages);
            this.pnlInnerOptions.Controls.Add(this.chkUpdates);
            this.pnlInnerOptions.Controls.Add(this.chkCopyItems);
            this.pnlInnerOptions.Location = new System.Drawing.Point(9, 19);
            this.pnlInnerOptions.Name = "pnlInnerOptions";
            this.pnlInnerOptions.Size = new System.Drawing.Size(216, 241);
            this.pnlInnerOptions.TabIndex = 5;
            // 
            // chkTopMost
            // 
            this.chkTopMost.AutoSize = true;
            this.chkTopMost.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.chkTopMost.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.chkTopMost.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkTopMost.Location = new System.Drawing.Point(3, 3);
            this.chkTopMost.Name = "chkTopMost";
            this.chkTopMost.Size = new System.Drawing.Size(89, 17);
            this.chkTopMost.TabIndex = 0;
            this.chkTopMost.Text = "Always on top";
            this.chkTopMost.UseVisualStyleBackColor = true;
            // 
            // chkCopyCollages
            // 
            this.chkCopyCollages.AutoSize = true;
            this.chkCopyCollages.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.chkCopyCollages.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.chkCopyCollages.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkCopyCollages.Location = new System.Drawing.Point(21, 102);
            this.chkCopyCollages.Name = "chkCopyCollages";
            this.chkCopyCollages.Size = new System.Drawing.Size(89, 17);
            this.chkCopyCollages.TabIndex = 4;
            this.chkCopyCollages.Text = "Copy collages";
            this.chkCopyCollages.UseVisualStyleBackColor = true;
            // 
            // chkUpdates
            // 
            this.chkUpdates.AutoSize = true;
            this.chkUpdates.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.chkUpdates.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.chkUpdates.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkUpdates.Location = new System.Drawing.Point(3, 26);
            this.chkUpdates.Name = "chkUpdates";
            this.chkUpdates.Size = new System.Drawing.Size(157, 17);
            this.chkUpdates.TabIndex = 1;
            this.chkUpdates.Text = "Check for updates at startup";
            this.chkUpdates.UseVisualStyleBackColor = true;
            // 
            // chkCopyItems
            // 
            this.chkCopyItems.AutoSize = true;
            this.chkCopyItems.FlatAppearance.BorderColor = System.Drawing.Color.Silver;
            this.chkCopyItems.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Silver;
            this.chkCopyItems.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.chkCopyItems.Location = new System.Drawing.Point(21, 79);
            this.chkCopyItems.Name = "chkCopyItems";
            this.chkCopyItems.Size = new System.Drawing.Size(105, 17);
            this.chkCopyItems.TabIndex = 3;
            this.chkCopyItems.Text = "Copy item images";
            this.chkCopyItems.UseVisualStyleBackColor = true;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(3, 57);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(149, 13);
            this.label1.TabIndex = 5;
            this.label1.Text = "Copy images to clipboard";
            // 
            // itemListBox1
            // 
            this.itemListBox1.BackColor = System.Drawing.Color.Black;
            this.itemListBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.itemListBox1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.itemListBox1.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawVariable;
            this.itemListBox1.ForeColor = System.Drawing.Color.White;
            this.itemListBox1.Location = new System.Drawing.Point(0, 0);
            this.itemListBox1.Name = "itemListBox1";
            this.itemListBox1.Size = new System.Drawing.Size(264, 290);
            this.itemListBox1.TabIndex = 4;
            this.itemListBox1.MouseLeave += new System.EventHandler(this.ListMouseLeave);
            this.itemListBox1.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ListMouseOver);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(264, 337);
            this.Controls.Add(this.pnlOptions);
            this.Controls.Add(this.itemListBox1);
            this.Controls.Add(this.pnlBottom);
            this.DoubleBuffered = true;
            this.ForeColor = System.Drawing.Color.White;
            this.MaximizeBox = false;
            this.MinimumSize = new System.Drawing.Size(250, 200);
            this.Name = "Form1";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Item Collage";
            this.TopMost = true;
            this.pnlBottom.ResumeLayout(false);
            this.pnlBottom.PerformLayout();
            this.pnlOptions.ResumeLayout(false);
            this.grpOptions.ResumeLayout(false);
            this.pnlInnerOptions.ResumeLayout(false);
            this.pnlInnerOptions.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel pnlBottom;
        private ItemListBox itemListBox1;
        private System.Windows.Forms.Label lblHelp;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.LinkLabel linkLabel1;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Panel pnlOptions;
        private System.Windows.Forms.CheckBox chkTopMost;
        private System.Windows.Forms.GroupBox grpOptions;
        private System.Windows.Forms.CheckBox chkUpdates;
        private System.Windows.Forms.CheckBox chkCopyCollages;
        private System.Windows.Forms.CheckBox chkCopyItems;
        private System.Windows.Forms.Panel pnlInnerOptions;
        private System.Windows.Forms.Label label1;
    }
}

