﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace Necrofy
{
    class PaintTileAction : UndoManager.Action
    {
        private readonly List<Point> points = new List<Point>();
        private readonly List<ushort> prevTileType = new List<ushort>();
        private readonly ushort tileType;

        public PaintTileAction(int x, int y, ushort tileType) {
            points.Add(new Point(x, y));
            this.tileType = tileType;
        }

        protected override void AfterSetEditor() {
            prevTileType.Add(level.background[points[0].X, points[0].Y]);
        }

        protected override void Undo() {
            for (int l = 0; l <= points.Count - 1; l++) {
                level.background[points[l].X, points[l].Y] = prevTileType[l];
            }
        }

        protected override void Redo() {
            for (int l = 0; l <= points.Count - 1; l++) {
                level.background[points[l].X, points[l].Y] = tileType;
            }
        }

        public override bool CanMerge => true;

        public override void Merge(UndoManager.Action action) {
            PaintTileAction paintTileAction = (PaintTileAction)action;
            if (!points.Contains(paintTileAction.points[0])) {
                points.Add(paintTileAction.points[0]);
                prevTileType.Add(paintTileAction.prevTileType[0]);
            }
        }

        public override string ToString() {
            return "Paint tiles";
        }
    }

    class FillSelectionAction : UndoManager.Action
    {
        private readonly List<Point> points = new List<Point>();
        private readonly List<ushort> prevTileType = new List<ushort>();
        private ushort tileType;

        public FillSelectionAction(ushort tileType) {
            this.tileType = tileType;
        }

        protected override void AfterSetEditor() {
            for (int y = 0; y < editor.level.Level.height; y++) {
                for (int x = 0; x < editor.level.Level.width; x++) {
                    if (editor.selection.GetPoint(x, y)) {
                        points.Add(new Point(x, y));
                        prevTileType.Add(editor.level.Level.background[x, y]);
                    }
                }
            }
        }

        protected override void Undo() {
            for (int l = 0; l <= points.Count - 1; l++) {
                level.background[points[l].X, points[l].Y] = prevTileType[l];
            }
        }

        protected override void Redo() {
            for (int l = 0; l <= points.Count - 1; l++) {
                level.background[points[l].X, points[l].Y] = tileType;
            }
        }

        public override bool CanMerge => true;

        public override void Merge(UndoManager.Action action) {
            tileType = ((FillSelectionAction)action).tileType;
        }

        public override string ToString() {
            return "Fill Selection";
        }
    }
}
