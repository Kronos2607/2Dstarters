// Sends the player's movement to the Animator: Speed (left/right), VerticalSpeed (up/down) and IsGrounded.
// The transitions in the Hero Animator Controller use them to pick Idle, Run, Jump, Fall or Land.
// It also flips the sprite to face the way the player is running.
using UnityEngine;

namespace PlatformerStarter
{
    public class PlayerAnimator : MonoBehaviour
    {
        // Small speeds (sliding to a stop) don't turn the sprite round.
        [SerializeField] private float turnThreshold = 0.1f;

        private Rigidbody2D body;
        private Animator animator;
        private SpriteRenderer spriteRenderer;
        private PlayerController controller;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            animator = GetComponent<Animator>();
            spriteRenderer = GetComponent<SpriteRenderer>();
            controller = GetComponent<PlayerController>();
        }

        private void Update()
        {
            Vector2 velocity = body.linearVelocity;

            animator.SetFloat("Speed", Mathf.Abs(velocity.x));
            animator.SetFloat("VerticalSpeed", velocity.y);
            animator.SetBool("IsGrounded", controller.IsGrounded);

            if (velocity.x < -turnThreshold)
            {
                spriteRenderer.flipX = true;
            }
            else if (velocity.x > turnThreshold)
            {
                spriteRenderer.flipX = false;
            }
        }

        // The PlayerController calls this on a double jump, so the jump animation starts again from frame 0.
        public void RestartJumpAnimation()
        {
            animator.Play("Jump", 0, 0f);
        }
    }
}
