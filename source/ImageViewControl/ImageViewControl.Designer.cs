namespace ImageViewControl
{
    partial class ImageViewControl
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

        #region Component Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.mHorizScrollBar = new System.Windows.Forms.HScrollBar();
            this.mVertScrollBar = new System.Windows.Forms.VScrollBar();
            this.SuspendLayout();
            // 
            // mHorizScrollBar
            // 
            this.mHorizScrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mHorizScrollBar.Location = new System.Drawing.Point(0, 495);
            this.mHorizScrollBar.Name = "mHorizScrollBar";
            this.mHorizScrollBar.Size = new System.Drawing.Size(491, 17);
            this.mHorizScrollBar.TabIndex = 0;
            this.mHorizScrollBar.Visible = false;
            this.mHorizScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.mScrollBars_Scroll);
            // 
            // mVertScrollBar
            // 
            this.mVertScrollBar.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.mVertScrollBar.Location = new System.Drawing.Point(495, 0);
            this.mVertScrollBar.Name = "mVertScrollBar";
            this.mVertScrollBar.Size = new System.Drawing.Size(17, 495);
            this.mVertScrollBar.TabIndex = 1;
            this.mVertScrollBar.Visible = false;
            this.mVertScrollBar.Scroll += new System.Windows.Forms.ScrollEventHandler(this.mScrollBars_Scroll);
            // 
            // ImageViewControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.mVertScrollBar);
            this.Controls.Add(this.mHorizScrollBar);
            this.Name = "ImageViewControl";
            this.Size = new System.Drawing.Size(512, 512);
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.ImageViewControl_Paint);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ImageViewControl_MouseDown);
            this.MouseMove += new System.Windows.Forms.MouseEventHandler(this.ImageViewControl_MouseMove);
            this.MouseUp += new System.Windows.Forms.MouseEventHandler(this.ImageViewControl_MouseUp);
            this.Resize += new System.EventHandler(this.ImageViewControl_Resize);
            this.ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.HScrollBar mHorizScrollBar;
        private System.Windows.Forms.VScrollBar mVertScrollBar;
    }
}
