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
         * <summary>Manages cameras and the objects they see</summary>
         */
        public class Radar
        {
            private static Radar instance;

            public static Radar GetInstance(Program program)
            {
                if (instance == null)
                    instance = new Radar(program);
                return instance;
            }

            private readonly Program program;
            private readonly List<IMyCameraBlock> cameras = new List<IMyCameraBlock>();
            private readonly Dictionary<long, MyDetectedEntityInfo> detectedEntities = new Dictionary<long, MyDetectedEntityInfo>();

            private Radar(Program program)
            {
                this.program = program;

                program.GridTerminalSystem.GetBlocksOfType(cameras);
            }

            /**
             * <summary>Performs a raycast to target (if possible) using any camera and stores it in <c>detectedEntities</c></summary>
             * <param name="distance">The distance to scan to</param>
             * <param name="direction">The direction in which to scan</param>
             * <remarks>Assumes that <c>direction</c> has length 1</remarks>
             * <returns>The detected entity or null if nothing was detected or no cameras can scan to the location; removes cameras from the list if they are broken</returns>
             */
            private MyDetectedEntityInfo? PerformRaycast(double distance, Vector3D direction)
            {
                return PerformRaycast(distance * direction);
            }

            /**
             * <summary>Performs a raycast to target (if possible) using any camera and stores it in <c>detectedEntities</c></summary>
             * <param name="target">The target to scan to</param>
             * <returns>The detected entity or null if nothing was detected or no cameras can scan to the location</returns>
             * <remarks>Removes cameras from the list if they are broken</remarks>
             */
            private MyDetectedEntityInfo? PerformRaycast(Vector3D target)
            {
                for (int i = cameras.Count - 1; i >= 0; i--)
                {
                    MyDetectedEntityInfo entity;
                    try
                    {
                        if (!cameras[i].CanScan(target)) // must be able to scan to where we want
                            continue;

                        entity = cameras[i].Raycast(target);
                    }
                    catch (NullReferenceException)
                    {
                        cameras.RemoveAt(i);
                        continue;
                    }

                    if (entity.IsEmpty())
                        return null; // nothing found

                    detectedEntities[entity.EntityId] = entity; // add or update the entity in the dictionary

                    return entity;
                }

                return null; // no camera could scan
            }

            /**
             * <summary>Enables or disables raycast for cameras so that they store the scanning capability for the given distance</summary>
             * <param name="distance">The scan distance they should store up to</param>
             * <remarks>Removes cameras from the list if they are broken</remarks>
             */
            private void EnableDisableCameras(double distance)
            {
                for (int i = cameras.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        cameras[i].EnableRaycast = !cameras[i].CanScan(distance); // enable raycast if they cannot scan this distance and vice-versa
                    }
                    catch (NullReferenceException)
                    {
                        cameras.RemoveAt(i);
                    }
                }
            }
        }
    }
}
