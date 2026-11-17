// The SceneLoader changes scenes with a fade to black. Like the GameManager, there is one of it and it
// survives scene changes, so the fade can carry on over the new scene.
// Use it from any script: SceneLoader.Instance.LoadScene("Platformer_Level2");
// A scene can only be loaded if it is in the scene list (File > Build Profiles > Scene List).
// The Platformer Starter adds its own scenes to that list for you.
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PlatformerStarter
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        // The black screen. A Canvas Group fades it (and anything on it) in and out.
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeTime = 0.4f;

        // Stops a second load starting while one is still going (the player can touch a goal twice).
        private bool isLoading;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }

        public void LoadScene(string sceneName)
        {
            if (isLoading)
            {
                return;
            }
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        // Loads the scene we are in again, from the start.
        public void ReloadScene()
        {
            LoadScene(SceneManager.GetActiveScene().name);
        }

        public void QuitGame()
        {
            Debug.Log("Quit Game");
            Application.Quit();

            // Application.Quit does nothing in the editor, so stop Play mode instead.
            // The #if means this line is only part of the game while it runs in the editor.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#endif
        }

        // A coroutine can pause with yield and carry on later, so one method can run over many frames.
        private IEnumerator LoadSceneRoutine(string sceneName)
        {
            isLoading = true;
            fadeGroup.blocksRaycasts = true;

            yield return Fade(0f, 1f);

            // LoadSceneAsync loads in the background, then switches to the new scene when it is ready.
            AsyncOperation loading = SceneManager.LoadSceneAsync(sceneName);
            while (!loading.isDone)
            {
                yield return null;
            }

            yield return Fade(1f, 0f);
            fadeGroup.blocksRaycasts = false;
            isLoading = false;
        }

        private IEnumerator Fade(float startAlpha, float endAlpha)
        {
            float time = 0f;
            while (time < fadeTime)
            {
                // Unscaled time, so fading still works when the game is paused (timeScale 0).
                time = time + Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, time / fadeTime);
                yield return null;
            }
            fadeGroup.alpha = endAlpha;
        }
    }
}
