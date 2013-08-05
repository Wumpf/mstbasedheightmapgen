
namespace MST_Heightmap_Generator_GUI.Layers
{
    class Voronoi : Layer
    {
        [LayerAttributes.LayerAttributeFloat(Default = 50, MinValue = 1.0, MaxValue = 100.0, Name = "Height")]
        public float Height { get; set; }

        [LayerAttributes.LayerAttributePointSet(InvertedPointSetRendering = true)]
        public PointSet PointSet { get; set; }
    }
}
