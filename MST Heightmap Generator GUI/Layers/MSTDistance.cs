
namespace MST_Heightmap_Generator_GUI.Layers
{
    class MSTDistance : Layer
    {
        [LayerAttributes.LayerAttributeFloat(Default = 50.0, MinValue=0.0, MaxValue=100.0, Name="Height")]
        public float Height { get; set; }

        [LayerAttributes.LayerAttributeFloat(Default = 0.3, MinValue = 0.0f, MaxValue = 1.0f, Name = "Quadratic Spline")]
        public float QuadraticSpline { get; set; }

        [LayerAttributes.LayerAttributePointSet(InvertedPointSetRendering = false)]
        public PointSet PointSet { get; set; }
    }

    class MSTDistanceInverse : Layer
    {
        [LayerAttributes.LayerAttributeFloat(Default = 50.0, MinValue = 0.0f, MaxValue = 100.0f, Name = "Height")]
        public float Height { get; set; }

        [LayerAttributes.LayerAttributeFloat(Default = 0.3, MinValue = 0.0f, MaxValue = 1.0f, Name = "Quadratic Spline")]
        public float QuadraticSpline { get; set; }

        [LayerAttributes.LayerAttributePointSet(InvertedPointSetRendering = true)]
        public PointSet PointSet { get; set; }
    }
}
