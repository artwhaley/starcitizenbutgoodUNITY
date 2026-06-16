using UnityEngine;

namespace FlightModel
{
    public class SimpleTarget : MonoBehaviour
    {
        [SerializeField] int hitPoints = 3;
        [SerializeField] float hitFlashSeconds = 0.12f;
        [SerializeField] Renderer meshRenderer;
        [SerializeField] Color hitFlashColor = Color.red;

        Color originalColor;
        int remainingHitPoints;
        float flashTimer;

        void Awake()
        {
            remainingHitPoints = hitPoints;
            if (meshRenderer != null)
            {
                originalColor = meshRenderer.material.color;
            }
        }

        void Update()
        {
            if (flashTimer <= 0f)
            {
                return;
            }

            flashTimer -= Time.deltaTime;
            if (flashTimer <= 0f && meshRenderer != null)
            {
                meshRenderer.material.color = originalColor;
            }
        }

        public void RegisterHit()
        {
            remainingHitPoints--;
            Debug.Log("TARGET HIT");

            if (meshRenderer != null)
            {
                meshRenderer.material.color = hitFlashColor;
                flashTimer = hitFlashSeconds;
            }

            if (remainingHitPoints <= 0)
            {
                gameObject.SetActive(false);
            }
        }
    }
}
