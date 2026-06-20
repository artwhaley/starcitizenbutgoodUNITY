using UnityEngine;

namespace FlightModel.World
{
    /// <summary>
    /// Attach to the root GameObject of any reference frame
    /// (zone, station, ship interior, docking port, EVA).
    /// </summary>
    public class ReferenceFrame : MonoBehaviour
    {
        [SerializeField] ReferenceFrameKind kind;
        [SerializeField] int serializedFrameId;

        ReferenceFrameId id;

        public ReferenceFrameId Id
        {
            get => id;
            private set => id = value;
        }

        public ReferenceFrameKind Kind
        {
            get => kind;
            set => kind = value;
        }

        public Transform Root => transform;

        public int SerializedFrameId => serializedFrameId;

        public void Assign(ReferenceFrameId newId, ReferenceFrameKind fallbackKind)
        {
            if (serializedFrameId > 0)
            {
                id = new ReferenceFrameId(serializedFrameId);
            }
            else
            {
                id = newId;
            }

            if (kind == ReferenceFrameKind.Unknown && fallbackKind != ReferenceFrameKind.Unknown)
            {
                kind = fallbackKind;
            }
        }
    }
}
