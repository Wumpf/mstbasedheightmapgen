using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using MST_Heightmap_Generator_GUI.Layers;
using SharpDX;

namespace MST_Heightmap_Generator_GUI
{
    public partial class MainWindow
    {
        /// <summary>
        /// Writes all parameters to a json file which are required
        /// to reconstruct the full map.
        /// </summary>
        /// <param name="fileName"></param>
        private void SaveMapAsJson(string fileName)
        {
            StreamWriter outfile = new StreamWriter(fileName);
            string json = SerializeSettingsToJSON();
            outfile.Write(json);
            outfile.Close();
        }


        private void DeserializeSettingsFromJSON(string jsonString)
        {
            // Remove old stuff
            Layers.Items.Clear();

            var json = JObject.Parse(jsonString);
            // Unfortunately the read back must be done manually.
            int resolution = (int)json["HeightmapWidth"];
            SetResolution(resolution);
            foreach (var layer in json["Layers"])
            {
                string type = layer["Type"].ToString();
                Type layerType = Layer.LayerTypes.FirstOrDefault(x => x.Value == type).Key;
                // Unfortunately there are generic and nongeneric methods with the same parameters
                // so GetMethod is ambigious.
                var method = typeof(JsonConvert).GetMethods().Where(m => m.Name == "DeserializeObject" && m.IsGenericMethod && m.GetParameters().Length==1).First();
                Layer param = (Layer)method.MakeGenericMethod(layerType).Invoke(this, new object[] { layer.ToString() });
                AddLayer(param);
            }
        }

        /// <summary>
        /// Reads a mapfile in json-format and resets the whole gui / parametrization
        /// </summary>
        /// <param name="fileName"></param>
        private void LoadFromJson(string fileName)
        {
            StreamReader infile = new StreamReader(fileName);
            string json = infile.ReadToEnd();
            DeserializeSettingsFromJSON(json);
            infile.Close();
        }
    }
}
