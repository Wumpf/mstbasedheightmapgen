
namespace MST_Heightmap_Generator_GUI.Layers
{
    class Voronoise : Layer
    {
        [LayerAttributes.LayerAttributeFloat(Default = 50, MinValue = 1.0, MaxValue = 100.0, Name = "Height")]
        public float Height { get; set; }

        [LayerAttributes.LayerAttributeFloat(Default = 0, MinValue = 0.0, MaxValue = 6, Name = "Min Octave", TickFrequency = 1)]
        public float MinOctave { get; set; }

        [LayerAttributes.LayerAttributeFloat(Default = 0, MinValue = 0.0, MaxValue = 6, Name = "Max Octave", TickFrequency = 1)]
        public float MaxOctave { get; set; }
    }
}
