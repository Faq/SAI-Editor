﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using SAI_Editor.Classes;

namespace SAI_Editor.Forms.SearchForms
{
    public enum DatabaseSearchFormType
    {
        DatabaseSearchFormTypeSpell,
        DatabaseSearchFormTypeFaction,
        DatabaseSearchFormTypeEmote,
        DatabaseSearchFormTypeQuest,
        DatabaseSearchFormTypeMap,
        DatabaseSearchFormTypeAreaOrZone,
        DatabaseSearchFormTypeCreatureEntry,
        DatabaseSearchFormTypeGameobjectEntry,
        DatabaseSearchFormTypeSound,
        DatabaseSearchFormTypeAreaTrigger,
        DatabaseSearchFormTypeCreatureGuid,
        DatabaseSearchFormTypeGameobjectGuid,
        DatabaseSearchFormTypeGameEvent,
        DatabaseSearchFormTypeItemEntry,
        DatabaseSearchFormTypeSummonsId,
        DatabaseSearchFormTypeTaxiPath,
        DatabaseSearchFormTypeEquipTemplate,
        DatabaseSearchFormTypeWaypoint,
        DatabaseSearchFormTypeNpcText,
        DatabaseSearchFormTypeGossipMenuOptionMenuId,
        DatabaseSearchFormTypeGossipMenuOptionId,
        DatabaseSearchFormTypeSkill,
        DatabaseSearchFormTypeAchievement,
        DatabaseSearchFormTypePlayerTitles,
        DatabaseSearchFormTypeSmartScriptId,
        DatabaseSearchFormTypeSmartScriptEntryOrGuid,
        DatabaseSearchFormTypeSmartScriptSourceType,
        DatabaseSearchFormTypeVendorEntry,
        DatabaseSearchFormTypeVendorItemEntry,
        DatabaseSearchFormTypeLootTemplateBase,

        DatabaseSearchFormTypeCreatureLootTemplateEntry,
        DatabaseSearchFormTypeDisenchantLootTemplateEntry,
        DatabaseSearchFormTypeFishingLootTemplateEntry,
        DatabaseSearchFormTypeGameobjectLootTemplateEntry,
        DatabaseSearchFormTypeItemLootTemplateEntry,
        DatabaseSearchFormTypeMailLootTemplateEntry,
        DatabaseSearchFormTypeMillingLootTemplateEntry,
        DatabaseSearchFormTypePickpocketingLootTemplateEntry,
        DatabaseSearchFormTypeProspectingLootTemplateEntry,
        DatabaseSearchFormTypeReferenceLootTemplateEntry,
        DatabaseSearchFormTypeSkinningLootTemplateEntry,
        DatabaseSearchFormTypeSpellLootTemplateEntry,

        DatabaseSearchFormTypeCreatureLootTemplateItem,
        DatabaseSearchFormTypeDisenchantLootTemplateItem,
        DatabaseSearchFormTypeFishingLootTemplateItem,
        DatabaseSearchFormTypeGameobjectLootTemplateItem,
        DatabaseSearchFormTypeItemLootTemplateItem,
        DatabaseSearchFormTypeMailLootTemplateItem,
        DatabaseSearchFormTypeMillingLootTemplateItem,
        DatabaseSearchFormTypePickpocketingLootTemplateItem,
        DatabaseSearchFormTypeProspectingLootTemplateItem,
        DatabaseSearchFormTypeReferenceLootTemplateItem,
        DatabaseSearchFormTypeSkinningLootTemplateItem,
        DatabaseSearchFormTypeSpellLootTemplateItem,
    }

    public partial class SearchFromDatabaseForm : Form
    {
        private Thread _searchThread = null;
        private readonly ListViewColumnSorter _lvwColumnSorter = new ListViewColumnSorter();
        private readonly TextBox _textBoxToChange = null;
        private readonly DatabaseSearchFormType _databaseSearchFormType;
        private int _amountOfListviewColumns = 2, _listViewItemIndexToCopy = 0;
        private string _baseQuery = String.Empty;
        private string[] _columns = new string[7];
        private bool _useWorldDatabase = false;

