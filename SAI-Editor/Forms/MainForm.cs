using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using MySql.Data.MySqlClient;
using SAI_Editor.Classes;
using SAI_Editor.Classes.Database.Classes;
using SAI_Editor.Enumerators;
using SAI_Editor.Forms.SearchForms;
using SAI_Editor.Properties;
using System.IO;
using System.Reflection;
using System.Net;
using System.Threading;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using SAI_Editor.Classes.CustomControls;
using SAI_Editor.Classes.Serialization;

namespace SAI_Editor.Forms
{
    public partial class MainForm : Form
    {
        public int ExpandAndContractSpeed = 5, LastSelectedWorkspaceIndex = 0;
        public bool RunningConstructor = false;
        private bool _contractingToLoginForm = false, _expandingToMainForm = false, _adjustedLoginSettings = false;
        private int _originalHeight = 0, _originalWidth = 0, _oldWidthTabControlWorkspaces = 0, _oldHeightTabControlWorkspaces = 0;
        private int MainFormWidth = (int)SaiEditorSizes.MainFormWidth, MainFormHeight = (int)SaiEditorSizes.MainFormHeight;
        private List<SmartScript> _lastDeletedSmartScripts = new List<SmartScript>(), _smartScriptsOnClipBoard = new List<SmartScript>();
        private Thread _updateSurveyThread = null, _checkIfUpdatesAvailableThread = null;
        private string _applicationVersion = String.Empty;
        private System.Windows.Forms.Timer _timerCheckForInternetConnection = new System.Windows.Forms.Timer();

        public UserControlSAI UserControl = null;

