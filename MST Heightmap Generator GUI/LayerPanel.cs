using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MST_Heightmap_Generator_GUI
{
    public partial class MainWindow
    {
        private void Layers_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            LayerBlending.SelectedIndex = (int)((GeneralLayerProperties)((TreeViewItem)e.NewValue).Tag).Blending;
        }

        private void LayerBlending_Selected(object sender, RoutedEventArgs e)
        {
            if (Layers.SelectedItem != null)
                ((GeneralLayerProperties)((TreeViewItem)Layers.SelectedItem).Tag).Blending = (GeneralLayerProperties.BlendOp)LayerBlending.SelectedIndex;
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
            slider.AutoToolTipPrecision = (int)(-Math.Min(0, Math.Log10(max - min) - 2));
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
            GeneralLayerProperties props = new GeneralLayerProperties();
            newItem.Tag = props;
            switch (LayerChoose.SelectedIndex)
            {
                case 0:
                    AddSliderProperty(newItem, "Height", 1, 100);
                    AddSliderProperty(newItem, "Quadratic Spline", 0, 1);
                    AddPointSetProperty(newItem);
                    props.Type = GeneralLayerProperties.LayerType.MST_DISTANCE;
                    break;
                case 1:
                    AddSliderProperty(newItem, "Height", 1, 100);
                    AddSliderProperty(newItem, "Quadratic Spline", 0, 1);
                    AddPointSetProperty(newItem);
                    props.Type = GeneralLayerProperties.LayerType.MST_INV_DISTANCE;
                    break;
                case 2:
                    AddSliderProperty(newItem, "Height", 1, 100);
                    AddSliderProperty(newItem, "Height Dependency", 0, 2.5);
                    AddSliderProperty(newItem, "Height Dependency Offset", 0, 1);
                    AddSliderProperty(newItem, "Gradient Dependency", 0, 0.025);
                    props.Type = GeneralLayerProperties.LayerType.VALUE_NOISE;
                    break;
            }
            if (Layers.Items.IsEmpty || (Layers.SelectedItem == null))
                Layers.Items.Add(newItem);
            else Layers.Items.Insert(Layers.Items.IndexOf(Layers.SelectedItem) + 1, newItem);
            newItem.ExpandSubtree();
        }

        private void MoveLayerUp(object sender, RoutedEventArgs e)
        {
            if (Layers.SelectedItem != null)
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
                if (index < Layers.Items.Count - 1)
                {
                    var swap = Layers.Items[index + 1];
                    Layers.Items.RemoveAt(index + 1);
                    Layers.Items.Insert(index, swap);
                }
            }
        }

        private void DeleteLayer(object sender, RoutedEventArgs e)
        {

        }
    }
}
