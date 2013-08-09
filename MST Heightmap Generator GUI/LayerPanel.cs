using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MST_Heightmap_Generator_GUI.Layers;

namespace MST_Heightmap_Generator_GUI
{
    public partial class MainWindow
    {
        private void InitLayerChooseComboBox()
        {
            foreach (Type type in Layer.LayerTypes.Keys)
            {
                ComboBoxItem newItem = new ComboBoxItem();
                newItem.Content = Layer.LayerTypes[type];
                newItem.Tag = type;
                LayerChoose.Items.Add(newItem);
            }
        }

        private void InitLayerBlendingComboBox()
        {
            foreach (Layer.BlendOp type in Enum.GetValues(typeof(Layer.BlendOp)))
            {
                ComboBoxItem newItem = new ComboBoxItem();
                string valueName = type.ToString().ToLower();
                valueName = valueName.Replace('_', ' ');
                newItem.Content = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(valueName);
                
                LayerBlending.Items.Add(newItem);
            }
        }

        private void Layers_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            TreeViewItem item = e.NewValue as TreeViewItem;
            if (item != null)
            {
                LayerBlending.SelectedIndex = (int)((Layer)item.Tag).Blending;
                BlendFactor.Value = ((Layer)item.Tag).BlendFactor;
            }
        }

        private void LayerBlending_Selected(object sender, RoutedEventArgs e)
        {
            if (Layers.SelectedItem != null)
                ((Layer)((TreeViewItem)Layers.SelectedItem).Tag).Blending = (Layer.BlendOp)LayerBlending.SelectedIndex;
        }

        private void Sl_BlendFactor_Changed(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (Layers.SelectedItem != null)
                ((Layer)((TreeViewItem)Layers.SelectedItem).Tag).BlendFactor = (float)BlendFactor.Value;
        }

        private void CreateLayer(Type layerClassType, Layer newLayerClass)
        {
            TreeViewItem newItem = new TreeViewItem();
            newItem.Header = Layer.LayerTypes[layerClassType];// ((ComboBoxItem)LayerChoose.SelectedItem).Content;
            newItem.Tag = newLayerClass;
            foreach (var p in layerClassType.GetProperties())
            {
                var property = p;   // we want to closure this value in lambdas - therefore there must be an additional variable - do not use the foreach variable p!
                LayerAttributes.LayerAttribute[] layerAttributeList = (LayerAttributes.LayerAttribute[])
                                        property.GetCustomAttributes(typeof(LayerAttributes.LayerAttribute), false);
                if (layerAttributeList.Length > 0)
                {
                    // create ui for this element
                    layerAttributeList[0].CreateTreeViewSubElement(newItem, (int)Layers.ActualWidth,
                                () => { return property.GetMethod.Invoke(newLayerClass, new object[0]); },
                                (value) => { property.SetMethod.Invoke(newLayerClass, new object[] { value }); });

                    // pointset layer must be registred
                    if (layerAttributeList[0] is LayerAttributes.LayerAttributePointSet)
                        terrainRenderingPreview.AddPointSet((PointSet)property.GetMethod.Invoke(newLayerClass, new object[0]));
                }
            }
            if (Layers.Items.IsEmpty || (Layers.SelectedItem == null))
                Layers.Items.Insert(0, newItem);
            else
                Layers.Items.Insert(Layers.Items.IndexOf(Layers.SelectedItem), newItem);
            newItem.ExpandSubtree();
            newItem.IsSelected = true;
        }

        private void AddLayer(object sender, RoutedEventArgs e)
        {
            Type layerClassType = (Type)((ComboBoxItem)LayerChoose.SelectedItem).Tag;
            Layer newLayerClass = (Layer)layerClassType.GetConstructor(new Type[0]).Invoke(new object[0]);

            CreateLayer(layerClassType, newLayerClass);
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
            if (Layers.SelectedItem != null)
            {
                PointSet set = null;

                foreach (var p in ((TreeViewItem)Layers.SelectedItem).Tag.GetType().GetProperties())
                {
                    if (p.PropertyType == typeof(PointSet))
                    {
                        set = (PointSet)p.GetValue(((TreeViewItem)Layers.SelectedItem).Tag);
                        break;
                    }
                }
                // remove pointset if necessary
                if(set != null) terrainRenderingPreview.RemovePointSet(set);
                Layers.Items.Remove(Layers.SelectedItem);
            }
        }
    }
}
