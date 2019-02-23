﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using WeifenLuo.WinFormsUI.Docking;
using System.Diagnostics;
using System.Windows.Forms.Layout;

namespace Necrofy
{
    partial class LevelEditor : EditorWindow
    {
        private const int LevelPadding = 64;

        private readonly LoadedLevel level;
        private readonly ScrollWrapper scrollWrapper;

        private readonly TilesetObjectBrowserContents tilesetObjectBrowserContents;
        private readonly SpriteObjectBrowserContents spriteObjectBrowserContents;

        public LevelEditor(LoadedLevel level) {
            InitializeComponent();

            this.level = level;
            this.Text = level.levelAsset.GetDisplayText();

            scrollWrapper = new ScrollWrapper(canvas, hscroll, vscroll);
            scrollWrapper.SetClientSize(level.Level.width * 64 + LevelPadding * 2, level.Level.height * 64 + LevelPadding * 2);
            scrollWrapper.Scrolled += scrollWrapper_Scrolled;

            tilesetObjectBrowserContents = new TilesetObjectBrowserContents(level);
            spriteObjectBrowserContents = new SpriteObjectBrowserContents(level.spriteGraphics);
            spriteObjectBrowserContents.AddCategory(SpriteDisplay.Category.Item);
            spriteObjectBrowserContents.AddCategory(SpriteDisplay.Category.Victim);
            spriteObjectBrowserContents.AddCategory(SpriteDisplay.Category.OneTimeMonster);
            spriteObjectBrowserContents.AddCategory(SpriteDisplay.Category.Monster);

            Repaint();
        }
        
        protected override void Displayed() {
            mainWindow.ObjectBrowser.Browser.Contents = spriteObjectBrowserContents;
            mainWindow.ObjectBrowser.Activate();
        }

        void scrollWrapper_Scrolled() {
            Repaint();
        }

        public void Repaint() {
            canvas.Invalidate();
        }

        private void canvas_Paint(object sender, PaintEventArgs e) {
            if (level == null) {
                return;
            }
            e.Graphics.TranslateTransform(scrollWrapper.LeftPosition + LevelPadding, scrollWrapper.TopPosition + LevelPadding);

            for (int y = 0; y < level.Level.height; y++) {
                for (int x = 0; x < level.Level.width; x++) {
                    e.Graphics.DrawImage(level.tiles[level.Level.background[x, y]], x * 64, y * 64);
                }
            }

            level.spriteGraphics.Render(SpriteDisplay.Key.Type.Player, 0, e.Graphics, level.Level.p1startX, level.Level.p1startY);
            level.spriteGraphics.Render(SpriteDisplay.Key.Type.Player, 1, e.Graphics, level.Level.p2startX, level.Level.p2startY);

            foreach (OneTimeMonster m in level.Level.oneTimeMonsters) {
                if (m.type == OneTimeMonster.CreditHeadType) {
                    level.spriteGraphics.Render(SpriteDisplay.Key.Type.CreditHead, m.extra, e.Graphics, m.x, m.y);
                } else {
                    level.spriteGraphics.Render(SpriteDisplay.Key.Type.Pointer, m.type, e.Graphics, m.x, m.y);
                }
            }
            foreach (Monster m in level.Level.monsters) {
                level.spriteGraphics.Render(SpriteDisplay.Key.Type.Pointer, m.type, e.Graphics, m.x, m.y);
            }
            foreach (Item i in level.Level.items) {
                level.spriteGraphics.Render(SpriteDisplay.Key.Type.Item, i.type, e.Graphics, i.x, i.y);
            }

            //for (int y = 0; y < level.Level.height; y++) {
            //    for (int x = 0; x < level.Level.width; x++) {
            //        e.Graphics.DrawImage(level.priorityTiles[level.Level.background[x, y]], x * 64, y * 64);
            //    }
            //}
        }

        private void UpdateSpriteCategory(SpriteDisplay.Category category, bool enabled) {
            if (enabled) {
                spriteObjectBrowserContents.AddCategory(category);
            } else {
                spriteObjectBrowserContents.RemoveCategory(category);
            }
        }

        private void spritesItems_CheckedChanged(object sender, EventArgs e) {
            UpdateSpriteCategory(SpriteDisplay.Category.Item, spritesItems.Checked);
        }

        private void spritesVictims_CheckedChanged(object sender, EventArgs e) {
            UpdateSpriteCategory(SpriteDisplay.Category.Victim, spritesVictims.Checked);
        }

        private void spritesOneShotMonsters_CheckedChanged(object sender, EventArgs e) {
            UpdateSpriteCategory(SpriteDisplay.Category.OneTimeMonster, spritesOneShotMonsters.Checked);
        }

        private void spritesMonsters_CheckedChanged(object sender, EventArgs e) {
            UpdateSpriteCategory(SpriteDisplay.Category.Monster, spritesMonsters.Checked);
        }

        private void spritesBossMonsters_CheckedChanged(object sender, EventArgs e) {
            // TODO
        }

        private void spritesAll_Click(object sender, EventArgs e) {
            spritesItems.Checked = true;
            spritesVictims.Checked = true;
            spritesOneShotMonsters.Checked = true;
            spritesMonsters.Checked = true;
            spritesBossMonsters.Checked = true;
        }
    }
}
