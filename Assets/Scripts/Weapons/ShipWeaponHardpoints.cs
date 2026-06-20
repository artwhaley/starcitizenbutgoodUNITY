using System.Collections.Generic;
using UnityEngine;

namespace FlightModel
{
    public class ShipWeaponHardpoints : MonoBehaviour
    {
        [SerializeField] WeaponHardpoint[] hardpoints;
        int nextMuzzleIndex;

        public int HardpointCount => hardpoints?.Length ?? 0;

        void Awake()
        {
            TryAutoWire();
        }

        public WeaponHardpoint GetNextMuzzle()
        {
            if (hardpoints == null || hardpoints.Length == 0)
            {
                return default;
            }

            for (int attempt = 0; attempt < hardpoints.Length; attempt++)
            {
                int index = (nextMuzzleIndex + attempt) % hardpoints.Length;
                if (hardpoints[index].IsValid)
                {
                    nextMuzzleIndex = (index + 1) % hardpoints.Length;
                    return hardpoints[index];
                }
            }

            return default;
        }

        static bool loggedHardpointsMissing;

        public void TryAutoWire()
        {
            if (hardpoints != null && hardpoints.Length > 0)
            {
                return;
            }

            ShipVisualReferences visuals = GetComponentInChildren<ShipVisualReferences>(true);
            if (visuals == null)
            {
                visuals = GetComponent<ShipVisualReferences>();
            }

            if (visuals == null)
            {
                if (!loggedHardpointsMissing)
                {
                    Debug.LogWarning(
                        "ShipWeaponHardpoints: no ShipVisualReferences found on visual root.", this);
                    loggedHardpointsMissing = true;
                }

                return;
            }

            IReadOnlyList<Transform> guns = visuals.GunNodes;
            if (guns == null || guns.Count == 0)
            {
                if (!loggedHardpointsMissing)
                {
                    Debug.LogWarning(
                        "ShipWeaponHardpoints: ShipVisualReferences found but no gun nodes " +
                        "discovered. Ensure gun node transforms are named 'node_gun*'.", this);
                    loggedHardpointsMissing = true;
                }

                return;
            }

            var list = new System.Collections.Generic.List<WeaponHardpoint>();
            for (int i = 0; i < guns.Count; i++)
            {
                if (guns[i] != null)
                {
                    list.Add(new WeaponHardpoint { node = guns[i] });
                }
            }

            hardpoints = list.ToArray();
        }
    }
}
