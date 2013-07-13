using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace MST_Heightmap_Generator_GUI.LayerAttributes
{
    interface LayerAttribute
    {
        void CreateTreeViewSubElement(TreeViewItem parent, int width, Func<object> valueGetFunc, Action<object> valueSetFunc);
    }
}
