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

            /**
             * <returns>The total MaxEffectiveThrust of all thrusters</returns.>
             */
            public double GetTotalEffectiveThrust()
            {
                double total = 0.0;
                for (int i = thrusters.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        if (!thrusters[i].IsWorking) // thruster must be working
                            continue;
                        total += thrusters[i].MaxEffectiveThrust;
                    }
                    catch (NullReferenceException)
                    {
                        thrusters.RemoveAt(i);
                    }
                }
                return total;
            }

            /**
             * <returns>The direction in which this group of thrusters thrusts in</returns>
             * <remarks>Returns null if there are no thrusters in this group. Will remove thrusters that have broken.</remarks>
             */
            public Vector3D? GetThrustDirection()
            {
                for (int i = thrusters.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        return thrusters[i].WorldMatrix.Forward;
                    }
                    catch (NullReferenceException)
                    {
                        thrusters.RemoveAt(i);
                    }
                }
                
                return null;
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
    }
}
