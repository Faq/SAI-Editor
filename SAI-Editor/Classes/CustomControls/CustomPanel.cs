using System.Windows.Forms;

namespace SAI_Editor.Classes.CustomControls
{
    public class CustomPanel : Panel
    {
        public CustomPanel()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.UserPaint, true);
            ResizeRedraw = true;
        }

        protected override bool DoubleBuffered
        {
            get
            {
                return true;
            }
        }
    }
}
