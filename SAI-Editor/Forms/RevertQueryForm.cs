﻿using SAI_Editor.Properties;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using SAI_Editor.Classes;

namespace SAI_Editor.Forms
{
    public partial class RevertQueryForm : Form
    {
        public RevertQueryForm()
        {
            InitializeComponent();

            if (!Directory.Exists("Reverts"))
                Directory.CreateDirectory("Reverts");

            if (!Settings.Default.CreateRevertQuery)
                labelWarningSettingOff.Visible = true;

            calenderScriptsToRevert.TodayDate = DateTime.Now.ToUniversalTime();
            FillListViewWithDate();
        }

        private void CalenderScriptsToRevert_DateChanged(object sender, DateRangeEventArgs e)
        {
            FillListViewWithDate();
        }

        private void FillListViewWithDate()
        {
            listViewScripts.Items.Clear();
            string[] allFiles = Directory.GetFiles("Reverts");
            List<string> allFilesList = allFiles.OrderByDescending(File.GetCreationTime).ToList();

            foreach (string file in allFilesList)
            {
                DateTime createTime = File.GetCreationTime(file).ToUniversalTime();

                //! If the file was created after or before the selection of the user, don't list it
                if (createTime.CompareTo(calenderScriptsToRevert.SelectionStart) < 0 && createTime.CompareTo(calenderScriptsToRevert.SelectionEnd) < 0)
                    continue;

                listViewScripts.Items.Add(file.Replace(@"Reverts\", "").Replace(".sql", "").Replace(";", ":"));
            }

            buttonExecuteSelectedScript.Enabled = listViewScripts.SelectedItems.Count > 0;
        }

        private void ButtonExecuteSelectedScript_Click(object sender, EventArgs e)
        {
            ExecutedSelectedScript();
        }

        private async void ExecutedSelectedScript()
        {
            if (listViewScripts.SelectedItems.Count == 0)
                return;

            string fileName = "Reverts\\" + listViewScripts.SelectedItems[0].Text + ".sql";
            fileName = fileName.Replace(":", ";");

            if (!File.Exists(fileName))
            {
                MessageBox.Show("The revert query could not be found!", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (await SAI_Editor_Manager.Instance.WorldDatabase.ExecuteNonQuery(File.ReadAllText(fileName)))
                MessageBox.Show("The query has been executed succesfully!", "Success!", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void ListViewScripts_SelectedIndexChanged(object sender, EventArgs e)
        {
            buttonExecuteSelectedScript.Enabled = listViewScripts.SelectedItems.Count > 0;
        }

        private void RevertQueryForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Close();
                    break;
            }
        }

        private void ListViewScripts_DoubleClick(object sender, EventArgs e)
        {
            ExecutedSelectedScript();
        }

        private void OpenFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewScripts.SelectedItems.Count == 0)
                return;

            string fileName = @"Reverts\" + listViewScripts.SelectedItems[0].Text + ".sql";
            fileName = fileName.Replace(":", ";");
            SAI_Editor_Manager.Instance.StartProcess(fileName);
        }

        private void OpenFileWithToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewScripts.SelectedItems.Count == 0)
                return;

            string fileName = @"Reverts\" + listViewScripts.SelectedItems[0].Text + ".sql";
            fileName = fileName.Replace(":", ";");
            SAI_Editor_Manager.Instance.StartProcess("rundll32.exe", "shell32.dll, OpenAs_RunDLL " + fileName);
        }

        private void OpenDirectoryOfFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewScripts.SelectedItems.Count == 0)
                return;

            string fileName = @"Reverts\" + listViewScripts.SelectedItems[0].Text + ".sql";
            fileName = fileName.Replace(":", ";");
            SAI_Editor_Manager.Instance.StartProcess("explorer.exe", String.Format("/select,\"{0}\"", fileName));
        }

        private void ListViewScripts_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
                if (listViewScripts.FocusedItem.Bounds.Contains(e.Location))
                    listViewContextMenu.Show(Cursor.Position);
        }

        private void DeleteRevertQueryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listViewScripts.SelectedItems.Count == 0)
                return;

            DialogResult dialogResult = MessageBox.Show("Are you sure you want to delete this revert query permanently? This action can not be undone!", "Are you sure!", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

            if (dialogResult != DialogResult.Yes)
                return;

            string fileName = @"Reverts\" + listViewScripts.SelectedItems[0].Text + ".sql";
            fileName = fileName.Replace(":", ";");
            File.Delete(fileName);
            FillListViewWithDate();
        }
    }
}
