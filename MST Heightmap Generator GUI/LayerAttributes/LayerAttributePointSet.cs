using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MST_Heightmap_Generator_GUI.LayerAttributes
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    class LayerAttributePointSet : System.Attribute, LayerAttribute
    {
        public void CreateTreeViewSubElement(System.Windows.Controls.TreeViewItem parent, int width, Func<object> valueGetFunc, Action<object> valueSetFunc)
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
    }
}
