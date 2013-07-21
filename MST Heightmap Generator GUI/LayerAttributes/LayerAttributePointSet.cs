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
            // set a new empty pointset
            valueSetFunc(new PointSet());

            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            int index = parent.Items.Add(panel);

            CheckBox visible = new CheckBox();
            visible.IsChecked = true;
            visible.Content = "Point Set";
            visible.Margin = new Thickness(0, 2, 6, 0);
            visible.ToolTip = "Visible in preview rendering";
            panel.Children.Add(visible);

            TextBox num = new TextBox();
            num.Width = 38;
            num.PreviewTextInput += (sender, e) => { e.Handled = !(e.Text.All(char.IsNumber) && num.Text.Length < 3); };
            num.ToolTip = "Number of points to be generated";
            num.Text = "15";
            panel.Children.Add(num);

            Button rand = new Button();
            rand.Content = "Randomize";
            panel.Children.Add(rand);

            TextBox seed = new TextBox();
            seed.Text = new Random(DateTime.UtcNow.Millisecond).Next(1000).ToString();
            seed.Width = 57;
            seed.ToolTip = "Initial value for randomize (number)";
            panel.Children.Add(seed);

            Action randomizePositions = () => 
                {
                    uint numPoints;
                    if(!uint.TryParse(num.Text, out numPoints))
                        numPoints = 15; // reset
                    num.Text = numPoints.ToString();
                    int seedValue;
                    if(!int.TryParse(seed.Text, out seedValue))
                        seedValue = new Random(DateTime.UtcNow.Millisecond).Next(1000); // reset
                    seed.Text = seedValue.ToString();
                    ((PointSet)valueGetFunc()).CreateRandomPoints(seedValue, numPoints, MainWindow.MAP_SIZE, MainWindow.MAP_SIZE);
                };
            rand.Click += (a, b) => randomizePositions();

            // create random points
            randomizePositions();
        }
    }
}
