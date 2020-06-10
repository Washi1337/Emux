using System.Collections.Generic;
using System.Windows;
using System.Windows.Media;

namespace Emux
{
    public class EnumDescriptions
    {
        public EnumDescriptions()
        {
            BitmapScalingModeItems = new Dictionary<BitmapScalingMode, string>
            {
                [BitmapScalingMode.Fant] = "Fant",
                [BitmapScalingMode.Linear] = "Linear",
                [BitmapScalingMode.NearestNeighbor] = "Nearest Neighbor"
            };
            StretchItems = new Dictionary<Stretch, string>
            {
                [Stretch.None] = "None",
                [Stretch.Fill] = "Fill",
                [Stretch.Uniform] = "Uniform",
                [Stretch.UniformToFill] = "Uniform to fill"
            };
            Scales = new Dictionary<int, string>
            {
                [0] = "Custom",
                [1] = "1x",
                [2] = "2x",
                [4] = "4x",
                [8] = "8x",
            };
        }

        public Dictionary<BitmapScalingMode, string> BitmapScalingModeItems
        {
            get;
        }

        public Dictionary<Stretch, string> StretchItems
        {
            get;
        }

        public Dictionary<int, string> Scales
        {
            get;
        }

    }
}
