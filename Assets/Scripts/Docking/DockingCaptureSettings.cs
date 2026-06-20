using UnityEngine;

namespace FlightModel.Docking
{
    /// <summary>
    /// Tunable thresholds and forces for the docking capture state machine.
    /// Editable in the inspector; defaults match the P0.1 ticket guidance.
    /// </summary>
    [System.Serializable]
    public class DockingCaptureSettings
    {
        [Tooltip("Max distance from ship docking node to port attach for magnetic capture to start.")]
        public float captureDistanceMeters = 0.5f;

        [Tooltip("Distance below which capture snaps the ship node exactly onto the port attach transform.")]
        public float snapDistanceMeters = 0.08f;

        [Tooltip("Max angle (deg) between ship node forward and -port forward to start capture.")]
        public float maxCaptureAngleDegrees = 8f;

        [Tooltip("Max roll offset (deg) to start capture.")]
        public float maxRollAngleDegrees = 8f;

        [Tooltip("Max closure velocity (m/s) along port forward to start capture.")]
        public float maxClosureSpeedMetersPerSecond = 1.5f;

        [Tooltip("Linear magnetic strength (1/s) used while pulling toward the port.")]
        public float magneticPositionStrength = 4f;

        [Tooltip("Angular magnetic strength (1/s) used while aligning with the port.")]
        public float magneticRotationStrength = 4f;

        [Tooltip("Cap on the magnetic acceleration magnitude.")]
        public float maxMagneticAcceleration = 2f;

        [Tooltip("Capture cannot begin unless the ship docking node is deployed.")]
        public bool requireShipPortDeployed = true;

        [Tooltip("Capture cannot begin unless the station port is available/deployed.")]
        public bool requireStationPortAvailable = true;

        public static DockingCaptureSettings Default => new DockingCaptureSettings();
    }
}
