// Runs and jumps the player with a Rigidbody2D and the Input System. It has:
//   - a ground check (a small circle at the feet),
//   - variable jump height: let go of Jump early for a small hop,
//   - faster falling, so jumps feel snappy rather than floaty,
//   - coyote time: you can still jump just after running off a ledge,
//   - jump buffering: pressing Jump just before landing still jumps,
//   - a double jump: one extra jump in the air (Extra Jumps 0 turns it off, 2 gives a triple jump).
// Input is read in Update (every frame) and the body is moved in FixedUpdate (every physics step).
using UnityEngine;
using UnityEngine.InputSystem;

namespace PlatformerStarter
{
    public class PlayerController : MonoBehaviour
    {
        [Header("Running")]
        [SerializeField] private float moveSpeed = 6f;

        [Header("Jumping")]
        [SerializeField] private float jumpSpeed = 15f;
        // Upward speed is multiplied by this when Jump is let go (1 = always full height).
        [SerializeField] private float jumpCutMultiplier = 0.5f;
        // More gravity coming down than going up.
        [SerializeField] private float normalGravityScale = 3f;
        [SerializeField] private float fallGravityScale = 5f;
        [SerializeField] private float maxFallSpeed = 18f;
        [SerializeField] private float coyoteTime = 0.1f;
        [SerializeField] private float jumpBufferTime = 0.1f;

        [Header("Double Jump")]
        [SerializeField] private int extraJumps = 1;

        [Header("Ground Check")]
        [SerializeField] private Transform groundCheck;
        [SerializeField] private float groundCheckRadius = 0.2f;
        // Which layers count as ground. Everything to start with; the player's own collider is skipped.
        [SerializeField] private LayerMask groundLayers = ~0;

        [Header("Input")]
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference jumpAction;

        [Header("Sounds")]
        [SerializeField] private AudioClip jumpSound;
        [SerializeField] private AudioClip doubleJumpSound;

        private Rigidbody2D body;
        private PlayerAnimator playerAnimator;
        private float horizontalInput;
        private bool jumpPressed;
        private bool jumpReleased;
        private bool isGrounded;
        private int extraJumpsLeft;

        // Count down from coyoteTime / jumpBufferTime. Below zero means "no longer allowed".
        private float coyoteCounter = -1f;
        private float jumpBufferCounter = -1f;

        // Other scripts (PlayerAnimator) can read this, but only this script can change it.
        public bool IsGrounded
        {
            get { return isGrounded; }
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
            playerAnimator = GetComponent<PlayerAnimator>();
        }

        private void OnEnable()
        {
            moveAction.action.Enable();
            jumpAction.action.Enable();
        }

        private void OnDisable()
        {
            moveAction.action.Disable();
            jumpAction.action.Disable();
        }

        private void Update()
        {
            // Paused (see PauseMenu): ignore the keys, or a jump pressed in the menu would happen afterwards.
            if (Time.timeScale == 0f)
            {
                return;
            }

            horizontalInput = moveAction.action.ReadValue<Vector2>().x;

            // Remember presses until FixedUpdate uses them: a quick tap can start and end between two physics steps.
            if (jumpAction.action.WasPressedThisFrame())
            {
                jumpPressed = true;
            }
            if (jumpAction.action.WasReleasedThisFrame())
            {
                jumpReleased = true;
            }
        }

        private void FixedUpdate()
        {
            CheckGround();

            if (isGrounded)
            {
                coyoteCounter = coyoteTime;
                extraJumpsLeft = extraJumps;
            }
            else
            {
                coyoteCounter = coyoteCounter - Time.fixedDeltaTime;
            }

            if (jumpPressed)
            {
                jumpBufferCounter = jumpBufferTime;
                jumpPressed = false;
            }
            else
            {
                jumpBufferCounter = jumpBufferCounter - Time.fixedDeltaTime;
            }

            // Work on a copy of the velocity, then hand it back to the body at the end.
            Vector2 velocity = body.linearVelocity;
            velocity.x = horizontalInput * moveSpeed;

            bool wantsToJump = jumpBufferCounter >= 0f;
            if (wantsToJump && coyoteCounter >= 0f)
            {
                // A normal jump from the ground (or just after leaving it).
                velocity.y = jumpSpeed;
                jumpBufferCounter = -1f;
                coyoteCounter = -1f;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(jumpSound);
                }
            }
            else if (wantsToJump && extraJumpsLeft > 0)
            {
                // Double jump: a jump in the air uses up one extra jump until we land again.
                velocity.y = jumpSpeed;
                extraJumpsLeft = extraJumpsLeft - 1;
                jumpBufferCounter = -1f;
                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlaySound(doubleJumpSound);
                }
                if (playerAnimator != null)
                {
                    playerAnimator.RestartJumpAnimation();
                }
            }

            // Variable jump height: letting go of Jump while still going up cuts the jump short.
            if (jumpReleased && velocity.y > 0f)
            {
                velocity.y = velocity.y * jumpCutMultiplier;
            }
            jumpReleased = false;

            // Faster fall: more gravity on the way down, and a top speed so we don't fall through floors.
            if (velocity.y < 0f)
            {
                body.gravityScale = fallGravityScale;
            }
            else
            {
                body.gravityScale = normalGravityScale;
            }
            if (velocity.y < -maxFallSpeed)
            {
                velocity.y = -maxFallSpeed;
            }

            body.linearVelocity = velocity;
        }

        // Looks for any solid collider touching the circle at the feet, other than the player's own.
        private void CheckGround()
        {
            isGrounded = false;
            Collider2D[] hits = Physics2D.OverlapCircleAll(groundCheck.position, groundCheckRadius, groundLayers);
            foreach (Collider2D hit in hits)
            {
                // Triggers (coins, the goal, spikes) are not ground, and neither is the player.
                if (hit.isTrigger)
                {
                    continue;
                }
                if (hit.attachedRigidbody == body)
                {
                    continue;
                }
                isGrounded = true;
            }
        }

        // Draws the ground check in the Scene view when the player is selected: green on the ground, red in the air.
        private void OnDrawGizmosSelected()
        {
            if (groundCheck == null)
            {
                return;
            }
            if (isGrounded)
            {
                Gizmos.color = Color.green;
            }
            else
            {
                Gizmos.color = Color.red;
            }
            Gizmos.DrawWireSphere(groundCheck.position, groundCheckRadius);
        }
    }
}
