// The GameManager: one object that lasts for the whole game and keeps track of the coins.
// It is a "singleton": there is only ever one, and any script can reach it with GameManager.Instance.
// Your own scripts need "using PlatformerStarter;" at the top to use it (see the README).
using UnityEngine;

namespace PlatformerStarter
{
    public class GameManager : MonoBehaviour
    {
        // The one GameManager. Static means it belongs to the class, not to any one object,
        // so GameManager.Instance works from anywhere. Only this script can change it (private set).
        public static GameManager Instance { get; private set; }

        // Serialized so you can watch it change in the Inspector while playing.
        [SerializeField] private int coins;

        public int Coins
        {
            get { return coins; }
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

            // Keep this object when a new scene loads, so the coins carry on into the next level.
            DontDestroyOnLoad(gameObject);
        }

        public void AddCoins(int amount)
        {
            coins = coins + amount;
        }

        // Back to zero for a new game (the main menu's Play button calls this).
        public void ResetCoins()
        {
            coins = 0;
        }
    }
}