        public MainForm()
        {
            InitializeComponent();

            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            ResizeRedraw = true;
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            RunningConstructor = true;

            Version version = Assembly.GetExecutingAssembly().GetName().Version;
            _applicationVersion = "v" + version.Major + "." + version.Minor + "." + version.Build;
            SetFormTitle("SAI-Editor " + _applicationVersion + ": Login");

            menuStrip.Visible = false; //! Doing this in main code so we can actually see the menustrip in designform
            pictureBoxDonate.Visible = false;

            //ImageList imgList = new ImageList();
            //imgList.Images.Add()
            //imgList.TransparentColor = Color.White;
            //pictureBoxDonate.Image = imgList.Images[0];

            Width = (int)SaiEditorSizes.LoginFormWidth;
            Height = (int)SaiEditorSizes.LoginFormHeight;

            _originalHeight = Height;
            _originalWidth = Width;

            if (Settings.Default.LastFormExtraWidth > 0)
                MainFormWidth += Settings.Default.LastFormExtraWidth;

            if (Settings.Default.LastFormExtraHeight > 0)
                MainFormHeight += Settings.Default.LastFormExtraHeight;

            if (MainFormWidth > SystemInformation.VirtualScreen.Width)
                MainFormWidth = SystemInformation.VirtualScreen.Width;

            if (MainFormHeight > SystemInformation.VirtualScreen.Height)
                MainFormHeight = SystemInformation.VirtualScreen.Height;

            tabControlWorkspaces.DisplayStyle = TabStyle.VisualStudio;
            tabControlWorkspaces.DisplayStyleProvider.ShowTabCloser = true;

            //! HAS to be called before try-catch block
            tabControlWorkspaces.TabPages.Clear(); //! We only have it in the designer to get an idea of how stuff looks

            tabControlWorkspaces.Visible = false;
            customPanelLogin.Visible = true;

            customPanelLogin.Location = new Point(9, 8);

            if (_oldWidthTabControlWorkspaces == 0)
                _oldWidthTabControlWorkspaces = (int)SaiEditorSizes.TabControlWorkspaceWidth;

            if (_oldHeightTabControlWorkspaces == 0)
                _oldHeightTabControlWorkspaces = (int)SaiEditorSizes.TabControlWorkspaceHeight;

            //! We first load the information and then change the parameter fields
            await SAI_Editor_Manager.Instance.LoadSQLiteDatabaseInfo();

            if (Settings.Default.HidePass)
                textBoxPassword.PasswordChar = '●';

            _timerCheckForInternetConnection.Interval = 600000; //! 10 minutes
            _timerCheckForInternetConnection.Tick += TimerCheckForInternetConnection_Tick;
            _timerCheckForInternetConnection.Enabled = false;

            if (!Settings.Default.InformedAboutSurvey)
            {
                string termsArgeementString = "By clicking 'Yes' you agree to the application keeping a record of the usage in a remote database. Keep " +
                                                "in mind that this data will not be disclosed to a third party.";

                DialogResult result = MessageBox.Show(termsArgeementString, "Agree to the terms", MessageBoxButtons.YesNo, MessageBoxIcon.Information);

                if (result != DialogResult.Yes)
                {
                    //! Not running this in a diff thread because we want this to complete before exiting.
                    using (WebClient client = new WebClient())
                    {
                        try
                        {
                            client.DownloadData("http://www.jasper-rietrae.com/SAI-Editor/survey.php?agreed=false");
                        }
                        catch
                        {

                        }
                    }
                }

                Settings.Default.InformedAboutSurvey = true;
                Settings.Default.AgreedToSurvey = result == DialogResult.Yes;
                Settings.Default.Save();
            }

            _updateSurveyThread = new Thread(UpdateSurvey);
            _updateSurveyThread.Start();

            _checkIfUpdatesAvailableThread = new Thread(CheckIfUpdatesAvailable);
            _checkIfUpdatesAvailableThread.Start();

            try
            {
                textBoxHost.Text = Settings.Default.Host;
                textBoxUsername.Text = Settings.Default.User;
                textBoxPassword.Text = SAI_Editor_Manager.Instance.GetPasswordSetting();
                textBoxWorldDatabase.Text = Settings.Default.Database;
                textBoxPort.Text = Settings.Default.Port > 0 ? Settings.Default.Port.ToString() : String.Empty;
                ExpandAndContractSpeed = Settings.Default.AnimationSpeed;
                radioButtonConnectToMySql.Checked = Settings.Default.UseWorldDatabase;
                radioButtonDontUseDatabase.Checked = !Settings.Default.UseWorldDatabase;
                SAI_Editor_Manager.Instance.Expansion = (WowExpansion)Settings.Default.WowExpansionIndex;

                menuItemRevertQuery.Enabled = Settings.Default.UseWorldDatabase;
                searchForAQuestToolStripMenuItem1.Enabled = Settings.Default.UseWorldDatabase;
                searchForACreatureEntryToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForACreatureGuidToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAGameobjectEntryToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAGameobjectGuidToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAGameEventToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAnItemEntryToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForACreatureSummonsIdToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAnEquipmentTemplateToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAWaypointToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForANpcTextToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAGossipMenuOptionToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                searchForAGossipOptionIdToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
                _adjustedLoginSettings = true;
            }
            catch (Exception)
            {
                MessageBox.Show("Something went wrong when loading the settings.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            Dictionary<int, SAIUserControlState> states = null;

            if (tabControlWorkspaces.TabPages.Count == 0)
            {
                CreateTabControl(true);
                tabControlWorkspaces.SelectedIndex = 0;

                if (!String.IsNullOrWhiteSpace(Settings.Default.LastStaticInfoPerTab))
                {
                    states = SAIUserControlState.StatesFromJson(Settings.Default.LastStaticInfoPerTab, UserControl);

                    if (states != null && states.Count > 0)
                    {
                        UserControl.States.Add(states.First().Value);
                        UserControl.CurrentState = states.First().Value;
                    }
                }
                else
                {
                    UserControl.States.Add(UserControl.DefaultState);
                    UserControl.CurrentState = UserControl.DefaultState;
                }
            }

            try
            {
                UserControl.checkBoxListActionlistsOrEntries.Enabled = Settings.Default.UseWorldDatabase;
                UserControl.buttonGenerateComments.Enabled = UserControl.ListView.Items.Count > 0 && Settings.Default.UseWorldDatabase;
                UserControl.buttonSearchForEntryOrGuid.Enabled = Settings.Default.UseWorldDatabase;
                menuItemGenerateComment.Enabled = UserControl.ListView.Items.Count > 0 && Settings.Default.UseWorldDatabase;
            }
            catch (Exception)
            {
                MessageBox.Show("Something went wrong when loading the settings.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            UserControl.tabControlParameters.AutoScrollOffset = new Point(5, 5);

            //! Static scrollbar to the parameters tabpage windows
            foreach (TabPage page in UserControl.tabControlParameters.TabPages)
            {
                page.HorizontalScroll.Enabled = false;
                page.HorizontalScroll.Visible = false;
                page.AutoScroll = true;
                page.AutoScrollMinSize = new Size(page.Width, page.Height);
            }

            if (!String.IsNullOrEmpty(Settings.Default.LastStaticInfoPerTab) && states != null)
            {
                int ctr = 0;

                foreach (var kvp in states.Skip(1))
                {
                    UserControl.States.Add(kvp.Value);

                    CreateTabControl();
                    ctr++;
                }

                tabControlWorkspaces.SelectedIndex = 0;
                UserControl.States.First().Load();
                UserControl.CurrentState = UserControl.States.First();
            }

            if (Settings.Default.AutoConnect)
            {
                checkBoxAutoConnect.Checked = true;

                if (Settings.Default.UseWorldDatabase)
                {
                    SAI_Editor_Manager.Instance.connString = new MySqlConnectionStringBuilder
                    {
                        Server = Settings.Default.Host,
                        UserID = Settings.Default.User,
                        Port = Settings.Default.Port,
                        Database = Settings.Default.Database
                    };

                    if (Settings.Default.Password.Length > 0)
                        SAI_Editor_Manager.Instance.connString.Password = SAI_Editor_Manager.Instance.GetPasswordSetting();// Settings.Default.Password.ToSecureString().EncryptString(Encoding.Unicode.GetBytes(Settings.Default.Entropy));

                    SAI_Editor_Manager.Instance.ResetWorldDatabase(true);
                }

                if (!Settings.Default.UseWorldDatabase || SAI_Editor_Manager.Instance.WorldDatabase.CanConnectToDatabase(SAI_Editor_Manager.Instance.connString, false))
                {
                    SAI_Editor_Manager.Instance.ResetWorldDatabase(Settings.Default.UseWorldDatabase);
                    buttonConnect.PerformClick();

                    if (Settings.Default.InstantExpand)
                        StartExpandingToMainForm(true);
                }
            }

            RunningConstructor = false;
        }

        private void SetSizable(bool sizable)
        {
            if (sizable)
            {
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.Sizable;
                MinimumSize = new Size(966, 542);
                tabControlWorkspaces.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom | AnchorStyles.Top;
            }
            else
            {
                FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
                MinimumSize = new Size(0, 0);
                tabControlWorkspaces.Anchor = AnchorStyles.Left | AnchorStyles.Top;
            }
        }

        protected override CreateParams CreateParams
        {
            get
            {
                CreateParams handleParam = base.CreateParams;
                handleParam.ExStyle |= 0x02000000; // WS_EX_COMPOSITED       
                return handleParam;
            }
        }

        protected override void WndProc(ref Message m)
        {
            //! Don't allow moving the window while we are expanding or contracting. This is required because
            //! the window often breaks and has an incorrect size in the end if the application had been moved
            //! while expanding or contracting.
            if (((m.Msg == 274 && m.WParam.ToInt32() == 61456) || (m.Msg == 161 && m.WParam.ToInt32() == 2)) && (_expandingToMainForm || _contractingToLoginForm))
                return;

            base.WndProc(ref m);
        }

        private void UpdateSurvey()
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    string url = "http://www.jasper-rietrae.com/SAI-Editor/survey.php?";

                    if (!Settings.Default.AgreedToSurvey)
                        url += "agreed=false";
                    else
                        url += "version=" + _applicationVersion.Replace('.', '-');

                    client.DownloadData(url);
                }
                catch (ThreadAbortException)
                {

                }
                catch (WebException)
                {
                    //! Try to connect to google.com. If it can't connect, it means no internet connection
                    //! is available. We then start a timer which checks for an internet connection every
                    //! 10 minutes.
                    if (!SAI_Editor_Manager.Instance.HasInternetConnection())
                        _timerCheckForInternetConnection.Enabled = true;
                }
                catch (Exception ex)
                {
                    //! Run the messagebox on the mainthread
                    Invoke(new Action(() =>
                    {
                        MessageBox.Show("Something went wrong while attempting to keep track of the use count. Please report the following message to developers:\n\n" + ex.ToString(), "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            }
        }

        private void CheckIfUpdatesAvailable()
        {
            using (WebClient client = new WebClient())
            {
                try
                {
                    using (Stream streamVersion = client.OpenRead("http://dl.dropbox.com/u/84527004/SAI-Editor/version.txt"))
                    {
                        if (streamVersion != null)
                        {
                            using (StreamReader streamReaderVersion = new StreamReader(streamVersion))
                            {
                                string newAppVersionStr = streamReaderVersion.ReadToEnd();
                                int newAppVersion = CustomConverter.ToInt32(newAppVersionStr.Replace("v", String.Empty).Replace(".", String.Empty));
                                int currAppVersion = CustomConverter.ToInt32(_applicationVersion.Replace("v", String.Empty).Replace(".", String.Empty));

                                if (newAppVersion > 0 && currAppVersion > 0 && newAppVersion > currAppVersion)
                                {
                                    //! Run the messagebox in the mainthread
                                    Invoke(new Action(() =>
                                    {
                                        DialogResult result = MessageBox.Show(this, "A new version of the application is available (" + newAppVersionStr + "). Do you wish to go to the download page?", "New version available!", MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk);

                                        if (result == DialogResult.Yes)
                                            SAI_Editor_Manager.Instance.StartProcess("http://www.trinitycore.org/f/files/file/17-sai-editor/");
                                    }));
                                }
                            }
                        }
                    }
                }
                catch (ThreadAbortException)
                {

                }
                catch (WebException)
                {
                    //! Try to connect to google.com. If it can't connect, it means no internet connection
                    //! is available. We then start a timer which checks for an internet connection every
                    //! 10 minutes.
                    if (!SAI_Editor_Manager.Instance.HasInternetConnection())
                        _timerCheckForInternetConnection.Enabled = true;
                }
                catch (Exception ex)
                {
                    Invoke(new Action(() =>
                    {
                        MessageBox.Show("Something went wrong while checking for updates. Please report the following message to developers:\n\n" + ex.Message, "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }));
                }
            }
        }

        [DllImportAttribute("user32.dll")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImportAttribute("user32.dll")]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        [DllImportAttribute("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        private void TimerCheckForInternetConnection_Tick(object sender, EventArgs e)
        {
            //! Try to connect to google.com. If it can't connect, it means no internet connection
            //! is available.
            if (SAI_Editor_Manager.Instance.HasInternetConnection())
            {
                _timerCheckForInternetConnection.Enabled = false;
                _checkIfUpdatesAvailableThread.Start();
                _updateSurveyThread.Start();
            }
        }

        private void TimerExpandOrContract_Tick(object sender, EventArgs e)
        {
            if (_expandingToMainForm)
            {
                MaximumSize = new Size(0, 0);
                MinimumSize = new Size(0, 0);

                if (Height < MainFormHeight)
                {
                    Height += ExpandAndContractSpeed;

                    if (Height > MainFormHeight)
                        TimerExpandOrContract_Tick(sender, e);
                }
                else
                {
                    Height = MainFormHeight;

                    if (Width >= MainFormWidth && timerExpandOrContract.Enabled) //! If both finished
                    {
                        Width = MainFormWidth;
                        timerExpandOrContract.Enabled = false;
                        _expandingToMainForm = false;
                        FinishedExpandingOrContracting(true);
                    }
                }

                if (Width < MainFormWidth)
                {
                    Width += ExpandAndContractSpeed;

                    if (Width > MainFormWidth)
                        TimerExpandOrContract_Tick(sender, e);
                }
                else
                {
                    Width = MainFormWidth;

                    if (Height >= MainFormHeight && timerExpandOrContract.Enabled) //! If both finished
                    {
                        Height = MainFormHeight;
                        timerExpandOrContract.Enabled = false;
                        _expandingToMainForm = false;
                        FinishedExpandingOrContracting(true);
                    }
                }
            }
            else if (_contractingToLoginForm)
            {
                if (Height > _originalHeight)
                    Height -= ExpandAndContractSpeed;
                else
                {
                    Height = _originalHeight;

                    if (Width <= _originalWidth && timerExpandOrContract.Enabled) //! If both finished
                    {
                        Width = _originalWidth;
                        timerExpandOrContract.Enabled = false;
                        _contractingToLoginForm = false;
                        FinishedExpandingOrContracting(false);
                    }
                }

                if (Width > _originalWidth)
                    Width -= ExpandAndContractSpeed;
                else
                {
                    Width = _originalWidth;

                    if (Height <= _originalHeight && timerExpandOrContract.Enabled) //! If both finished
                    {
                        Height = _originalHeight;
                        timerExpandOrContract.Enabled = false;
                        _contractingToLoginForm = false;
                        FinishedExpandingOrContracting(false);
                    }
                }
            }

            Invalidate();
            Update();
        }

        private void ButtonConnect_Click(object sender, EventArgs e)
        {
            if (radioButtonConnectToMySql.Checked)
            {
                if (String.IsNullOrEmpty(textBoxHost.Text))
                {
                    MessageBox.Show("The host field has to be filled!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (String.IsNullOrEmpty(textBoxUsername.Text))
                {
                    MessageBox.Show("The username field has to be filled!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (textBoxPassword.Text.Length > 0 && String.IsNullOrEmpty(textBoxPassword.Text))
                {
                    MessageBox.Show("The password field can not consist of only whitespaces!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (String.IsNullOrEmpty(textBoxWorldDatabase.Text))
                {
                    MessageBox.Show("The world database field has to be filled!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (String.IsNullOrEmpty(textBoxPort.Text))
                {
                    MessageBox.Show("The port field has to be filled!", "An error has occurred!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                SAI_Editor_Manager.Instance.connString = new MySqlConnectionStringBuilder
                {
                    Server = textBoxHost.Text,
                    UserID = textBoxUsername.Text,
                    Port = CustomConverter.ToUInt32(textBoxPort.Text),
                    Database = textBoxWorldDatabase.Text
                };

                if (textBoxPassword.Text.Length > 0)
                    SAI_Editor_Manager.Instance.connString.Password = textBoxPassword.Text;

                SAI_Editor_Manager.Instance.ResetWorldDatabase(true);
            }

            buttonConnect.Enabled = false;

            Settings.Default.UseWorldDatabase = radioButtonConnectToMySql.Checked;
            Settings.Default.Save();

            if (!radioButtonConnectToMySql.Checked || SAI_Editor_Manager.Instance.WorldDatabase.CanConnectToDatabase(SAI_Editor_Manager.Instance.connString))
            {
                StartExpandingToMainForm(Settings.Default.InstantExpand);
                HandleUseWorldDatabaseSettingChanged();
            }

            buttonConnect.Enabled = true;
        }

        private void StartExpandingToMainForm(bool instant = false)
        {
            if (radioButtonConnectToMySql.Checked)
            {
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                byte[] buffer = new byte[1024];
                rng.GetBytes(buffer);
                string salt = BitConverter.ToString(buffer);
                rng.Dispose();

                Settings.Default.Entropy = salt;
                Settings.Default.Host = textBoxHost.Text;
                Settings.Default.User = textBoxUsername.Text;
                Settings.Default.Password = textBoxPassword.Text.Length == 0 ? String.Empty : textBoxPassword.Text.ToSecureString().EncryptString(Encoding.Unicode.GetBytes(salt));
                Settings.Default.Database = textBoxWorldDatabase.Text;
                Settings.Default.AutoConnect = checkBoxAutoConnect.Checked;
                Settings.Default.Port = CustomConverter.ToUInt32(textBoxPort.Text);
                Settings.Default.UseWorldDatabase = true;
                Settings.Default.Save();
            }

            ResetFieldsToDefault();

            if (radioButtonConnectToMySql.Checked)
                SetFormTitle("SAI-Editor " + _applicationVersion + " - Connection: " + textBoxUsername.Text + ", " + textBoxHost.Text + ", " + textBoxPort.Text);
            else
                SetFormTitle("SAI-Editor " + _applicationVersion + " - Creator-only mode, no database connection");

            if (instant)
            {
                Width = MainFormWidth;
                Height = MainFormHeight;
                FinishedExpandingOrContracting(true);
            }
            else
            {
                SAI_Editor_Manager.FormState = FormState.FormStateExpandingOrContracting;
                timerExpandOrContract.Enabled = true;
                _expandingToMainForm = true;
            }

            customPanelLogin.Visible = false;

            UserControl.panelStaticTooltipTypes.Visible = false;
            UserControl.panelStaticTooltipParameters.Visible = false;
        }

        private void ResetFieldsToDefault()
        {
            UserControl.ResetFieldsToDefault();
        }

        private void StartContractingToLoginForm(bool instant = false)
        {
            SetSizable(false);

            SetFormTitle("SAI-Editor " + _applicationVersion + ": Login");

            if (Settings.Default.ShowTooltipsStaticly)
                UserControl.ListView.Height += (int)SaiEditorSizes.ListViewHeightContract;

            if (instant)
            {
                Width = _originalWidth;
                Height = _originalHeight;
                FinishedExpandingOrContracting(false);
            }
            else
            {
                SAI_Editor_Manager.FormState = FormState.FormStateExpandingOrContracting;
                timerExpandOrContract.Enabled = true;
                _contractingToLoginForm = true;
            }

            tabControlWorkspaces.Visible = false;
            menuStrip.Visible = false;
            pictureBoxDonate.Visible = false;
        }

        private void ButtonClear_Click(object sender, EventArgs e)
        {
            textBoxHost.Text = String.Empty;
            textBoxUsername.Text = String.Empty;
            textBoxPassword.Text = String.Empty;
            textBoxWorldDatabase.Text = String.Empty;
            textBoxPort.Text = String.Empty;
            checkBoxAutoConnect.Checked = false;
            radioButtonConnectToMySql.Checked = true;
        }

        private void ButtonCancel_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    switch (SAI_Editor_Manager.FormState)
                    {
                        case FormState.FormStateLogin:
                            buttonConnect.PerformClick();
                            break;
                        case FormState.FormStateMain:
                            break;
                    }
                    break;
            }
        }

        private void MenuItemReconnect_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain || UserControl.ContractingListView || UserControl.ExpandingListView)
                return;

            for (int i = 0; i < Application.OpenForms.Count; ++i)
                if (Application.OpenForms[i] != this)
                    Application.OpenForms[i].Close();

            UserControl.panelStaticTooltipTypes.Visible = false;
            UserControl.panelStaticTooltipParameters.Visible = false;

            SaveLastUsedFields();
            ResetFieldsToDefault();

            UserControl.ListViewList.ClearScripts();

            StartContractingToLoginForm(Settings.Default.InstantExpand);
        }

        private void FinishedExpandingOrContracting(bool expanding)
        {
            SAI_Editor_Manager.FormState = expanding ? FormState.FormStateMain : FormState.FormStateLogin;
            customPanelLogin.Visible = !expanding;
            tabControlWorkspaces.Visible = expanding;
            menuStrip.Visible = expanding;
            pictureBoxDonate.Visible = expanding;
            Invalidate();

            if (!expanding)
                SetHeightLoginFormBasedOnSetting();

            int width = (int)SaiEditorSizes.TabControlWorkspaceWidth + MainFormWidth - (int)SaiEditorSizes.MainFormWidth;
            int height = (int)SaiEditorSizes.TabControlWorkspaceHeight + MainFormHeight - (int)SaiEditorSizes.MainFormHeight;
            tabControlWorkspaces.Size = new Size(width, height);
            HandleTabControlWorkspacesResized();

            UserControl.FinishedExpandingOrContracting(expanding);

            SetSizable(expanding);

            Update();

            HandleTabControlWorkspacesResized(true);
        }

        private void MenuItemExit_Click(object sender, System.EventArgs e)
        {
            if (SAI_Editor_Manager.FormState == FormState.FormStateMain)
                TryCloseApplication();
        }

        private void TryCloseApplication()
        {
            if (!Settings.Default.PromptToQuit || DialogResult.Yes == MessageBox.Show("Are you sure you want to quit?", "Are you sure?", MessageBoxButtons.YesNo, MessageBoxIcon.Question))
                Close();
        }

        private void MenuItemSettings_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            using (SettingsForm settingsForm = new SettingsForm())
                settingsForm.ShowDialog(this);
        }

        private void MenuItemAbout_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            using (AboutForm aboutForm = new AboutForm())
                aboutForm.ShowDialog(this);
        }

        private void MenuOptionDeleteSelectedRow_Click(object sender, EventArgs e)
        {
            CustomObjectListView listViewSmartScripts = UserControl.ListView;

            if (SAI_Editor_Manager.FormState != FormState.FormStateMain || ((SmartScriptList)listViewSmartScripts.List).SelectedScript == null)
                return;

            UserControl.DeleteSelectedRow();
        }

        private void MenuItemCopySelectedRowListView_Click(object sender, EventArgs e)
        {
            CustomObjectListView listViewSmartScripts = UserControl.ListView;

            if (SAI_Editor_Manager.FormState != FormState.FormStateMain || ((SmartScriptList)listViewSmartScripts.List).SelectedScript == null)
                return;

            _smartScriptsOnClipBoard.Add(((SmartScriptList)listViewSmartScripts.List).SelectedScript.Clone());
        }

        private void MenuItemPasteLastCopiedRow_Click(object sender, EventArgs e)
        {
            CustomObjectListView listViewSmartScripts = UserControl.ListView;

            if (SAI_Editor_Manager.FormState != FormState.FormStateMain || ((SmartScriptList)listViewSmartScripts.List).SelectedScript == null)
                return;

            if (_smartScriptsOnClipBoard.Count <= 0)
            {
                MessageBox.Show("No smart scripts have been copied in this session!", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            SmartScript newSmartScript = _smartScriptsOnClipBoard.Last().Clone();
            listViewSmartScripts.List.AddScript(newSmartScript);
        }

        private async void ButtonSearchWorldDb_Click(object sender, EventArgs e)
        {
            buttonSearchWorldDb.Enabled = false;

            SAI_Editor_Manager.Instance.ResetWorldDatabase(false);
            List<string> databaseNames = await SAI_Editor_Manager.Instance.GetDatabasesInConnection(textBoxHost.Text, textBoxUsername.Text, CustomConverter.ToUInt32(textBoxPort.Text), textBoxPassword.Text);

            if (databaseNames != null && databaseNames.Count > 0)
                using (SelectDatabaseForm selectDatabaseForm = new SelectDatabaseForm(databaseNames, textBoxWorldDatabase))
                    selectDatabaseForm.ShowDialog(this);

            buttonSearchWorldDb.Enabled = true;
        }

        private void TestToolStripMenuItemDeleteRow_Click(object sender, EventArgs e)
        {
            UserControl.DeleteSelectedRow();
        }

        private void SmartAIWikiToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SAI_Editor_Manager.Instance.StartProcess("https://trinitycore.atlassian.net/wiki/spaces/tc/pages/2130108/smart+scripts");
        }

        private async void GenerateSQLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            using (SqlOutputForm sqlOutputForm = new SqlOutputForm(await UserControl.GenerateSmartAiSqlFromListView(), true, await UserControl.GenerateSmartAiRevertQuery()))
                sqlOutputForm.ShowDialog(this);
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (UserControl != null)
                foreach (Control control in UserControl.Controls)
                    control.Enabled = false;

            foreach (Control control in Controls)
                control.Enabled = false;

            if (SAI_Editor_Manager.SaveSettingsOnExit)
            {
                if (_adjustedLoginSettings)
                    SaveLastUsedFields();

                if (SAI_Editor_Manager.FormState == FormState.FormStateMain)
                {
                    Settings.Default.LastFormExtraWidth = Width - (int)SaiEditorSizes.MainFormWidth;
                    Settings.Default.LastFormExtraHeight = Height - (int)SaiEditorSizes.MainFormHeight;
                    Settings.Default.Save();
                }
            }

            if (_updateSurveyThread != null)
                _updateSurveyThread.Abort();
        }

        private void SaveLastUsedFields()
        {
            Settings.Default.ShowBasicInfo = UserControl.checkBoxShowBasicInfo.Checked;
            Settings.Default.LockSmartScriptId = UserControl.checkBoxLockEventId.Checked;
            Settings.Default.ListActionLists = UserControl.checkBoxListActionlistsOrEntries.Checked;
            Settings.Default.AllowChangingEntryAndSourceType = UserControl.checkBoxAllowChangingEntryAndSourceType.Checked;
            Settings.Default.PhaseHighlighting = false;// userControl.checkBoxUsePhaseColors.Checked;
            Settings.Default.ShowTooltipsStaticly = UserControl.checkBoxUseStaticTooltips.Checked;

            string lastStaticInfoPerTab = String.Empty;

            var objs = new List<object>();

            UserControl.CurrentState.Save(UserControl);

            int ctr = 0;

            foreach (SAIUserControlState state in UserControl.States)
            {
                objs.Add(new
                {
                    Workspace = ctr++,
                    Value = JsonConvert.DeserializeObject(JsonConvert.SerializeObject(state.ToStateObjects(), new CustomStateSerializer()))
                });
            }

            Settings.Default.LastStaticInfoPerTab = JsonConvert.SerializeObject(new { Workspaces = objs }, Formatting.Indented);

            if (SAI_Editor_Manager.FormState == FormState.FormStateLogin)
            {
                RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
                byte[] buffer = new byte[1024];
                rng.GetBytes(buffer);
                string salt = BitConverter.ToString(buffer);
                rng.Dispose();

                Settings.Default.Entropy = salt;
                Settings.Default.Host = textBoxHost.Text;
                Settings.Default.User = textBoxUsername.Text;
                Settings.Default.Password = textBoxPassword.Text.Length == 0 ? String.Empty : textBoxPassword.Text.ToSecureString().EncryptString(Encoding.Unicode.GetBytes(salt));
                Settings.Default.Database = textBoxWorldDatabase.Text;
                Settings.Default.Port = CustomConverter.ToUInt32(textBoxPort.Text);
                Settings.Default.UseWorldDatabase = radioButtonConnectToMySql.Checked;
                Settings.Default.AutoConnect = checkBoxAutoConnect.Checked;
            }

            Settings.Default.Save();
        }

        private void MenuItemRevertQuery_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            using (RevertQueryForm revertQueryForm = new RevertQueryForm())
                revertQueryForm.ShowDialog(this);
        }

        private async void MenuItemGenerateCommentListView_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain || !Settings.Default.UseWorldDatabase)
                return;

            await UserControl.GenerateCommentListView();
        }

        private void MenuItemDuplicateSelectedRow_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            UserControl.DuplicateSelectedRow();
        }

        private void MenuItemLoadSelectedEntry_Click(object sender, EventArgs e)
        {
            UserControl.LoadSelectedEntry();
        }

        private void RadioButtonConnectToMySql_CheckedChanged(object sender, EventArgs e)
        {
            HandleRadioButtonUseDatabaseChanged();
        }

        private void RadioButtonDontUseDatabase_CheckedChanged(object sender, EventArgs e)
        {
            HandleRadioButtonUseDatabaseChanged();
        }

        private void HandleRadioButtonUseDatabaseChanged()
        {
            textBoxHost.Enabled = radioButtonConnectToMySql.Checked;
            textBoxUsername.Enabled = radioButtonConnectToMySql.Checked;
            textBoxPassword.Enabled = radioButtonConnectToMySql.Checked;
            textBoxWorldDatabase.Enabled = radioButtonConnectToMySql.Checked;
            textBoxPort.Enabled = radioButtonConnectToMySql.Checked;
            buttonSearchWorldDb.Enabled = radioButtonConnectToMySql.Checked;
            labelDontUseDatabaseWarning.Visible = !radioButtonConnectToMySql.Checked;

            Settings.Default.UseWorldDatabase = radioButtonConnectToMySql.Checked;
            Settings.Default.Save();

            SetHeightLoginFormBasedOnSetting();
        }

        private void SetHeightLoginFormBasedOnSetting()
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
            {
                if (radioButtonConnectToMySql.Checked)
                {
                    MaximumSize = new Size((int)SaiEditorSizes.MainFormWidth, (int)SaiEditorSizes.MainFormHeight);
                    Height = (int)SaiEditorSizes.LoginFormHeight;
                }
                else
                {
                    MaximumSize = new Size((int)SaiEditorSizes.MainFormWidth, (int)SaiEditorSizes.MainFormHeight);
                    Height = (int)SaiEditorSizes.LoginFormHeightShowWarning;
                }
            }
        }

        public void HandleUseWorldDatabaseSettingChanged()
        {
            radioButtonConnectToMySql.Checked = Settings.Default.UseWorldDatabase;
            radioButtonDontUseDatabase.Checked = !Settings.Default.UseWorldDatabase;

            UserControl.buttonSearchForEntryOrGuid.Enabled = Settings.Default.UseWorldDatabase || UserControl.comboBoxSourceType.SelectedIndex == 2;
            UserControl.pictureBoxLoadScript.Enabled = UserControl.textBoxEntryOrGuid.Text.Length > 0 && Settings.Default.UseWorldDatabase;
            UserControl.checkBoxListActionlistsOrEntries.Enabled = Settings.Default.UseWorldDatabase;
            UserControl.buttonGenerateComments.Enabled = UserControl.ListView.Items.Count > 0 && Settings.Default.UseWorldDatabase;

            menuItemRevertQuery.Enabled = Settings.Default.UseWorldDatabase;
            menuItemGenerateComment.Enabled = UserControl.ListView.Items.Count > 0 && Settings.Default.UseWorldDatabase;
            menuItemGenerateCommentListView.Enabled = Settings.Default.UseWorldDatabase;
            menuItemLoadSelectedEntryListView.Enabled = Settings.Default.UseWorldDatabase;
            searchForAQuestToolStripMenuItem1.Enabled = Settings.Default.UseWorldDatabase;
            searchForACreatureEntryToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForACreatureGuidToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAGameobjectEntryToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAGameobjectGuidToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAGameEventToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAnItemEntryToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForACreatureSummonsIdToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAnEquipmentTemplateToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAWaypointToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForANpcTextToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAGossipMenuOptionToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;
            searchForAGossipOptionIdToolStripMenuItem.Enabled = Settings.Default.UseWorldDatabase;

            string newTitle = "SAI-Editor " + _applicationVersion + " - ";

            if (Settings.Default.UseWorldDatabase)
                newTitle += "Connection: " + Settings.Default.User + ", " + Settings.Default.Host + ", " + Settings.Default.Port.ToString();
            else
                newTitle += "Creator-only mode, no database connection";

            SetFormTitle(newTitle);
        }

        private void MenuItemRetrieveLastDeletedRow_Click(object sender, EventArgs e)
        {
            if (_lastDeletedSmartScripts.Count == 0)
            {
                MessageBox.Show("There are no items deleted in this session ready to be restored.", "Nothing to retrieve!", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            UserControl.ListViewList.AddScript(_lastDeletedSmartScripts.Last());
            _lastDeletedSmartScripts.Remove(_lastDeletedSmartScripts.Last());
        }

        private void ShowSearchFromDatabaseForm(TextBox textBoxToChange, DatabaseSearchFormType searchType)
        {
            UserControl.ShowSearchFromDatabaseForm(textBoxToChange, searchType);
        }

        private void SearchForASpellToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeSpell);
        }

        private void SearchForAFactionToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeFaction);
        }

        private void SearchForAnEmoteToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeEmote);
        }

        private void SearchForAMapToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeQuest);
        }

        private void SearchForAQuestToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeMap);
        }

        private void SearchForAZoneToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeAreaOrZone);
        }

        private void SearchForACreatureEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeCreatureEntry);
        }

