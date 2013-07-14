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
        private void InitLayerChooseComobBox()
        {
            foreach (Type type in Layer.LayerTypes.Keys)
            {
                ComboBoxItem newItem = new ComboBoxItem();
                newItem.Content = Layer.LayerTypes[type];
                newItem.Tag = type;
                LayerChoose.Items.Add(newItem);
            }
        }

        private void Layers_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            LayerBlending.SelectedIndex = (int)((Layer)((TreeViewItem)e.NewValue).Tag).Blending;
        }

        private void LayerBlending_Selected(object sender, RoutedEventArgs e)
        {
            if (Layers.SelectedItem != null)
                ((Layer)((TreeViewItem)Layers.SelectedItem).Tag).Blending = (Layer.BlendOp)LayerBlending.SelectedIndex;
        }

        private void AddLayer(object sender, RoutedEventArgs e)
        {
            Type layerClassType = (Type)((ComboBoxItem)LayerChoose.SelectedItem).Tag;
            Layer newLayerClass = (Layer)layerClassType.GetConstructor(new Type[0]).Invoke(new object[0]);

            TreeViewItem newItem = new TreeViewItem();
            newItem.Header = ((ComboBoxItem)LayerChoose.SelectedItem).Content;
            newItem.Tag = newLayerClass;
            foreach(var p in layerClassType.GetProperties())
            {
                var property = p;   // we want to closure this value in lambdas - therefore there must be an additional variable - do not use the foreach variable p!
                LayerAttributes.LayerAttribute[] layerAttributeList = (LayerAttributes.LayerAttribute[])
                                        property.GetCustomAttributes(typeof(LayerAttributes.LayerAttribute), false);
                if(layerAttributeList.Length > 0)
                {
                    Type floattype = typeof(float);
                    layerAttributeList[0].CreateTreeViewSubElement(newItem, (int)Layers.Width,
                                () => { return property.GetMethod.Invoke(newLayerClass, new object[0]); },
                                (value) => { property.SetMethod.Invoke(newLayerClass, new object[] { value }); });
                }
            }
            if (Layers.Items.IsEmpty || (Layers.SelectedItem == null))
                Layers.Items.Add(newItem);
            else
                Layers.Items.Insert(Layers.Items.IndexOf(Layers.SelectedItem) + 1, newItem);
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
            // TODO: Delete Layer
        }
    }
}
