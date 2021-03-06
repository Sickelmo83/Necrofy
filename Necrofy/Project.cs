﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;

namespace Necrofy
{
    /// <summary>Manages aspects of a ROM hack project including creation, modification, and building.</summary>
    public class Project
    {
        public const string baseROMFilename = "base.sfc";
        public const string buildFilename = "build.sfc";
        public const string runFromLevelFilename = "runFromLevel.sfc";
        private const string internalProjectFilesFolder = "ProjectFiles";
        public static readonly string internalProjectFilesPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, internalProjectFilesFolder);

        public readonly string path;
        public readonly string settingsFilename;
        public string SettingsPath => Path.Combine(path, settingsFilename);
        public readonly ProjectSettings settings;

        /// <summary>Creates a new project from the given base ROM.</summary>
        /// <param name="baseROM">The path to a ROM that the files in the project will be extracted from.</param>
        /// <param name="path">The path to a directory in which all of the project files will be placed.</param>
        public Project(string baseROM, string path) {
            this.path = FixPath(path);

            if (Directory.Exists(path)) {
                Directory.Delete(path, true); // TODO: Warn about this
            }
            Directory.CreateDirectory(path);
            string newBaseROM = Path.Combine(path, baseROMFilename);
            File.Copy(baseROM, newBaseROM);
            // TODO: Remove header if it exists

            NStream s = new NStream(new FileStream(newBaseROM, FileMode.Open, FileAccess.Read, FileShare.Read));
            ROMInfo info = new ROMInfo(s);

            foreach (Asset asset in info.assets) {
                asset.WriteFile(this);
            }

            s.Close();
            settingsFilename = "project.nfyp";
            settings = new ProjectSettings();
            WriteSettings();
        }

        /// <summary>Loads an existing project from the given settings file.</summary>
        /// <param name="path">The filename of the settings file.</param>
        public Project(string settingsFile) {
            path = FixPath(Path.GetDirectoryName(settingsFile));
            settingsFilename = Path.GetFileName(settingsFile);
            settings = JsonConvert.DeserializeObject<ProjectSettings>(File.ReadAllText(settingsFile));
        }

        private static string FixPath(string path) {
            if (!path.EndsWith(Path.DirectorySeparatorChar.ToString())) {
                path += Path.DirectorySeparatorChar;
            }
            return path;
        }

        private void WriteSettings() {
            File.WriteAllText(SettingsPath, JsonConvert.SerializeObject(settings));
        }

        public string GetRelativePath(string filename) {
            Debug.Assert(filename.StartsWith(path));
            return filename.Substring(path.Length);
        }

        /// <summary>Builds the project</summary>
        public void Build() {
            string outputROM = Path.Combine(path, buildFilename);
            File.Copy(Path.Combine(path, baseROMFilename), outputROM, true);

            NStream s = new NStream(new FileStream(outputROM, FileMode.Open, FileAccess.ReadWrite, FileShare.Read));
            ROMInfo info = new ROMInfo(s);
            info.assets.Clear();
            AddEndOfBankFreespace(s, info.Freespace);

            foreach (string filename in Directory.GetFiles(path, "*", SearchOption.AllDirectories)) {
                string relativeFilename = GetRelativePath(filename);
                Asset asset = Asset.FromFile(this, relativeFilename);
                if (asset == null) {
                    // TODO: some sort of error
                    Debug.WriteLine("No asset handled for filename " + relativeFilename);
                    continue;
                }
                info.assets.Add(asset);
            }

            info.assets.Sort();
            foreach (Asset asset in info.assets) {
                asset.ReserveSpace(info.Freespace);
            }
            foreach (Asset asset in info.assets) {
                asset.Insert(s, info, this);
            }

            // Round size up to the nearest bank
            // TODO: Check if everything still works if more than one bank of extra stuff has been added
            s.SetLength((long)Math.Ceiling(s.Length / (double)Freespace.BankSize) * Freespace.BankSize);
            byte sizeValue = (byte)(Math.Ceiling(Math.Log(s.Length, 2)) - 10);
            s.Seek(ROMPointers.ROMSize, SeekOrigin.Begin);
            s.WriteByte(sizeValue);

            info.Freespace.Fill(s, 0xFF);
            s.Close();
            
            Patch(outputROM, "ROMExpand.asm");
        }

        public void Run() {
            // TODO: Don't build if not necessary
            Build();
            Process.Start(Path.Combine(path, buildFilename));
        }

        public void RunFromLevel(int level, RunSettings settings) {
            // TODO: Don't build if not necessary
            Build();
            string runROM = Path.Combine(path, runFromLevelFilename);
            File.Copy(Path.Combine(path, buildFilename), runROM, true);

            Dictionary<string, string> defines = new Dictionary<string, string> {
                ["LEVEL"] = level.ToString(),
                ["VICTIMS"] = "$" + settings.victimCount.ToString(),
                ["WEAPON_COUNT"] = settings.weaponAmounts.Length.ToString(),
                ["SPECIAL_COUNT"] = settings.specialAmounts.Length.ToString(),
            };
            for (int i = 0; i < settings.weaponAmounts.Length; i++) {
                defines["WEAPON" + i.ToString()] = "$" + settings.weaponAmounts[i].ToString();
            }
            for (int i = 0; i < settings.specialAmounts.Length; i++) {
                defines["SPECIAL" + i.ToString()] = "$" + settings.specialAmounts[i].ToString();
            }

            Patch(runROM, "RunFromLevel.asm", defines);
            Process.Start(runROM);
        }

        private static void AddEndOfBankFreespace(Stream s, Freespace freespace) {
            for (int bankEndPos = Freespace.BankSize - 1; bankEndPos < s.Length; bankEndPos += Freespace.BankSize) {
                s.Seek(bankEndPos, SeekOrigin.Begin);
                int length = 0;
                while (s.ReadByte() == 0xff) {
                    length++;
                    s.Seek(-2, SeekOrigin.Current);
                }
                // Leave 2 bytes of 0xff in case they were part of the end of some data
                if (length >= 2) {
                    freespace.AddSize((int)s.Position + 2, length - 2);
                }
            }
        }

        private static void Patch(string rom, string patch, Dictionary<string, string> defines = null) {
            string args = "";
            if (defines != null) {
                foreach (KeyValuePair<string, string> define in defines) {
                    args += string.Format("\"-D{0}={1}\" ", define.Key, define.Value);
                }
            }
            args += string.Format("\"{0}\" \"{1}\"", Path.Combine(internalProjectFilesPath, "Patches", patch), rom);

            ProcessStartInfo processInfo = new ProcessStartInfo(Path.Combine("Tools", "asar.exe")) {
                Arguments = args,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            Process p = Process.Start(processInfo);
            p.WaitForExit();
        }
    }
}
