// Builds the Platformer Starter (tiles, animations, prefabs, scenes) from the art in Assets/PlatformerStarter
// and the text maps in Assets/_KitBuilder/Levels. Not exported, not student code.
using System.Collections.Generic;
using System.IO;
using System.Linq;
using PlatformerStarter;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class PlatformerKitBuilder
{
    const string Root = "Assets/PlatformerStarter";
    const string Cliffside = Root + "/Art/Tiles/Cliffside_Greens.png";
    const string Animations = Root + "/Art/Tiles/Cliffside_Greens_Animations.png";
    const string Sprites = Root + "/Art/Sprites/";
    const string Backgrounds = Root + "/Art/Backgrounds/";
    const string TileFolder = Root + "/Tiles";
    const string AnimationFolder = Root + "/Animation";
    const string PrefabFolder = Root + "/Prefabs";
    const string SceneFolder = Root + "/Scenes";
    const string ActionsPath = Root + "/Input/PlatformerControls.inputactions";
    const string MixerPath = Root + "/Audio/MainMixer.mixer";
    const string MusicPath = Root + "/Audio/Music/Placeholder_Chiptune_Loop.ogg";
    const string Sfx = Root + "/Audio/SFX/";
    const string Levels = "Assets/_KitBuilder/Levels/";

    // Cliffside sheet sprites for the edges of a block of ground.
    const int TopLeft = 45, Top = 22, TopRight = 49, Left = 70, Fill = 72, Right = 74, InnerUpLeft = 46, InnerUpRight = 48;
    static readonly Color Sky = new Color32(0x00, 0xd4, 0xff, 255);

    static Dictionary<string, InputActionReference> actions;
    static GameObject playerPrefab, coinPrefab, portalPrefab, gameManagerPrefab, audioManagerPrefab, sceneLoaderPrefab, levelUiPrefab;

    public static string Build()
    {
        string log = "";
        actions = KitHelpers.ActionReferences(ActionsPath);
        MakeTiles();
        MakeAnimations();
        MakePrefabs();
        log += MenuScene() + "\n";
        log += LevelScene("Platformer_Level1", "Platformer_Level2") + "\n";
        log += LevelScene("Platformer_Level2", "Platformer_MainMenu") + "\n";
        AssetDatabase.SaveAssets();
        return log;
    }

    // ---------- Tiles and animations ----------

    static void MakeTiles()
    {
        // Every terrain piece of the Cliffside sheet (the top 11 rows of its 25x25 grid: cliffside_0 to 274,
        // brown rock and grassy), laid out in the palette as on the sheet.
        var cells = new Dictionary<Vector3Int, TileBase>();
        foreach (var sprite in AssetDatabase.LoadAllAssetsAtPath(Cliffside).OfType<Sprite>())
        {
            if (!sprite.name.StartsWith("cliffside_")) continue;
            int index = int.Parse(sprite.name.Substring("cliffside_".Length));
            if (index >= 275) continue;
            var tile = KitHelpers.CreateTile(TileFolder + "/Cliffside", sprite.name, sprite, Tile.ColliderType.Grid);
            cells[new Vector3Int(index % 25, -(index / 25), 0)] = tile;
        }
        var spikes = KitHelpers.CreateTile(TileFolder, "spikes", AssetDatabase.LoadAssetAtPath<Sprite>(Sprites + "spikes.png"), Tile.ColliderType.Sprite);
        cells[new Vector3Int(26, 0, 0)] = spikes;
        KitHelpers.Palette(TileFolder, "Platformer Palette", cells);
    }

    static void MakeAnimations()
    {
        // There's no run or land sheet: Run is the walk played faster, Land is two crouched jump frames.
        var idle = KitHelpers.CreateClip(AnimationFolder, "Hero_Idle", KitHelpers.Frames(Sprites + "Hero_007_Idle.png", "hero_idle_", 0, 6), 8, true, null);
        var run = KitHelpers.CreateClip(AnimationFolder, "Hero_Run", KitHelpers.Frames(Sprites + "Hero_007_Walk.png", "hero_walk_", 0, 6), 14, true, null);
        var jump = KitHelpers.CreateClip(AnimationFolder, "Hero_Jump", KitHelpers.Frames(Sprites + "Hero_007_Jump.png", "hero_jump_", 0, 6), 10, true, null);
        var fall = KitHelpers.CreateClip(AnimationFolder, "Hero_Fall", KitHelpers.Frames(Sprites + "Hero_007_Fall.png", "hero_fall_", 0, 6), 10, true, null);
        var land = KitHelpers.CreateClip(AnimationFolder, "Hero_Land", KitHelpers.Frames(Sprites + "Hero_007_Jump.png", "hero_jump_", 0, 2), 12, false, null);

        string path = AnimationFolder + "/Hero.controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("VerticalSpeed", AnimatorControllerParameterType.Float);
        controller.AddParameter("IsGrounded", AnimatorControllerParameterType.Bool);
        var machine = controller.layers[0].stateMachine;
        var idleState = machine.AddState("Idle", new Vector3(300, 0));
        idleState.motion = idle;
        var runState = machine.AddState("Run", new Vector3(300, 100));
        runState.motion = run;
        var jumpState = machine.AddState("Jump", new Vector3(600, -50));
        jumpState.motion = jump;
        var fallState = machine.AddState("Fall", new Vector3(600, 100));
        fallState.motion = fall;
        var landState = machine.AddState("Land", new Vector3(600, 250));
        landState.motion = land;
        machine.defaultState = idleState;

        Transition(idleState, runState, AnimatorConditionMode.Greater, 0.1f, "Speed");
        Transition(runState, idleState, AnimatorConditionMode.Less, 0.1f, "Speed");
        var toJump = machine.AddAnyStateTransition(jumpState);
        KitHelpers.Instant(toJump, false);
        toJump.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        toJump.AddCondition(AnimatorConditionMode.Greater, 0.1f, "VerticalSpeed");
        var toFall = machine.AddAnyStateTransition(fallState);
        KitHelpers.Instant(toFall, false);
        toFall.AddCondition(AnimatorConditionMode.IfNot, 0, "IsGrounded");
        toFall.AddCondition(AnimatorConditionMode.Less, -0.1f, "VerticalSpeed");
        var fallToLand = fallState.AddTransition(landState);
        KitHelpers.Instant(fallToLand, false);
        fallToLand.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        var jumpToIdle = jumpState.AddTransition(idleState);
        KitHelpers.Instant(jumpToIdle, false);
        jumpToIdle.AddCondition(AnimatorConditionMode.If, 0, "IsGrounded");
        var landToRun = landState.AddTransition(runState);
        KitHelpers.Instant(landToRun, false);
        landToRun.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
        var landToIdle = landState.AddTransition(idleState);
        KitHelpers.Instant(landToIdle, true);

        var coinSpin = KitHelpers.CreateClip(AnimationFolder, "Coin_Spin", KitHelpers.Frames(Sprites + "PS_Coins_3.png", "coin_", 0, 8), 10, true, null);
        KitHelpers.SimpleController(AnimationFolder, "Coin", coinSpin);
        var portal = KitHelpers.CreateClip(AnimationFolder, "Portal_Swirl", KitHelpers.Frames(Animations, "portal_", 0, 8), 10, true, null);
        KitHelpers.SimpleController(AnimationFolder, "Portal", portal);
    }

    static void Transition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, float threshold, string parameter)
    {
        var transition = from.AddTransition(to);
        KitHelpers.Instant(transition, false);
        transition.AddCondition(mode, threshold, parameter);
    }

    // ---------- Prefabs ----------

    static void MakePrefabs()
    {
        // No friction on the player, so it doesn't stick to walls when pushing against them in the air.
        string noFrictionPath = Root + "/Physics/No Friction.physicsMaterial2D";
        KitHelpers.EnsureFolder(Root + "/Physics");
        AssetDatabase.DeleteAsset(noFrictionPath);
        var noFriction = new PhysicsMaterial2D("No Friction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(noFriction, noFrictionPath);

        var player = new GameObject("Player");
        player.tag = "Player";
        var renderer = player.AddComponent<SpriteRenderer>();
        renderer.sprite = KitHelpers.SpriteNamed(Sprites + "Hero_007_Idle.png", "hero_idle_0");
        renderer.sortingOrder = 10;
        var body = player.AddComponent<Rigidbody2D>();
        body.gravityScale = 3f;
        body.freezeRotation = true;
        body.interpolation = RigidbodyInterpolation2D.Interpolate;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        var capsule = player.AddComponent<CapsuleCollider2D>();
        capsule.size = new Vector2(0.75f, 1f);
        capsule.offset = new Vector2(0f, 0.5f);
        capsule.sharedMaterial = noFriction;
        player.AddComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimationFolder + "/Hero.controller");
        var groundCheck = new GameObject("Ground Check").transform;
        groundCheck.SetParent(player.transform, false);
        var controller = player.AddComponent<PlayerController>();
        KitHelpers.Set(controller, "groundCheck", groundCheck);
        KitHelpers.Set(controller, "moveAction", actions["Move"]);
        KitHelpers.Set(controller, "jumpAction", actions["Jump"]);
        KitHelpers.Set(controller, "jumpSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + "Jump.ogg"));
        KitHelpers.Set(controller, "doubleJumpSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + "DoubleJump.ogg"));
        player.AddComponent<PlayerAnimator>();
        var respawn = player.AddComponent<PlayerRespawn>();
        KitHelpers.Set(respawn, "hurtSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + "GS1_Damage_1.ogg"));
        playerPrefab = KitHelpers.SavePrefab(player, PrefabFolder);

        var coin = new GameObject("Coin");
        var coinRenderer = coin.AddComponent<SpriteRenderer>();
        coinRenderer.sprite = KitHelpers.SpriteNamed(Sprites + "PS_Coins_3.png", "coin_0");
        coinRenderer.sortingOrder = 5;
        coin.AddComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimationFolder + "/Coin.controller");
        var coinTrigger = coin.AddComponent<CircleCollider2D>();
        coinTrigger.isTrigger = true;
        coinTrigger.radius = 0.35f;
        var coinScript = coin.AddComponent<Coin>();
        KitHelpers.Set(coinScript, "collectSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + "GS2_Item_Acquire_1.ogg"));
        coinPrefab = KitHelpers.SavePrefab(coin, PrefabFolder);

        var portal = new GameObject("Goal Portal");
        portal.transform.localScale = new Vector3(2f, 2f, 1f);
        var portalRenderer = portal.AddComponent<SpriteRenderer>();
        portalRenderer.sprite = KitHelpers.SpriteNamed(Animations, "portal_0");
        portalRenderer.sortingOrder = 4;
        portal.AddComponent<Animator>().runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AnimationFolder + "/Portal.controller");
        var portalTrigger = portal.AddComponent<BoxCollider2D>();
        portalTrigger.isTrigger = true;
        portalTrigger.size = new Vector2(0.8f, 1.6f);
        portalTrigger.offset = new Vector2(0f, 0.8f);
        var goal = portal.AddComponent<LevelGoal>();
        KitHelpers.Set(goal, "reachedSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + "GS2_Treasure_Chest_Unlock_Open.ogg"));
        portalPrefab = KitHelpers.SavePrefab(portal, PrefabFolder);

        string managers = PrefabFolder + "/Managers";
        var gameManager = new GameObject("GameManager");
        gameManager.AddComponent<GameManager>();
        gameManagerPrefab = KitHelpers.SavePrefab(gameManager, managers);
        audioManagerPrefab = KitHelpers.SavePrefab(KitHelpers.AudioManagerObject(typeof(AudioManager), MixerPath, MusicPath), managers);
        sceneLoaderPrefab = KitHelpers.SavePrefab(KitHelpers.SceneLoaderObject(typeof(SceneLoader)), managers);

        levelUiPrefab = KitHelpers.SavePrefab(LevelUi(), PrefabFolder);
    }

    // The HUD (coin counter) and the pause menu, on one canvas.
    static GameObject LevelUi()
    {
        var canvas = KitHelpers.Canvas("Level UI", 10);
        var hudBox = KitHelpers.Panel("Coin Box", canvas, new Vector2(0f, 1f), new Vector2(190, -60), new Vector2(320, 80));
        var coinText = KitHelpers.Text("Coin Text", hudBox, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(280, 80), "Coins: 0", 48, TextAlignmentOptions.Left);
        var hintBox = KitHelpers.Panel("Pause Hint Box", canvas, new Vector2(1f, 1f), new Vector2(-140, -50), new Vector2(220, 60));
        KitHelpers.Text("Pause Hint", hintBox, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200, 60), "Esc: Pause", 30, TextAlignmentOptions.Center);
        canvas.gameObject.AddComponent<CoinCounter>();
        KitHelpers.Set(canvas.GetComponent<CoinCounter>(), "coinText", coinText);

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

    // Sky, and two layers of trees, following the camera. Tiled so they fill any screen width.
    static void BackgroundsOn(Camera camera)
    {
        string[] layers = { "PS_Tileset_07_Background00", "PS_Tileset_07_Background01", "PS_Tileset_07_Background02" };
        for (int i = 0; i < layers.Length; i++)
        {
            var layer = new GameObject("Background " + (i + 1));
            layer.transform.SetParent(camera.transform, false);
            // 15 units tall (the view is 11.25), lowered so the tree tops sit around the middle of the view,
            // the nearer trees (layer 3) lower than the far ones.
            float[] heights = { 0f, -1.5f, -3.5f };
            layer.transform.localPosition = new Vector3(0f, heights[i], 10f);
            var renderer = layer.AddComponent<SpriteRenderer>();
            renderer.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Backgrounds + layers[i] + ".png");
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.size = new Vector2(45f, 15f);
            renderer.sortingOrder = -30 + i * 10;
        }
    }

    // ---------- Scenes ----------

    static string MenuScene()
    {
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        KitHelpers.GlobalLight();
        Managers();
        var camera = KitHelpers.MainCamera(Sky);
        BackgroundsOn(camera);
        KitHelpers.EventSystemObject();

        var canvas = KitHelpers.Canvas("Menu Canvas", 0);
        var menu = canvas.gameObject.AddComponent<MainMenu>();
        KitHelpers.Panel("Menu Box", canvas, new Vector2(0.5f, 0.5f), new Vector2(0, 40), new Vector2(1640, 880));
        KitHelpers.Set(menu, "clickSound", AssetDatabase.LoadAssetAtPath<AudioClip>(Sfx + "SciFi_UI_Button_1.ogg"));
        KitHelpers.Text("Title", canvas, new Vector2(0.5f, 0.5f), new Vector2(0, 360), new Vector2(1400, 160), "Platformer Starter", 120, TextAlignmentOptions.Center);
        KitHelpers.Text("Controls", canvas, new Vector2(0.5f, 0.5f), new Vector2(0, 240), new Vector2(1400, 80),
            "Arrows or A/D to run, Space to jump. Jump again in the air to double jump.", 36, TextAlignmentOptions.Center);
        KitHelpers.Button(canvas, "Play", new Vector2(0, 100), menu, menu.PlayGame);
        KitHelpers.VolumeSlider(canvas, "Music", new Vector2(0, -30), typeof(VolumeSlider), "MusicVolume");
        KitHelpers.VolumeSlider(canvas, "Sound Effects", new Vector2(0, -110), typeof(VolumeSlider), "SFXVolume");
        KitHelpers.Button(canvas, "Quit", new Vector2(0, -250), menu, menu.QuitGame);

        return Save(scene, "Platformer_MainMenu");
    }

    static string LevelScene(string sceneName, string nextScene)
    {
        string[] rows = File.ReadAllLines(Levels + sceneName + ".txt").Where(r => r.Length > 0).ToArray();
        int width = rows.Max(r => r.Length);
        int height = rows.Length;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        KitHelpers.GlobalLight();
        Managers();

        var grid = new GameObject("Grid").AddComponent<Grid>();
        var ground = TilemapObject(grid, "Ground", 0);
        // Empty, for painting things to look at (no collider: the player passes in front of them).
        TilemapObject(grid, "Decoration", -1);
        var spikes = TilemapObject(grid, "Spikes", 1);

        var filled = new HashSet<Vector2Int>();
        for (int row = 0; row < height; row++)
            for (int column = 0; column < rows[row].Length; column++)
                if (rows[row][column] == '#') filled.Add(new Vector2Int(column, height - 1 - row));

        var coins = new GameObject("Coins").transform;
        Vector3 start = Vector3.zero;
        int coinNumber = 0;
        for (int row = 0; row < height; row++)
        {
            for (int column = 0; column < rows[row].Length; column++)
            {
                char cell = rows[row][column];
                var position = new Vector3Int(column, height - 1 - row, 0);
                if (cell == '#') ground.SetTile(position, GroundTile(filled, new Vector2Int(position.x, position.y), width));
                if (cell == '^') spikes.SetTile(position, AssetDatabase.LoadAssetAtPath<TileBase>(TileFolder + "/spikes.asset"));
                if (cell == 'P') start = new Vector3(column + 0.5f, position.y, 0);
                if (cell == 'c')
                {
                    coinNumber++;
                    var coin = KitHelpers.Place(coinPrefab, new Vector3(column + 0.5f, position.y + 0.5f, 0), coins);
                    coin.name = "Coin " + coinNumber;
                }
                if (cell == 'D')
                {
                    var portal = KitHelpers.Place(portalPrefab, new Vector3(column + 0.5f, position.y, 0), null);
                    KitHelpers.SetString(portal.GetComponent<LevelGoal>(), "nextSceneName", nextScene);
                }
            }
        }

        // Ground: one merged collider (smoother to run along than one box per tile).
        var groundBody = ground.gameObject.AddComponent<Rigidbody2D>();
        groundBody.bodyType = RigidbodyType2D.Static;
        ground.gameObject.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
        ground.gameObject.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;
        if (spikes.GetUsedTilesCount() > 0)
        {
            spikes.gameObject.AddComponent<TilemapCollider2D>().isTrigger = true;
            spikes.gameObject.AddComponent<KillZone>();
        }
        else Object.DestroyImmediate(spikes.gameObject);

        // A trigger under the whole level catches falls; walls at both ends keep the player in.
        var edges = new GameObject("Level Edges").transform;
        var fallZone = new GameObject("Fall Zone");
        fallZone.transform.SetParent(edges);
        var fallBox = fallZone.AddComponent<BoxCollider2D>();
        fallBox.isTrigger = true;
        fallBox.offset = new Vector2(width / 2f, -3f);
        fallBox.size = new Vector2(width + 20f, 2f);
        fallZone.AddComponent<KillZone>();
        Wall(edges, "Left Wall", new Vector2(-0.5f, height / 2f + 5f), new Vector2(1f, height + 10f));
        Wall(edges, "Right Wall", new Vector2(width + 0.5f, height / 2f + 5f), new Vector2(1f, height + 10f));

        var player = KitHelpers.Place(playerPrefab, start, null);
        player.name = "Player";

        var camera = KitHelpers.MainCamera(Sky);
        camera.transform.position = new Vector3(start.x, start.y, -10f);
        BackgroundsOn(camera);
        var follow = camera.gameObject.AddComponent<CameraFollow>();
        KitHelpers.Set(follow, "target", player.transform);
        // The bottom edge is 2 below the map, so the pits show a little.
        KitHelpers.SetVector2(follow, "levelBottomLeft", new Vector2(0f, -2f));
        KitHelpers.SetVector2(follow, "levelTopRight", new Vector2(width, height + 2f));

        PrefabUtility.InstantiatePrefab(levelUiPrefab);
        KitHelpers.EventSystemObject();
        return Save(scene, sceneName) + " (" + width + "x" + height + ", " + coinNumber + " coins)";
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

    static TileBase GroundTile(HashSet<Vector2Int> filled, Vector2Int cell, int width)
    {
        bool up = filled.Contains(cell + Vector2Int.up);
        // The map's left and right edges count as more ground, so no cliff edge is drawn there.
        bool left = filled.Contains(cell + Vector2Int.left) || cell.x == 0;
        bool right = filled.Contains(cell + Vector2Int.right) || cell.x == width - 1;
        int index;
        if (!up)
        {
            if (!left) index = TopLeft;
            else if (!right) index = TopRight;
            else index = Top;
        }
        else if (!left) index = Left;
        else if (!right) index = Right;
        else if (!filled.Contains(cell + new Vector2Int(-1, 1))) index = InnerUpLeft;
        else if (!filled.Contains(cell + new Vector2Int(1, 1))) index = InnerUpRight;
        else index = Fill;
        return AssetDatabase.LoadAssetAtPath<TileBase>(TileFolder + "/Cliffside/cliffside_" + index + ".asset");
    }

    static string Save(UnityEngine.SceneManagement.Scene scene, string name)
    {
        KitHelpers.EnsureFolder(SceneFolder);
        string path = SceneFolder + "/" + name + ".unity";
        EditorSceneManager.SaveScene(scene, path);
        return "Saved " + path;
    }
}
