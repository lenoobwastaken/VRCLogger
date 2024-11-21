using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VRCLogger
{
    public static class Confwig
    {
        public class Config
        {
            public GeneralSettings GeneralSettings { get; set; }
            public OverlaySettings OverlaySettings { get; set; }
        }

        public class GeneralSettings
        {
            public bool Vr { get; set; }
            public bool AvatarLog { get; set; }
            public bool JoinLog { get; set; }
            public bool FirstTime { get; set; }

        }

        public class OverlaySettings
        {
            public int FontSize { get; set; }
            public Position Position { get; set; }
            public ColorSettings TextColor { get; set; }
            public ColorSettings BackgroundColor { get; set; }

        }
        public class Position
        {
            public float X { get; set; }
            public float Y { get; set; }
            public float Z { get; set; }
        }
        public class ColorSettings
        {
            public int Red { get; set; }
            public int Blue { get; set; }
            public int Green { get; set; }
            public int Alpha { get; set; }
        }
    }
}
