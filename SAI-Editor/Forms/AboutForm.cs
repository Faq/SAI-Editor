using System;
using System.Windows.Forms;
using SAI_Editor.Classes;

namespace SAI_Editor.Forms
{
    public partial class AboutForm : Form
    {
        public AboutForm()
        {
            InitializeComponent();
        }

        private void AboutForm_Load(object sender, EventArgs e)
        {

        }

        private void AboutForm_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Escape:
                    Close();
                    break;
            }
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void ButtonGithub_Click(object sender, EventArgs e)
        {
            SAI_Editor_Manager.Instance.StartProcess("https://github.com/Discover-/SAI-Editor/");
        }

        private void PictureBoxDiscover_Click(object sender, EventArgs e)
        {
            SAI_Editor_Manager.Instance.StartProcess("https://github.com/Discover-/");
        }

        private void PictureBoxMitch_Click(object sender, EventArgs e)
        {
            SAI_Editor_Manager.Instance.StartProcess("https://github.com/Mitch528/");
        }

        private void ButtonTrinitycore_Click(object sender, EventArgs e)
        {
            SAI_Editor_Manager.Instance.StartProcess("https://github.com/TrinityCore/");
        }
    }
}
