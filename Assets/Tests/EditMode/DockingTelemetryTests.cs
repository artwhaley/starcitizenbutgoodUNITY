using FlightModel.Docking;
using NUnit.Framework;
using UnityEngine;

namespace FlightModel.Tests
{
    public class DockingTelemetryTests
    {
        readonly System.Collections.Generic.List<Object> created = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < created.Count; i++)
            {
                if (created[i] != null)
                {
                    Object.DestroyImmediate(created[i]);
                }
            }

            created.Clear();
        }

        [Test]
        public void CoincidentOpposingDockingAxes_ReportZeroGuidance()
        {
            Quaternion shipRotation = RotationForDockingFrame(Vector3.forward, Vector3.up);
            StationDockingPort target = CreatePort(
                Vector3.zero,
                RotationForDockingFrame(Vector3.back, Vector3.up));

            DockingTelemetry telemetry = DockingTelemetryUtility.ComputeFromPoses(
                Vector3.zero,
                shipRotation,
                BlenderImportedAxes.DefaultActionAxisLocal,
                Vector3.zero,
                true,
                target);

            Assert.IsTrue(telemetry.hasTarget);
            Assert.That(telemetry.distanceMeters, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(telemetry.lateralOffsetMeters.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(telemetry.lateralOffsetMeters.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(telemetry.lateralAxisOffsetMeters.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(telemetry.lateralAxisOffsetMeters.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(telemetry.lateralGuidanceDegrees.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(telemetry.lateralGuidanceDegrees.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(telemetry.lateralNeedleNormalized.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(telemetry.lateralNeedleNormalized.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(telemetry.angularAxisError.x, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(telemetry.angularAxisError.y, Is.EqualTo(0f).Within(0.0001f));
            Assert.That(telemetry.rollOffsetDegrees, Is.EqualTo(0f).Within(0.0001f));
        }

        [Test]
        public void LateralOffset_IsMeasuredInShipDockingCameraFrame()
        {
            Quaternion shipRotation = RotationForDockingFrame(Vector3.forward, Vector3.up);
            StationDockingPort target = CreatePort(
                Vector3.right * 2f + Vector3.up * -3f + Vector3.forward * 10f,
                RotationForDockingFrame(Vector3.back, Vector3.up));

            DockingTelemetry telemetry = DockingTelemetryUtility.ComputeFromPoses(
                Vector3.zero,
                shipRotation,
                BlenderImportedAxes.DefaultActionAxisLocal,
                Vector3.zero,
                true,
                target);

            Assert.That(telemetry.lateralOffsetMeters.x, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(telemetry.lateralOffsetMeters.y, Is.EqualTo(-3f).Within(0.0001f));
        }

        [Test]
        public void LateralNeedles_UseTargetPortAxisGuidance()
        {
            Quaternion shipRotation = RotationForDockingFrame(Vector3.forward, Vector3.up);
            Quaternion targetRotation = RotationForDockingFrame(Vector3.back, Vector3.up);
            StationDockingPort rightTarget = CreatePort(Vector3.right * 2f + Vector3.forward * 10f, targetRotation);
            StationDockingPort leftTarget = CreatePort(Vector3.left * 2f + Vector3.forward * 10f, targetRotation);

            DockingTelemetry right = DockingTelemetryUtility.ComputeFromPoses(
                Vector3.zero,
                shipRotation,
                BlenderImportedAxes.DefaultActionAxisLocal,
                Vector3.zero,
                true,
                rightTarget);
            DockingTelemetry left = DockingTelemetryUtility.ComputeFromPoses(
                Vector3.zero,
                shipRotation,
                BlenderImportedAxes.DefaultActionAxisLocal,
                Vector3.zero,
                true,
                leftTarget);

            Assert.That(Mathf.Abs(right.lateralGuidanceDegrees.x), Is.GreaterThan(0.1f));
            Assert.That(Mathf.Sign(right.lateralGuidanceDegrees.x), Is.EqualTo(-Mathf.Sign(left.lateralGuidanceDegrees.x)));
            Assert.That(Mathf.Sign(right.lateralNeedleNormalized.x), Is.EqualTo(Mathf.Sign(right.lateralGuidanceDegrees.x)));
        }

        [Test]
        public void LateralGuidanceAngle_IncreasesAsClosureDistanceShrinks()
        {
            Quaternion shipRotation = RotationForDockingFrame(Vector3.forward, Vector3.up);
            Quaternion targetRotation = RotationForDockingFrame(Vector3.back, Vector3.up);
            StationDockingPort target = CreatePort(Vector3.zero, targetRotation);

            DockingTelemetry far = DockingTelemetryUtility.ComputeFromPoses(
                Vector3.right * 2f + Vector3.back * 20f,
                shipRotation,
                BlenderImportedAxes.DefaultActionAxisLocal,
                Vector3.zero,
                true,
                target);
            DockingTelemetry near = DockingTelemetryUtility.ComputeFromPoses(
                Vector3.right * 2f + Vector3.back * 2f,
                shipRotation,
                BlenderImportedAxes.DefaultActionAxisLocal,
                Vector3.zero,
                true,
                target);

            Assert.That(Mathf.Abs(far.lateralAxisOffsetMeters.x), Is.EqualTo(2f).Within(0.0001f));
            Assert.That(Mathf.Abs(near.lateralAxisOffsetMeters.x), Is.EqualTo(2f).Within(0.0001f));
            Assert.That(Mathf.Abs(near.lateralGuidanceDegrees.x), Is.GreaterThan(Mathf.Abs(far.lateralGuidanceDegrees.x)));
            Assert.That(Mathf.Abs(near.lateralNeedleNormalized.x), Is.GreaterThan(Mathf.Abs(far.lateralNeedleNormalized.x)));
        }

        [Test]
        public void CameraFrameRotation_UsesAuthoredRollWithoutWorldAxisSnaps()
        {
            Quaternion first = RotationForDockingFrame(Vector3.forward, Vector3.up);
            Quaternion rolled = Quaternion.AngleAxis(45f, Vector3.forward) * first;

            DockingFrame firstFrame = DockingFrameUtility.FromPose(
                Vector3.zero,
                first,
                BlenderImportedAxes.DefaultActionAxisLocal);
            DockingFrame rolledFrame = DockingFrameUtility.FromPose(
                Vector3.zero,
                rolled,
                BlenderImportedAxes.DefaultActionAxisLocal);

            Assert.That(Vector3.Dot(firstFrame.forward, rolledFrame.forward), Is.EqualTo(1f).Within(0.0001f));
            Assert.That(Vector3.Angle(firstFrame.up, rolledFrame.up), Is.EqualTo(45f).Within(0.0001f));
        }

        StationDockingPort CreatePort(Vector3 position, Quaternion rotation)
        {
            var go = new GameObject("node_docking_port");
            created.Add(go);
            go.transform.SetPositionAndRotation(position, rotation);
            return go.AddComponent<StationDockingPort>();
        }

        static Quaternion RotationForDockingFrame(Vector3 forward, Vector3 up)
        {
            Quaternion desiredFrame = Quaternion.LookRotation(forward.normalized, up.normalized);
            Quaternion localFrame = Quaternion.LookRotation(
                BlenderImportedAxes.DefaultActionAxisLocal,
                Vector3.forward);
            return desiredFrame * Quaternion.Inverse(localFrame);
        }
    }
}
