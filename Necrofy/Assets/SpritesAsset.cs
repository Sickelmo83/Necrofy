﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Necrofy
{
    class SpritesAsset : Asset
    {
        private const AssetCategory AssetCat = AssetCategory.Sprites;

        public static void RegisterLoader() {
            AddCreator(new SpritesCreator());
        }

        private readonly SpritesNameInfo nameInfo;
        public readonly Sprite[] sprites;

        public static SpritesAsset FromProject(Project project) {
            return new SpritesCreator().FromProject(project);
        }

        private SpritesAsset(SpritesNameInfo nameInfo, Sprite[] sprites) {
            this.nameInfo = nameInfo;
            this.sprites = sprites;
        }

        public override void WriteFile(Project project) {
            File.WriteAllText(nameInfo.GetFilename(project.path), JsonConvert.SerializeObject(sprites));
        }

        public override void Insert(NStream rom, ROMInfo romInfo, Project project) {
            int i = 0;
            i = Sprite.WriteToROM(sprites, i, rom, ROMPointers.SpriteData1);
            i = Sprite.WriteToROM(sprites, i, rom, ROMPointers.SpriteData2);
        }

        protected override AssetCategory Category => nameInfo.Category;
        protected override string Name => nameInfo.Name;

        class SpritesCreator : Creator
        {
            public SpritesAsset FromProject(Project project) {
                NameInfo nameInfo = new SpritesNameInfo();
                string filename = nameInfo.FindFilename(project.path);
                return (SpritesAsset)FromFile(nameInfo, filename);
            }

            public override NameInfo GetNameInfo(NameInfo.PathParts pathParts, Project project) {
                return SpritesNameInfo.FromPath(pathParts);
            }

            public override Asset FromFile(NameInfo nameInfo, string filename) {
                return new SpritesAsset((SpritesNameInfo)nameInfo, JsonConvert.DeserializeObject<Sprite[]>(File.ReadAllText(filename)));
            }

            public override List<DefaultParams> GetDefaults() {
                return new List<DefaultParams>() {
                    new DefaultParams(0, new SpritesNameInfo()),
                };
            }

            public override Asset FromRom(NameInfo nameInfo, NStream romStream, int? size) {
                List<Sprite> sprites = new List<Sprite>();
                Sprite.AddFromROM(sprites, romStream, ROMPointers.SpriteData1);
                Sprite.AddFromROM(sprites, romStream, ROMPointers.SpriteData2);
                return new SpritesAsset((SpritesNameInfo)nameInfo, sprites.ToArray());
            }

            public override bool AutoTrackFreespace => false;
        }

        class SpritesNameInfo : NameInfo
        {
            private const string Folder = "Tilemaps";
            private const string FileName = "Sprites";
            private const string Extension = "json";

            public SpritesNameInfo() { }

            public override string Name => FileName;
            public override string DisplayName => FileName;
            public override AssetCategory Category => AssetCat;

            protected override PathParts GetPathParts() {
                return new PathParts(Folder, null, FileName, Extension, null);
            }

            public static SpritesNameInfo FromPath(PathParts parts) {
                if (parts.topFolder != Folder) return null;
                if (parts.subFolder != null) return null;
                if (parts.name != FileName) return null;
                if (parts.fileExtension != Extension) return null;
                if (parts.pointer != null) return null;
                return new SpritesNameInfo();
            }
        }
    }
}
