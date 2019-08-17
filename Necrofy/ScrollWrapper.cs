﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Necrofy
{
    class ScrollWrapper
    {
        private readonly Control control;
        private readonly HScrollBar hscroll;
        private readonly VScrollBar vscroll;
        
        public event EventHandler Scrolled;

        public int LeftPosition { get; private set; }
        public int TopPosition { get; private set; }

        private int clientWidth;
        private int clientHeight;

        private int dragStartX;
        private int dragStartY;

        public ScrollWrapper(Control control, HScrollBar hscroll, VScrollBar vscroll, bool autoSize = true) {
            this.control = control;
            this.hscroll = hscroll;
            this.vscroll = vscroll;

            if (autoSize) {
                control.SizeChanged += UpdateSize;
            }
            control.MouseDown += control_MouseDown;
            control.MouseMove += control_MouseMove;
            control.MouseWheel += control_MouseWheel;
            hscroll.ValueChanged += UpdatePosition;
            vscroll.ValueChanged += UpdatePosition;
        }

        public void SetClientSize(int width, int height) {
            clientWidth = width;
            clientHeight = height;
            TopPosition = 0;
            LeftPosition = 0;
            UpdateSize();
        }

        public void ScrollToPoint(int x, int y) {
            if (hscroll.Enabled) {
                SetScrollBarValue(hscroll, x - control.Width / 2);
            }
            if (vscroll.Enabled) {
                SetScrollBarValue(vscroll, y - control.Height / 2);
            }
        }

        private void SetScrollBarValue(ScrollBar bar, int value) {
            bar.Value = Math.Max(bar.Minimum, Math.Min(bar.Maximum - bar.LargeChange + 1, value));
        }

        private void UpdateSize(object sender, EventArgs e) {
            UpdateSize();
        }

        private void UpdateSize() {
            if (clientWidth <= control.Width) {
                hscroll.Enabled = false;
                LeftPosition = (control.Width - clientWidth) / 2;
            } else {
                hscroll.Enabled = true;
                hscroll.Minimum = 0;
                hscroll.Maximum = clientWidth;
                hscroll.LargeChange = control.Width;
                SetScrollBarValue(hscroll, hscroll.Value);
                LeftPosition = -hscroll.Value;
            }

            if (clientHeight <= control.Height) {
                vscroll.Enabled = false;
                TopPosition = (control.Height - clientHeight) / 2;
            } else {
                vscroll.Enabled = true;
                vscroll.Minimum = 0;
                vscroll.Maximum = clientHeight;
                vscroll.LargeChange = control.Height;
                SetScrollBarValue(vscroll, vscroll.Value);
                TopPosition = -vscroll.Value;
            }
            
            Scrolled?.Invoke(this, EventArgs.Empty);
        }

        void UpdatePosition(object sender, EventArgs e) {
            UpdatePosition();
        }

        private void UpdatePosition() {
            if (vscroll.Enabled) {
                TopPosition = -vscroll.Value;
            }
            if (hscroll.Enabled) {
                LeftPosition = -hscroll.Value;
            }
            
            Scrolled?.Invoke(this, EventArgs.Empty);
        }

        void control_MouseDown(object sender, MouseEventArgs e) {
            control.Focus();
            if (e.Button == MouseButtons.Middle) {
                dragStartX = hscroll.Value + e.X;
                dragStartY = vscroll.Value + e.Y;
            }
        }

        void control_MouseMove(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Middle) {
                SetScrollBarValue(hscroll, dragStartX - e.X);
                SetScrollBarValue(vscroll, dragStartY - e.Y);
            }
        }

        void control_MouseWheel(object sender, MouseEventArgs e) {
            if (vscroll.Enabled) {
                SetScrollBarValue(vscroll, vscroll.Value - 64 * Math.Sign(e.Delta));
            } else if (hscroll.Enabled) {
                SetScrollBarValue(hscroll, hscroll.Value - 64 * Math.Sign(e.Delta));
            }
        }
    }
}
