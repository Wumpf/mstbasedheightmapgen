using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MST_Heightmap_Generator_GUI
{
    /// <summary>
    /// Interaction logic for TerrainRenderingPreviewWindow.xaml
    /// </summary>
    public partial class TerrainRenderingPreviewWindow : Window
    {
        public TerrainRenderingPreview TerrainPreview
        { get { return (TerrainRenderingPreview)DX11Display.Scene; } }

        public TerrainRenderingPreviewWindow()
        {
            InitializeComponent();

            DX11Display.Scene = new TerrainRenderingPreview();
        }
    }
}
