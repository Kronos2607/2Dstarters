// Shows the GameManager's gems on the screen (the HUD in each level).
using TMPro;
using UnityEngine;

namespace TopDownStarter
{
    public class GemCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text gemText;

        private void Update()
        {
            if (GameManager.Instance == null)
            {
                return;
            }
            gemText.text = "Gems: " + GameManager.Instance.Gems;
        }
    }
}
