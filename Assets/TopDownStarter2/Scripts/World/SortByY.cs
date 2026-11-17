// Draws sprites lower on the screen in front of sprites higher up, so the player can walk behind a tree
// (hidden by its leaves) and in front of it. It sets the Sprite Renderer's Order in Layer from the Y position:
// the lower the object, the bigger the number, and bigger numbers are drawn on top.
// The object's position should be at its feet (or the bottom of the trunk), which is where the sprites'
// pivots are in this starter.
// (Unity can also do this for the whole project: Transparency Sort Axis in the 2D Renderer asset.)
using UnityEngine;

namespace TopDownStarter
{
    public class SortByY : MonoBehaviour
    {
        // Trees and rocks never move, so they only need sorting once. Tick this for things that move.
        [SerializeField] private bool updateEveryFrame = false;

        private SpriteRenderer spriteRenderer;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            UpdateOrder();
        }

        // LateUpdate runs after the movement scripts, so we sort by where the object is this frame.
        private void LateUpdate()
        {
            if (updateEveryFrame)
            {
                UpdateOrder();
            }
        }

        private void UpdateOrder()
        {
            // Times 100 so objects 0.01 units apart still get different numbers.
            spriteRenderer.sortingOrder = Mathf.RoundToInt(-transform.position.y * 100f);
        }
    }
}
