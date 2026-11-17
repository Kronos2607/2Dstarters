// The portal at the end of a level. Touching it plays a sound and loads the next scene by name
// through the SceneLoader. On the last level, set Next Scene to the main menu.
using UnityEngine;

namespace PlatformerStarter
{
    public class LevelGoal : MonoBehaviour
    {
        [SerializeField] private string nextSceneName = "Platformer_Level2";
        [SerializeField] private AudioClip reachedSound;

        private bool reached;

        private void OnTriggerEnter2D(Collider2D other)
        {
            // reached stops the sound playing twice if the player touches the portal again while the screen fades.
            if (reached || !other.CompareTag("Player"))
            {
                return;
            }

            reached = true;
            AudioManager.Instance.PlaySound(reachedSound);
            SceneLoader.Instance.LoadScene(nextSceneName);
        }
    }
}
