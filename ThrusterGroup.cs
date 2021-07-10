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
        /**
         * <summary>This class groups thrusters that thrust in the same direction</summary>
         */
        public class ThrusterGroup
        {
            private readonly Program program;
            private readonly List<IMyThrust> thrusters = new List<IMyThrust>();
            
            public ThrusterGroup(Program program)
            {
                this.program = program;
            }

            /**
             * <summary>Adds a thruster to this group</summary>
             * <remarks>Assumes that this thruster is in the same direction as all thrusters in this group</remarks>
             */
            public void AddThruster(IMyThrust thruster)
            {
                thrusters.Add(thruster);
            }

            public ThrusterValues? GetThrusterValues()
            {
                Vector3D? thrustDirection = null;
                float totalEffectiveThrust = 0f;

                for (int i = thrusters.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        if (!thrustDirection.HasValue) // set thrustdirection if still unknown
                            thrustDirection = thrusters[i].WorldMatrix.Forward;

                        if (!thrusters[i].IsWorking) // only count thrust if the thruster is working
                            continue;
                        totalEffectiveThrust += thrusters[i].MaxEffectiveThrust;
                    }
                    catch (NullReferenceException)
                    {
                        thrusters.RemoveAt(i);
                    }
                }

                if (!thrustDirection.HasValue)
                    return null;

                return new ThrusterValues(thrustDirection.Value, totalEffectiveThrust);
            }

            /**
             * <summary>Sets the ThrustOverridePercentage of all thrusters in this group</summary>
             * <param name="percentage">The percentage to set the thrust to</param>
             * <remarks>Assumes that percentage is between 0f and 1f; Will remove thrusters that have broken.</remarks>
             */
            public void SetThrustPercentage(float percentage)
            {
                for (int i = thrusters.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        thrusters[i].ThrustOverridePercentage = percentage;
                    }
                    catch (NullReferenceException)
                    {
                        thrusters.RemoveAt(i);
                    }
                }
            }
        }

        public struct ThrusterValues
        {
            public Vector3D ThrustDirection { get; }
            public float TotalEffectiveThrust { get; }

            public ThrusterValues(Vector3D thrustDirection, float totalEffectiveThrust)
            {
                ThrustDirection = thrustDirection;
                TotalEffectiveThrust = totalEffectiveThrust;
            }
        }
    }
}
