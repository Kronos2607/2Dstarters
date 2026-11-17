// Shows the GameManager's coins on the screen (the HUD in each level).
using TMPro;
using UnityEngine;

namespace PlatformerStarter
{
    public class CoinCounter : MonoBehaviour
    {
        [SerializeField] private TMP_Text coinText;

        private void Update()
        {
            if (GameManager.Instance == null)
            {
                return;
            }
            coinText.text = "Coins: " + GameManager.Instance.Coins;
        }
    }
}
