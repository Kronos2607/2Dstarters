// Moves the camera smoothly after the player, and stops it at the edges of the level so you never see
// past the end of the map. (Cinemachine does the same job; this is the hand-made version.)
// LateUpdate runs after every Update, so the player has already moved this frame.
using UnityEngine;

namespace PlatformerStarter
{
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        // Roughly the time, in seconds, the camera takes to catch up. 0 = stuck to the player.
        [SerializeField] private float smoothTime = 0.15f;
        // Look a little above the player's feet.
        [SerializeField] private Vector2 offset = new Vector2(0f, 1.5f);

        [Header("Level Edges")]
        [SerializeField] private bool stayInsideLevel = true;
        [SerializeField] private Vector2 levelBottomLeft = new Vector2(0f, 0f);
        [SerializeField] private Vector2 levelTopRight = new Vector2(60f, 15f);

        private Camera cameraComponent;
        private Vector3 currentVelocity;
        // The first frame jumps straight to the player instead of gliding there from wherever the camera was.
        private bool firstFrame = true;

        private void Awake()
        {
            cameraComponent = GetComponent<Camera>();
        }

        private void LateUpdate()
        {
            if (target == null)
            {
                return;
            }

            Vector3 wanted = new Vector3(target.position.x + offset.x, target.position.y + offset.y, transform.position.z);

            if (stayInsideLevel)
            {
                // Half the size of what the camera sees, in world units.
                float halfHeight = cameraComponent.orthographicSize;
                float halfWidth = halfHeight * cameraComponent.aspect;
                wanted.x = KeepBetween(wanted.x, levelBottomLeft.x + halfWidth, levelTopRight.x - halfWidth);
                wanted.y = KeepBetween(wanted.y, levelBottomLeft.y + halfHeight, levelTopRight.y - halfHeight);
            }

            if (firstFrame)
            {
                transform.position = wanted;
                firstFrame = false;
            }
            else
            {
                transform.position = Vector3.SmoothDamp(transform.position, wanted, ref currentVelocity, smoothTime);
            }
        }

        // Like Mathf.Clamp, but if the level is smaller than the view it stays in the middle.
        private float KeepBetween(float value, float lowest, float highest)
        {
            if (lowest > highest)
            {
                return (lowest + highest) / 2f;
            }
            return Mathf.Clamp(value, lowest, highest);
        }
    }
}
