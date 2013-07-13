
namespace MST_Heightmap_Generator_GUI.Layers
{
    class MSTDistance : Layer
    {
        [LayerAttributes.LayerAttributeFloat(MinValue=0.0f, MaxValue=100.0f, Name="Height")]
        public float Height { get; set; }

        [LayerAttributes.LayerAttributeFloat(MinValue = 0.0f, MaxValue = 1.0f, Name = "Quadratic Spline")]
        public float QuadraticSpline { get; set; }

        [LayerAttributes.LayerAttributePointSet]
        public PointSet PointSet { get; set; }
    }

    class MSTDistanceInverse : MSTDistance
    {
    }
}
