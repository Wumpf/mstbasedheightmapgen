﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MST_Heightmap_Generator_GUI.Layers
{
    abstract class Layer
    {
        public enum BlendOp
        {
            INTERPOLATE = 0,
            ADDITIVE = 1,
            MULTIPLICATIVE = 2,
            REFACTIVE = 3,
        }
        public BlendOp Blending { get; set; }
        public float BlendFactor { get; set; }


        public static readonly Dictionary<Type, string> LayerTypes = new Dictionary<Type, string>()
        {
            { typeof(MSTDistance) , "MST Distance" },
            { typeof(MSTDistanceInverse) , "MST Inverse Distance" },
            { typeof(ValueNoise) , "Value Noise" },
            { typeof(Refraction) , "Refraction" }
        };
    }
}
