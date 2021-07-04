using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Diagnostics;
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
    partial class Program : MyGridProgram
    {
        private readonly IMyBroadcastListener locationListener;

        private Vector3D? target;

        public Program()
        {
            locationListener = IGC.RegisterBroadcastListener("location"); // initiaize broadcastlistener
            locationListener.SetMessageCallback();

            Runtime.UpdateFrequency |= UpdateFrequency.Update10; // set update frequency

            Echo("Compiled");
        }

        public void Main(string argument, UpdateType updateSource)
        {
            if ((updateSource & UpdateType.IGC) != 0)
            {
                while (locationListener.HasPendingMessage)
                {
                    HandleIGC(locationListener.AcceptMessage());
                }
            }

            if ((updateSource & UpdateType.Update10) != 0)
            {
            }
        }

        private void HandleIGC(MyIGCMessage message)
        {
            if (message.Tag == "location")
            {
                Vector3D? location = message.Data as Vector3D?;
                if (!location.HasValue)
                    return;
                target = location.Value;
            }
        }
    }
}
