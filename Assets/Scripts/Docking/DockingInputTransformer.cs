using UnityEngine;

namespace FlightModel.Docking
{
    /// <summary>
    /// Translates pilot ShipInputCommand from the docking camera/node frame into
    /// ship-local axes that the existing flight solver accepts.
    ///
    /// Conceptual pipeline (matches ticket T10 guidance):
    ///   pilot axes -> desired world vector in docking node frame
    ///   world vector -> ship local via inverse ship rotation
    ///   ship local vector -> ShipInputCommand.thrustRight/thrustUp/thrustForward
    ///
    /// Forced for docking mode:
    ///   fineControl = true (current solver uses maneuver forward in fine control,
    ///   bypassing main engines; per T09 `ShipSimulator.BuildThrusterOutput`).
    ///   boost       = false (no main engine authority in docking).
    /// </summary>
    public static class DockingInputTransformer
    {
        public static ShipInputCommand Transform(
            in ShipInputCommand pilot,
            in ShipState shipState,
            ShipDockingNode dockingNode)
        {
            if (dockingNode == null)
            {
                return pilot;
            }

            DockingFrame dockingFrame = DockingFrameUtility.FromTransform(
                dockingNode.NodeTransform,
                dockingNode.ActionAxisLocal);

            // Translation axes expressed in docking frame, summed into a world vector.
            Vector3 desiredWorldLinear =
                  pilot.thrustForward * dockingFrame.forward
                + pilot.thrustRight * dockingFrame.right
                + pilot.thrustUp * dockingFrame.up;

            Quaternion worldToShip = Quaternion.Inverse(shipState.rotation);
            Vector3 shipLocalLinear = worldToShip * desiredWorldLinear;

            // Angular axes: pitch about dockRight, yaw about dockUp, roll about dockFwd.
            Vector3 desiredWorldAngular =
                  pilot.pitch * dockingFrame.right
                + pilot.yaw * dockingFrame.up
                + pilot.roll * dockingFrame.forward;
            Vector3 shipLocalAngular = worldToShip * desiredWorldAngular;

            return new ShipInputCommand
            {
                thrustRight = ClampAxis(shipLocalLinear.x),
                thrustUp = ClampAxis(shipLocalLinear.y),
                thrustForward = ClampAxis(shipLocalLinear.z),
                pitch = ClampAxis(shipLocalAngular.x),
                yaw = ClampAxis(shipLocalAngular.y),
                roll = ClampAxis(shipLocalAngular.z),
                boost = false,
                fineControl = true,
                brake = pilot.brake,
                firePrimary = pilot.firePrimary,
            };
        }

        static float ClampAxis(float value) => Mathf.Clamp(value, -1f, 1f);
    }
}
