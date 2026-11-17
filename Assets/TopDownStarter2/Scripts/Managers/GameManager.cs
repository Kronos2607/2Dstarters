// The GameManager: one object that lasts for the whole game and keeps track of the gems.
// It is a "singleton": there is only ever one, and any script can reach it with GameManager.Instance.
// Your own scripts need "using TopDownStarter;" at the top to use it (see the README).
using System.Collections.Generic;
using UnityEngine;

namespace TopDownStarter
{
    public class GameManager : MonoBehaviour
    {
        // The one GameManager. Static means it belongs to the class, not to any one object,
        // so GameManager.Instance works from anywhere. Only this script can change it (private set).
        public static GameManager Instance { get; private set; }

        // Serialized so you can watch it change in the Inspector while playing.
        [SerializeField] private int gems;

        // The gems already picked up. A scene starts fresh every time it loads, so without this list
        // the gems would come back when you walk out of an area and in again.
        private List<string> collectedGems = new List<string>();

        public int Gems
        {
            get { return gems; }
        }

        private void Awake()
        {
            // Every scene has a GameManager, so you can press Play in any of them. When a second one
            // turns up (we loaded another scene), the first one stays and the new copy removes itself.
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            // Keep this object when a new scene loads, so the gems carry on into the next area.
            DontDestroyOnLoad(gameObject);
        }

        public void AddGems(int amount)
        {
            gems = gems + amount;
        }

        public void RememberCollected(string gemName)
        {
            collectedGems.Add(gemName);
        }

        public bool WasCollected(string gemName)
        {
            return collectedGems.Contains(gemName);
        }

        // Back to the start for a new game (the main menu's Play button calls this).
        public void ResetGems()
        {
            gems = 0;
            collectedGems.Clear();
        }
    }
}
