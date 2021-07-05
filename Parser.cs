using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program
    {
        public static class Parser
        { // GPS:mchl12 #1:2661664.23:1149816.23:576506.26:#FF75C9F1:
            public static Vector3D ParseGPS(string gps)
            {
                string[] args = gps.Split(':');
                if (args.Length != 7 || args[0] != "GPS")
                    throw new FormatException("Invalid GPS format");

                return new Vector3D(
                    Double.Parse(args[2]),
                    Double.Parse(args[3]),
                    Double.Parse(args[4])
                );
            }

            public static string CreateGPS(string name, Vector3D coordinates)
            {
                return $"GPS:{name}:{coordinates.X}:{coordinates.Y}:{coordinates.Z}:#FF75C9F1:";
            }
        }
    }
}
