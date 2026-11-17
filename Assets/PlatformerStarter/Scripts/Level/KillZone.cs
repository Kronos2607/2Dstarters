// Spikes, and the invisible strip below the level that catches the player when they fall down a pit.
// Touching it sends the player back to the start of the level (PlayerRespawn).
using UnityEngine;

namespace PlatformerStarter
{
    public class KillZone : MonoBehaviour
    {
        private void OnTriggerEnter2D(Collider2D other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            PlayerRespawn respawn = other.GetComponent<PlayerRespawn>();
            if (respawn != null)
            {
                respawn.Respawn();
            }
        }
    }
}
