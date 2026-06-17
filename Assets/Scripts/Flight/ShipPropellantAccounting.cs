using UnityEngine;

namespace FlightModel
{
    public static class ShipPropellantAccounting
    {
        public struct BurnResult
        {
            public float fuelBurnKg;
            public float hypergolicBurnKg;
            public float mainEngineNewtons;
            public float maneuverNewtons;
            public float rcsNewtons;
        }

        public static BurnResult ComputeBurn(
            in ShipTuning tuning,
            in ShipControlRequest request,
            Vector3 localLinearAccel,
            Vector3 localAngularAccel,
            float currentMassKg,
            float deltaSeconds)
        {
            float mass = Mathf.Max(1f, currentMassKg);
            Vector3 appliedForceLocal = localLinearAccel * mass;

            float mainEngineNewtons = 0f;
            if (!request.fineControl && request.linearForward > 0f && localLinearAccel.z > 0f)
            {
                mainEngineNewtons = appliedForceLocal.z;
            }

            float maneuverForwardNewtons = request.fineControl || request.linearForward <= 0f
                ? Mathf.Abs(appliedForceLocal.z)
                : 0f;
            float maneuverLateralNewtons = new Vector2(appliedForceLocal.x, appliedForceLocal.y).magnitude;
            float maneuverNewtons = maneuverForwardNewtons + maneuverLateralNewtons;
            float rcsNewtons = localAngularAccel.magnitude * mass;

            float fuelBurnKg = mainEngineNewtons > 1f
                ? mainEngineNewtons * tuning.fuelBurnRatePerNewtonSecond * deltaSeconds
                : 0f;
            float hypergolicBurnKg = maneuverNewtons + rcsNewtons > 1f
                ? (maneuverNewtons + rcsNewtons) * tuning.hypergolicBurnRatePerNewtonSecond * deltaSeconds
                : 0f;

            return new BurnResult
            {
                fuelBurnKg = fuelBurnKg,
                hypergolicBurnKg = hypergolicBurnKg,
                mainEngineNewtons = mainEngineNewtons,
                maneuverNewtons = maneuverNewtons,
                rcsNewtons = rcsNewtons
            };
        }

        public static void ApplyBurn(ref ShipState state, in BurnResult burn)
        {
            state.remainingFuelKg = Mathf.Max(0f, state.remainingFuelKg - burn.fuelBurnKg);
            state.remainingHypergolicKg = Mathf.Max(0f, state.remainingHypergolicKg - burn.hypergolicBurnKg);
            state.currentMassKg = state.currentMassKg > 0f ? state.currentMassKg : 0f;
        }

        public static bool HasMainEngineFuel(in ShipState state) => state.remainingFuelKg > 0f;

        public static bool HasHypergolic(in ShipState state) => state.remainingHypergolicKg > 0f;
    }
}
