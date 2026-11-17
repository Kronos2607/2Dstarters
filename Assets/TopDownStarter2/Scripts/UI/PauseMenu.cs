// Escape (or Start on a gamepad) pauses the game and shows the pause panel.
// Time.timeScale = 0 stops physics and anything that uses Time.deltaTime; 1 is normal speed.
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownStarter
{
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private InputActionReference pauseAction;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private string menuSceneName = "TopDown_MainMenu";
        [SerializeField] private AudioClip clickSound;

        private bool isPaused;

        private void OnEnable()
        {
            pauseAction.action.Enable();
        }

        private void OnDisable()
        {
            pauseAction.action.Disable();
        }

        private void Start()
        {
            pausePanel.SetActive(false);
        }

        private void Update()
        {
            if (pauseAction.action.WasPressedThisFrame())
            {
                if (isPaused)
                {
                    Resume();
                }
                else
                {
                    Pause();
                }
            }
        }

        public void Pause()
        {
            isPaused = true;
            pausePanel.SetActive(true);
            Time.timeScale = 0f;
        }

        public void Resume()
        {
            AudioManager.Instance.PlaySound(clickSound);
            isPaused = false;
            pausePanel.SetActive(false);
            Time.timeScale = 1f;
        }

        public void QuitToMenu()
        {
            AudioManager.Instance.PlaySound(clickSound);
            // Unpause first, or the menu (and everything after it) would stay frozen.
            Time.timeScale = 1f;
            SceneLoader.Instance.LoadScene(menuSceneName);
        }
    }
}
