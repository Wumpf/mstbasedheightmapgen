using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MST_Heightmap_Generator_GUI.Layers
{
    abstract class Layer
    {
        public Layer()
        {
            BlendFactor = 1.0f;
        }

        public enum BlendOp
        {
            INTERPOLATE = 0,
            ADDITIVE = 1,
            MULTIPLICATIVE = 2,
            REFRACTIVE = 3,
        }
        public BlendOp Blending { get; set; }
        public float BlendFactor { get; set; }


        public static readonly Dictionary<Type, string> LayerTypes = new Dictionary<Type, string>()
        {
            { typeof(MSTDistance) , "MST Distance" },
            { typeof(MSTDistanceInverse) , "MST Inverse Distance" },
            { typeof(ValueNoise) , "Value Noise" },
            { typeof(Voronoi), "Voronoi" },
            { typeof(WorleyNoise), "Worley Noise" },
            { typeof(Voronoise), "Voronoise" }
        };
    }
}