        public SearchFromDatabaseForm(TextBox textBoxToChange, DatabaseSearchFormType databaseSearchFormType)
        {
            InitializeComponent();
            this._databaseSearchFormType = databaseSearchFormType;

            MinimumSize = new Size(Width, Height);
            MaximumSize = new Size(Width, Height + 800);

            //! If the textboxtochange is null, the search form is called from a place where it's just searching and not using
            //! the search results anywhere.
            if (textBoxToChange != null)
            {
                this._textBoxToChange = textBoxToChange;
                textBoxCriteria.Text = textBoxToChange.Text;
            }

            string lootTemplateKey = databaseSearchFormType.ToString().Replace("DatabaseSearchFormType", "").Replace("LootTemplateEntry", "").Replace("LootTemplateItem", "");

            switch (databaseSearchFormType)
            {
                case DatabaseSearchFormType.DatabaseSearchFormTypeSpell:
                    Text = "Search for a spell";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Spell id");
                    comboBoxSearchType.Items.Add("Spell name");
                    _baseQuery = "SELECT id, spellName FROM " + SAI_Editor_Manager.GetSpellTableName();
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeFaction:
                    Text = "Search for a faction";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Faction id");
                    comboBoxSearchType.Items.Add("Faction name");
                    _baseQuery = "SELECT m_ID, m_name_lang_1 FROM " + SAI_Editor_Manager.GetFactionsTableName();
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeEmote:
                    Text = "Search for an emote";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Emote id");
                    comboBoxSearchType.Items.Add("Emote name");
                    _baseQuery = "SELECT id, emote FROM " + SAI_Editor_Manager.GetEmotesTableName();
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeQuest:
                    Text = "Search for a quest";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Quest id");
                    comboBoxSearchType.Items.Add("Quest name");
                    _baseQuery = "SELECT id, title FROM quest_template";
                    _useWorldDatabase = true;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeMap:
                    Text = "Search for a map id";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Map id");
                    comboBoxSearchType.Items.Add("Map name");
                    _baseQuery = "SELECT m_ID, m_MapName_lang1 FROM " + SAI_Editor_Manager.GetMapsTableName();
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeAreaOrZone:
                    Text = "Search for a zone id";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Zone id");
                    comboBoxSearchType.Items.Add("Zone name");
                    _baseQuery = "SELECT m_ID, m_AreaName_lang FROM " + SAI_Editor_Manager.GetAreasAndZonesTableName();
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeCreatureEntry:
                    Text = "Search for a creature";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Creature entry");
                    comboBoxSearchType.Items.Add("Creature name");
                    _baseQuery = "SELECT entry, name FROM creature_template";
                    _useWorldDatabase = true;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeGameobjectEntry:
                    Text = "Search for a gameobject";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Gameobject entry");
                    comboBoxSearchType.Items.Add("Gameobject name");
                    _baseQuery = "SELECT entry, name FROM gameobject_template";
                    _useWorldDatabase = true;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeSound:
                    Text = "Search for a sound id";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Sound id");
                    comboBoxSearchType.Items.Add("Sound name");
                    _baseQuery = "SELECT id, name FROM " + SAI_Editor_Manager.GetSoundEntriesTableName();
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeAreaTrigger:
                    Text = "Search for a sound id";
                    listViewEntryResults.Columns.Add("Id", 52);
                    listViewEntryResults.Columns.Add("Mapid", 52);
                    listViewEntryResults.Columns.Add("X", 75);
                    listViewEntryResults.Columns.Add("Y", 75);
                    listViewEntryResults.Columns.Add("Z", 75);
                    comboBoxSearchType.Items.Add("Areatrigger id");
                    comboBoxSearchType.Items.Add("Areatrigger map id");
                    _baseQuery = "SELECT m_id, m_mapId, m_posX, m_posY, m_posZ FROM " + SAI_Editor_Manager.GetAreatriggerTableName();
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeCreatureGuid:
                    Text = "Search for a creature";
                    listViewEntryResults.Columns.Add("Guid", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Creature guid");
                    comboBoxSearchType.Items.Add("Creature name");
                    _baseQuery = "SELECT c.guid, ct.name FROM creature c JOIN creature_template ct ON ct.entry = c.id";
                    _useWorldDatabase = true;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeGameobjectGuid:
                    Text = "Search for a gameobject";
                    listViewEntryResults.Columns.Add("Guid", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Gameobject guid");
                    comboBoxSearchType.Items.Add("Gameobject name");
                    _baseQuery = "SELECT g.guid, gt.name FROM gameobject g JOIN gameobject_template gt ON gt.entry = g.id";
                    _useWorldDatabase = true;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeGameEvent:
                    Text = "Search for a game event";
                    listViewEntryResults.Columns.Add("Entry", 45);
                    listViewEntryResults.Columns.Add("Description", 284);
                    comboBoxSearchType.Items.Add("Game event entry");
                    comboBoxSearchType.Items.Add("Game event description");
                    _baseQuery = "SELECT eventEntry, description FROM game_event";
                    _useWorldDatabase = true;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeItemEntry:
                    Text = "Search for an item";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Item id");
                    comboBoxSearchType.Items.Add("Item name");
                    _baseQuery = "SELECT entry, name FROM item_template";
                    _useWorldDatabase = true;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeSummonsId:
                    Text = "Search for a summons id";
                    listViewEntryResults.Columns.Add("Owner", 63);
                    listViewEntryResults.Columns.Add("Entry", 63);
                    listViewEntryResults.Columns.Add("Group", 66);
                    listViewEntryResults.Columns.Add("Type", 66);
                    listViewEntryResults.Columns.Add("Time", 66);
                    comboBoxSearchType.Items.Add("Owner entry");
                    comboBoxSearchType.Items.Add("Target entry");
                    _baseQuery = "SELECT summonerId, entry, groupId, summonType, summonTime FROM creature_summon_groups";
                    _useWorldDatabase = true;
                    _listViewItemIndexToCopy = 2;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeTaxiPath:
                    Text = "Search for a taxi path";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Taxi id");
                    comboBoxSearchType.Items.Add("Taxi name");
                    _baseQuery = "SELECT id, taxiName FROM " + SAI_Editor_Manager.GetTaxiNodesTableName();;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeEquipTemplate:
                    Text = "Search for creature equipment";
                    listViewEntryResults.Columns.Add("Entry", 63);
                    listViewEntryResults.Columns.Add("Id", 63);
                    listViewEntryResults.Columns.Add("Item 1", 66);
                    listViewEntryResults.Columns.Add("Item 2", 66);
                    listViewEntryResults.Columns.Add("Item 3", 66);
                    comboBoxSearchType.Items.Add("Entry");
                    comboBoxSearchType.Items.Add("Item entries");
                    _baseQuery = "SELECT entry, id, itemEntry1, itemEntry2, itemEntry3 FROM creature_equip_template";
                    _useWorldDatabase = true;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeWaypoint:
                    Text = "Search for a waypoints path";
                    listViewEntryResults.Columns.Add("Entry", 52);
                    listViewEntryResults.Columns.Add("Point", 52);
                    listViewEntryResults.Columns.Add("X", 75);
                    listViewEntryResults.Columns.Add("Y", 75);
                    listViewEntryResults.Columns.Add("Z", 75);
                    comboBoxSearchType.Items.Add("Creature entry");
                    _baseQuery = "SELECT entry, pointid, position_x, position_y, position_z FROM waypoints";
                    _useWorldDatabase = true;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeNpcText:
                    Text = "Search for an npc_text entry";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Text", 284);
                    comboBoxSearchType.Items.Add("Id");
                    comboBoxSearchType.Items.Add("Text");
                    _baseQuery = "SELECT id, text0_0 FROM npc_text";
                    _useWorldDatabase = true;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeGossipMenuOptionId:
                    _listViewItemIndexToCopy = 1;
                    goto case DatabaseSearchFormType.DatabaseSearchFormTypeGossipMenuOptionMenuId;
                case DatabaseSearchFormType.DatabaseSearchFormTypeGossipMenuOptionMenuId:
                    Text = "Search for a gossip item";
                    listViewEntryResults.Columns.Add("Menu id", 55);
                    listViewEntryResults.Columns.Add("Id", 35);
                    listViewEntryResults.Columns.Add("Text", 239);
                    comboBoxSearchType.Items.Add("Menu");
                    comboBoxSearchType.Items.Add("Id");
                    comboBoxSearchType.Items.Add("Text");
                    _baseQuery = "SELECT menu_id, id, option_text FROM gossip_menu_option";
                    _useWorldDatabase = true;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeSkill:
                    Text = "Search for a skill";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Skill id");
                    comboBoxSearchType.Items.Add("Skill name");
                    _baseQuery = "SELECT id, name FROM " + SAI_Editor_Manager.GetSkillsTableName();
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeAchievement:
                    Text = "Search for an achievement";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Achievement id");
                    comboBoxSearchType.Items.Add("Achievement name");
                    _baseQuery = "SELECT id, name FROM " + SAI_Editor_Manager.GetAchievementsTableName();
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypePlayerTitles:
                    Text = "Search for a title";
                    listViewEntryResults.Columns.Add("Id", 45);
                    listViewEntryResults.Columns.Add("Name", 284);
                    comboBoxSearchType.Items.Add("Title id");
                    comboBoxSearchType.Items.Add("Title");
                    _baseQuery = "SELECT id, title FROM " + SAI_Editor_Manager.GetPlayerTitlesTableName();
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeSmartScriptId:
                    _listViewItemIndexToCopy = 1;
                    goto case DatabaseSearchFormType.DatabaseSearchFormTypeSmartScriptEntryOrGuid;
                case DatabaseSearchFormType.DatabaseSearchFormTypeSmartScriptSourceType:
                    _listViewItemIndexToCopy = 2;
                    goto case DatabaseSearchFormType.DatabaseSearchFormTypeSmartScriptEntryOrGuid;
                case DatabaseSearchFormType.DatabaseSearchFormTypeSmartScriptEntryOrGuid:
                    _useWorldDatabase = true;
                    Text = "Search for a smart script";
                    listViewEntryResults.Columns.Add("Entryorguid", 75);
                    listViewEntryResults.Columns.Add("Id", 75);
                    listViewEntryResults.Columns.Add("Sourcetype", 75);
                    listViewEntryResults.Columns.Add("Event", 52);
                    listViewEntryResults.Columns.Add("Action", 52);
                    comboBoxSearchType.Items.Add("Smart script id");
                    comboBoxSearchType.Items.Add("Smart script entryorguid");
                    _baseQuery = "SELECT entryorguid, id, source_type, event_type, action_type FROM smart_scripts";
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeVendorItemEntry:
                    _listViewItemIndexToCopy = 1;
                    goto case DatabaseSearchFormType.DatabaseSearchFormTypeVendorEntry;
                case DatabaseSearchFormType.DatabaseSearchFormTypeVendorEntry:
                    Text = "Search for a vendor (item) entry";
                    listViewEntryResults.Columns.Add("Vendor entry", 164);
                    listViewEntryResults.Columns.Add("Item entry", 164);
                    comboBoxSearchType.Items.Add("Vendor entry");
                    comboBoxSearchType.Items.Add("Vendor item entry");
                    _baseQuery = "SELECT entry, item FROM npc_vendor";
                    _useWorldDatabase = true;
                    break;
                case DatabaseSearchFormType.DatabaseSearchFormTypeCreatureLootTemplateItem:
                case DatabaseSearchFormType.DatabaseSearchFormTypeDisenchantLootTemplateItem:
                case DatabaseSearchFormType.DatabaseSearchFormTypeFishingLootTemplateItem:
                case DatabaseSearchFormType.DatabaseSearchFormTypeGameobjectLootTemplateItem:
                case DatabaseSearchFormType.DatabaseSearchFormTypeItemLootTemplateItem:
                case DatabaseSearchFormType.DatabaseSearchFormTypeMailLootTemplateItem:
                case DatabaseSearchFormType.DatabaseSearchFormTypeMillingLootTemplateItem:
                case DatabaseSearchFormType.DatabaseSearchFormTypePickpocketingLootTemplateItem:
                case DatabaseSearchFormType.DatabaseSearchFormTypeProspectingLootTemplateItem:
                case DatabaseSearchFormType.DatabaseSearchFormTypeReferenceLootTemplateItem:
                case DatabaseSearchFormType.DatabaseSearchFormTypeSkinningLootTemplateItem:
                case DatabaseSearchFormType.DatabaseSearchFormTypeSpellLootTemplateItem:
                    _listViewItemIndexToCopy = 1;
                    goto case DatabaseSearchFormType.DatabaseSearchFormTypeLootTemplateBase;
                case DatabaseSearchFormType.DatabaseSearchFormTypeDisenchantLootTemplateEntry:
                case DatabaseSearchFormType.DatabaseSearchFormTypeCreatureLootTemplateEntry:
                case DatabaseSearchFormType.DatabaseSearchFormTypeFishingLootTemplateEntry:
                case DatabaseSearchFormType.DatabaseSearchFormTypeGameobjectLootTemplateEntry:
                case DatabaseSearchFormType.DatabaseSearchFormTypeItemLootTemplateEntry:
                case DatabaseSearchFormType.DatabaseSearchFormTypeMailLootTemplateEntry:
                case DatabaseSearchFormType.DatabaseSearchFormTypeMillingLootTemplateEntry:
                case DatabaseSearchFormType.DatabaseSearchFormTypePickpocketingLootTemplateEntry:
                case DatabaseSearchFormType.DatabaseSearchFormTypeProspectingLootTemplateEntry:
                case DatabaseSearchFormType.DatabaseSearchFormTypeReferenceLootTemplateEntry:
                case DatabaseSearchFormType.DatabaseSearchFormTypeSkinningLootTemplateEntry:
                case DatabaseSearchFormType.DatabaseSearchFormTypeSpellLootTemplateEntry:
                case DatabaseSearchFormType.DatabaseSearchFormTypeLootTemplateBase:
                    Text = "Search for a loot template";
                    listViewEntryResults.Columns.Add("Loot entry", 164);
                    listViewEntryResults.Columns.Add("Item entry", 164);
                    comboBoxSearchType.Items.Add("Loot entry");
                    comboBoxSearchType.Items.Add("Item entry");
                    _baseQuery = "SELECT entry, item FROM " + lootTemplateKey + "_loot_template";
                    _useWorldDatabase = true;
                    break;
                default:
                    MessageBox.Show("Unknown database search type!", "Something went wrong...", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;
            }

            string[] columnsSplit = _baseQuery.Replace("SELECT ", "").Replace(" FROM " + _baseQuery.Split(' ').LastOrDefault(), "").Split(',');
            _columns = new string[columnsSplit.Length + 1];

            for (int i = 0; i < columnsSplit.Length; ++i)
                _columns[i] = columnsSplit[i];

            _amountOfListviewColumns = _columns.Length - 1;
            comboBoxSearchType.SelectedIndex = 0;
            FillListView(true);
        }

        private void listViewEntryResults_DoubleClick(object sender, EventArgs e)
        {
            //! If the textboxtochange is null, the search form is called from a place where it's just searching and not using
            //! the search results anywhere.
            if (_textBoxToChange == null)
                return;

            StopRunningThread();

            if (_listViewItemIndexToCopy > 0)
                _textBoxToChange.Text = listViewEntryResults.SelectedItems[0].SubItems[_listViewItemIndexToCopy].Text;
            else
                _textBoxToChange.Text = listViewEntryResults.SelectedItems[0].Text;

            Close();
        }

        private async void FillListView(bool limit = false)
        {
            try
            {
                string queryToExecute = _baseQuery;

                if (!limit)
                {
                    switch (GetSelectedIndexOfComboBox(comboBoxSearchType))
                    {
                        case 0: //! First column
                            if (String.IsNullOrWhiteSpace(textBoxCriteria.Text))
                                break;

                            if (checkBoxFieldContainsCriteria.Checked)
                                queryToExecute += " WHERE " + _columns[0] + " LIKE '%" + textBoxCriteria.Text + "%'";
                            else
                                queryToExecute += " WHERE " + _columns[0] + " = '" + textBoxCriteria.Text + "'";

                            break;
                        case 1: //! Second column
                            switch (_databaseSearchFormType)
                            {
                                case DatabaseSearchFormType.DatabaseSearchFormTypeEquipTemplate:
                                {
                                    if (checkBoxFieldContainsCriteria.Checked)
                                        queryToExecute += " WHERE itemEntry1 LIKE '%" + textBoxCriteria.Text + "%' OR itemEntry2 LIKE '%" + textBoxCriteria.Text + "%' OR itemEntry3 LIKE '%" + textBoxCriteria.Text + "%'";
                                    else
                                        queryToExecute += " WHERE itemEntry1 = " + textBoxCriteria.Text + " OR itemEntry2 = " + textBoxCriteria.Text + " OR itemEntry3 = " + textBoxCriteria.Text;

                                    break;
                                }
                                default:
                                {
                                    if (String.IsNullOrWhiteSpace(textBoxCriteria.Text))
                                        break;

                                    if (checkBoxFieldContainsCriteria.Checked)
                                        queryToExecute += " WHERE " + _columns[1] + " LIKE '%" + textBoxCriteria.Text + "%'";
                                    else
                                        queryToExecute += " WHERE " + _columns[1] + " = '" + textBoxCriteria.Text + "'";

                                    break;
                                }
                            }
                            break;
                        case 2: //! Third column
                            if (String.IsNullOrWhiteSpace(textBoxCriteria.Text))
                                break;

                            if (checkBoxFieldContainsCriteria.Checked)
                                queryToExecute += " WHERE " + _columns[2] + " LIKE '%" + textBoxCriteria.Text + "%'";
                            else
                                queryToExecute += " WHERE " + _columns[2] + " = '" + textBoxCriteria.Text + "'";

                            break;
                        default:
                            return;
                    }

                    if (_databaseSearchFormType == DatabaseSearchFormType.DatabaseSearchFormTypeAreaOrZone && !String.IsNullOrWhiteSpace(textBoxCriteria.Text))
                        queryToExecute += " AND m_ParentAreaID = 0";
                }
                else if (_databaseSearchFormType == DatabaseSearchFormType.DatabaseSearchFormTypeAreaOrZone)
                    queryToExecute += " WHERE m_ParentAreaID = 0";

                queryToExecute += " ORDER BY " + _columns[0];

                if (limit)
                    queryToExecute += " LIMIT 1000";

                DataTable dt = await SAI_Editor_Manager.Instance.ExecuteQuery(_useWorldDatabase, queryToExecute);

                if (dt.Rows.Count > 0)
                {
                    List<ListViewItem> items = new List<ListViewItem>();

                    foreach (DataRow row in dt.Rows)
                    {
                        ListViewItem listViewItem = new ListViewItem(row.ItemArray[0].ToString());

                        for (int i = 1; i < _amountOfListviewColumns; ++i)
                            listViewItem.SubItems.Add(row.ItemArray[i].ToString());

                        items.Add(listViewItem);
                    }

                    //listViewEntryResults.Items.AddRange(items.ToArray());
                    AddItemRangeToListView(listViewEntryResults, items.ToArray());
                }
            }
            catch (ObjectDisposedException)
            {

            }
            catch
            {
                MessageBox.Show("The data could not be read from the database.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void buttonSearch_Click(object sender, EventArgs e)
        {
            _searchThread = new Thread(StartSearching);
            _searchThread.Start();
        }

        private void StartSearching()
        {
            try
            {
                SetEnabledOfControl(buttonSearch, false);
                SetEnabledOfControl(buttonStopSearching, true);
                ClearItemsOfListView(listViewEntryResults);

                try
                {
                    FillListView();
                }
                finally
                {
                    SetEnabledOfControl(buttonSearch, true);
                    SetEnabledOfControl(buttonStopSearching, false);
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
                MessageBox.Show("Something went wrong while trying to search for items.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SearchFromDatabaseForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                {
                    //! If the textboxtochange is null, the search form is called from a place where it's just searching and not using
                    //! the search results anywhere.
                    if (_textBoxToChange != null && listViewEntryResults.SelectedItems.Count > 0 && listViewEntryResults.Focused)
                    {
                        if (_listViewItemIndexToCopy > 0)
                            _textBoxToChange.Text = listViewEntryResults.SelectedItems[0].SubItems[_listViewItemIndexToCopy].Text;
                        else
                            _textBoxToChange.Text = listViewEntryResults.SelectedItems[_listViewItemIndexToCopy].Text;

                        Close();
                    }
                    else if (buttonSearch.Enabled)
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
            if (_searchThread != null && _searchThread.IsAlive)
            {
                _searchThread.Abort();
                _searchThread = null;
            }
        }

        private void comboBoxSearchType_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (Char.IsLetter(e.KeyChar) || Char.IsDigit(e.KeyChar))
                e.Handled = true; //! Disallow changing content of the combobox, but setting it to 3D looks like shit
        }

        private int GetSelectedIndexOfComboBox(ComboBox comboBox)
        {
            if (comboBox.InvokeRequired)
                return (int)comboBox.Invoke(new Func<int>(() => GetSelectedIndexOfComboBox(comboBox)));

            return comboBox.SelectedIndex;
        }

        private void AddItemRangeToListView(ListView listView, ListViewItem[] items)
        {
            if (listView.InvokeRequired)
            {
                Invoke((MethodInvoker)(() => listView.Items.AddRange(items)));
                return;
            }

            listView.Items.AddRange(items);
        }

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

        private void SearchFromDatabaseForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopRunningThread();
        }
    }
}
