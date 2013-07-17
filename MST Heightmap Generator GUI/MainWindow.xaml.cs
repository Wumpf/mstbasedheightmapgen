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
using System.Windows.Controls.Primitives;
using SharpDX;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using MST_Heightmap_Generator_GUI.Layers;

namespace MST_Heightmap_Generator_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int MAP_SIZE = 256;
        const float heightmapPixelPerWorldUnit = 2.0f;
        MstBasedHeightmap.HeightmapFactory _heightmapFactory = new MstBasedHeightmap.HeightmapFactory(MAP_SIZE, MAP_SIZE, heightmapPixelPerWorldUnit);
        float[,] _heightmapData;

        // Second dimension is always 3
        float[,] _summitList;

      //  private WriteableBitmap imageContent;
        private TerrainRenderingPreview terrainRenderingPreview;

        public MainWindow()
        {
            InitializeComponent();
            InitLayerChooseComobBox();

            terrainRenderingPreview = new TerrainRenderingPreview();
            DX11Display.Scene = terrainRenderingPreview;
         
            int width = (int)_heightmapFactory.GetWidth();
            int height = (int)_heightmapFactory.GetHeight();
            _heightmapData = new float[_heightmapFactory.GetWidth(), _heightmapFactory.GetHeight()];
        //    imageContent = new WriteableBitmap(width, height, -1.0f, -1.0f, PixelFormats.Gray32Float, null);
         //   heightmapView.Source = imageContent;

            GenerateRandomSummits(20);
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
        //    renderWindow.Closing = true;

            base.OnClosing(e);
        }

        private void GenerateHeightmap(object sender, RoutedEventArgs e)
        {
            // Send points only on generated (they are modified interactive)
            _heightmapFactory.SetParameter(9, _summitList);

            _heightmapFactory.Generate(_heightmapData);
        /*    imageContent.WritePixels(new Int32Rect(0, 0, (int)_heightmapFactory.GetWidth(), (int)_heightmapFactory.GetHeight()), 
                                            (Array)_heightmapData, (int)(sizeof(float) * _heightmapFactory.GetWidth()), 0);

            */   
            float[,] heightmapPixelsPerWorld = new float[1, 1];
            uint width, height;
            _heightmapFactory.GetParameter(2, heightmapPixelsPerWorld, out width, out height);
            terrainRenderingPreview.LoadNewHeightMap(_heightmapData, heightmapPixelsPerWorld[0, 0]);
            terrainRenderingPreview.ClearPointSet();
            terrainRenderingPreview.AddPointSet(new PointSet(_summitList, heightmapPixelsPerWorld[0, 0], _heightmapData.GetLength(0), _heightmapData.GetLength(1), terrainRenderingPreview.GraphicsDevice));

            Sl_VisualScaleSlider_Changed(null, null);
        }

        private void GenerateRandomSummits(int num)
        {
            _summitList = new float[num,3];
            Random rnd = new Random(SeedEdit.Text.GetHashCode());
            for (int i = 0; i < num; ++i)
            {
                _summitList[i, 0] = rnd.Next(10000) / 10000.0f * MAP_SIZE;
                _summitList[i, 1] = rnd.Next(10000) / 10000.0f * MAP_SIZE;
                _summitList[i, 2] = rnd.Next(10000) / 10000.0f;
            }
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
        }

        private void TB_Seed_Changed(object sender, TextChangedEventArgs e)
        {
            // Convert a string to an integer seed - by hashing
            int currentHash = ((System.Windows.Controls.TextBox)(e.Source)).Text.GetHashCode();
            float[,] value = { { (float)currentHash } };
            _heightmapFactory.SetParameter(5, value);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }

        private void Sl_NoiseIntensity_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float[,] value = { { (float)e.NewValue } };
            _heightmapFactory.SetParameter(6, value);
        }

        private void Sl_NoiseHeightDependency_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float[,] value = { { (float)e.NewValue } };
            _heightmapFactory.SetParameter(7, value);
        }

        private void Sl_GradientDependency_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float[,] value = { { (float)e.NewValue } };
            _heightmapFactory.SetParameter(8, value);
        }

        private void Cb_Generator_Changed(object sender, SelectionChangedEventArgs e)
        {
            float[,] value = { { (float)GeneratorSelection.SelectedIndex } };
            _heightmapFactory.SetParameter(0, value);
        }

        private void Sl_RefractionNoise_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float[,] value = { { (float)e.NewValue } };
            _heightmapFactory.SetParameter(10, value);
        }

        private void Sl_VisualScaleSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (terrainRenderingPreview != null)
            {
                // Query the terrain size and update shader constants
                float[,] minmaxHeights = new float[2, 1];
                uint width, height;
                _heightmapFactory.GetParameter(11, minmaxHeights, out width, out height);
                terrainRenderingPreview.RescaleHeight(minmaxHeights[0, 0] * (float)VisualScaleSlider.Value, minmaxHeights[1, 0] * (float)VisualScaleSlider.Value);
            }
        }

        private void Sl_HeightDependencyOffset_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            float[,] value = { { (float)e.NewValue } };
            _heightmapFactory.SetParameter(12, value);
        }
        private void CanvasMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            terrainRenderingPreview.OnLeftMouseDown();
        }

        private void CanvasMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            terrainRenderingPreview.OnLeftMouseUp();
        }

        private void CanvasMouseMove(object sender, MouseEventArgs e)
        {
            terrainRenderingPreview.OnMouseMove();
        }

        private void CanvasMouseWheel(object sender, MouseWheelEventArgs e)
        {
            terrainRenderingPreview.OnMouseWheel(e.Delta);
        }

        private void CanvasMouseLeave(object sender, MouseEventArgs e)
        {
            terrainRenderingPreview.OnLeftMouseUp();
        }

        private string SerializeSettingsToJSON()
        {
            JObject json = new JObject();
            json["HeightmapWidth"] = MAP_SIZE;
            json["HeightmapHeight"] = MAP_SIZE;
            json["HeightmapPixelPerWorldUnit"] = heightmapPixelPerWorldUnit;
            
            var jsonSerializer = JsonSerializer.CreateDefault();
            jsonSerializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());    // convert enums directly to string -> default would be int
            json["Layers"] = new JArray(Layers.Items.OfType<TreeViewItem>().Select(treeViewItem => 
                {
                    JToken jlayer = JValue.FromObject(treeViewItem.Tag, jsonSerializer);
                    jlayer["Type"] = Layer.LayerTypes[treeViewItem.Tag.GetType()];
                    return jlayer;
                }));

            return json.ToString();
        }

        private void RegenerateHeightmap(object sender, RoutedEventArgs e)
        {
            string json = SerializeSettingsToJSON();
        }
    }
}