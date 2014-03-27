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
using System.Diagnostics;

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
        private Render.RenderPreviewPanel terrainRenderingPreview = new Render.RenderPreviewPanel();

        /// <summary>
        /// Last generated raw Heightmap data
        /// </summary>
        float[,] heightmapData;

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
            json["HeightmapPixelPerWorldUnit"] = (float)GetResolution() / MAP_SIZE;
            
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

        private int GetResolution()
        {
            string resolutionString = (string)((ComboBoxItem)ResolutionDropDown.SelectedItem).Content;
            return int.Parse(resolutionString.Split('x')[0]);
         //   int resolutionY = int.Parse(resolutionString.Split('x')[1]);
        }

        private void SetResolution(int res)
        {
            ResolutionDropDown.SelectedIndex = (int)Math.Log(res, 2)-5;
        }

        private void RegenerateHeightmap(object sender, RoutedEventArgs e)
        {
            // stop rendering
            terrainRenderingPreview.DeactivateRendering = true;

            int resolution = GetResolution();
            heightmapData = new float[resolution, resolution];

            // predeclare progress bar
            ProgressBar progressBar = null;
            var mainWindowDispatcher = Dispatcher;

            long heightmapGentimeMS = 0;
            
            string json = SerializeSettingsToJSON();    // access to stuff from this thread - not possible in Task
            Task generateTask = new Task(() =>
                {
                    try
                    {
                        Stopwatch sw = new Stopwatch();
                        sw.Start();
                        new MstBasedHeightmap.GeneratorPipeline(json).Execute(heightmapData);
                        heightmapGentimeMS = sw.ElapsedMilliseconds;
                    }
                    catch
                    {
                        System.Diagnostics.Debugger.Break();
                    }
                });
            generateTask.Start();

            progressBar = new ProgressBar(() => generateTask.IsCompleted ||generateTask.IsCanceled || generateTask.IsFaulted);
            progressBar.TaskDescription.Content = "Generating Heightmap ...";
            progressBar.ShowDialog();

            // upload to graphics cards (needs to stay in mainthread!
            terrainRenderingPreview.LoadNewHeightMap(heightmapData, resolution / (float)MAP_SIZE);

            // continue rendering
            terrainRenderingPreview.DeactivateRendering = false;

            // display timings
            MessageBox.Show("Heightmap Computation Time:\t " + heightmapGentimeMS + "ms", "Done!");
        }

        private void TimeOfDaySlider_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            terrainRenderingPreview.TimeOfDay = (float)e.NewValue;
        }

        private void SaveBtn_Click(object sender, RoutedEventArgs e)
        {
            // Ask for a type and a destination
            System.Windows.Forms.SaveFileDialog saveFileDialog = new System.Windows.Forms.SaveFileDialog();
            saveFileDialog.Filter = "Map Parametrization|*.json|Raw Heightmap|*.raw";
            saveFileDialog.Title = "Save or Export Map";
            saveFileDialog.ShowDialog();

            // Something chosen?
            if (saveFileDialog.FileName != "")
            {
                // Save different things depending on the format
                switch (saveFileDialog.FilterIndex)
                {
                    case 1: SaveMapAsJson(saveFileDialog.FileName);
                        break;
                    case 2: SaveMapAsRaw(saveFileDialog.FileName);// raw heightmap
                        break;
                }
            }
        }

        private void LoadBtn_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Forms.OpenFileDialog openFileDialog = new System.Windows.Forms.OpenFileDialog();
            openFileDialog.Filter = "Map Parametrization|*.json";
            openFileDialog.Title = "Load Map Parameters";
            openFileDialog.ShowDialog();

            // Something with correct format?
            if (openFileDialog.FileName != "")
            {
                LoadFromJson(openFileDialog.FileName);
                RegenerateHeightmap(null, null);
            }
        }

    }
}