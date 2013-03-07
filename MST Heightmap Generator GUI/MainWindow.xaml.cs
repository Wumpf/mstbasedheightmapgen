using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MST_Heightmap_Generator_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        MstBasedHeightmap.HeightmapFactory _heightmapFactory = new MstBasedHeightmap.HeightmapFactory(512, 512, 1);
        float[,] _heightmapData;

        WriteableBitmap imageContent; 
        
        public MainWindow()
        {
            InitializeComponent();

            _heightmapData = new float[_heightmapFactory.GetWidth(), _heightmapFactory.GetHeight()];

            imageContent = new WriteableBitmap((int)_heightmapFactory.GetWidth(), (int)_heightmapFactory.GetHeight(), -1.0f, -1.0f, PixelFormats.Gray32Float, null);
            heightmapView.Source = imageContent;
        }

        private void GenerateHeightmap(object sender, RoutedEventArgs e)
        {
            _heightmapFactory.Generate(_heightmapData);
            imageContent.WritePixels(new Int32Rect(0, 0, (int)_heightmapFactory.GetWidth(), (int)_heightmapFactory.GetHeight()), 
                                            (Array)_heightmapData, (int)(sizeof(float) * _heightmapFactory.GetWidth()), 0);
        }
    }
}
