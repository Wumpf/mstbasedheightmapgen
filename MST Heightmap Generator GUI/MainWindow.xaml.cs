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
        // TODO: temporary solution - check dependencies when refactoring
        public const int MAP_SIZE = 256;

      //  private WriteableBitmap imageContent;
        private TerrainRenderingPreview terrainRenderingPreview = new TerrainRenderingPreview();

        public MainWindow()
        {
            InitializeComponent();
            InitLayerChooseComboBox();
            InitLayerBlendingComboBox();

            terrainRenderingPreview.SetScaleFactor((float)VisualScaleSlider.Value);
            terrainRenderingPreview.TimeOfDay = (float)TimeOfDaySlider.Value;
            DX11Display.Scene = terrainRenderingPreview;
        }


        
        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
        //    renderWindow.Closing = true;

            base.OnClosing(e);
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            App.Current.Shutdown();
        }

        private void Sl_VisualScaleSlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (terrainRenderingPreview != null)
            {
                terrainRenderingPreview.SetScaleFactor((float)VisualScaleSlider.Value);
            }
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
            json["HeightmapPixelPerWorldUnit"] = (1 << (int)Math.Round(SlResolution.Value)) / (float)MAP_SIZE;
            
            var jsonSerializer = JsonSerializer.CreateDefault();
            jsonSerializer.Converters.Add(new Newtonsoft.Json.Converters.StringEnumConverter());    // convert enums directly to string -> default would be int
            json["Layers"] = new JArray(Layers.Items.OfType<TreeViewItem>().Reverse().Select(treeViewItem => 
                { 
                    JToken jlayer = JValue.FromObject(treeViewItem.Tag, jsonSerializer);
                    jlayer["Type"] = Layer.LayerTypes[treeViewItem.Tag.GetType()];
                    return jlayer;
                }));

            return json.ToString();
        }

        private void RegenerateHeightmap(object sender, RoutedEventArgs e)
        {
            int resolution = 1 << (int)Math.Round(SlResolution.Value);
            float[,] heightmapData = new float[resolution, resolution];
            string json = SerializeSettingsToJSON();
            new MstBasedHeightmap.GeneratorPipeline(json).Execute(heightmapData);

            // update view
            terrainRenderingPreview.LoadNewHeightMap(heightmapData, resolution / (float)MAP_SIZE);
            //terrainRenderingPreview.AddPointSet(new PointSet(_summitList, heightmapPixelsPerWorld[0, 0], _heightmapData.GetLength(0), _heightmapData.GetLength(1), terrainRenderingPreview.GraphicsDevice));
        }

        private void TimeOfDaySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            terrainRenderingPreview.TimeOfDay = (float)e.NewValue;
        }

    }
}