        private void SearchForACreatureGuidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeCreatureGuid);
        }

        private void SearchForAGameobjectEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeGameobjectEntry);
        }

        private void SearchForAGameobjectGuidToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeGameobjectGuid);
        }

        private void SearchForASoundToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeSound);
        }

        private void SearchForAnAreatriggerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeAreaTrigger);
        }

        private void SearchForAGameEventToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeGameEvent);
        }

        private void SearchForAnItemEntryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeItemEntry);
        }

        private void SearchForACreatureSummonsIdToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeSummonsId);
        }

        private void SearchForATaxiPathToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeTaxiPath);
        }

        private void SearchForAnEquipmentTemplateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeEquipTemplate);
        }

        private void SearchForAWaypointToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeWaypoint);
        }

        private void SearchForANpcTextToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeNpcText);
        }

        private void SearchForAGossipOptionIdToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeGossipMenuOptionMenuId);
        }

        private void SearchForAGossipMenuOptionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ShowSearchFromDatabaseForm(null, DatabaseSearchFormType.DatabaseSearchFormTypeGossipMenuOptionId);
        }

        private void ConditionEditorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            foreach (Form form in Application.OpenForms)
            {
                if (form.Name == "ConditionForm")
                {
                    form.BringToFront();
                    form.Show(); //! Show the form in case it's hidden
                    (form as ConditionForm).formHidden = false;
                    return;
                }
            }

            ConditionForm conditionForm = new ConditionForm
            {
                formHidden = false
            };
            conditionForm.Show();
        }

        Dictionary<string, Type> _searchEventHandlers = new Dictionary<string, Type>()
        {
            {"Search for gameobject flags", typeof(MultiSelectForm<GoFlags>)},
            {"Search for unit flags", typeof(MultiSelectForm<UnitFlags>)},
            {"Search for unit flags 2", typeof(MultiSelectForm<UnitFlags2>)},
            {"Search for dynamic flags", typeof(MultiSelectForm<DynamicFlags>)},
            {"Search for npc flags", typeof(MultiSelectForm<NpcFlags>)},
            {"Search for unit stand flags", typeof(SingleSelectForm<UnitStandStateType>)},
            {"Search for unit bytes1 flags", typeof(MultiSelectForm<UnitBytes1_Flags>)},
            {"Search for SAI event flags", typeof(MultiSelectForm<SmartEventFlags>)},
            {"Search for SAI phase masks", typeof(MultiSelectForm<SmartPhaseMasks>)},
            {"Search for SAI cast flags", typeof(MultiSelectForm<SmartCastFlags>)},
            {"Search for SAI templates", typeof(SingleSelectForm<SmartAiTemplates>)},
            {"Search for SAI respawn conditions", typeof(SingleSelectForm<SmartRespawnCondition>)},
            {"Search for SAI event types", typeof(SingleSelectForm<SmartEvent>)},
            {"Search for SAI action types", typeof(SingleSelectForm<SmartAction>)},
            {"Search for SAI target types", typeof(SingleSelectForm<SmartTarget>)},
            {"Search for SAI actionlist timer update types", typeof(SingleSelectForm<SmartActionlistTimerUpdateType>)},
            {"Search for gameobject states", typeof(SingleSelectForm<GoStates>)},
            {"Search for react states", typeof(SingleSelectForm<ReactState>)},
            {"Search for sheath states", typeof(SingleSelectForm<SheathState>)},
            {"Search for movement generator types", typeof(SingleSelectForm<MovementGeneratorType>)},
            {"Search for spell schools", typeof(SingleSelectForm<SpellSchools>)},
            {"Search for power types", typeof(SingleSelectForm<PowerTypes>)},
            {"Search for unit stand state types", typeof(SingleSelectForm<UnitStandStateType>)},
            {"Search for temp summon types", typeof(SingleSelectForm<TempSummonType>)},
        };

        private void SearchForFlagsMenuItem_Click(object sender, EventArgs e)
        {
            using (Form selectForm = (Form)Activator.CreateInstance(_searchEventHandlers[((ToolStripItem)sender).Text], new object[] { null }))
                selectForm.ShowDialog(this);
        }

        private void TabControlWorkspaces_SelectedIndexChanged(object sender, EventArgs e)
        {
            //! New workspace is being created
            if (tabControlWorkspaces.SelectedTab != null && tabControlWorkspaces.SelectedTab.Text == "+")
            {
                if (tabControlWorkspaces.TabPages.Count > (int)MiscEnumerators.MaxWorkSpaceCount)
                {
                    MessageBox.Show("You can't have more than " + (int)MiscEnumerators.MaxWorkSpaceCount +
                        " different workspaces open at the same time. This limit is created to avoid start-up delays.",
                        "Workspace limit", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                    tabControlWorkspaces.SelectedIndex = LastSelectedWorkspaceIndex;
                    return;
                }

                UserControl.AddWorkSpace();

                CreateTabControl();
            }

            if (LastSelectedWorkspaceIndex < tabControlWorkspaces.TabPages.Count)
                tabControlWorkspaces.TabPages[LastSelectedWorkspaceIndex].Controls.Remove(UserControl);

            tabControlWorkspaces.TabPages[tabControlWorkspaces.SelectedIndex].Controls.Add(UserControl);

            if (tabControlWorkspaces.SelectedIndex < UserControl.States.Count)
                UserControl.CurrentState = UserControl.States[tabControlWorkspaces.SelectedIndex];

            LastSelectedWorkspaceIndex = tabControlWorkspaces.SelectedIndex;

            if (UserControl.ListView.Objects.Cast<object>().Count<object>() > 0)
                UserControl.ListView.SelectObject(UserControl.ListView.Objects.Cast<object>().ElementAt(0));

            UserControl.ListView.Select();
            UserControl.ListView.Focus();
        }

        private void CreateTabControl(bool first = false, bool addWorkspace = false)
        {
            if (tabControlWorkspaces.TabPages.Count > (int)MiscEnumerators.MaxWorkSpaceCount)
                return;

            if (!first)
                tabControlWorkspaces.TabPages.RemoveAt(tabControlWorkspaces.TabPages.Count - 1);

            UserControlSAI userControlSai;

            if (first && UserControl == null)
            {
                userControlSai = new UserControlSAI
                {
                    Parent = this
                };
                userControlSai.LoadUserControl();
            }
            else
                userControlSai = UserControl;

            TabPage newPage = new TabPage
            {
                Text = "Workspace " + (tabControlWorkspaces.TabPages.Count + 1)
            };
            newPage.Controls.Add(userControlSai);

            for (int i = 0; i < tabControlWorkspaces.TabPages.Count; i++)
            {
                if (tabControlWorkspaces.TabPages[i].Text == "+")
                {
                    tabControlWorkspaces.TabPages.RemoveAt(i);
                    break;
                }
            }

            tabControlWorkspaces.TabPages.Add(newPage);
            tabControlWorkspaces.TabPages.Add(new TabPage("+"));

            if (addWorkspace)
                userControlSai.AddWorkSpace();

            if (first && UserControl == null)
                UserControl = userControlSai;

            if (!first)
                tabControlWorkspaces.SelectedIndex = tabControlWorkspaces.TabPages.Count - 2;
        }

        private void PictureBoxDonate_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("https://www.paypal.com/cgi-bin/webscr" +
                    "?cmd=" + "_donations" +
                    "&business=jasper.rietrae@gmail.com" +
                    "&lc=NL" +
                    "&item_name=Donating to the creator of SAI-Editor" +
                    "&currency_code=USD" +
                    "&bn=PP%2dDonationsBF");
            }
            catch
            {
                MessageBox.Show("Something went wrong attempting to open the donation page.", "Something went wrong!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        void SetFormTitle(string title)
        {
            Text = title;

            switch (SAI_Editor_Manager.Instance.Expansion)
            {
                case WowExpansion.ExpansionWotlk:
                    Text += ": Wrath of the Lich King";
                    break;
                case WowExpansion.ExpansionCata:
                    Text += ": Cataclysm";
                    break;
                case WowExpansion.ExpansionMop:
                    Text += ": Mists of Pandaria";
                    break;
                case WowExpansion.ExpansionWod:
                    Text += ": Warlords of Draenor";
                    break;
                default:
                    Text += ": ERROR - No expansion!";
                    break;
            }
        }

        private void SettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            using (SettingsForm settingsForm = new SettingsForm())
                settingsForm.ShowDialog(this);
        }

        private void TabControlWorkspaces_TabClosing(object sender, TabControlCancelEventArgs e)
        {
            tabControlWorkspaces.SelectedIndex = e.TabPageIndex > 0 ? e.TabPageIndex - 1 : 0;
            UserControl.States.RemoveAt(e.TabPageIndex);

            List<TabPage> tabPages = new List<TabPage>();

            //! .Count - 1 because we don't need to rename the "+" tab.
            for (int i = 0; i < tabControlWorkspaces.TabPages.Count - 1; ++i)
                if (i != e.TabPageIndex)
                    tabPages.Add(tabControlWorkspaces.TabPages[i]);

            for (int i = 0; i < tabPages.Count; ++i)
                tabPages[i].Text = "Workspace " + (i + 1).ToString();

            tabControlWorkspaces.Invalidate();
            tabControlWorkspaces.Update();
        }

        private void TabControlWorkspaces_SizeChanged(object sender, EventArgs e)
        {
            HandleTabControlWorkspacesResized(true);
        }

        private void HandleTabControlWorkspacesResized(bool fromResize = false)
        {
            //! This happens on Windows 7 when minimizing for some reason
            if (tabControlWorkspaces.Width == 0 && tabControlWorkspaces.Height == 0)
                return;

            SynchronizeSizeOfUserControlAndListView(fromResize);
        }

        private void SynchronizeSizeOfUserControlAndListView(bool fromResize = false)
        {
            UserControl.Width = tabControlWorkspaces.Width;
            UserControl.Height = tabControlWorkspaces.Height;

            //! Not sure why but height is really off...
            int contractHeightFromTabControl = 252, contractWidthFromTabControl = (int)SaiEditorSizes.StaticTooltipsPadding;

            if (fromResize && UserControl.checkBoxUseStaticTooltips.Checked)
                contractHeightFromTabControl += 60 + 12; //! Height of two panels plus some extra padding

            UserControl.ListView.Width = tabControlWorkspaces.Width - contractWidthFromTabControl;
            UserControl.ListView.Height = tabControlWorkspaces.Height - contractHeightFromTabControl;

            UserControl.panelStaticTooltipTypes.Width = tabControlWorkspaces.Width - (int)SaiEditorSizes.StaticTooltipsPadding;
            UserControl.panelStaticTooltipParameters.Width = tabControlWorkspaces.Width - (int)SaiEditorSizes.StaticTooltipsPadding;

            int increaseY = tabControlWorkspaces.Height - _oldHeightTabControlWorkspaces;
            UserControl.panelStaticTooltipTypes.Location = new Point(UserControl.panelStaticTooltipTypes.Location.X, UserControl.panelStaticTooltipTypes.Location.Y + increaseY);
            UserControl.panelStaticTooltipParameters.Location = new Point(UserControl.panelStaticTooltipParameters.Location.X, UserControl.panelStaticTooltipParameters.Location.Y + increaseY);

            _oldHeightTabControlWorkspaces = tabControlWorkspaces.Height;
            _oldWidthTabControlWorkspaces = tabControlWorkspaces.Width;
        }

        private void MenuItemReportIssue_Click(object sender, EventArgs e)
        {
            SAI_Editor_Manager.Instance.StartProcess("https://github.com/jasperrietrae/SAI-Editor/issues/new");
        }

        private void MenuItemGiveFeedback_Click(object sender, EventArgs e)
        {
            SAI_Editor_Manager.Instance.StartProcess("http://jasper-rietrae.com/#contact/");
        }

        private void MainForm_Resize(object sender, EventArgs e)
        {
            Invalidate();
            Update();
        }

        private void MenuItemViewAllPastebins_Click(object sender, EventArgs e)
        {
            if (SAI_Editor_Manager.FormState != FormState.FormStateMain)
                return;

            using (ViewAllPastebinsForm viewAllPastebinsForm = new ViewAllPastebinsForm())
                viewAllPastebinsForm.ShowDialog(this);
        }
    }
}
