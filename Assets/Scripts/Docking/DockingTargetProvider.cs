using UnityEngine;

namespace FlightModel.Docking
{
    /// <summary>
    /// Resolves which StationDockingPort the docking HUD/camera should target.
    /// Priority: serialized explicit port > nearest active port within range.
    /// No station traffic control, port reservations, or networking for phase 0.1.
    /// </summary>
    public class DockingTargetProvider : MonoBehaviour
    {
        [SerializeField] StationDockingPort explicitTarget;
        [SerializeField] Transform searchOrigin;
        [SerializeField] float fallbackSearchRangeMeters = 100f;

        public StationDockingPort CurrentTarget { get; private set; }
        public bool HasTarget => CurrentTarget != null;

        void LateUpdate() => UpdateTarget();

        public void UpdateTarget()
        {
            if (explicitTarget != null && explicitTarget.IsDockingActive)
            {
                CurrentTarget = explicitTarget;
                return;
            }

            if (searchOrigin == null || fallbackSearchRangeMeters <= 0f)
            {
                CurrentTarget = null;
                return;
            }

            float rangeSqr = fallbackSearchRangeMeters * fallbackSearchRangeMeters;
            if (CurrentTarget != null
                && CurrentTarget.IsDockingActive
                && (CurrentTarget.WorldPosition - searchOrigin.position).sqrMagnitude <= rangeSqr)
            {
                return;
            }

            StationDockingPort[] all = FindObjectsByType<StationDockingPort>(
                FindObjectsInactive.Exclude, FindObjectsSortMode.None);

            StationDockingPort best = null;
            float bestSqr = rangeSqr;
            for (int i = 0; i < all.Length; i++)
            {
                StationDockingPort port = all[i];
                if (port == null || !port.IsDockingActive)
                {
                    continue;
                }

                float sqr = (port.WorldPosition - searchOrigin.position).sqrMagnitude;
                if (sqr <= bestSqr)
                {
                    best = port;
                    bestSqr = sqr;
                }
            }

            CurrentTarget = best;
        }

        public void SetSearchOrigin(Transform origin)
        {
            searchOrigin = origin;
            UpdateTarget();
        }

        public void SetExplicitTarget(StationDockingPort port)
        {
            explicitTarget = port;
            UpdateTarget();
        }
    }
}
