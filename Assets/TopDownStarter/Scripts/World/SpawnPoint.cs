// A named place where the player can arrive from another scene (see SceneExit and PlayerSpawner).
// It is an empty object: it only marks a position. The icon shows it in the Scene view.
using UnityEngine;

namespace TopDownStarter
{
    public class SpawnPoint : MonoBehaviour
    {
        [SerializeField] private string spawnName = "From Meadow";

        public string SpawnName
        {
            get { return spawnName; }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, 0.3f);
        }
    }
}
