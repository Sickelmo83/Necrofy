﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Newtonsoft.Json;
using WeifenLuo.WinFormsUI.Docking;

namespace Necrofy
{
    partial class MainWindow : Form
    {
        private Project project;

        private Dictionary<string, RunSettings> savedRunSettings;
        private string currentRunSettings;

        public ObjectBrowserForm ObjectBrowser { get; private set; }
        public ProjectBrowser ProjectBrowser { get; private set; }

        private readonly List<ToolStripMenuItem> projectMenuItems;

        private readonly HashSet<EditorWindow> openEditors = new HashSet<EditorWindow>();
        private readonly HashSet<EditorWindow> dirtyEditors = new HashSet<EditorWindow>();
        private EditorWindow activeEditor = null;
        private readonly List<ToolStripItem> editorToolStripItems = new List<ToolStripItem>();
        private readonly List<ToolStripItem> editorMenuStripItems = new List<ToolStripItem>();

        public MainWindow()
        {
            InitializeComponent();

            ObjectBrowser = new ObjectBrowserForm();
            ObjectBrowser.Show(dockPanel, DockState.DockLeft);

            ProjectBrowser = new ProjectBrowser(this);
            ProjectBrowser.Show(dockPanel, DockState.DockLeft);

            string recentProjectsString = Properties.Settings.Default.RecentProjects;
            if (recentProjectsString != "") {
                recentProjects.Files = recentProjectsString.Split(Path.PathSeparator);
            }

            LoadRunSettings();
            
            ToolBarMenuLinker.Link(saveButton, fileSave);
            ToolBarMenuLinker.Link(saveAllButton, fileSaveAll);
            ToolBarMenuLinker.Link(cutButton, editCut);
            ToolBarMenuLinker.Link(copyButton, editCopy);
            ToolBarMenuLinker.Link(pasteButton, editPaste);
            ToolBarMenuLinker.Link(undoButton, editUndo);
            ToolBarMenuLinker.Link(redoButton, editRedo);
            ToolBarMenuLinker.Link(buildProjectButton, buildBuildProject);
            ToolBarMenuLinker.Link(runProjectButton, buildRunProject);
            ToolBarMenuLinker.Link(runFromLevelButton, buildRunFromLevel);
            projectMenuItems = new List<ToolStripMenuItem>() { buildBuildProject, buildRunProject };
        }

        private void LoadRunSettings() {
            if (string.IsNullOrEmpty(Properties.Settings.Default.SavedRunSettings)) {
                SetDefaultRunSettings();
            } else {
                try {
                    savedRunSettings = JsonConvert.DeserializeObject<Dictionary<string, RunSettings>>(Properties.Settings.Default.SavedRunSettings);
                    List<string> invalidSettings = new List<string>();
                    foreach (string settingsName in savedRunSettings.Keys) {
                        RunSettings settings = savedRunSettings[settingsName];
                        if (settings.weaponAmounts.Length != RunSettings.WeaponCount || settings.specialAmounts.Length != RunSettings.SpecialCount) {
                            invalidSettings.Add(settingsName);
                        }
                    }
                    foreach (string settingsName in invalidSettings) {
                        savedRunSettings.Remove(settingsName);
                    }
                    if (savedRunSettings.Count == 0) {
                        SetDefaultRunSettings();
                    } else {
                        currentRunSettings = Properties.Settings.Default.CurrentRunSettingsName;
                        if (!savedRunSettings.ContainsKey(currentRunSettings)) {
                            currentRunSettings = savedRunSettings.Keys.First();
                            SaveRunSettings();
                        }
                    }
                } catch {
                    SetDefaultRunSettings();
                }
            }
        }

        private void SetDefaultRunSettings() {
            savedRunSettings = new Dictionary<string, RunSettings>();

            RunSettings starting = new RunSettings();
            starting.weaponAmounts[0] = 150;
            starting.specialAmounts[7] = 1;
            savedRunSettings["Starting"] = starting;

            RunSettings max = new RunSettings();
            for (int i = 0; i < max.weaponAmounts.Length; i++) {
                max.weaponAmounts[i] = 999;
            }
            max.weaponAmounts[9] = 0;
            for (int i = 0; i < max.specialAmounts.Length; i++) {
                max.specialAmounts[i] = 99;
            }
            max.specialAmounts[5] = 0;
            max.specialAmounts[6] = 0;
            max.specialAmounts[11] = 0;
            savedRunSettings["Max"] = max;

            currentRunSettings = "Max";
            SaveRunSettings();
        }

