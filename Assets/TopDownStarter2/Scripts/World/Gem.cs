// A gem the player picks up by walking over it (its collider is a trigger).
// The GameManager remembers which gems were taken, so they don't come back when the scene loads again.
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownStarter
{
    public class Gem : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [SerializeField] private AudioClip pickupSound;

        // The scene and object name, for example "TopDown_Meadow/Gem 3". Every gem in a scene needs its own name.
        private string gemName;

        private void Start()
        {
            gemName = SceneManager.GetActiveScene().name + "/" + gameObject.name;

            if (GameManager.Instance.WasCollected(gemName))
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            GameManager.Instance.AddGems(value);
            GameManager.Instance.RememberCollected(gemName);
            AudioManager.Instance.PlaySound(pickupSound);
            Destroy(gameObject);
        }
    }
}
