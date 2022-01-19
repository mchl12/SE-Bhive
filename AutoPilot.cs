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
            private static Action<string> Echo;

            private static readonly double BRAKING_THRESHOLD = 0.9;

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
            private readonly Radar radar;

            private AutoPilot(Program program)
            {
                this.program = program;
                radar = Radar.GetInstance(program);

                Echo = program.Echo;
                //Echo = (string text) => { program.Me.CustomData += $"{text}\n"; };
                //Echo = (string text) => { };

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
                program.Me.CustomData = "";

                // get relevant information from the ship controller
                ShipValues? shipValues = TryGetShipValues();
                if (!shipValues.HasValue)
                    return;

                Vector3D position = shipValues.Value.Position;
                Vector3D currentVelocity = shipValues.Value.LinearVelocity;
                Vector3D gravity = shipValues.Value.Gravity;
                float mass = shipValues.Value.Mass;

                Vector3D distanceToTravel = target - position;

                for (int i = 0; i < 6; i++)
                {
                    CalculateAndSetThrust(thrusterGroups[i], distanceToTravel, currentVelocity, gravity, mass);
                }
            }

            /**
             * <returns>Information about the physics of the ship</returns>
             * <remarks>Returns null if there are no ship controllers; can add and remove elements to and from the ship controller list</remarks>
             */
            private ShipValues? TryGetShipValues()
            {
                if (shipControllers.Count == 0)
                {
                    program.GridTerminalSystem.GetBlocksOfType(shipControllers); // try to find new controllers
                    if (shipControllers.Count == 0)
                    {
                        Echo("ERROR: Autopilot: no ship controllers");
                        return null;
                    }
                }

                IMyShipController shipController = shipControllers[shipControllers.Count - 1];

                try
                {
                    return new ShipValues(
                        shipController.GetPosition(),
                        shipController.GetShipVelocities().LinearVelocity,
                        shipController.GetNaturalGravity(),
                        shipController.CalculateShipMass().TotalMass
                    );
                }
                catch (NullReferenceException) // catch disappeared ship controller
                {
                    shipControllers.RemoveAt(shipControllers.Count - 1); // remove last element
                    return TryGetShipValues(); // try again
                }
            }

            private void CalculateAndSetThrust(ThrusterGroup thrusters, Vector3D distanceToTravel, Vector3D currentVelocity, Vector3D gravity, float mass)
            {
                // get necessary information
                ThrusterValues? thrusterValues = thrusters.GetThrusterValues();
                if (!thrusterValues.HasValue) // this means no thrusters in the group
                    return;
                
                Vector3D thrustDirection = -thrusterValues.Value.ThrustDirection;
                float totalEffectiveThrust = thrusterValues.Value.TotalEffectiveThrust;

                // calculate inproducts
                double inproductDistanceToTravel = distanceToTravel.Dot(thrustDirection);
                double inproductCurrentVelocity = currentVelocity.Dot(thrustDirection);
                double inproductGravity = gravity.Dot(thrustDirection);

                // calculate needed decelleration
                double neededDecelleration; // this represents the acceleration (in the opposite direction of currentVelocity) needed to stand still exactly when we arrive
                if (inproductDistanceToTravel == 0.0)
                    neededDecelleration = inproductCurrentVelocity * 6.0; // try to stand still in 10 ticks
                else
                    neededDecelleration = 0.5 * inproductCurrentVelocity * inproductCurrentVelocity / Math.Abs(inproductDistanceToTravel) + Math.Sign(inproductCurrentVelocity) * inproductGravity; // a = 0.5 * v^2 / |x| - g (for constant acceleration)
                                                                                                                                                                    // gravity added or subtracted depending on whether it is with or against the velocity
                // set thrust
                double decellerationForcePercentage = neededDecelleration * mass / totalEffectiveThrust; // the percentage of available effective force needed to decellerate the amount we want
                
                if (decellerationForcePercentage > BRAKING_THRESHOLD) // compare percentage of force needed to decellerate with the threshold
                { // we should start braking
                    if (inproductCurrentVelocity < 0) // only attempt to break if we thrust opposite to the current velocity
                    {
                        thrusters.SetThrustPercentage((float)Math.Min(decellerationForcePercentage, 1.0)); // limit to 100%
                    }
                    else // either thrusters don't help or do the opposite of what we want
                        thrusters.SetThrustPercentage(0f);
                }
                else
                { // we can accelerate some more
                    Vector3D desiredVelocity = distanceToTravel;
                    if (desiredVelocity.LengthSquared() > 100.0*100.0) // limit the speed to 100 m/s
                        desiredVelocity = desiredVelocity / desiredVelocity.Length() * 100.0;
                    double inproductDesiredVelocity = desiredVelocity.Dot(thrustDirection);
                    double inproductDesiredAcceleration = 6.0 * (inproductDesiredVelocity - inproductCurrentVelocity) - inproductGravity; // * 6.0 to accelerate to that speed within 10 ticks

                    if (inproductDesiredAcceleration > 0) // only attempt if we can accelerate in the desired direction
                    {
                        double accelerationForcePercentage = inproductDesiredAcceleration * mass / totalEffectiveThrust; // percentage of available force
                        thrusters.SetThrustPercentage((float)Math.Min(accelerationForcePercentage, 1.0)); // limit to 100%
                    }
                    else // thrusters don't help or do the opposite of what we want
                        thrusters.SetThrustPercentage(0f);
                }
            }

            public struct ShipValues
            {
                public Vector3D Position { get; }
                public Vector3D LinearVelocity { get; }
                public Vector3D Gravity { get; }
                public float Mass { get; }

                public ShipValues(Vector3D position, Vector3D linearVelocity, Vector3D gravity, float mass)
                {
                    Position = position;
                    LinearVelocity = linearVelocity;
                    Gravity = gravity;
                    Mass = mass;
                }
            }
        }
    }
}