        private void SaveRunSettings() {
            Properties.Settings.Default.SavedRunSettings = JsonConvert.SerializeObject(savedRunSettings);
            Properties.Settings.Default.CurrentRunSettingsName = currentRunSettings;
            Properties.Settings.Default.Save();
        }

        public ToolStripSplitButton UndoButton => undoButton;
        public ToolStripSplitButton RedoButton => redoButton;

        public void ShowEditor(EditorWindow editor) {
            openEditors.Add(editor);
            editor.Setup(this);
            editor.Show(dockPanel, DockState.Document);
            editor.DirtyChanged += Editor_DirtyChanged;
            editor.SelectionChanged += Editor_SelectionChanged;
        }

        private void Editor_DirtyChanged(object sender, EventArgs e) {
            EditorWindow editor = (EditorWindow)sender;
            if (editor != null) {
                if (editor.Dirty) {
                    if (dirtyEditors.Add(editor)) {
                        editor.Text += "*";
                        // TODO: fix "*" getting cut off if the name is too long
                    }
                } else {
                    if (dirtyEditors.Remove(editor)) {
                        editor.Text = editor.Text.Substring(0, editor.Text.Length - 1);
                    }
                }
            }
            if (editor == activeEditor) {
                saveButton.Enabled = editor?.Dirty ?? false;
            }
            saveAllButton.Enabled = dirtyEditors.Count > 0;
        }

        private void Editor_SelectionChanged(object sender, EventArgs e) {
            if (sender == activeEditor) {
                editCopy.Enabled = activeEditor?.CanCopy ?? false;
                editPaste.Enabled = activeEditor?.CanPaste ?? false;
                editDelete.Enabled = activeEditor?.CanDelete ?? false;
                editCut.Enabled = editCopy.Enabled && editDelete.Enabled;

                editSelectAll.Enabled = activeEditor?.HasSelection ?? false;
                editSelectNone.Enabled = activeEditor?.HasSelection ?? false;
            }
        }

        private void dockPanel_ContentRemoved(object sender, DockContentEventArgs e) {
            if (e.Content is EditorWindow editor) {
                openEditors.Remove(editor);
                dirtyEditors.Remove(editor);
                Editor_DirtyChanged(null, e); // Update the "save all" button state
                if (openEditors.Count == 0) {
                    ProjectBrowser.Activate();
                    undoButton.Enabled = false;
                    redoButton.Enabled = false;
                }
            }
        }

        private void dockPanel_ActiveDocumentChanged(object sender, EventArgs e) {
            EditorWindow editor = dockPanel.ActiveContent as EditorWindow;

            if (activeEditor != null) {
                if (activeEditor.EditorMenuStrip != null) {
                    foreach (ToolStripItem item in editorMenuStripItems) {
                        // Item is automatically removed from the existing tools strip when it is added to a different one
                        activeEditor.EditorMenuStrip.Items.Add(item);
                    }
                }
                if (activeEditor.EditorToolStrip != null) {
                    foreach (ToolStripItem item in editorToolStripItems) {
                        activeEditor.EditorToolStrip.Items.Add(item);
                    }
                }
            }
            editorMenuStripItems.Clear();
            editorToolStripItems.Clear();

            activeEditor = editor;
            if (editor != null) {
                if (editor.EditorMenuStrip != null) {
                    while (editor.EditorMenuStrip.Items.Count > 0) {
                        ToolStripItem item = editor.EditorMenuStrip.Items[0];
                        menuStrip1.Items.Add(item);
                        editorMenuStripItems.Add(item);
                    }
                }
                if (editor.EditorToolStrip != null) {
                    while (editor.EditorToolStrip.Items.Count > 0) {
                        ToolStripItem item = editor.EditorToolStrip.Items[0];
                        toolStrip1.Items.Add(item);
                        editorToolStripItems.Add(item);
                    }
                }

                editor.Displayed();
            }
            endToolStripSeparator.Visible = editorToolStripItems.Count > 0;
            Editor_DirtyChanged(editor, e);
            Editor_SelectionChanged(editor, e);
            ObjectBrowser.Browser.Contents = activeEditor?.BrowserContents;
            buildRunFromLevel.Enabled = activeEditor?.LevelNumber  != null;
        }

