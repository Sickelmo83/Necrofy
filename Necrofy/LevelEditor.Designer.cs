﻿namespace Necrofy
{
    partial class LevelEditor
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.canvas = new Necrofy.Canvas();
            this.hscroll = new System.Windows.Forms.HScrollBar();
            this.vscroll = new System.Windows.Forms.VScrollBar();
            this.SuspendLayout();
            // 
            // canvas
            // 
            this.canvas.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.canvas.BackColor = System.Drawing.SystemColors.ControlDarkDark;
            this.canvas.Location = new System.Drawing.Point(0, 0);
            this.canvas.Name = "canvas";
            this.canvas.Size = new System.Drawing.Size(283, 283);
            this.canvas.TabIndex = 0;
            this.canvas.Paint += new System.Windows.Forms.PaintEventHandler(this.canvas_Paint);
            // 
            // hscroll
            // 
            this.hscroll.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.hscroll.LargeChange = 100;
            this.hscroll.Location = new System.Drawing.Point(0, 283);
            this.hscroll.Name = "hscroll";
            this.hscroll.Size = new System.Drawing.Size(283, 17);
            this.hscroll.SmallChange = 16;
            this.hscroll.TabIndex = 1;
            // 
            // vscroll
            // 
            this.vscroll.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.vscroll.LargeChange = 100;
            this.vscroll.Location = new System.Drawing.Point(283, 0);
            this.vscroll.Name = "vscroll";
            this.vscroll.Size = new System.Drawing.Size(17, 283);
            this.vscroll.SmallChange = 16;
            this.vscroll.TabIndex = 2;
            // 
            // LevelEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(300, 300);
            this.Controls.Add(this.vscroll);
            this.Controls.Add(this.hscroll);
            this.Controls.Add(this.canvas);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "LevelEditor";
            this.Text = "LevelEditor";
            this.ResumeLayout(false);

        }

        #endregion

        private Canvas canvas;
        private System.Windows.Forms.HScrollBar hscroll;
        private System.Windows.Forms.VScrollBar vscroll;

    }
}