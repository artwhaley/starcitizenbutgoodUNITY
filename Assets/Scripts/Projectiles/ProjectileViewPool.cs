using System.Collections.Generic;
using UnityEngine;

namespace FlightModel
{
    public class ProjectileViewPool : MonoBehaviour
    {
        [SerializeField] int poolSize = 60;
        [SerializeField] float boltLengthMeters = 6f;
        [SerializeField] Color defaultColor = new(3f, 4f, 2f, 1f);

        readonly List<ProjectileView> activeViews = new();
        readonly Queue<ProjectileView> pool = new();

        void Awake()
        {
            for (int i = 0; i < poolSize; i++)
            {
                CreatePooledView(i);
            }
        }

        void CreatePooledView(int index)
        {
            var go = new GameObject($"ProjectileView_{index}");
            go.transform.SetParent(transform, false);
            go.SetActive(false);
            var view = go.AddComponent<ProjectileView>();
            pool.Enqueue(view);
        }

        public ProjectileView SpawnView(int projectileId, Vector3 position)
        {
            if (pool.Count == 0)
            {
                CreatePooledView(pool.Count + activeViews.Count);
            }

            ProjectileView view = pool.Dequeue();
            view.gameObject.SetActive(true);
            view.Initialize(projectileId, position, defaultColor);
            activeViews.Add(view);
            return view;
        }

        public void DespawnView(ProjectileView view)
        {
            if (view == null)
            {
                return;
            }

            activeViews.Remove(view);
            view.FadeOut(0.3f);
            StartCoroutine(ReturnAfterFade(view, 0.35f));
        }

        System.Collections.IEnumerator ReturnAfterFade(ProjectileView view, float delay)
        {
            yield return new WaitForSeconds(delay);
            if (view != null)
            {
                view.gameObject.SetActive(false);
                pool.Enqueue(view);
            }
        }

        public void SyncViews(ProjectileWorld projectileWorld)
        {
            var states = new List<ProjectileState>();
            projectileWorld.GetActiveProjectiles(states);

            var stateById = new Dictionary<int, ProjectileState>();
            for (int i = 0; i < states.Count; i++)
            {
                stateById[states[i].projectileId] = states[i];
            }

            // Update or spawn views for active projectiles
            for (int i = 0; i < states.Count; i++)
            {
                ProjectileState state = states[i];
                ProjectileView view = null;
                for (int j = activeViews.Count - 1; j >= 0; j--)
                {
                    if (activeViews[j] != null && activeViews[j].ProjectileId == state.projectileId)
                    {
                        view = activeViews[j];
                        break;
                    }
                }

                if (view == null)
                {
                    view = SpawnView(state.projectileId, state.position);
                }

                view.UpdateState(state.position, boltLengthMeters);
            }

            // Despawn views for despawned projectiles
            for (int i = activeViews.Count - 1; i >= 0; i--)
            {
                ProjectileView view = activeViews[i];
                if (view != null && !stateById.ContainsKey(view.ProjectileId))
                {
                    DespawnView(view);
                }
            }
        }
    }
}
