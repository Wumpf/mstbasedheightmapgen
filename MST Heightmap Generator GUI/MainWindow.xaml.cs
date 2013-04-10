using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
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

        RenderWindow renderWindow;

        public MainWindow()
        {
            InitializeComponent();

            _heightmapData = new float[_heightmapFactory.GetWidth(), _heightmapFactory.GetHeight()];

            imageContent = new WriteableBitmap((int)_heightmapFactory.GetWidth(), (int)_heightmapFactory.GetHeight(), -1.0f, -1.0f, PixelFormats.Gray32Float, null);
            heightmapView.Source = imageContent;


            new Thread(
                /*System.Threading.Tasks.Parallel.Invoke(*/x =>
            {
                using (renderWindow = new RenderWindow())
                    renderWindow.Run();
            }).Start();
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            renderWindow.Closing = true;

            base.OnClosing(e);
        }

        private void GenerateHeightmap(object sender, RoutedEventArgs e)
        {
            _heightmapFactory.Generate(_heightmapData);
            imageContent.WritePixels(new Int32Rect(0, 0, (int)_heightmapFactory.GetWidth(), (int)_heightmapFactory.GetHeight()), 
                                            (Array)_heightmapData, (int)(sizeof(float) * _heightmapFactory.GetWidth()), 0);
        }

        private void Sl_MaxHeight_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float[,] value = {{(float)e.NewValue}};
            _heightmapFactory.SetParameter(3, value);
        }

        private void Sl_QuadraticSpline_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float[,] value = { { (float)e.NewValue } };
            _heightmapFactory.SetParameter(4, value);
            System.Security.Cryptography.MD5.Create();
        }

        private void TB_Seed_Changed(object sender, TextChangedEventArgs e)
        {
            int currentHash = ((System.Windows.Controls.TextBox)(e.Source)).Text.GetHashCode();
            float[,] value = { { (float)currentHash } };
            // TODO Parameter für seed
        }
    }
}
