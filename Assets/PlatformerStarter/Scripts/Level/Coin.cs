// A coin the player collects by touching it. Its collider is a trigger, so the player passes through it.
// It adds to the GameManager's coins, plays a sound through the AudioManager, then removes itself.
using UnityEngine;

namespace PlatformerStarter
{
    public class Coin : MonoBehaviour
    {
        [SerializeField] private int value = 1;
        [SerializeField] private AudioClip collectSound;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            GameManager.Instance.AddCoins(value);
            AudioManager.Instance.PlaySound(collectSound);
            Destroy(gameObject);
        }
    }
}
