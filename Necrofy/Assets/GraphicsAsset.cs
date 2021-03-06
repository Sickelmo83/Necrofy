﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Necrofy
{
    class GraphicsAsset : Asset
    {
        private const AssetCategory AssetCat = AssetCategory.Graphics;

        public const string SpritesName = "Sprites";

        public static void RegisterLoader() {
            AddCreator(new GraphicsCreator());
        }
        
        public static string GetAssetName(NStream romStream, ROMInfo romInfo, int pointer) {
            return GetAssetName(romStream, romInfo, pointer, new GraphicsCreator(), AssetCat);
        }

        private readonly GraphicsNameInfo nameInfo;
        public readonly byte[] data;

        public static GraphicsAsset FromProject(Project project, string graphicsName) {
            return new GraphicsCreator().FromProject(project, graphicsName);
        }

        private GraphicsAsset(GraphicsNameInfo nameInfo, byte[] data) {
            this.nameInfo = nameInfo;
            this.data = data;
        }

        public override void WriteFile(Project project) {
            File.WriteAllBytes(nameInfo.GetFilename(project.path), data);
        }

        public override void ReserveSpace(Freespace freespace) {
            ReserveSpace(freespace, nameInfo.pointer, data.Length);
        }

        public override void Insert(NStream rom, ROMInfo romInfo, Project project) {
            InsertByteArray(rom, romInfo, data, nameInfo.pointer);
        }

        protected override AssetCategory Category => nameInfo.Category;
        protected override string Name => nameInfo.Name;

        class GraphicsCreator : Creator
        {
            public GraphicsAsset FromProject(Project project, string graphicsName) {
                NameInfo nameInfo = new GraphicsNameInfo(graphicsName, null);
                string filename = nameInfo.FindFilename(project.path);
                return (GraphicsAsset)FromFile(nameInfo, filename);
            }

            public override NameInfo GetNameInfo(NameInfo.PathParts pathParts, Project project) {
                return GraphicsNameInfo.FromPath(pathParts);
            }

            public override Asset FromFile(NameInfo nameInfo, string filename) {
                return new GraphicsAsset((GraphicsNameInfo)nameInfo, File.ReadAllBytes(filename));
            }

            public override List<DefaultParams> GetDefaults() {
                return new List<DefaultParams>() {
                    new DefaultParams(0x20000, new GraphicsNameInfo(SpritesName, 0x20000), 0x5D300)
                };
            }

            public override Asset FromRom(NameInfo nameInfo, NStream romStream, int? size) {
                return new GraphicsAsset((GraphicsNameInfo)nameInfo, romStream.ReadBytes((int)size));
            }

            public override NameInfo GetNameInfoForName(string name) {
                return new GraphicsNameInfo(name, null);
            }
        }

        class GraphicsNameInfo : NameInfo
        {
            private const string Folder = "Graphics";
            private const string Extension = "bin";

            public readonly string name;
            public readonly int? pointer;

            public GraphicsNameInfo(string name, int? pointer) {
                this.name = name;
                this.pointer = pointer;
            }

            public override string Name => name;
            public override string DisplayName => name;
            public override AssetCategory Category => AssetCat;

            protected override PathParts GetPathParts() {
                return new PathParts(Folder, null, name, Extension, pointer);
            }

            public static GraphicsNameInfo FromPath(PathParts parts) {
                if (parts.topFolder != Folder) return null;
                if (parts.subFolder != null) return null;
                if (parts.fileExtension != Extension) return null;
                return new GraphicsNameInfo(parts.name, parts.pointer);
            }
        }
    }
}
