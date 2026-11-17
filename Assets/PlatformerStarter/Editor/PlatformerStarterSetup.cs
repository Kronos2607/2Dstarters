// Editor helper (not part of the game). When the package is imported (and whenever scripts recompile)
// it adds the Platformer Starter scenes to the scene list (File > Build Profiles > Scene List), because
// SceneManager can only load scenes that are in that list. It never removes or reorders your own scenes.
// It also adds a menu: Tools > Platformer Starter > Open Main Menu.
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;

namespace PlatformerStarter
{
    [InitializeOnLoad]
    public static class PlatformerStarterSetup
    {
        // The scene names, in the order they are added to the list.
        private static readonly string[] SceneNames = { "Platformer_MainMenu", "Platformer_Level1", "Platformer_Level2" };

        // Runs when Unity loads the editor scripts (after the import and after each recompile).
        static PlatformerStarterSetup()
        {
            // Wait until the editor has finished importing, so the scenes can be found.
            EditorApplication.delayCall += AddScenesToBuildList;
        }

        // Unity calls this after importing assets: if our scenes were among them, list them straight away.
        private class ImportWatcher : AssetPostprocessor
        {
            private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
            {
                foreach (string path in importedAssets)
                {
                    foreach (string sceneName in SceneNames)
                    {
                        if (path.EndsWith("/" + sceneName + ".unity"))
                        {
                            AddScenesToBuildList();
                            return;
                        }
                    }
                }
            }
        }

        [MenuItem("Tools/Platformer Starter/Add Scenes to Build List")]
        public static void AddScenesToBuildList()
        {
            List<EditorBuildSettingsScene> scenes = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            int added = 0;

            foreach (string sceneName in SceneNames)
            {
                string path = FindScenePath(sceneName);
                if (path == null)
                {
                    continue;
                }

                bool alreadyListed = false;
                foreach (EditorBuildSettingsScene scene in scenes)
                {
                    if (scene.path == path)
                    {
                        alreadyListed = true;
                    }
                }

                if (!alreadyListed)
                {
                    scenes.Add(new EditorBuildSettingsScene(path, true));
                    added = added + 1;
                }
            }

            if (added > 0)
            {
                EditorBuildSettings.scenes = scenes.ToArray();
                UnityEngine.Debug.Log("Platformer Starter: added " + added + " scenes to File > Build Profiles > Scene List.");
            }
        }

        [MenuItem("Tools/Platformer Starter/Open Main Menu")]
        public static void OpenMainMenu()
        {
            string path = FindScenePath("Platformer_MainMenu");
            if (path != null && EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            {
                EditorSceneManager.OpenScene(path);
            }
        }

        // Finds a scene by its exact name anywhere in Assets, so it still works if the folder is moved.
        private static string FindScenePath(string sceneName)
        {
            string[] guids = AssetDatabase.FindAssets(sceneName + " t:Scene");
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == sceneName)
                {
                    return path;
                }
            }
            return null;
        }
    }
}
