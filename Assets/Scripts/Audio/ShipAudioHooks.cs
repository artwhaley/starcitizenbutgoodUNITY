using UnityEngine;

namespace FlightModel
{
    public class ShipAudioHooks : MonoBehaviour
    {
        [SerializeField] AudioSource engineLoop;
        [SerializeField] AudioSource boostLoop;
        [SerializeField] AudioSource fireLoop;

        public void UpdateFromCommand(in ShipInputCommand command)
        {
            float throttle = Mathf.Clamp01(Mathf.Max(
                Mathf.Abs(command.thrustForward),
                Mathf.Abs(command.thrustRight),
                Mathf.Abs(command.thrustUp)));

            SetEngineLevel(throttle);
            SetBoostActive(command.boost);
            SetFireActive(command.firePrimary);
        }

        public void SetEngineLevel(float throttle01)
        {
            if (engineLoop == null)
            {
                return;
            }

            engineLoop.volume = throttle01;
            if (engineLoop.clip != null)
            {
                if (throttle01 > 0.01f && !engineLoop.isPlaying)
                {
                    engineLoop.Play();
                }
                else if (throttle01 <= 0.01f && engineLoop.isPlaying)
                {
                    engineLoop.Stop();
                }
            }
        }

        public void SetBoostActive(bool active)
        {
            if (boostLoop == null)
            {
                return;
            }

            boostLoop.volume = active ? 1f : 0f;
            if (boostLoop.clip != null)
            {
                if (active && !boostLoop.isPlaying)
                {
                    boostLoop.Play();
                }
                else if (!active && boostLoop.isPlaying)
                {
                    boostLoop.Stop();
                }
            }
        }

        public void SetFireActive(bool firing)
        {
            if (fireLoop == null)
            {
                return;
            }

            fireLoop.volume = firing ? 1f : 0f;
            if (fireLoop.clip != null)
            {
                if (firing && !fireLoop.isPlaying)
                {
                    fireLoop.Play();
                }
                else if (!firing && fireLoop.isPlaying)
                {
                    fireLoop.Stop();
                }
            }
        }
    }
}
