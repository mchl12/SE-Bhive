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
         * <summary>AutoPilot offers functions for flying and maneuvering ships</summary>
         */
        public class AutoPilot
        {
            private static AutoPilot instance; // singleton pattern because there can only be one instance

            /**
             * <summary>Converts a direction vector in the world grid to a direction vector in the local grid</summary>
             * <param name="v">The vector to convert</param>
             * <param name="worldMatrix">The world matrix to use for the conversion</param>
             */
            private static Vector3D WorldDirectionToGrid(Vector3D v, MatrixD worldMatrix)
            {
                return Vector3D.TransformNormal(v, MatrixD.Transpose(worldMatrix));
            }

            /**
             * <summary>Converts a direction vector in the local grid to a direction vector in the world grid</summary>
             * <param name="v">The vector to convert</param>
             * <param name="worldMatrix">The world matrix to use for the conversion</param>
             */
            private static Vector3D GridDirectionToWorld(Vector3D v, MatrixD worldMatrix)
            {
                return Vector3D.TransformNormal(v, worldMatrix);
            }

            private readonly Program program;

            private readonly List<IMyShipController> shipControllers = new List<IMyShipController>(); // we keep track of multiple ship controllers in case one breaks

            private readonly ThrusterGroup[] thrusterGroups;

            private AutoPilot(Program program)
            {
                this.program = program;

                thrusterGroups = new ThrusterGroup[6] { // one group for each of the 6 directions
                    new ThrusterGroup(program),
                    new ThrusterGroup(program),
                    new ThrusterGroup(program),
                    new ThrusterGroup(program),
                    new ThrusterGroup(program),
                    new ThrusterGroup(program)
                };

                program.GridTerminalSystem.GetBlocksOfType(null, (IMyTerminalBlock b) =>
                {
                    if (b is IMyShipController) // add ship controllers
                        shipControllers.Add((IMyShipController)b);
                    else if (b is IMyThrust) // add thrusters to the correct group
                    {
                        IMyThrust t = (IMyThrust)b;
                        Base6Directions.Direction dir = t.Orientation.Forward;
                        thrusterGroups[(int)dir].AddThruster(t);
                    }

                    return false;
                });
            }

            /**
             * <returns>The (only) instance of this class</returns>
             */
            public static AutoPilot GetInstance(Program program)
            {
                if (instance == null)
                    instance = new AutoPilot(program);
                return instance;
            }

            /**
             * <summary>Tries to set the thrusters so they drive the ship to the target</summary>
             * <remarks>Can fail if thrusters are missing or if there are no ship controllers</remarks>
             */
            public void TrySetThrustersToTarget(Vector3D target)
            {
                Vector3D? desiredForce = ComputeTargetForce(target);
                if (!desiredForce.HasValue)
                    return;

                SetThrustersToForce(desiredForce.Value);
            }

            /**
             * <summary>Calculate the desired force to steer the ship into the direction of the target</summary>
             * <param name="target">The target to steer towards</param>
             * <returns>The needed force (world grid vector).</returns>
             * <remarks>Returns null if there are no ship controllers; Will try to find new ship controllers</remarks>
             */
            private Vector3D? ComputeTargetForce(Vector3D target)
            {
                if (shipControllers.Count == 0)
                {
                    program.GridTerminalSystem.GetBlocksOfType(shipControllers); // try to find new controllers
                    if (shipControllers.Count == 0)
                    {
                        program.Echo("ERROR: Autopilot: no ship controllers");
                        return null;
                    }
                }

                IMyShipController shipController = shipControllers[shipControllers.Count - 1];

                try
                {
                    Vector3D v_desired = (target - shipController.GetPosition()) / 5; // part of the distance (don't go too fast)
                    if (v_desired.LengthSquared() > 10000.0) // if faster than the max speed
                        v_desired = v_desired / v_desired.Length() * 100; // adjust to max speed

                    Vector3D a_desired = v_desired - shipController.GetShipVelocities().LinearVelocity;
                    return a_desired * shipController.Mass * 60; // multiply by game ticks so we accelerate this amount in 1 tick
                }
                catch (NullReferenceException) // catch disappeared ship controller
                {
                    shipControllers.RemoveAt(shipControllers.Count - 1); // remove last element
                    return ComputeTargetForce(target); // try again
                }
            }

            /**
             * <summary>Sets the thrusters so they get as close as possible to the target force</summary>
             * <param name="force">The desired force of the ship</param>
             */
            private void SetThrustersToForce(Vector3D targetForce)
            {
                for (int i = 0; i < 6; i++)
                {
                    Vector3D? optThrustDir = thrusterGroups[i].GetThrustDirection();
                    if (!optThrustDir.HasValue) // if there are no thrusters in this direction we continue
                        continue;
                    Vector3D thrustDir = -optThrustDir.Value; // invert the value because we want the direction in which the ship will be pushed

                    double componentOfTargetForce = thrustDir.Dot(targetForce); // the component of the target force that goes in the direction of thrustDir
                    if (componentOfTargetForce <= 0) // thruster won't help
                    {
                        thrusterGroups[i].SetThrustPercentage(0f); // turn off thrusters
                    }

                    double totalEffectiveThrust = thrusterGroups[i].GetTotalEffectiveThrust(); // the maximum effective thrust we can exert in this direction
                    double thrustAmount = Math.Min(componentOfTargetForce, totalEffectiveThrust); // as much as the thrusters can do or as much as is required
                    thrusterGroups[i].SetThrustPercentage((float)(thrustAmount / totalEffectiveThrust)); // set the thrusters
                }
            }
        }
    }
}
