using System.Collections.Generic;
using UnityEngine;

namespace FlightModel.World
{
    /// <summary>
    /// Local registry for ReferenceFrame instances.
    /// Ensures a local zone frame always exists.
    /// </summary>
    public class ReferenceFrameRegistry : MonoBehaviour
    {
        int nextFrameId = 2; // 1 is reserved for LocalZone
        readonly Dictionary<ReferenceFrameId, ReferenceFrame> frames = new();

        static bool loggedLocalZoneCreated;

        public IReadOnlyDictionary<ReferenceFrameId, ReferenceFrame> Frames => frames;

        void Awake()
        {
            EnsureLocalZoneFrame();

            ReferenceFrame[] existing = FindObjectsByType<ReferenceFrame>(
                FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < existing.Length; i++)
            {
                Register(existing[i]);
            }
        }

        public ReferenceFrame Register(ReferenceFrame frame)
        {
            if (frame == null)
            {
                return null;
            }

            if (frame.Id.IsValid && frames.TryGetValue(frame.Id, out ReferenceFrame registered))
            {
                return registered == frame ? frame : registered;
            }

            ReferenceFrameId targetId = ReferenceFrameId.Invalid;
            if (frame.Id.IsValid)
            {
                targetId = frame.Id;
            }
            else if (frame.SerializedFrameId > 0)
            {
                targetId = new ReferenceFrameId(frame.SerializedFrameId);
            }

            if (targetId.IsValid && frames.TryGetValue(targetId, out ReferenceFrame existing))
            {
                if (existing == frame)
                {
                    return frame;
                }

                Debug.LogError(
                    $"ReferenceFrameRegistry: frame '{frame.name}' requested duplicate ID {targetId.Value}. " +
                    "Allocating a fresh frame ID instead.", frame);
                targetId = ReferenceFrameId.Invalid;
            }

            if (!targetId.IsValid)
            {
                targetId = AllocateId();
            }

            frame.Assign(targetId, frame.Kind);
            frames[frame.Id] = frame;
            return frame;
        }

        public bool TryGet(ReferenceFrameId id, out ReferenceFrame frame)
            => frames.TryGetValue(id, out frame);

        ReferenceFrameId AllocateId()
        {
            while (frames.ContainsKey(new ReferenceFrameId(nextFrameId)))
            {
                nextFrameId++;
            }

            return new ReferenceFrameId(nextFrameId++);
        }

        void EnsureLocalZoneFrame()
        {
            if (frames.ContainsKey(ReferenceFrameId.LocalZone))
            {
                return;
            }

            var go = new GameObject("LocalZoneFrame");
            var frame = go.AddComponent<ReferenceFrame>();
            frame.Assign(ReferenceFrameId.LocalZone, ReferenceFrameKind.Zone);
            DontDestroyOnLoad(go);
            frames[ReferenceFrameId.LocalZone] = frame;

            if (!loggedLocalZoneCreated)
            {
                Debug.Log("ReferenceFrameRegistry: created LocalZoneFrame (ID=1).");
                loggedLocalZoneCreated = true;
            }
        }
    }
}
