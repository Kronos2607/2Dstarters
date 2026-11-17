// The SceneLoader changes scenes with a fade to black. Like the GameManager, there is one of it and it
// survives scene changes, so the fade can carry on over the new scene.
// Use it from any script: SceneLoader.Instance.LoadScene("TopDown_Lake");
// To arrive at a particular place in the new scene, give the name of a SpawnPoint there:
// SceneLoader.Instance.LoadScene("TopDown_Lake", "From Meadow");
// A scene can only be loaded if it is in the scene list (File > Build Profiles > Scene List).
// The Top-Down Starter adds its own scenes to that list for you.
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace TopDownStarter
{
    public class SceneLoader : MonoBehaviour
    {
        public static SceneLoader Instance { get; private set; }

        // The black screen. A Canvas Group fades it (and anything on it) in and out.
        [SerializeField] private CanvasGroup fadeGroup;
        [SerializeField] private float fadeTime = 0.4f;

        // The SpawnPoint the player should appear at in the next scene ("" = where the Player was placed).
        private string spawnPointName = "";

        // Stops a second load starting while one is still going (the player can walk into an exit twice).
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

        // The PlayerSpawner reads this when the new scene starts.
        public string SpawnPointName
        {
            get { return spawnPointName; }
        }

        public void LoadScene(string sceneName)
        {
            LoadScene(sceneName, "");
        }

        // Two methods can share a name if they take different inputs (this is called overloading).
        public void LoadScene(string sceneName, string arriveAtSpawnPoint)
        {
            if (isLoading)
            {
                return;
            }
            spawnPointName = arriveAtSpawnPoint;
            StartCoroutine(LoadSceneRoutine(sceneName));
        }

        // Loads the scene we are in again, from the start.
        public void ReloadScene()
        {
            LoadScene(SceneManager.GetActiveScene().name, spawnPointName);
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
