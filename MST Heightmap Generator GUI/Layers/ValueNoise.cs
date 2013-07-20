
namespace MST_Heightmap_Generator_GUI.Layers
{
    class ValueNoise : Layer
    {
        [LayerAttributes.LayerAttributeFloat(Default = 50, MinValue = 1.0, MaxValue = 100.0, Name = "Height")]
        public float Height { get; set; }

        [LayerAttributes.LayerAttributeFloat(Default = 0.5, MinValue = 0.0, MaxValue = 2.5, Name = "Height Dependency")]
        public float HeightDependency { get; set; }

        [LayerAttributes.LayerAttributeFloat(Default = 1.0, MinValue = 0.0, MaxValue = 1.0, Name = "Height Dependency Offset")]
        public float HeightDependencyOffset { get; set; }

        [LayerAttributes.LayerAttributeFloat(Default = 0.005, MinValue = 0.0, MaxValue = 0.025, Name = "Gradient Dependency")]
        public float GradientDependency { get; set; }
    }
}
