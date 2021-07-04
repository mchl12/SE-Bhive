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
        public class AutoPilot
        {
            private static AutoPilot instance;

            public static Vector3D WorldDirectionToGrid(Vector3D v, MatrixD worldMatrix)
            {
                return Vector3D.TransformNormal(v, MatrixD.Transpose(worldMatrix));
            }

            public static Vector3D GridDirectionToWorld(Vector3D v, MatrixD worldMatrix)
            {
                return Vector3D.TransformNormal(v, worldMatrix);
            }

            private readonly Program program;

            private readonly List<IMyShipController> shipControllers = new List<IMyShipController>();
            private readonly List<IMyThrust> thrusters = new List<IMyThrust>();

            private AutoPilot(Program program)
            {
                this.program = program;
                
                program.GridTerminalSystem.GetBlocksOfType(shipControllers); // get ship controllers
                program.GridTerminalSystem.GetBlocksOfType(thrusters); // get thrusters
            }

            public AutoPilot GetInstance(Program program)
            {
                if (instance == null)
                    instance = new AutoPilot(program);
                return instance;
            }

            /**
             * <summary>Tries to set the thrusters so they drive the ship to the target</summary>
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
                    Vector3D v_desired = (target - shipController.GetPosition()) / 2; // half of the distance (don't go too fast)
                    if (v_desired.LengthSquared() > 10000.0) // if faster than the max speed
                        v_desired = v_desired / v_desired.Length() * 100; // adjust to max speed

                    Vector3D a_desired = v_desired - shipController.GetShipVelocities().LinearVelocity;
                    return a_desired * shipController.Mass;
                }
                catch (NullReferenceException) // catch disappeared ship controller
                {
                    shipControllers.RemoveAt(shipControllers.Count - 1); // remove last element
                    return ComputeTargetForce(target); // try again
                }
            }

            /**
             * <summary>Sets the thrusters so they get as close as possible to the target force</summary>
             * <param name="force">The desired force</param>
             */
            private void SetThrustersToForce(Vector3D targetForce)
            {
                Vector3D gridTargetForce = WorldDirectionToGrid(targetForce, program.Me.WorldMatrix);

                for (int i = thrusters.Count - 1; i >= 0; i--)
                {
                    try
                    {
                        if (!thrusters[i].IsWorking)
                            continue;

                        Vector3D thrustDir = thrusters[i].GridThrustDirection;

                        
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
