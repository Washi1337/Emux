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
        }

        public Dictionary<BitmapScalingMode, string> BitmapScalingModeItems
        {
            get;
        }

        public Dictionary<Stretch, string> StretchItems
        {
            get;
        }

        
    }
}
