using FlightModel.World;
using UnityEngine;

namespace FlightModel.MultiplayerFuture
{
    public struct EntitySnapshot
    {
        public int entityId;
        public ReferenceFrameId frameId;
        public Vector3 position;
        public Quaternion rotation;
        public Vector3 linearVelocity;
    }
}
