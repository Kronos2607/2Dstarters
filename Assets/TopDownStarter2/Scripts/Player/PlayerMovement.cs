// Moves a top-down character in eight directions with the Input System.
// The Rigidbody2D has Gravity Scale 0, so nothing pulls it down the screen, and its collider stops it
// at water, trees, rocks and the edges of the map.
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownStarter
{
    public class PlayerMovement : MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 4f;
        [SerializeField] private InputActionReference moveAction;

        private Rigidbody2D body;
        private Vector2 moveInput;

        private void Awake()
        {
            body = GetComponent<Rigidbody2D>();
        }

        private void OnEnable()
        {
            moveAction.action.Enable();
        }

        private void OnDisable()
        {
            moveAction.action.Disable();
            // Stop when switched off, or the body would keep sliding.
            if (body != null)
            {
                body.linearVelocity = Vector2.zero;
            }
        }

        private void Update()
        {
            moveInput = moveAction.action.ReadValue<Vector2>();

            // Up + Right on a keyboard gives (1, 1), which is 1.41 long, so diagonals would be faster.
            // Normalising makes it 1 long. Only longer inputs are shortened, so a gamepad stick pushed
            // part of the way still walks slowly.
            if (moveInput.magnitude > 1f)
            {
                moveInput = moveInput.normalized;
            }
        }

        // Physics happens in FixedUpdate, so the body is moved here.
        private void FixedUpdate()
        {
            body.linearVelocity = moveInput * moveSpeed;
        }
    }
}
