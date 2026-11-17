// Remembers where the player started the level and puts them back there when they fall or touch spikes
// (KillZone calls Respawn). The coins already collected stay collected.
using UnityEngine;

namespace PlatformerStarter
{
    public class PlayerRespawn : MonoBehaviour
    {
        [SerializeField] private AudioClip hurtSound;

        private Rigidbody2D body;
        private Vector3 startPosition;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            startPosition = transform.position;
        }

        public void Respawn()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlaySound(hurtSound);
            }

            // Stop the fall first, or the player would keep falling from the start position.
            body.linearVelocity = Vector2.zero;
            transform.position = startPosition;
        }
    }
}
