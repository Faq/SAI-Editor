using System.ComponentModel;
using System.Windows.Forms;

namespace SAI_Editor.Classes.CustomControls
{
    public partial class LabelWithTooltip : Label
    {
        public LabelWithTooltip()
        {

        }

        private int _tooltipParameterId;

        [Category("Custom")]
        public int TooltipParameterId
        {
            get { return _tooltipParameterId; }
            set { _tooltipParameterId = value; }
        }
    }
}
