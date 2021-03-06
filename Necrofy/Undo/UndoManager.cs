﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace Necrofy
{
    class UndoManager<T> : UndoManager
    {
        private readonly T editor;

        private readonly ActionStack undoActions;
        private readonly ActionStack redoActions;
        private bool merge = true;
        private int savePos = 0;

        public UndoManager(ToolStripSplitButton undoButton, ToolStripSplitButton redoButton, T editor) {
            this.editor = editor;
            undoActions = new ActionStack(this, undoButton, action => action.DoUndo());
            redoActions = new ActionStack(this, redoButton, action => action.DoRedo());
        }

        public void Do(UndoAction<T> action, bool performAction = true) {
            action.SetEditor(editor);
            if (action.cancel) {
                return;
            }
            if (performAction) {
                action.DoRedo();
            }
            if (merge && undoActions.Count > 0 && undoActions.Peek().CanMerge && undoActions.Peek().GetType() == action.GetType()) {
                undoActions.Peek().Merge(action);
            } else {
                undoActions.Push(action);
            }
            if (undoActions.Count <= savePos) {
                savePos = -1;
            }
            redoActions.Clear();
            merge = true;
            InvokeDirtyChanged();
        }

        public void Perform(UndoAction<T> action) {
            action.SetEditor(editor);
            if (action.cancel) {
                return;
            }
            action.DoRedo();
        }

        public override bool Dirty {
            get {
                return savePos != undoActions.Count;
            }
        }

        public override void Clean() {
            savePos = undoActions.Count;
            InvokeDirtyChanged();
        }

        public override void ForceDirty() {
            savePos = -1;
            InvokeDirtyChanged();
        }

        public override void ForceNoMerge() {
            merge = false;
        }

        public override void UndoLast() {
            UndoLastAndGet();
            InvokeDirtyChanged();
        }

        private UndoAction<T> UndoLastAndGet() {
            UndoAction<T> action = undoActions.Pop();
            redoActions.Push(action);
            merge = false;
            return action;
        }

        public override void RedoLast() {
            RedoLastAndGet();
            InvokeDirtyChanged();
        }

        private UndoAction<T> RedoLastAndGet() {
            UndoAction<T> action = redoActions.Pop();
            undoActions.Push(action);
            merge = false;
            return action;
        }

        private void UndoOrRedoUpTo(UndoAction<T> action) {
            if (undoActions.Contains(action)) {
                while (UndoLastAndGet() != action) ;
            } else if (redoActions.Contains(action)) {
                while (RedoLastAndGet() != action) ;
            } else {
                throw new Exception("Unknown UndoAction: " + action);
            }
            InvokeDirtyChanged();
        }

        public override void RefreshItems() {
            undoActions.RefreshItems();
            redoActions.RefreshItems();
        }

        private ToolStripMenuItem CreateToolStripItem(UndoAction<T> action) {
            ToolStripMenuItem item = new ToolStripMenuItem(action.ToString());
            item.Click += (sender, e) => {
                UndoOrRedoUpTo(action);
            };
            return item;
        }

        private class ActionStack
        {
            private const int MaxToolStripItems = 20;

            private readonly Stack<UndoAction<T>> actions = new Stack<UndoAction<T>>();
            private readonly UndoManager<T> manager;
            private readonly ToolStripSplitButton button;
            private readonly Action<UndoAction<T>> performAction;

            public ActionStack(UndoManager<T> manager, ToolStripSplitButton button, Action<UndoAction<T>> performAction) {
                this.manager = manager;
                this.button = button;
                this.performAction = performAction;
            }

            public int Count => actions.Count;

            public bool Contains(UndoAction<T> action) {
                return actions.Contains(action);
            }

            public void Push(UndoAction<T> action) {
                actions.Push(action);
                button.DropDownItems.Insert(0, manager.CreateToolStripItem(action));
                if (button.DropDownItems.Count > MaxToolStripItems) {
                    button.DropDownItems.RemoveAt(button.DropDownItems.Count - 1);
                }
                UpdateEnabled();
            }

            public UndoAction<T> Pop() {
                UndoAction<T> action = actions.Pop();
                performAction(action);

                button.DropDownItems.RemoveAt(0);
                if (button.DropDownItems.Count < actions.Count) {
                    button.DropDownItems.Add(manager.CreateToolStripItem(actions.ElementAt(button.DropDownItems.Count)));
                }
                UpdateEnabled();
                return action;
            }

            public UndoAction<T> Peek() {
                return actions.Peek();
            }

            public void Clear() {
                actions.Clear();
                button.DropDownItems.Clear();
                UpdateEnabled();
            }

            public void RefreshItems() {
                button.DropDownItems.Clear();
                foreach (UndoAction<T> action in actions) {
                    button.DropDownItems.Add(manager.CreateToolStripItem(action));
                    if (button.DropDownItems.Count == MaxToolStripItems) {
                        break;
                    }
                }
                UpdateEnabled();
            }

            private void UpdateEnabled() {
                button.Enabled = actions.Count > 0;
            }
        }
    }

    abstract class UndoManager
    {
        public event EventHandler DirtyChanged;

        protected void InvokeDirtyChanged() {
            DirtyChanged?.Invoke(this, EventArgs.Empty);
        }

        public abstract bool Dirty { get; }
        public abstract void Clean();
        public abstract void ForceDirty();
        public abstract void ForceNoMerge();
        public abstract void UndoLast();
        public abstract void RedoLast();
        public abstract void RefreshItems();
    }

    public abstract class UndoAction<T>
    {
        protected T editor;

        public void DoUndo() {
            this.Undo();
            this.AfterAction();
        }
        public void DoRedo() {
            this.Redo();
            this.AfterAction();
        }
        protected virtual void Undo() { }
        protected virtual void Redo() { }
        protected virtual void AfterAction() { }
        public virtual bool CanMerge => false;
        public virtual void Merge(UndoAction<T> action) { }
        public virtual void SetEditor(T editor) {
            this.editor = editor;
        }
        public bool cancel;

        protected void UpdateSelection() {
            // TODO
            //if (EdControl.t != null)
            //    EdControl.t.UpdateSelection();
        }
    }
}
