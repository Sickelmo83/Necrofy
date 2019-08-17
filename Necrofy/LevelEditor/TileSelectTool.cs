﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Necrofy
{
    class TileSelectTool : Tool
    {
        private int prevX;
        private int prevY;
        private bool ignoreTileChange = false;

        public TileSelectTool(LevelEditor editor) : base(editor) { }

        public override ObjectType objectType => ObjectType.Tiles;

        public override void MouseDown(LevelMouseEventArgs e) {
            prevX = -1;
            prevY = -1;
            MouseMove(e);
            editor.undoManager.ForceNoMerge();
        }

        public override void MouseMove(LevelMouseEventArgs e) {
            if ((e.TileX != prevX || e.TileY != prevY) && e.InBounds) {
                ushort tileType = editor.level.Level.background[e.TileX, e.TileY];
                editor.selection.SetAllPoints((x, y) => editor.level.Level.background[x, y] == tileType);

                ignoreTileChange = true;
                editor.tilesetObjectBrowserContents.SelectedIndex = tileType;
                ignoreTileChange = false;
                editor.ScrollObjectBrowserToSelection();

                prevX = e.TileX;
                prevY = e.TileY;
            }
        }

        public override void TileChanged() {
            if (!ignoreTileChange) {
                editor.FillSelection();
            }
        }
    }
}
