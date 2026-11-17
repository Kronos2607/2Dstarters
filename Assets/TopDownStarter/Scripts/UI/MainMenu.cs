// The main menu's buttons. Each Button's On Click list (in the Inspector) calls one of these methods.
using UnityEngine;

namespace TopDownStarter
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private string firstSceneName = "TopDown_Meadow";
        [SerializeField] private AudioClip clickSound;

        public void PlayGame()
        {
            AudioManager.Instance.PlaySound(clickSound);
            // A new game starts with no gems.
            GameManager.Instance.ResetGems();
            SceneLoader.Instance.LoadScene(firstSceneName);
        }

        public void QuitGame()
        {
            AudioManager.Instance.PlaySound(clickSound);
            SceneLoader.Instance.QuitGame();
        }
    }
}
