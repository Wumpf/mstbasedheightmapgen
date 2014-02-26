using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MST_Heightmap_Generator_GUI.LayerAttributes
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    class LayerAttributePointSet : System.Attribute, LayerAttribute
    {
        public bool InvertedPointSetRendering = false;

        public void CreateTreeViewSubElement(System.Windows.Controls.TreeViewItem parent, int width, Func<object> valueGetFunc, Action<object> valueSetFunc)
        {
            PointSet pointSet = (PointSet)valueGetFunc();
            // set a new empty pointset
            if (pointSet == null)
            {
                pointSet = new PointSet();
            }
            pointSet.InvertedRendering = InvertedPointSetRendering;
            valueSetFunc(pointSet);

            StackPanel panel = new StackPanel();
            panel.Orientation = Orientation.Horizontal;
            int index = parent.Items.Add(panel);
            
            CheckBox visible = new CheckBox();
            visible.IsChecked = true;
            visible.Content = "Point Set";
            visible.Margin = new Thickness(0, 2, 6, 0);
            visible.ToolTip = "Visible in preview rendering";
            visible.Click += (sender, e) => { pointSet.Visible = visible.IsChecked.Value; };
            panel.Children.Add(visible);
            
            TextBox num = new TextBox();
            num.Width = 40;
            num.PreviewTextInput += (sender, e) => { e.Handled = !(e.Text.All(char.IsNumber) && num.Text.Length < 3); };
            num.ToolTip = "Number of points to be generated";
            if (pointSet.Points == null)
                num.Text = "15";
            else num.Text = pointSet.Points.Length.ToString();
            panel.Children.Add(num);

            Button rand = new Button();
            rand.Content = "Randomize";
            panel.Children.Add(rand);

            TextBox seed = new TextBox();
            seed.PreviewTextInput += (sender, e) => { e.Handled = !(e.Text.All(char.IsNumber) && seed.Text.Length < 3); };
            seed.Text = new Random(DateTime.UtcNow.Millisecond).Next(1000).ToString();
            seed.Width = 40;
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


            Rectangle rect = new Rectangle();
            rect.Fill = new SolidColorBrush(new Color() { R = pointSet.Color.R, G = pointSet.Color.G, B = pointSet.Color.B, A = 255 });
            rect.StrokeThickness = 0;
            rect.Width = 20;
            rect.Height = 20;
            panel.Children.Add(rect);

            if (pointSet.Points.Length > 0)
            {
                num.Text = pointSet.Points.Length.ToString();
                seed.Text = pointSet.seed.ToString();
            }

            // create random points or recreate the ones from the file
            randomizePositions();
        }
    }
}
