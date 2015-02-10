using System;
using System.Windows.Controls;

namespace MST_Heightmap_Generator_GUI.LayerAttributes
{
    [System.AttributeUsage(System.AttributeTargets.Property)]
    class LayerAttributeFloat : System.Attribute, LayerAttribute
    {
        public string Name = "__NAME__";
        public double Default = 0.5;
        public double MinValue = 0.0;
        public double MaxValue = 1.0;
        public double TickFrequency = 0.01;
        

        public void CreateTreeViewSubElement(System.Windows.Controls.TreeViewItem parent, int width, Func<object> valueGetFunc, Action<object> valueSetFunc)
        {
            double defValue = (float)valueGetFunc();
            if (defValue == 0.0f) defValue = Default;
            System.Diagnostics.Debug.Assert(MinValue <= MaxValue && defValue >= MinValue && defValue <= MaxValue, "invalid slider ranges/default value");

            Label text = new Label();
            Slider slider = new Slider();
            int index = parent.Items.Add(text);

            text.Content = Name + ": " + defValue;
            text.BorderThickness = new System.Windows.Thickness(1.0);
            text.BorderBrush = System.Windows.Media.Brushes.Gray;
            text.MouseEnter += (s, b) => { parent.Items[index] = slider; };
            text.Width = width - 58;    // TODO: does this work without width? some auto alignment stuff would be nice
            text.Height = 22;
            text.Padding = new System.Windows.Thickness(1.0);

            ToolTip tip = new ToolTip();
            tip.Placement = System.Windows.Controls.Primitives.PlacementMode.Left;
            tip.PlacementTarget = slider;
            tip.Content = Name;
            tip.Height = 22;
            tip.Padding = text.Padding;

            slider.ValueChanged += (s, value) => valueSetFunc((float)value.NewValue);
            slider.Width = width - 58;  // TODO: does this work without width? some auto alignment stuff would be nice
            slider.Height = 22;
            slider.Minimum = MinValue;
            slider.Maximum = MaxValue;
            slider.TickFrequency = TickFrequency;
            slider.IsSnapToTickEnabled = true;
            slider.Value = defValue;
            slider.BorderThickness = new System.Windows.Thickness(1.0);
            slider.BorderBrush = System.Windows.Media.Brushes.Gray;
            slider.AutoToolTipPlacement = System.Windows.Controls.Primitives.AutoToolTipPlacement.TopLeft;
            slider.AutoToolTipPrecision = (int)(-Math.Min(0, Math.Log10(MaxValue - MinValue) - 2));
            slider.MouseEnter += (s, b) => { tip.IsOpen = true; };
            slider.MouseLeave += (s, b) => { Slider sl = (Slider)s; tip.IsOpen = false; text.Content = tip.Content + ": " + Math.Round(sl.Value, sl.AutoToolTipPrecision); parent.Items[index] = text; };
        }
    }
}