        private void CreateProject(object sender, EventArgs e) {
            NewProjectDialog newProjectDialog = new NewProjectDialog();
            if (newProjectDialog.ShowDialog() == DialogResult.OK) {
                // TODO: close already open project if there is one
                project = new Project(newProjectDialog.BaseROM, newProjectDialog.ProjectLocation);
                ProjectReady();
            }
        }

        private void OpenProject(object sender, EventArgs e) {
            if (openProjectDialog.ShowDialog() == DialogResult.OK) {
                // TODO: close already open project if there is one
                project = new Project(openProjectDialog.FileName);
                ProjectReady();
            }
        }

        private void recentProjects_FileClicked(string file) {
            // TODO: close already open project if there is one
            project = new Project(file);
            ProjectReady();
        }

        private void ProjectReady() {
            recentProjects.Add(project.SettingsPath);
            Properties.Settings.Default.RecentProjects = string.Join(Path.PathSeparator.ToString(), recentProjects.Files);
            Properties.Settings.Default.Save();

            ProjectBrowser.OpenProject(project);
            ProjectBrowser.Activate();

            foreach (ToolStripMenuItem item in projectMenuItems) {
                item.Enabled = true;
            }
        }

        private void Save(object sender, EventArgs e) {
            activeEditor?.Save(project);
        }

        private void SaveAll(object sender, EventArgs e) {
            // Calling save on the editors will modify this set, so operate on a copy
            HashSet<EditorWindow> allDirtyEditors = new HashSet<EditorWindow>(dirtyEditors);
            foreach (EditorWindow editor in allDirtyEditors) {
                editor.Save(project);
            }
        }

        private void Undo(object sender, EventArgs e) {
            activeEditor?.Undo();
        }

        private void Redo(object sender, EventArgs e) {
            activeEditor?.Redo();
        }

        private void Cut(object sender, EventArgs e) {
            activeEditor?.Copy();
            activeEditor?.Delete();
        }

        private void Copy(object sender, EventArgs e) {
            activeEditor?.Copy();
        }

        private void Paste(object sender, EventArgs e) {
            activeEditor?.Paste();
        }

        private void Delete(object sender, EventArgs e) {
            activeEditor?.Delete();
        }

        private void SelectAll(object sender, EventArgs e) {
            activeEditor?.SelectAll();
        }

        private void SelectNone(object sender, EventArgs e) {
            activeEditor?.SelectNone();
        }

        private void BuildProject(object sender, EventArgs e) {
            project?.Build();
            // TODO tell the user that it finished
        }

        private void RunProject(object sender, EventArgs e) {
            project?.Run();
        }

        private void RunFromLevel(object sender, EventArgs e) {
            if (activeEditor?.LevelNumber != null) {
                project?.RunFromLevel((int)activeEditor?.LevelNumber, savedRunSettings[currentRunSettings]);
            }
        }

        private void RunFromLevelSettings(object sender, EventArgs e) {
            RunSettingsDialog dialog = new RunSettingsDialog(savedRunSettings, currentRunSettings);
            dialog.FormClosed += (sender2, e2) => {
                currentRunSettings = dialog.currentSettings;
                SaveRunSettings();
            };
            dialog.Show();
        }

        private void debugToolStripMenuItem_Click(object sender, EventArgs e) {
            // Convert demo data
            //OpenFileDialog ofd = new OpenFileDialog();
            //if (ofd.ShowDialog() == DialogResult.OK) {
            //    FileStream fs = new FileStream(ofd.FileName, FileMode.Open);
            //    int frameCount = fs.ReadInt16();

            //    ushort prevInput = fs.ReadInt16();
            //    ushort inputCount = 1;

            //    List<byte> output = new List<byte>();
            //    for (int i = 1; i < frameCount; i++) {
            //        ushort nextInput = fs.ReadInt16();
            //        if (nextInput == prevInput) {
            //            inputCount++;
            //        } else {
            //            output.AddInt16(prevInput);
            //            output.AddInt16(inputCount);
            //            prevInput = nextInput;
            //            inputCount = 1;
            //        }
            //    }

            //    output.AddInt16(prevInput);
            //    output.AddInt16(inputCount);

            //    SaveFileDialog sfd = new SaveFileDialog();
            //    if (sfd.ShowDialog() == DialogResult.OK) {
            //        File.WriteAllBytes(sfd.FileName, output.ToArray());
            //    }
            //}

            if (project == null)
                return;
            new SpriteViewer().Show(project);
        }
    }
}
