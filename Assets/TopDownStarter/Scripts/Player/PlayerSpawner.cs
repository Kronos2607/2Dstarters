// When a scene starts, moves the player to the SpawnPoint the SceneLoader was asked for,
// so walking out of the Lake puts you on the path at the edge of the Meadow, not at the start.
using UnityEngine;

namespace TopDownStarter
{
    public class PlayerSpawner : MonoBehaviour
    {
        private void Start()
        {
            if (SceneLoader.Instance == null || SceneLoader.Instance.SpawnPointName == "")
            {
                return;
            }

            SpawnPoint[] spawnPoints = FindObjectsByType<SpawnPoint>(FindObjectsSortMode.None);
            foreach (SpawnPoint spawnPoint in spawnPoints)
            {
                if (spawnPoint.SpawnName == SceneLoader.Instance.SpawnPointName)
                {
                    transform.position = spawnPoint.transform.position;
                    return;
                }
            }

            Debug.LogWarning("No SpawnPoint called " + SceneLoader.Instance.SpawnPointName + " in this scene.");
        }
    }
}
