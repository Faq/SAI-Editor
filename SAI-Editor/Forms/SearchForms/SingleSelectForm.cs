﻿using System;
using System.Windows.Forms;
using SAI_Editor.Classes;

namespace SAI_Editor.Forms.SearchForms
{
    public partial class SingleSelectForm<T> : Form where T : struct, IConvertible
    {
        private readonly ListViewColumnSorter _lvwColumnSorter = new ListViewColumnSorter();
        private readonly TextBox _textBoxToChange = null;

        public SingleSelectForm(TextBox textBoxToChange)
        {
            InitializeComponent();

            this._textBoxToChange = textBoxToChange;
            listViewSelectableItems.Columns.Add(typeof(T).Name, 235, HorizontalAlignment.Left);

            foreach (var en in Enum.GetNames(typeof(T)))
                listViewSelectableItems.Items.Add(en);

            listViewSelectableItems.AutoResizeColumns(ColumnHeaderAutoResizeStyle.HeaderSize);

            if (textBoxToChange != null && !String.IsNullOrWhiteSpace(textBoxToChange.Text) && textBoxToChange.Text != "0")
            {
                foreach (ListViewItem item in listViewSelectableItems.Items)
                {
                    int value = (int)Enum.Parse(typeof(T), item.Text);

                    if (item.Index > 0 && textBoxToChange.Text == value.ToString())
                    {
                        item.Selected = true;
                        item.EnsureVisible();
                        break; //! It's a SINGLE select form so only one item can be selected anyway.
                    }
                }
            }
            else
                listViewSelectableItems.Items[0].Selected = true;
        }

        private void buttonContinue_Click(object sender, EventArgs e)
        {
            if (_textBoxToChange == null)
            {
                Close();
                return;
            }

            if (listViewSelectableItems.SelectedItems.Count == 0)
                return;

            int selectedValue = (int)Enum.Parse(typeof(T), listViewSelectableItems.SelectedItems[0].Text);
            _textBoxToChange.Text = selectedValue.ToString();
            Close();
        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void listViewSelectableItems_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var myListView = (ListView)sender;
            myListView.ListViewItemSorter = _lvwColumnSorter;

            if (e.Column != _lvwColumnSorter.SortColumn)
            {
                _lvwColumnSorter.SortColumn = e.Column;
                _lvwColumnSorter.Order = SortOrder.Ascending;
            }
            else
                _lvwColumnSorter.Order = _lvwColumnSorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;

            myListView.Sort();
        }

        private void SingleSelectForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Close();
                    break;
                case Keys.Enter:
                    if (listViewSelectableItems.SelectedItems.Count > 0)
                        buttonContinue.PerformClick();

                    break;
            }
        }

        private void listViewSelectableItems_DoubleClick(object sender, EventArgs e)
        {
            buttonContinue.PerformClick();
        }

        private void listViewSelectableItems_SelectedIndexChanged(object sender, EventArgs e)
        {
            listViewSelectableItems.Items[0].Checked = listViewSelectableItems.CheckedItems.Count == 0;
        }
    }
}
