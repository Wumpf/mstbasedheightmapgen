
namespace MST_Heightmap_Generator_GUI.Layers
{
    class Refraction : Layer
    {
        [LayerAttributes.LayerAttributeFloat(MinValue = 0.0f, MaxValue = 100.0f, Name = "Intensity")]
        public float Intensity { get; set; }
    }
}
