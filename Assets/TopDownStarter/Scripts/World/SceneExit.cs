// A trigger at the end of a path. Walking into it loads another scene and puts the player
// at the SpawnPoint with the name in Arrive At.
using UnityEngine;

namespace TopDownStarter
{
    public class SceneExit : MonoBehaviour
    {
        [SerializeField] private string sceneToLoad = "TopDown_Lake";
        [SerializeField] private string arriveAt = "From Meadow";
        [SerializeField] private AudioClip exitSound;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            AudioManager.Instance.PlaySound(exitSound);
            SceneLoader.Instance.LoadScene(sceneToLoad, arriveAt);
        }
    }
}
