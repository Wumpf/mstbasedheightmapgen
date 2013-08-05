
namespace MST_Heightmap_Generator_GUI.Layers
{
    class WorleyNoise : Layer
    {
        [LayerAttributes.LayerAttributeFloat(Default = 50, MinValue = 1.0, MaxValue = 100.0, Name = "Height")]
        public float Height { get; set; }

        [LayerAttributes.LayerAttributeFloat(Default = 0, MinValue = 0.0, MaxValue = 2.0, Name = "n-th Neighbor")]
        public float NthNeighbor { get; set; }

        [LayerAttributes.LayerAttributePointSet(InvertedPointSetRendering = false)]
        public PointSet PointSet { get; set; }
    }
}
