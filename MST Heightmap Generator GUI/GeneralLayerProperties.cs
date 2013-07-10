using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MST_Heightmap_Generator_GUI
{
    class GeneralLayerProperties
    {
        public enum BlendOp
        {
            ADDITIVE = 0,
            MULTIPLICATIVE = 1,
            REFRACTIVE = 2
        }

        public BlendOp Blending { get; set; }

        public enum LayerType
        {
            MST_DISTANCE = 0,
            MST_INV_DISTANCE = 1,
            VALUE_NOISE = 2
        }

        public LayerType Type { get; set; }
    }
}
