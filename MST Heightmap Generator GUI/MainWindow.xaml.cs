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

namespace MST_Heightmap_Generator_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int MAP_SIZE = 256;
        MstBasedHeightmap.HeightmapFactory _heightmapFactory = new MstBasedHeightmap.HeightmapFactory(MAP_SIZE, MAP_SIZE, 2);
        float[,] _heightmapData;

        // Second dimension is always 3
        float[,] _summitList;

      //  private WriteableBitmap imageContent;
        private TerrainRenderingPreview terrainRenderingPreview;

        public MainWindow()
        {
            InitializeComponent();

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

        private void CanvasMouseLeave(object sender, MouseEventArgs e)
        {
            terrainRenderingPreview.OnLeftMouseUp();
        }

        private void AddSliderProperty(TreeViewItem parent, string name, double min, double max)
        {
            Label text = new Label();
            Slider slider = new Slider();
            int index = parent.Items.Add(text);

            text.Content = name + ": " + min;
            text.BorderThickness = new System.Windows.Thickness(1.0);
            text.BorderBrush = System.Windows.Media.Brushes.Gray;
            text.MouseEnter += (s, b) => { parent.Items[index] = slider; };
            text.Width = Layers.Width - 48;
            text.Height = 22;
            text.Padding = new System.Windows.Thickness(1.0);

            ToolTip tip = new ToolTip();
            tip.Placement = System.Windows.Controls.Primitives.PlacementMode.Left;
            tip.PlacementTarget = slider;
            tip.Content = name;
            tip.Height = 22;
            tip.Padding = text.Padding;

            slider.Width = Layers.Width - 48;
            slider.Height = 22;
            slider.Minimum = min;
            slider.Maximum = max;
            slider.BorderThickness = new System.Windows.Thickness(1.0);
            slider.BorderBrush = System.Windows.Media.Brushes.Gray;
            slider.AutoToolTipPlacement = System.Windows.Controls.Primitives.AutoToolTipPlacement.TopLeft;
            slider.AutoToolTipPrecision = (int)(-Math.Min(0,Math.Log10(max-min)-2));
            slider.MouseEnter += (s, b) => { tip.IsOpen = true; };
            slider.MouseLeave += (s, b) => { Slider sl = (Slider)s; tip.IsOpen = false; text.Content = tip.Content + ": " + Math.Round(sl.Value, sl.AutoToolTipPrecision); parent.Items[index] = text; };
        }

        private void AddPointSetProperty(TreeViewItem parent)
        {
            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            int index = parent.Items.Add(panel);

            CheckBox visible = new CheckBox();
            visible.IsChecked = true;
            visible.Content = "Point Set";
            visible.Margin = new Thickness(0, 2, 6, 0);
            panel.Children.Add(visible);

            TextBox num = new TextBox();
            num.Width = 38;
            num.PreviewTextInput += (sender, e) => { e.Handled = !(e.Text.All(char.IsNumber) && num.Text.Length < 3); };
            num.ToolTip = "Number of points to be generated";
            num.Text = "20";
            panel.Children.Add(num);

            TextBox seed = new TextBox();
            seed.Text = "Seed";
            seed.Width = 57;
            seed.ToolTip = "Initial value for randomize";
            panel.Children.Add(seed);

            Button rand = new Button();
            rand.Content = "Randomize";
            panel.Children.Add(rand);
        }

        private void AddLayer(object sender, RoutedEventArgs e)
        {
            TreeViewItem newItem = new TreeViewItem();
            newItem.Header = ((ComboBoxItem)LayerChoose.SelectedItem).Content;
            switch (LayerChoose.SelectedIndex)
            {
                case 0:
                case 1:
                    AddSliderProperty(newItem, "Height", 1, 100);
                    AddSliderProperty(newItem, "Quadratic Spline", 0, 1);
                    AddPointSetProperty(newItem);
                    break;
                case 2:
                    AddSliderProperty(newItem, "Height", 1, 100);
                    AddSliderProperty(newItem, "Height Dependency", 0, 2.5);
                    AddSliderProperty(newItem, "Height Dependency Offset", 0, 1);
                    AddSliderProperty(newItem, "Gradient Dependency", 0, 0.025);
                break;
            }
            if(Layers.Items.IsEmpty || (Layers.SelectedItem==null))
                Layers.Items.Add(newItem);
            else Layers.Items.Insert(Layers.Items.IndexOf(Layers.SelectedItem)+1, newItem);
            newItem.ExpandSubtree();
        }

        private void MoveLayerUp(object sender, RoutedEventArgs e)
        {
            if(Layers.SelectedItem != null)
            {
                int index = Layers.Items.IndexOf(Layers.SelectedItem);
                if (index > 0)
                {
                    var swap = Layers.Items[index - 1];
                    Layers.Items.RemoveAt(index - 1);
                    Layers.Items.Insert(index, swap);
                }
            }
        }

        private void MoveLayerDown(object sender, RoutedEventArgs e)
        {
            if (Layers.SelectedItem != null)
            {
                int index = Layers.Items.IndexOf(Layers.SelectedItem);
                if (index < Layers.Items.Count-1)
                {
                    var swap = Layers.Items[index + 1];
                    Layers.Items.RemoveAt(index + 1);
                    Layers.Items.Insert(index, swap);
                }
            }
        }
    }
}
