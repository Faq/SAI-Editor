﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using SAI_Editor.Classes.Database.Classes;
using SAI_Editor.Enumerators;
using SAI_Editor.Properties;
using SAI_Editor.Classes;

namespace SAI_Editor.Forms.SearchForms
{
    public partial class SearchForEntryForm : Form
    {
        private Thread _searchThread = null;
        private readonly SourceTypes _sourceTypeToSearchFor;
        private readonly ListViewColumnSorter _lvwColumnSorter = new ListViewColumnSorter();
        private CancellationTokenSource _cts;
        private int _previousSearchType = 0;
        private bool _isBusy = false;

        public class Item
        {
            public string ItemName { get; set; }
            public List<string> SubItems { get; set; }
        }

        public SearchForEntryForm(string startEntryString, SourceTypes sourceTypeToSearchFor)
        {
            InitializeComponent();

            this._sourceTypeToSearchFor = sourceTypeToSearchFor;
            textBoxCriteria.Text = startEntryString;

            MinimumSize = new Size(Width, Height);
            MaximumSize = new Size(Width, Height + 800);

            if (sourceTypeToSearchFor != SourceTypes.SourceTypeAreaTrigger)
            {
                listViewEntryResults.Columns.Add("Entry/guid", 70, HorizontalAlignment.Right);
                listViewEntryResults.Columns.Add("Name", 260, HorizontalAlignment.Left);
            }
        }

        private void SearchForEntryForm_Load(object sender, EventArgs e)
        {
            switch (_sourceTypeToSearchFor)
            {
                case SourceTypes.SourceTypeCreature:
                    comboBoxSearchType.SelectedIndex = 0; //! Creature entry
                    FillListViewWithMySqlQuery("SELECT entry, name FROM creature_template ORDER BY entry LIMIT 1000");
                    break;
                case SourceTypes.SourceTypeGameobject:
                    comboBoxSearchType.SelectedIndex = 3; //! Gameobject entry
                    FillListViewWithMySqlQuery("SELECT entry, name FROM gameobject_template ORDER BY entry LIMIT 1000");
                    break;
                case SourceTypes.SourceTypeAreaTrigger:
                    comboBoxSearchType.SelectedIndex = 6; //! Areatrigger entry
                    listViewEntryResults.Columns.Add("Id", 53, HorizontalAlignment.Right);
                    listViewEntryResults.Columns.Add("Mapid", 52, HorizontalAlignment.Left);
                    listViewEntryResults.Columns.Add("X", 75, HorizontalAlignment.Left);
                    listViewEntryResults.Columns.Add("Y", 75, HorizontalAlignment.Left);
                    listViewEntryResults.Columns.Add("Z", 75, HorizontalAlignment.Left);
                    FillListViewWithAreaTriggers(String.Empty, String.Empty, true);
                    break;
                case SourceTypes.SourceTypeScriptedActionlist:
                    checkBoxHasAiName.Enabled = false;
                    comboBoxSearchType.SelectedIndex = 8; //! Actionlist entry
                    //! We don't list 1000 actionlists like all other source types because we can't get the entry/name combination
                    //! of several sources (considering the actionlist can be called from _ANY_ source_type (including actionlists
                    //! itself). It's simply not worth the time.
                    break;
            }
        }

        private void listViewEntryResults_DoubleClick(object sender, EventArgs e)
        {

            StopRunningThread();
            FillMainFormFields(sender, e);
        }

