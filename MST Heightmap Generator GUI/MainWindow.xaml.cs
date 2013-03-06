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
        
        public MainWindow()
        {
            InitializeComponent();

          //  _heightmapData = new float[_heightmapFactory.GetWidth(), _heightmapFactory.GetHeight()];
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
     //       mstbasedheightmap.Class1 letsgo = new mstbasedheightmap.Class1();
      //      letsgo.Generate();
            
        }
    }
}
