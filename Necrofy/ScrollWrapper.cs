﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Necrofy
{
    class ScrollWrapper
    {
        private readonly Control control;
        
        public event EventHandler Scrolled;

        private readonly Dimension xDimension;
        private readonly Dimension yDimension;

        public int LeftPosition {
            get {
                return xDimension.Position;
            }
        }
        public int TopPosition {
            get {
                return yDimension.Position;
            }
        }
        
        public bool ExpandingDrag { get; set; }
        private readonly Timer dragTimer = new Timer() {
            Interval = 7,
        };

        public ScrollWrapper(Control control, HScrollBar hscroll, VScrollBar vscroll, bool autoSize = true) {
            this.control = control;

            xDimension = new Dimension(hscroll, () => control.Width);
            yDimension = new Dimension(vscroll, () => control.Height);
            xDimension.Scrolled += Dimension_Scrolled;
            yDimension.Scrolled += Dimension_Scrolled;

            if (autoSize) {
                control.SizeChanged += Control_SizeChanged;
            }
            control.MouseDown += Control_MouseDown;
            control.MouseMove += Control_MouseMove;
            control.MouseUp += Control_MouseUp;
            control.MouseWheel += Control_MouseWheel;
            dragTimer.Tick += DragTimer_Tick;
        }

        private void Dimension_Scrolled(object sender, EventArgs e) {
            Scrolled?.Invoke(this, e);
        }

        private class Dimension
        {
            private readonly ScrollBar scrollBar;
            private readonly Func<int> controlSize;

            private int clientSize;
            private int clientPosition;
            private int dragStart;

            public int Position { get; private set; }

            public event EventHandler Scrolled;

            public Dimension(ScrollBar scrollBar, Func<int> controlSize) {
                this.scrollBar = scrollBar;
                this.controlSize = controlSize;
                scrollBar.ValueChanged += ScrollBar_ValueChanged;
            }

            private void ScrollBar_ValueChanged(object sender, EventArgs e) {
                UpdatePosition();
            }

            public void SetClientSize(int size) {
                clientSize = size - 1;
                UpdateSize();
            }

            public void ScrollToPoint(int point) {
                if (scrollBar.Enabled) {
                    SetScrollBarValue(point - controlSize() / 2);
                }
            }

            private void SetScrollBarValue(int value) {
                scrollBar.Value = Math.Max(scrollBar.Minimum, Math.Min(scrollBar.GetMaximumValue(), value));
            }

            public void UpdateSize() {
                clientPosition = Math.Max(0, (controlSize() - clientSize) / 2);

                scrollBar.Minimum = 0;
                scrollBar.Maximum = Math.Max(controlSize() - 1, clientSize);
                scrollBar.LargeChange = controlSize();
                scrollBar.Enabled = clientSize > controlSize();
                SetScrollBarValue(scrollBar.Value);

                UpdatePosition();
            }

            public void UpdatePosition() {
                Position = -scrollBar.Value + clientPosition;
                Scrolled?.Invoke(this, EventArgs.Empty);
            }

            public void MouseDown(int position, MouseButtons button) {
                if (button == MouseButtons.Middle) {
                    dragStart = scrollBar.Value + position;
                }
            }

            /// <summary>Process mouse movement</summary>
            /// <param name="position">The mouse position in this dimension</param>
            /// <param name="button">The mouse button</param>
            /// <returns>Whether this dimension requires a scrolling drag</returns>
            public bool MouseMove(int position, MouseButtons button) {
                if (button == MouseButtons.Middle) {
                    SetScrollBarValue(dragStart - position);
                } else if (button == MouseButtons.Left && (position < 0 || position > controlSize())) {
                    return true;
                }
                return false;
            }

            public void MouseUp(int position, MouseButtons button) {
                
            }

            /// <summary>Process a scrolling drag frame</summary>
            /// <param name="position">The mouse position in this dimension</param>
            /// <returns>Whether this dimension is still in scrolling drag</returns>
            public bool ScrollingDrag(int position, bool expanding) {
                int delta = 0;

                if (position < 0) {
                    delta = -1;
                } else if (position > controlSize()) {
                    delta = 1;
                }

                if (delta != 0) {
                    int newValue = scrollBar.Value + delta * 3;
                    if (expanding) {
                        if (newValue < scrollBar.Minimum) {
                            scrollBar.Minimum = newValue;
                            scrollBar.Enabled = true;
                        } else if (newValue > scrollBar.GetMaximumValue()) {
                            scrollBar.Maximum += newValue - scrollBar.GetMaximumValue();
                            scrollBar.Enabled = true;
                        }
                        UpdatePosition();
                    }
                    SetScrollBarValue(newValue);
                    return true;
                }
                return false;
            }

            /// <summary>Process a mouse wheel</summary>
            /// <param name="delta">The wheel delta</param>
            /// <returns>Whether the wheel event was used</returns>
            public bool MouseWheel(int delta) {
                if (scrollBar.Enabled) {
                    SetScrollBarValue(scrollBar.Value - 64 * Math.Sign(delta));
                    return true;
                }
                return false;
            }
        }

        public void SetClientSize(int width, int height) {
            xDimension.SetClientSize(width);
            yDimension.SetClientSize(height);
        }

        public void ScrollToPoint(int x, int y) {
            xDimension.ScrollToPoint(x);
            yDimension.ScrollToPoint(y);
        }

        private void Control_SizeChanged(object sender, EventArgs e) {
            xDimension.UpdateSize();
            yDimension.UpdateSize();
        }
        
        void Control_MouseDown(object sender, MouseEventArgs e) {
            control.Focus();
            xDimension.MouseDown(e.X, e.Button);
            yDimension.MouseDown(e.Y, e.Button);
        }

        void Control_MouseMove(object sender, MouseEventArgs e) {
            bool scrollingDrag = false;
            scrollingDrag |= xDimension.MouseMove(e.X, e.Button);
            scrollingDrag |= yDimension.MouseMove(e.Y, e.Button);
            if (scrollingDrag) {
                dragTimer.Start();
            }
        }

        private void Control_MouseUp(object sender, MouseEventArgs e) {
            xDimension.MouseUp(e.X, e.Button);
            yDimension.MouseUp(e.Y, e.Button);
            dragTimer.Stop();
        }

        private void DragTimer_Tick(object sender, EventArgs e) {
            Point mousePos = control.PointToClient(Control.MousePosition);
            bool scrollingDrag = false;
            scrollingDrag |= xDimension.ScrollingDrag(mousePos.X, ExpandingDrag);
            scrollingDrag |= yDimension.ScrollingDrag(mousePos.Y, ExpandingDrag);
            if (!scrollingDrag) {
                dragTimer.Stop();
            }
        }

        void Control_MouseWheel(object sender, MouseEventArgs e) {
            if (!yDimension.MouseWheel(e.Delta)) {
                xDimension.MouseWheel(e.Delta);
            }
        }
    }
}
