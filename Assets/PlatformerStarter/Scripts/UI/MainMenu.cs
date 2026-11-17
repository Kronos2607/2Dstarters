// The main menu's buttons. Each Button's On Click list (in the Inspector) calls one of these methods.
using UnityEngine;

namespace PlatformerStarter
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string firstLevelName = "Platformer_Level1";
        [SerializeField] private AudioClip clickSound;

        public void PlayGame()
        {
            AudioManager.Instance.PlaySound(clickSound);
            // A new game starts with no coins.
            GameManager.Instance.ResetCoins();
            SceneLoader.Instance.LoadScene(firstLevelName);
        }

        public void QuitGame()
        {
            AudioManager.Instance.PlaySound(clickSound);
            SceneLoader.Instance.QuitGame();
        }
    }
}