        private void FillListViewWithMySqlQuery(string queryToExecute)
        {
            if (_cts != null)
                _cts.Dispose();

            _cts = new CancellationTokenSource();

            try
            {
                using (var connection = new MySqlConnection(SAI_Editor_Manager.Instance.connString.ToString()))
                {
                    connection.Open();

                    List<Item> items = new List<Item>();

                    using (var query = new MySqlCommand(queryToExecute, connection))
                    {
                        using (MySqlDataReader reader = query.ExecuteReader())
                        {
                            while (reader != null && reader.Read())
                            {
                                if (_cts.IsCancellationRequested)
                                    break;

                                items.Add(new Item { ItemName = reader.GetInt32(0).ToString(CultureInfo.InvariantCulture), SubItems = new List<string> { reader.GetString(1) } });
                            }
                        }
                    }

                    AddItemToListView(listViewEntryResults, items);

                }
            }
            catch (MySqlException)
            {
                MessageBox.Show("Something went wrong retrieving the results from your database.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopRunningThread();
            }
        }

        private async void FillListViewWithAreaTriggers(string idFilter, string mapIdFilter, bool limit)
        {
            if (_cts != null)
                _cts.Dispose();

            _cts = new CancellationTokenSource();

            try
            {
                string queryToExecute = "SELECT * FROM " + SAI_Editor_Manager.GetAreatriggerTableName();

                if (idFilter.Length > 0 || mapIdFilter.Length > 0)
                {
                    if (checkBoxFieldContainsCriteria.Checked)
                    {
                        if (idFilter.Length > 0)
                        {
                            queryToExecute += " WHERE id LIKE '%" + idFilter + "%'";

                            if (mapIdFilter.Length > 0)
                                queryToExecute += " AND mapId LIKE '%" + mapIdFilter + "%'";
                        }
                        else if (mapIdFilter.Length > 0)
                        {
                            queryToExecute += " WHERE mapId LIKE '%" + mapIdFilter + "%'";

                            if (idFilter.Length > 0)
                                queryToExecute += " AND id LIKE '%" + idFilter + "%'";
                        }
                    }
                    else
                    {
                        if (idFilter.Length > 0)
                        {
                            queryToExecute += " WHERE id = " + idFilter;

                            if (mapIdFilter.Length > 0)
                                queryToExecute += " AND mapId = " + mapIdFilter;
                        }
                        else if (mapIdFilter.Length > 0)
                        {
                            queryToExecute += " WHERE mapId = " + mapIdFilter;

                            if (idFilter.Length > 0)
                                queryToExecute += " AND id = " + idFilter;
                        }
                    }
                }

                if (limit)
                    queryToExecute += " LIMIT 1000";

                DataTable dt = await SAI_Editor_Manager.Instance.SqliteDatabase.ExecuteQueryWithCancellation(_cts.Token, queryToExecute);

                if (dt.Rows.Count > 0)
                {
                    List<Item> items = new List<Item>();

                    foreach (DataRow row in dt.Rows)
                    {
                        if (_cts.IsCancellationRequested)
                            break;

                        AreaTrigger areaTrigger = SAI_Editor_Manager.Instance.SqliteDatabase.BuildAreaTrigger(row);

                        if (!checkBoxHasAiName.Checked || await SAI_Editor_Manager.Instance.WorldDatabase.AreaTriggerHasSmartAI(areaTrigger.id))
                            items.Add(new Item { ItemName = areaTrigger.id.ToString(), SubItems = new List<string> { areaTrigger.map_id.ToString(), areaTrigger.posX.ToString(), areaTrigger.posY.ToString(), areaTrigger.posZ.ToString() } });
                    }

                    AddItemToListView(listViewEntryResults, items);
                }
            }
            catch (MySqlException)
            {
                MessageBox.Show("Something went wrong retrieving the results from your database.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopRunningThread();
            }
        }

        private void ButtonSearch_Click(object sender, EventArgs e)
        {
            if (_isBusy)
                return;

            _isBusy = true;
            _searchThread = new Thread(StartSearching);
            _searchThread.Start();
        }

        private async void StartSearching()
        {
            if (_cts != null)
                _cts.Dispose();

            _cts = new CancellationTokenSource();
            string criteria = textBoxCriteria.Text.Trim();

            try
            {
                string query = "";
                bool criteriaLeftEmpty = String.IsNullOrEmpty(criteria) || String.IsNullOrWhiteSpace(criteria);

                if (!criteriaLeftEmpty && IsNumericIndex(GetSelectedIndexOfComboBox(comboBoxSearchType)) && Convert.ToInt32(criteria) < 0)
                {
                    bool pressedYes = true;

                    this.Invoke(new Action(() =>
                    {
                        pressedYes = MessageBox.Show("The criteria field can not be a negative value, would you like me to set it to a positive number and continue the search?", "Something went wrong!", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes;
                    }));

                    if (!pressedYes)
                    {
                        StopRunningThread();
                        return;
                    }

                    SetTextOfControl(textBoxCriteria, (Convert.ToInt32(criteria) * -1).ToString());
                }

                SetEnabledOfControl(buttonSearch, false);
                SetEnabledOfControl(buttonStopSearching, true);

                switch (GetSelectedIndexOfComboBox(comboBoxSearchType))
                {
                    case 0: //! Creature entry
                        query = "SELECT entry, name FROM creature_template";

                        if (!criteriaLeftEmpty)
                        {
                            if (checkBoxFieldContainsCriteria.Checked)
                                query += " WHERE entry LIKE '%" + criteria + "%'";
                            else
                                query += " WHERE entry=" + criteria;
                        }

                        if (checkBoxHasAiName.Checked)
                            query += (criteriaLeftEmpty ? " WHERE" : " AND") + " AIName='SmartAI'";

                        query += " ORDER BY entry";
                        break;
                    case 1: //! Creature name
                        query = "SELECT entry, name FROM creature_template WHERE name LIKE '%" + criteria + "%'";

                        if (checkBoxHasAiName.Checked)
                            query += " AND AIName='SmartAI'";

                        query += " ORDER BY entry";
                        break;
                    case 2: //! Creature guid
                        if (criteriaLeftEmpty)
                        {
                            if (Settings.Default.PromptExecuteQuery)
                            {
                                bool pressedYes = true;

                                this.Invoke(new Action(() =>
                                {
                                    pressedYes = MessageBox.Show(this, "Are you sure you wish to continue? This query will take a long time to execute because the criteria field was left empty!", "Are you sure you want to continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
                                }));

                                if (!pressedYes)
                                {
                                    StopRunningThread();
                                    return;
                                }
                            }

                            if (checkBoxHasAiName.Checked)
                                query = "SELECT c.guid, ct.name FROM creature_template ct JOIN creature c ON ct.entry = c.id JOIN smart_scripts ss ON ss.entryorguid < 0 AND ss.entryorguid = -c.guid AND ss.source_type = 0";
                            else
                                query = "SELECT c.guid, ct.name FROM creature_template ct JOIN creature c ON ct.entry = c.id";
                        }
                        else
                        {
                            if (checkBoxHasAiName.Checked)
                            {
                                if (checkBoxFieldContainsCriteria.Checked)
                                    query = "SELECT c.guid, ct.name FROM creature c JOIN creature_template ct ON ct.entry = c.id JOIN smart_scripts ss ON ss.entryorguid = -c.guid WHERE c.guid LIKE '%" + criteria + "%' AND ss.source_type = 0";
                                else
                                    query = "SELECT c.guid, ct.name FROM creature_template ct JOIN creature c ON ct.entry = c.id JOIN smart_scripts ss ON ss.entryorguid = -c.guid WHERE c.guid = " + criteria;
                            }
                            else
                            {
                                if (checkBoxFieldContainsCriteria.Checked)
                                    query = "SELECT c.guid, ct.name FROM creature c JOIN creature_template ct ON ct.entry = c.id WHERE c.guid LIKE '%" + criteria + "%'";
                                else
                                    query = "SELECT c.guid, ct.name FROM creature_template ct JOIN creature c ON ct.entry = c.id WHERE c.guid = " + criteria;
                            }
                        }

                        query += " ORDER BY c.guid";
                        break;
                    case 3: //! Gameobject entry
                        query = "SELECT entry, name FROM gameobject_template";

                        if (!criteriaLeftEmpty)
                        {
                            if (checkBoxFieldContainsCriteria.Checked)
                                query += " WHERE entry LIKE '%" + criteria + "%'";
                            else
                                query += " WHERE entry=" + criteria;
                        }

                        if (checkBoxHasAiName.Checked)
                            query += (criteriaLeftEmpty ? " WHERE" : " AND") + " AIName='SmartGameObjectAI'";

                        query += " ORDER BY entry";
                        break;
                    case 4: //! Gameobject name
                        query = "SELECT entry, name FROM gameobject_template WHERE name LIKE '%" + criteria + "%'";

                        if (checkBoxHasAiName.Checked)
                            query += " AND AIName='SmartGameObjectAI'";

                        query += " ORDER BY entry";
                        break;
                    case 5: //! Gameobject guid
                        if (criteriaLeftEmpty)
                        {
                            if (Settings.Default.PromptExecuteQuery)
                            {
                                bool pressedYes = true;

                                this.Invoke(new Action(() =>
                                {
                                    pressedYes = MessageBox.Show(this, "Are you sure you wish to continue? This query will take a long time to execute because the criteria field was left empty!", "Are you sure you want to continue?", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes;
                                }));

                                if (!pressedYes)
                                {
                                    StopRunningThread();
                                    return;
                                }
                            }

                            if (checkBoxHasAiName.Checked)
                                query = "SELECT g.guid, gt.name FROM gameobject_template gt JOIN gameobject g ON gt.entry = g.id JOIN smart_scripts ss ON ss.entryorguid < 0 AND ss.entryorguid = -g.guid AND ss.source_type = 1";
                            else
                                query = "SELECT g.guid, gt.name FROM gameobject_template gt JOIN gameobject g ON gt.entry = g.id";
                        }
                        else
                        {
                            if (checkBoxHasAiName.Checked)
                            {
                                if (checkBoxFieldContainsCriteria.Checked)
                                    query = "SELECT g.guid, gt.name FROM gameobject g JOIN gameobject_template gt ON gt.entry = g.id JOIN smart_scripts ss ON ss.entryorguid = -g.guid WHERE g.guid LIKE '%" + criteria + "%' AND ss.source_type = 1";
                                else
                                    query = "SELECT g.guid, gt.name FROM gameobject_template gt JOIN gameobject g ON gt.entry = g.id JOIN smart_scripts ss ON ss.entryorguid = -g.guid WHERE g.guid = " + criteria + " AND ss.source_type = 1";
                            }
                            else
                            {
                                if (checkBoxFieldContainsCriteria.Checked)
                                    query = "SELECT g.guid, gt.name FROM gameobject g JOIN gameobject_template gt ON gt.entry = g.id WHERE g.guid LIKE '%" + criteria + "%'";
                                else
                                    query = "SELECT g.guid, gt.name FROM gameobject_template gt JOIN gameobject g ON gt.entry = g.id WHERE g.guid = " + criteria;
                            }
                        }

                        query += " ORDER BY g.guid";
                        break;
                    case 6: //! Areatrigger id
                    case 7: //! Areatrigger map id
                        ClearItemsOfListView(listViewEntryResults);

                        try
                        {
                            string areaTriggerIdFilter = "", areaTriggerMapIdFilter = "";

                            if (GetSelectedIndexOfComboBox(comboBoxSearchType) == 6)
                                areaTriggerIdFilter = criteria;
                            else
                                areaTriggerMapIdFilter = criteria;

                            FillListViewWithAreaTriggers(areaTriggerIdFilter, areaTriggerMapIdFilter, false);
                        }
                        catch (ThreadAbortException) //! Don't show a message when the thread was already cancelled
                        {
                            SetEnabledOfControl(buttonSearch, true);
                            SetEnabledOfControl(buttonStopSearching, false);
                        }
                        catch
                        {
                            SetEnabledOfControl(buttonSearch, true);
                            SetEnabledOfControl(buttonStopSearching, false);
                            MessageBox.Show("Something went wrong retrieving the results from your database.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            StopRunningThread();
                        }
                        finally
                        {
                            _isBusy = false;
                            SetEnabledOfControl(buttonSearch, true);
                            SetEnabledOfControl(buttonStopSearching, false);
                        }

                        return;
                    case 8: //! Actionlist entry
                        ClearItemsOfListView(listViewEntryResults);

                        try
                        {
                            List<SmartScript> smartScriptActionlists = await SAI_Editor_Manager.Instance.WorldDatabase.GetSmartScriptActionLists(criteria, checkBoxFieldContainsCriteria.Checked);

                            if (smartScriptActionlists != null)
                            {

                                List<Item> items = new List<Item>();

                                foreach (SmartScript smartScript in smartScriptActionlists)
                                {
                                    if (_cts.IsCancellationRequested)
                                        break;

                                    int entryorguid = smartScript.entryorguid;
                                    int sourceType = smartScript.source_type;

                                    //! If the entryorguid is below 0 it means the script is for a creature. We need to get
                                    //! the creature_template.entry by the guid in order to obtain the creature_template.name
                                    //! field now.
                                    if (entryorguid < 0)
                                        entryorguid = await SAI_Editor_Manager.Instance.WorldDatabase.GetObjectIdByGuidAndSourceType(entryorguid * -1, sourceType);

                                    string name = await SAI_Editor_Manager.Instance.WorldDatabase.GetObjectNameByIdAndSourceType(entryorguid, sourceType);
                                    int actionParam1 = smartScript.action_param1;
                                    int actionParam2 = smartScript.action_param2;

                                    switch ((SmartAction)smartScript.action_type) //! action type
                                    {
                                        case SmartAction.SMART_ACTION_CALL_TIMED_ACTIONLIST:
                                            items.Add(new Item { ItemName = actionParam1.ToString(), SubItems = new List<string>() { name } });
                                            //AddItemToListView(listViewEntryResults, actionParam1.ToString(), name);
                                            break;
                                        case SmartAction.SMART_ACTION_CALL_RANDOM_TIMED_ACTIONLIST:

                                            items.Add(new Item { ItemName = smartScript.action_param1.ToString(), SubItems = new List<string> { name } });
                                            items.Add(new Item { ItemName = smartScript.action_param2.ToString(), SubItems = new List<string> { name } });


                                            if (smartScript.action_param3 > 0)
                                                items.Add(new Item { ItemName = smartScript.action_param3.ToString(), SubItems = new List<string> { name } });

                                            if (smartScript.action_param4 > 0)
                                                items.Add(new Item { ItemName = smartScript.action_param4.ToString(), SubItems = new List<string> { name } });

                                            if (smartScript.action_param5 > 0)
                                                items.Add(new Item { ItemName = smartScript.action_param5.ToString(), SubItems = new List<string> { name } });

                                            if (smartScript.action_param6 > 0)
                                                items.Add(new Item { ItemName = smartScript.action_param6.ToString(), SubItems = new List<string> { name } });

                                            break;
                                        case SmartAction.SMART_ACTION_CALL_RANDOM_RANGE_TIMED_ACTIONLIST:
                                            for (int i = actionParam1; i <= actionParam2; ++i)
                                                items.Add(new Item { ItemName = i.ToString(), SubItems = new List<string> { name } });

                                                break;
                                    }
                                }

                                AddItemToListView(listViewEntryResults, items);

                            }
                        }
                        catch (ThreadAbortException) //! Don't show a message when the thread was already cancelled
                        {
                            SetEnabledOfControl(buttonSearch, true);
                            SetEnabledOfControl(buttonStopSearching, false);
                        }
                        catch
                        {
                            SetEnabledOfControl(buttonSearch, true);
                            SetEnabledOfControl(buttonStopSearching, false);
                            MessageBox.Show("Something went wrong retrieving the results from your database.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            StopRunningThread();
                        }
                        finally
                        {
                            _isBusy = false;
                            SetEnabledOfControl(buttonSearch, true);
                            SetEnabledOfControl(buttonStopSearching, false);
                        }

                        return; //! We did everything in the switch block (we only do this for actionlists)
                    default:
                        MessageBox.Show("An unknown index was found in the search type box!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        StopRunningThread();
                        return;
                }

                ClearItemsOfListView(listViewEntryResults);

                try
                {
                    FillListViewWithMySqlQuery(query);
                }
                finally
                {
                    SetEnabledOfControl(buttonSearch, true);
                    SetEnabledOfControl(buttonStopSearching, false);
                    _isBusy = false;
                }
            }
            catch (ThreadAbortException) //! Don't show a message when the thread was already cancelled
            {
                SetEnabledOfControl(buttonSearch, true);
                SetEnabledOfControl(buttonStopSearching, false);
                StopRunningThread();
            }
            catch
            {
                MessageBox.Show("Something went wrong retrieving the results from your database.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                StopRunningThread();
            }
        }

        private void SearchForEntryForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                {
                    if (listViewEntryResults.SelectedItems.Count > 0 && listViewEntryResults.Focused)
                        FillMainFormFields(sender, e);
                    else
                        buttonSearch.PerformClick();

                    break;
                }
                case Keys.Escape:
                {
                    Close();
                    break;
                }
            }
        }

        private void buttonStopSearchResults_Click(object sender, EventArgs e)
        {
            StopRunningThread();
        }

        private void StopRunningThread()
        {
            if (_searchThread != null && _cts != null && _searchThread.IsAlive)
            {
                _cts.Cancel();
                _cts.Dispose();
                _cts = null;
            }

            _isBusy = false;
        }

        private void ComboBoxSearchType_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Char.IsLetter(e.KeyChar) || Char.IsDigit(e.KeyChar))
                e.Handled = true; //! Disallow changing content of the combobox, but setting it to 3D looks like shit
        }

        private void FillMainFormFields(object sender, EventArgs e)
        {
            string entryToPlace = "";

            //! If we're searching for a creature guid or gameobject guid we have to make the value negative.
            if (comboBoxSearchType.SelectedIndex == 2 || comboBoxSearchType.SelectedIndex == 5)
                entryToPlace = "-";

            entryToPlace += listViewEntryResults.SelectedItems[0].Text;
            ((MainForm)Owner).UserControl.textBoxEntryOrGuid.Text = entryToPlace;

            switch (comboBoxSearchType.SelectedIndex)
            {
                case 0: //! Creature entry
                case 1: //! Creature name
                case 2: //! Creature guid
                    ((MainForm)Owner).UserControl.comboBoxSourceType.SelectedIndex = 0;
                    break;
                case 3: //! Gameobject entry
                case 4: //! Gameobject name
                case 5: //! Gameobject guid
                    ((MainForm)Owner).UserControl.comboBoxSourceType.SelectedIndex = 1;
                    break;
                case 6: //! Areatrigger id
                case 7: //! Areatrigger map id
                    ((MainForm)Owner).UserControl.comboBoxSourceType.SelectedIndex = 2;
                    break;
                case 8: //! Actionlist entry
                    ((MainForm)Owner).UserControl.comboBoxSourceType.SelectedIndex = 3;
                    break;
            }

            if (((MainForm)Owner).UserControl.pictureBoxLoadScript.Enabled)
                ((MainForm)Owner).UserControl.TryToLoadScript(-1, SourceTypes.SourceTypeNone, true, true);

            Close();
        }

        private bool IsNumericIndex(int index)
        {
            switch (index)
            {
                case 1: //! Creature name
                case 4: //! Gameobject name
                    return false;
                default:
                    return true;
            }
        }

        //! Cross-thread functions:
        private int GetSelectedIndexOfComboBox(ComboBox comboBox)
        {
            if (comboBox.InvokeRequired)
                return (int)comboBox.Invoke(new Func<int>(() => GetSelectedIndexOfComboBox(comboBox)));

            return comboBox.SelectedIndex;
        }

        private void AddItemToListView(ListView listView, IEnumerable<Item> items)
        {
            try
            {
                List<ListViewItem> lvItems = new List<ListViewItem>();

                Invoke((MethodInvoker)delegate
                {
                    foreach (var item in items)
                    {
                        var lvi = new ListViewItem(item.ItemName);

                        foreach (string subItem in item.SubItems)
                            lvi.SubItems.Add(subItem);

                        lvItems.Add(lvi);
                    }

                    listView.Items.AddRange(lvItems.ToArray());
                });
            }
            catch (InvalidOperationException)
            {
            }
            catch
            {
                MessageBox.Show("Something went wrong retrieving the results from your database.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        //private void AddItemToListView(ListView listView, string item, string subItem1, string subItem2, string subItem3, string subItem4)
        //{
        //    if (listView.InvokeRequired)
        //    {
        //        Invoke((MethodInvoker)delegate
        //        {
        //            ListViewItem listViewItem = listView.Items.Add(item);
        //            listViewItem.SubItems.Add(subItem1);
        //            listViewItem.SubItems.Add(subItem2);
        //            listViewItem.SubItems.Add(subItem3);
        //            listViewItem.SubItems.Add(subItem4);
        //        });
        //        return;
        //    }

        //    ListViewItem listViewItem2 = listView.Items.Add(item);
        //    listViewItem2.SubItems.Add(subItem1);
        //    listViewItem2.SubItems.Add(subItem2);
        //    listViewItem2.SubItems.Add(subItem3);
        //    listViewItem2.SubItems.Add(subItem4);
        //}

        private void SetEnabledOfControl(Control control, bool enable)
        {
            if (control.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    control.Enabled = enable;
                });
                return;
            }

            control.Enabled = enable;
        }

        private void ClearItemsOfListView(ListView listView)
        {
            if (listView.InvokeRequired)
            {
                Invoke((MethodInvoker)(() => listView.Items.Clear()));
                return;
            }

            listView.Items.Clear();
        }

        private void SetTextOfControl(Control control, string text)
        {
            if (control.InvokeRequired)
            {
                Invoke((MethodInvoker)delegate
                {
                    control.Text = text;
                });
                return;
            }

            control.Text = text;
        }

        private void listViewEntryResults_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            var myListView = (ListView)sender;
            myListView.ListViewItemSorter = _lvwColumnSorter;
            //! Determine if clicked column is already the column that is being sorted
            if (e.Column != _lvwColumnSorter.SortColumn)
            {
                //! Set the column number that is to be sorted; default to ascending
                _lvwColumnSorter.SortColumn = e.Column;
                _lvwColumnSorter.Order = SortOrder.Ascending;
            }
            else
                //! Reverse the current sort direction for this column
                _lvwColumnSorter.Order = _lvwColumnSorter.Order == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;

            //! Perform the sort with these new sort options
            myListView.Sort();
        }

        private void comboBoxSearchType_SelectedIndexChanged(object sender, EventArgs e)
        {
            //! Disable the 'has ainame' checkbox when the user selected actionlist for search type
            checkBoxHasAiName.Enabled = comboBoxSearchType.SelectedIndex != 8;
            listViewEntryResults.Columns.Clear();
            bool previousSearchForAreaTrigger = _previousSearchType == 6 || _previousSearchType == 7;

            switch (comboBoxSearchType.SelectedIndex)
            {
                case 0: //! Creature entry
                case 2: //! Creature guid
                case 3: //! Gameobject entry
                case 5: //! Gameobject guid
                case 8: //! Actionlist
                    if (previousSearchForAreaTrigger)
                    {
                        StopRunningThread();
                        listViewEntryResults.Items.Clear();
                    }

                    textBoxCriteria.Text = Regex.Replace(textBoxCriteria.Text, "[^.0-9]", "");
                    listViewEntryResults.Columns.Add("Entry/guid", 70, HorizontalAlignment.Right);
                    listViewEntryResults.Columns.Add("Name", 260, HorizontalAlignment.Left);
                    break;
                case 1: //! Creature name
                case 4: //! Gameobject name
                    if (previousSearchForAreaTrigger)
                    {
                        StopRunningThread();
                        listViewEntryResults.Items.Clear();
                    }

                    listViewEntryResults.Columns.Add("Entry/guid", 70, HorizontalAlignment.Right);
                    listViewEntryResults.Columns.Add("Name", 260, HorizontalAlignment.Left);
                    break;
                case 6: //! Areatrigger id
                case 7: //! Areatrigger map id
                    if (!previousSearchForAreaTrigger)
                    {
                        StopRunningThread();
                        listViewEntryResults.Items.Clear();
                    }

                    textBoxCriteria.Text = Regex.Replace(textBoxCriteria.Text, "[^.0-9]", "");
                    listViewEntryResults.Columns.Add("Id", 53, HorizontalAlignment.Right);
                    listViewEntryResults.Columns.Add("Mapid", 52, HorizontalAlignment.Left);
                    listViewEntryResults.Columns.Add("X", 75, HorizontalAlignment.Left);
                    listViewEntryResults.Columns.Add("Y", 75, HorizontalAlignment.Left);
                    listViewEntryResults.Columns.Add("Z", 75, HorizontalAlignment.Left);
                    break;
            }

            _previousSearchType = comboBoxSearchType.SelectedIndex;
        }

        private void SearchForEntryForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopRunningThread();
        }
    }
}
