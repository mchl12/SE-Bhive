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
            private readonly Dictionary<long, DetectedEntity> detectedEntities = new Dictionary<long, DetectedEntity>();

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

                    detectedEntities[entity.EntityId] = new DetectedEntity(entity); // add or update the entity in the dictionary

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

            struct DetectedEntity
            {
                public BoundingBoxD BoundingBox { get; }
                public long EntityId { get; }
                public Vector3D HitPosition { get; }
                public string Name { get; }
                public MatrixD Orientation { get; }
                public Vector3D Position { get; }
                public MyRelationsBetweenPlayerAndBlock Relationship { get; }
                public long TimeStamp { get; }
                public MyDetectedEntityType Type { get; }
                public Vector3 Velocity { get; }

                public DetectedEntity(MyDetectedEntityInfo info)
                {
                    BoundingBox = info.BoundingBox;
                    EntityId = info.EntityId;
                    HitPosition = info.HitPosition.Value;
                    Name = info.Name;
                    Orientation = info.Orientation;
                    Position = info.Position;
                    Relationship = info.Relationship;
                    TimeStamp = info.TimeStamp;
                    Type = info.Type;
                    Velocity = info.Velocity;
                }

                public DetectedEntity(MyTuple<BoundingBoxD, long, Vector3D, string, MatrixD, MyTuple<Vector3D, uint, long, uint, Vector3>> info)
                {
                    BoundingBox = info.Item1;
                    EntityId = info.Item2;
                    HitPosition = info.Item3;
                    Name = info.Item4;
                    Orientation = info.Item5;
                    Position = info.Item6.Item1;
                    Relationship = (MyRelationsBetweenPlayerAndBlock)info.Item6.Item2;
                    TimeStamp = info.Item6.Item3;
                    Type = (MyDetectedEntityType)info.Item6.Item4;
                    Velocity = info.Item6.Item5;
                }

                public MyTuple<BoundingBoxD, long, Vector3D, string, MatrixD, MyTuple<Vector3D, uint, long, uint, Vector3>>
                    AsIGCCompatible()
                {
                    return new MyTuple<BoundingBoxD, long, Vector3D, string, MatrixD, MyTuple<Vector3D, uint, long, uint, Vector3>>(
                        BoundingBox,
                        EntityId,
                        HitPosition,
                        Name,
                        Orientation,
                        new MyTuple<Vector3D, uint, long, uint, Vector3>(
                            Position,
                            (uint)Relationship,
                            TimeStamp,
                            (uint)Type,
                            Velocity
                        )
                    );
                }
            }
        }
    }
}
