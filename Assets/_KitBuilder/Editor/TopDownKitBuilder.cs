// Builds the Top-Down Starter (tiles, animations, prefabs, scenes) from the art in Assets/TopDownStarter
// and the text maps in Assets/_KitBuilder/Levels. Not exported, not student code.
// Map letters: . grass  , dirt path  ~ water  T tree  r rock  b bush  g gem  f flowers
//              P player start  E exit to the other area  S spawn point (arriving from the other area)
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using TopDownStarter;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class TopDownKitBuilder
{
    const string Root = "Assets/TopDownStarter";
    const string PlayerSheet = Root + "/Art/Sprites/player.png";
    const string Objects = Root + "/Art/Sprites/objects.png";
    const string GemSprite = Root + "/Art/Sprites/gem_01c.png";
    const string TileArt = Root + "/Art/Tiles/";
    const string TileFolder = Root + "/Tiles";
    const string AnimationFolder = Root + "/Animation";
    const string PrefabFolder = Root + "/Prefabs";
    const string SceneFolder = Root + "/Scenes";
    const string ActionsPath = Root + "/Input/TopDownControls.inputactions";
    const string MixerPath = Root + "/Audio/MainMixer.mixer";
    const string MusicPath = Root + "/Audio/Music/Placeholder_Chiptune_Loop.ogg";
    const string Sfx = Root + "/Audio/SFX/";
    const string Levels = "Assets/_KitBuilder/Levels/";

    static readonly Color Grass = new Color32(0x3b, 0x7d, 0x4f, 255);
    static readonly string[] Directions = { "Down", "Right", "Left", "Up" };
    static readonly Vector2[] DirectionVectors = { new Vector2(0, -1), new Vector2(1, 0), new Vector2(-1, 0), new Vector2(0, 1) };

    // Tilemaps are drawn behind everything SortByY gives an order to (orders go down to about -2400).
    const int GroundOrder = -30000, WaterOrder = -29999, DecorOrder = -29998;

    static Dictionary<string, InputActionReference> actions;
    static GameObject playerPrefab, gemPrefab, treePrefab, rockPrefab, bushPrefab;
    static GameObject gameManagerPrefab, audioManagerPrefab, sceneLoaderPrefab, levelUiPrefab;

    // Run once before Build: the player's sprites get their pivot at the feet (5 px up a 48 px frame),
    // so the Player object's position is where it stands (for collisions and SortByY).
    public static void Prepare()
    {
        KitHelpers.SetPivots(PlayerSheet, new Vector2(0.5f, 5f / 48f));
    }

    public static string Build()
    {
        string log = "";
        actions = KitHelpers.ActionReferences(ActionsPath);
        MakeTiles();
        MakeAnimations();
        MakePrefabs();
        log += MenuScene() + "\n";
        log += AreaScene("TopDown_Meadow", "TopDown_Lake", "From Meadow", "From Lake") + "\n";
        log += AreaScene("TopDown_Lake", "TopDown_Meadow", "From Lake", "From Meadow") + "\n";
        AssetDatabase.SaveAssets();
        return log;
    }

    // ---------- Tiles and animations ----------

    static void MakeTiles()
    {
        // Every piece of each sheet becomes a tile, laid out in the palette as on its sheet:
        // grass, then the dirt path sheet (6 wide), the water sheet (6 wide, solid) and the flowers (4 wide).
        var cells = new Dictionary<Vector3Int, TileBase>();
        cells[new Vector3Int(0, 0, 0)] = KitHelpers.CreateTile(TileFolder, "grass", AssetDatabase.LoadAssetAtPath<Sprite>(TileArt + "grass.png"), Tile.ColliderType.None);
        SheetTiles("plains.png", "plains_", "plains_", 6, 2, Tile.ColliderType.None, cells);
        SheetTiles("water1.png", "water1_", "water_", 6, 9, Tile.ColliderType.Grid, cells);
        SheetTiles("decor_16x16.png", "decor_16x16_", "decor_", 4, 16, Tile.ColliderType.None, cells);
        KitHelpers.Palette(TileFolder, "Top-Down Palette", cells);
    }

    static void SheetTiles(string sheet, string spritePrefix, string tilePrefix, int columns, int paletteX, Tile.ColliderType collider, Dictionary<Vector3Int, TileBase> cells)
    {
        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(TileArt + sheet).OfType<Sprite>())
        {
            if (!sprite.name.StartsWith(spritePrefix)) continue;
            int index = int.Parse(sprite.name.Substring(spritePrefix.Length));
            var tile = KitHelpers.CreateTile(TileFolder, tilePrefix + index, sprite, collider);
            cells[new Vector3Int(paletteX + index % columns, -(index / columns), 0)] = tile;
        }
    }

    static void MakeAnimations()
    {
        // player.png rows: 0-2 idle down/right/up, 3-5 walk down/right/up (6 frames each, player_0-35).
        // Left uses the right-facing frames with Flip X ticked in the clip.
        var idle = new Dictionary<string, AnimationClip>();
        var walk = new Dictionary<string, AnimationClip>();
        int[] rowFor = { 0, 1, 1, 2 };
        for (int i = 0; i < 4; i++)
        {
            string direction = Directions[i];
            bool flip = direction == "Left";
            idle[direction] = KitHelpers.CreateClip(AnimationFolder, "Player_Idle_" + direction, KitHelpers.Frames(PlayerSheet, "player_", rowFor[i] * 6, 6), 8, true, flip);
            walk[direction] = KitHelpers.CreateClip(AnimationFolder, "Player_Walk_" + direction, KitHelpers.Frames(PlayerSheet, "player_", (rowFor[i] + 3) * 6, 6), 10, true, flip);
        }

        string path = AnimationFolder + "/Player.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("DirectionX", AnimatorControllerParameterType.Float);
        controller.AddParameter("DirectionY", AnimatorControllerParameterType.Float);
        var machine = controller.layers[0].stateMachine;
        var idleState = DirectionalState(controller, machine, "Idle", idle, new Vector3(300, 0));
        var walkState = DirectionalState(controller, machine, "Walk", walk, new Vector3(300, 120));
        machine.defaultState = idleState;
        var toWalk = idleState.AddTransition(walkState);
        KitHelpers.Instant(toWalk, false);
        toWalk.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        var toIdle = walkState.AddTransition(idleState);
        KitHelpers.Instant(toIdle, false);
        toIdle.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
        EditorUtility.SetDirty(controller);
    }

    // A state that is a 2D blend tree on DirectionX/DirectionY with one clip per direction.
    static AnimatorState DirectionalState(AnimatorController controller, AnimatorStateMachine machine, string name, Dictionary<string, AnimationClip> clips, Vector3 position)
    {
        var state = controller.CreateBlendTreeInController(name, out BlendTree tree, 0);
        tree.blendType = BlendTreeType.SimpleDirectional2D;
        tree.blendParameter = "DirectionX";
        tree.blendParameterY = "DirectionY";
        for (int i = 0; i < 4; i++) tree.AddChild(clips[Directions[i]], DirectionVectors[i]);
        var states = machine.states;
        for (int i = 0; i < states.Length; i++)
            if (states[i].state == state) states[i].position = position;
        machine.states = states;
        return state;
    }

    // ---------- Prefabs ----------

    static void MakePrefabs()
    {
        var player = new GameObject("Player");
        player.tag = "Player";
        var renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = KitHelpers.SpriteNamed(PlayerSheet, "player_0");
        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 0f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        // A small box at the feet: only the feet bump into tree trunks, rocks and water.
        var feet = player.AddComponent<BoxCollider2D>();
        feet.size = new Vector2(0.6f, 0.35f);
        feet.offset = new Vector2(0f, 0.15f);
        player.AddComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimationFolder + "/Player.controller");
        var movement = player.AddComponent<PlayerMovement>();
        KitHelpers.Set(movement, "moveAction", actions["Move"]);
        player.AddComponent<PlayerAnimator>();
        player.AddComponent<PlayerSpawner>();
        var sort = player.AddComponent<SortByY>();
        KitHelpers.SetBool(sort, "updateEveryFrame", true);
        playerPrefab = KitHelpers.SavePrefab(player, PrefabFolder);

        var gem = new GameObject("Gem");
        gem.AddComponent<SpriteRenderer>().sprite = AssetDatabase.LoadAssetAtPath<Sprite>(GemSprite);
        var trigger = gem.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.4f;
        var gemScript = gem.AddComponent<Gem>();
        KitHelpers.Set(gemScript, "pickupSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + "GS2_Item_Acquire_1.ogg"));
        gem.AddComponent<SortByY>();
        gemPrefab = KitHelpers.SavePrefab(gem, PrefabFolder);

        // Props: the sprites' pivots are at the bottom, and the colliders only cover the base, so you
        // walk round a trunk but can walk behind the leaves.
        string environment = PrefabFolder + "/Environment";
        treePrefab = KitHelpers.SavePrefab(Prop("Tree", "tree", new Vector2(0.6f, 0.4f), new Vector2(0f, 0.2f)), environment);
        rockPrefab = KitHelpers.SavePrefab(Prop("Rock", "rock_1", new Vector2(0.8f, 0.5f), new Vector2(0f, 0.25f)), environment);
        bushPrefab = KitHelpers.SavePrefab(Prop("Bush", "bush", new Vector2(1.2f, 0.5f), new Vector2(0f, 0.25f)), environment);

        string managers = PrefabFolder + "/Managers";
        var gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();
        gameManagerPrefab = KitHelpers.SavePrefab(gameManager, managers);
        audioManagerPrefab = KitHelpers.SavePrefab(KitHelpers.AudioManagerObject(typeof(AudioManager), MixerPath, MusicPath), managers);
        sceneLoaderPrefab = KitHelpers.SavePrefab(KitHelpers.SceneLoaderObject(typeof(SceneLoader)), managers);

        levelUiPrefab = KitHelpers.SavePrefab(LevelUi(), PrefabFolder);
    }

    static GameObject Prop(string name, string sprite, Vector2 colliderSize, Vector2 colliderOffset)
    {
        var prop = new GameObject(name);
        prop.AddComponent<SpriteRenderer>().sprite = KitHelpers.SpriteNamed(Objects, sprite);
        var box = prop.AddComponent<BoxCollider2D>();
        box.size = colliderSize;
        box.offset = colliderOffset;
        prop.AddComponent<SortByY>();
        return prop;
    }

    static GameObject LevelUi()
    {
        var canvas = KitHelpers.Canvas("Level UI", 10);
        var hudBox = KitHelpers.Panel("Gem Box", canvas, new Vector2(0f, 1f), new Vector2(190, -60), new Vector2(320, 80));
        var gemText = KitHelpers.Text("Gem Text", hudBox, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 80), "Gems: 0", 48, TextAlignmentOptions.Left);
        var hintBox = KitHelpers.Panel("Pause Hint Box", canvas, new Vector2(1f, 1f), new Vector2(-140, -50), new Vector2(220, 60));
        KitHelpers.Text("Pause Hint", hintBox, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 60), "Esc: Pause", 30, TextAlignmentOptions.Center);
        var counter = canvas.gameObject.AddComponent<GemCounter>();
        KitHelpers.Set(counter, "gemText", gemText);

        var panel = KitHelpers.UiRect("Pause Panel", canvas, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        panel.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
        KitHelpers.Text("Paused Title", panel, new Vector2(0.5f, 0.5f), new Vector2(0, 160), new Vector2(800, 120), "Paused", 96, TextAlignmentOptions.Center);
        var pause = canvas.gameObject.AddComponent<PauseMenu>();
        KitHelpers.Set(pause, "pauseAction", actions["Pause"]);
        KitHelpers.Set(pause, "pausePanel", panel.gameObject);
        KitHelpers.Set(pause, "clickSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + "SciFi_UI_Button_1.ogg"));
        KitHelpers.Button(panel, "Resume", new Vector2(0, 0), pause, pause.Resume);
        KitHelpers.Button(panel, "Main Menu", new Vector2(0, -110), pause, pause.QuitToMenu);
        panel.gameObject.SetActive(false);
        return canvas.gameObject;
    }

    static void Managers()
    {
        PrefabUtility.InstantiatePrefab(gameManagerPrefab);
        PrefabUtility.InstantiatePrefab(audioManagerPrefab);
        PrefabUtility.InstantiatePrefab(sceneLoaderPrefab);
    }

    // ---------- Scenes ----------

    static string MenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        KitHelpers.GlobalLight();
        Managers();
        KitHelpers.MainCamera(Grass);
        KitHelpers.EventSystemObject();

        var canvas = KitHelpers.Canvas("Menu Canvas", 0);
        var menu = canvas.gameObject.AddComponent<MainMenu>();
        KitHelpers.Panel("Menu Box", canvas, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1640, 880));
        KitHelpers.Set(menu, "clickSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + "SciFi_UI_Button_1.ogg"));
        KitHelpers.Text("Title", canvas, new Vector2(0.5f, 0.5f), new Vector2(0, 360), new Vector2(1400, 160), "Top-Down Starter", 120, TextAlignmentOptions.Center);
        KitHelpers.Text("Controls", canvas, new Vector2(0.5f, 0.5f), new Vector2(0, 240), new Vector2(1500, 80),
            "Arrows or WASD to walk. Collect the gems, and follow the path east to the lake.", 36, TextAlignmentOptions.Center);
        KitHelpers.Button(canvas, "Play", new Vector2(0, 100), menu, menu.PlayGame);
        KitHelpers.VolumeSlider(canvas, "Music", new Vector2(0, -30), typeof(VolumeSlider), "MusicVolume");
        KitHelpers.VolumeSlider(canvas, "Sound Effects", new Vector2(0, -110), typeof(VolumeSlider), "SFXVolume");
        KitHelpers.Button(canvas, "Quit", new Vector2(0, -250), menu, menu.QuitGame);

        return Save(scene, "TopDown_MainMenu");
    }

    static string AreaScene(string sceneName, string exitScene, string arriveAtOther, string thisSpawnName)
    {
        string[] rows = File.ReadAllLines(Levels + sceneName + ".txt").Where(r => r.Length > 0).ToArray();
        int width = rows.Max(r => r.Length);
        int height = rows.Length;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        KitHelpers.GlobalLight();
        Managers();

        var grid = new GameObject("Grid").AddComponent<Grid>();
        var ground = TilemapObject(grid, "Ground", GroundOrder);
        var water = TilemapObject(grid, "Water", WaterOrder);
        var decor = TilemapObject(grid, "Flowers", DecorOrder);

        var dirt = new HashSet<Vector2Int>();
        var pond = new HashSet<Vector2Int>();
        for (int row = 0; row < height; row++)
            for (int column = 0; column < rows[row].Length; column++)
            {
                var cell = new Vector2Int(column, height - 1 - row);
                if (rows[row][column] == ',') dirt.Add(cell);
                if (rows[row][column] == '~') pond.Add(cell);
            }
        // Something standing on a path (the player, a spawn point, an exit, a gem): the ground under it is dirt.
        for (int row = 0; row < height; row++)
            for (int column = 0; column < rows[row].Length; column++)
            {
                if ("PSEgf".IndexOf(rows[row][column]) < 0) continue;
                var cell = new Vector2Int(column, height - 1 - row);
                int dirtNeighbours = 0;
                foreach (var step in new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right })
                    if (dirt.Contains(step + cell)) dirtNeighbours++;
                if (dirtNeighbours >= 2) dirt.Add(cell);
            }

        var random = new System.Random(sceneName.Length * 7919);
        var props = new GameObject("Props").transform;
        var gems = new GameObject("Gems").transform;
        var exits = new GameObject("Exits").transform;
        var grass = AssetDatabase.LoadAssetAtPath<TileBase>(TileFolder + "/grass.asset");
        Vector3 start = Vector3.zero;
        bool hasStart = false;
        Vector3 spawn = Vector3.zero;
        int gemNumber = 0;
        int[] flowers = { 8, 9, 10, 11 };
        int[] tufts = { 0, 1, 3 };
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < width; column++)
            {
                char letter = column < rows[row].Length ? rows[row][column] : '.';
                var position = new Vector3Int(column, height - 1 - row, 0);
                var cell = new Vector2Int(position.x, position.y);
                var centre = new Vector3(column + 0.5f, position.y + 0.5f, 0);
                if (dirt.Contains(cell)) ground.SetTile(position, EdgedTile(dirt, cell, "plains_", true));
                else ground.SetTile(position, grass);
                if (letter == '~') water.SetTile(position, EdgedTile(pond, cell, "water_", false));
                if (!dirt.Contains(cell) && letter == 'f') decor.SetTile(position, Decor(flowers[random.Next(flowers.Length)]));
                else if (letter == '.' && random.NextDouble() < 0.06) decor.SetTile(position, Decor(tufts[random.Next(tufts.Length)]));
                if (letter == 'T') KitHelpers.Place(treePrefab, new Vector3(column + 0.5f, position.y + 0.2f, 0), props);
                if (letter == 'r') KitHelpers.Place(rockPrefab, new Vector3(column + 0.5f, position.y + 0.3f, 0), props);
                if (letter == 'b') KitHelpers.Place(bushPrefab, new Vector3(column + 0.5f, position.y + 0.2f, 0), props);
                if (letter == 'g')
                {
                    gemNumber++;
                    var gem = KitHelpers.Place(gemPrefab, centre, gems);
                    gem.name = "Gem " + gemNumber;
                }
                if (letter == 'P') { start = centre; hasStart = true; }
                if (letter == 'S')
                {
                    spawn = centre;
                    var spawnPoint = new GameObject("Spawn Point (" + thisSpawnName + ")");
                    spawnPoint.transform.position = centre;
                    var spawnScript = spawnPoint.AddComponent<SpawnPoint>();
                    KitHelpers.SetString(spawnScript, "spawnName", thisSpawnName);
                }
                if (letter == 'E')
                {
                    var exit = new GameObject("Exit to " + exitScene.Replace("TopDown_", ""));
                    exit.transform.SetParent(exits);
                    exit.transform.position = centre;
                    exit.AddComponent<BoxCollider2D>().isTrigger = true;
                    var exitScript = exit.AddComponent<SceneExit>();
                    KitHelpers.SetString(exitScript, "sceneToLoad", exitScene);
                    KitHelpers.SetString(exitScript, "arriveAt", arriveAtOther);
                    KitHelpers.Set(exitScript, "exitSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + "GS2_Treasure_Chest_Unlock_Open.ogg"));
                }
            }
        }

        // Water is solid: one merged collider round each pond.
        var waterBody = water.gameObject.AddComponent<Rigidbody2D>();
        waterBody.bodyType = RigidbodyType2D.Static;
        water.gameObject.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
        water.gameObject.AddComponent<CompositeCollider2D>();

        // Invisible walls just outside the map.
        var edges = new GameObject("Map Edges").transform;
        Wall(edges, "Bottom", new Vector2(width / 2f, -0.5f), new Vector2(width + 2f, 1f));
        Wall(edges, "Top", new Vector2(width / 2f, height + 0.5f), new Vector2(width + 2f, 1f));
        Wall(edges, "Left", new Vector2(-0.5f, height / 2f), new Vector2(1f, height + 2f));
        Wall(edges, "Right", new Vector2(width + 0.5f, height / 2f), new Vector2(1f, height + 2f));

        // Feet a little below the middle of the start cell.
        Vector3 playerPosition = spawn;
        if (hasStart) playerPosition = start;
        var player = KitHelpers.Place(playerPrefab, playerPosition + new Vector3(0f, -0.3f, 0f), null);
        player.name = "Player";

        var camera = KitHelpers.MainCamera(Grass);
        camera.transform.position = new Vector3(player.transform.position.x, player.transform.position.y, -10f);
        var follow = camera.gameObject.AddComponent<CameraFollow>();
        KitHelpers.Set(follow, "target", player.transform);
        KitHelpers.SetVector2(follow, "levelBottomLeft", Vector2.zero);
        KitHelpers.SetVector2(follow, "levelTopRight", new Vector2(width, height));

        PrefabUtility.InstantiatePrefab(levelUiPrefab);
        KitHelpers.EventSystemObject();
        return Save(scene, sceneName) + " (" + width + "x" + height + ", " + gemNumber + " gems)";
    }

    static TileBase Decor(int index)
    {
        return AssetDatabase.LoadAssetAtPath<TileBase>(TileFolder + "/decor_" + index + ".asset");
    }

    // 3x3 edge sets: columns 1-3, rows 0-2 of a 6-column sheet; plus inner corners (4, 5, 10, 11) for path crossings.
    static TileBase EdgedTile(HashSet<Vector2Int> area, Vector2Int cell, string prefix, bool innerCorners)
    {
        bool up = area.Contains(cell + Vector2Int.up);
        bool down = area.Contains(cell + Vector2Int.down);
        bool left = area.Contains(cell + Vector2Int.left);
        bool right = area.Contains(cell + Vector2Int.right);
        int row = !up ? 0 : (!down ? 2 : 1);
        int column = !left ? 1 : (!right ? 3 : 2);
        int index = row * 6 + column;
        if (index == 8 && innerCorners)
        {
            if (!area.Contains(cell + new Vector2Int(-1, 1))) index = 11;
            else if (!area.Contains(cell + new Vector2Int(1, 1))) index = 10;
            else if (!area.Contains(cell + new Vector2Int(-1, -1))) index = 5;
            else if (!area.Contains(cell + new Vector2Int(1, -1))) index = 4;
        }
        return AssetDatabase.LoadAssetAtPath<TileBase>(TileFolder + "/" + prefix + index + ".asset");
    }

    static void Wall(Transform parent, string name, Vector2 centre, Vector2 size)
    {
        var wall = new GameObject(name);
        wall.transform.SetParent(parent);
        var box = wall.AddComponent<BoxCollider2D>();
        box.offset = centre;
        box.size = size;
    }

    static Tilemap TilemapObject(Grid grid, string name, int order)
    {
        var tilemapObject = new GameObject(name);
        tilemapObject.transform.SetParent(grid.transform);
        var tilemap = tilemapObject.AddComponent<Tilemap>();
        tilemapObject.AddComponent<TilemapRenderer>().sortingOrder = order;
        return tilemap;
    }

    static string Save(UnityEngine.SceneManagement.Scene scene, string name)
    {
        KitHelpers.EnsureFolder(SceneFolder);
        string path = SceneFolder + "/" + name + ".unity";
        EditorSceneManager.SaveScene(scene, path);
        return "Saved " + path;
    }
}
