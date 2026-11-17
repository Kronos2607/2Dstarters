// Shared helpers for the starter kit builders (not exported, not student code).
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.Tilemaps;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

public static class KitHelpers
{
    // ---------- One-off project preparation ----------

    // TMP Essentials are needed in this builder project for the default font. They are NOT exported:
    // students import them from the TMP prompt (their GUIDs are the same in every project).
    public static void ImportTMPEssentials()
    {
        if (Directory.Exists("Assets/TextMesh Pro")) return;
        string package = Path.GetFullPath("Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage");
        AssetDatabase.ImportPackage(package, false);
    }

    // ---------- Serialized fields ----------

    public static void Set(Object target, string field, Object value)
    {
        var so = new SerializedObject(target);
        var property = so.FindProperty(field);
        if (property == null) throw new System.Exception("No field " + field + " on " + target);
        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetString(Object target, string field, string value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).stringValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetBool(Object target, string field, bool value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    public static void SetVector2(Object target, string field, Vector2 value)
    {
        var so = new SerializedObject(target);
        so.FindProperty(field).vector2Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    // ---------- Assets ----------

    public static Sprite SpriteNamed(string path, string name)
    {
        var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault(s => s.name == name);
        if (sprite == null) throw new System.Exception("No sprite " + name + " in " + path);
        return sprite;
    }

    public static Sprite[] Frames(string path, string prefix, int first, int count)
    {
        var frames = new Sprite[count];
        for (int i = 0; i < count; i++) frames[i] = SpriteNamed(path, prefix + (first + i));
        return frames;
    }

    public static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    public static Tile CreateTile(string folder, string name, Sprite sprite, Tile.ColliderType colliderType)
    {
        EnsureFolder(folder);
        string path = folder + "/" + name + ".asset";
        AssetDatabase.DeleteAsset(path);
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = colliderType;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    // A sprite-swap clip. flip: set the SpriteRenderer's Flip X in every clip (true for left-facing ones),
    // so moving between clips always leaves the flip right. Null = don't touch Flip X.
    public static AnimationClip CreateClip(string folder, string name, Sprite[] frames, float fps, bool loop, bool? flip)
    {
        EnsureFolder(folder);
        var clip = new AnimationClip { frameRate = fps };
        int count = loop ? frames.Length + 1 : frames.Length;
        var keys = new ObjectReferenceKeyframe[count];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };
        if (loop) keys[frames.Length] = new ObjectReferenceKeyframe { time = frames.Length / fps, value = frames[0] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        if (flip.HasValue)
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_FlipX"), AnimationCurve.Constant(0f, frames.Length / fps, flip.Value ? 1f : 0f));
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        string path = folder + "/" + name + ".anim";
        AssetDatabase.DeleteAsset(path);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    public static AnimatorController SimpleController(string folder, string name, AnimationClip clip)
    {
        EnsureFolder(folder);
        string path = folder + "/" + name + ".controller";
        AssetDatabase.DeleteAsset(path);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        controller.layers[0].stateMachine.AddState(clip.name).motion = clip;
        return controller;
    }

    public static void Instant(AnimatorStateTransition transition, bool exitTime)
    {
        transition.hasExitTime = exitTime;
        transition.exitTime = 1f;
        transition.duration = 0f;
        transition.canTransitionToSelf = false;
    }

    // Changes the pivot of every sprite in a sliced sheet (sprite IDs stay the same, so clips still work).
    public static void SetPivots(string path, Vector2 pivot)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();
        var rects = provider.GetSpriteRects();
        foreach (var rect in rects)
        {
            rect.alignment = SpriteAlignment.Custom;
            rect.pivot = pivot;
        }
        provider.SetSpriteRects(rects);
        provider.Apply();
        importer.SaveAndReimport();
    }

    public static Dictionary<string, InputActionReference> ActionReferences(string actionsPath)
    {
        AssetDatabase.ImportAsset(actionsPath);
        return AssetDatabase.LoadAllAssetsAtPath(actionsPath).OfType<InputActionReference>()
            .Where(r => r.action != null).GroupBy(r => r.action.name).ToDictionary(g => g.Key, g => g.First());
    }

    public static GameObject SavePrefab(GameObject gameObject, string folder)
    {
        EnsureFolder(folder);
        string path = folder + "/" + gameObject.name + ".prefab";
        AssetDatabase.DeleteAsset(path);
        var prefab = PrefabUtility.SaveAsPrefabAsset(gameObject, path);
        Object.DestroyImmediate(gameObject);
        return prefab;
    }

    public static GameObject Place(GameObject prefab, Vector3 position, Transform parent)
    {
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        instance.transform.position = position;
        if (parent != null) instance.transform.SetParent(parent, true);
        return instance;
    }

    // A Tile Palette (Window > 2D > Tile Palette) holding the given tiles at the given cells.
    public static void Palette(string folder, string name, Dictionary<Vector3Int, TileBase> cells)
    {
        EnsureFolder(folder);
        string path = folder + "/" + name + ".prefab";
        AssetDatabase.DeleteAsset(path);
        GridPaletteUtility.CreateNewPalette(folder, name, GridLayout.CellLayout.Rectangle,
            GridPalette.CellSizing.Automatic, Vector3.one, GridLayout.CellSwizzle.XYZ);
        var root = PrefabUtility.LoadPrefabContents(path);
        var tilemap = root.GetComponentInChildren<Tilemap>();
        foreach (var cell in cells) tilemap.SetTile(cell.Key, cell.Value);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    // ---------- Managers ----------

    // The AudioManager object: two AudioSources routed to the mixer's Music and SFX groups.
    public static GameObject AudioManagerObject(System.Type type, string mixerPath, string musicPath)
    {
        var root = new GameObject("AudioManager");
        var mixer = AssetDatabase.LoadAssetAtPath<AudioMixer>(mixerPath);
        var music = root.AddComponent<AudioSource>();
        music.playOnAwake = false;
        music.loop = true;
        music.outputAudioMixerGroup = mixer.FindMatchingGroups("Music")[0];
        var effects = root.AddComponent<AudioSource>();
        effects.playOnAwake = false;
        effects.outputAudioMixerGroup = mixer.FindMatchingGroups("SFX")[0];
        var manager = root.AddComponent(type);
        Set(manager, "mixer", mixer);
        Set(manager, "musicSource", music);
        Set(manager, "effectsSource", effects);
        Set(manager, "startingMusic", AssetDatabase.LoadAssetAtPath<AudioClip>(musicPath));
        return root;
    }

    // The SceneLoader object with its fade: a screen-space canvas drawn over everything, with a black image.
    public static GameObject SceneLoaderObject(System.Type type)
    {
        var root = new GameObject("SceneLoader");
        var canvasObject = new GameObject("Fade Canvas", typeof(RectTransform));
        canvasObject.transform.SetParent(root.transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObject.AddComponent<CanvasScaler>();
        canvasObject.AddComponent<GraphicRaycaster>();
        var group = canvasObject.AddComponent<CanvasGroup>();
        group.alpha = 0f;
        group.blocksRaycasts = false;
        var black = UiRect("Black", canvasObject.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        black.gameObject.AddComponent<Image>().color = Color.black;
        var loader = root.AddComponent(type);
        Set(loader, "fadeGroup", group);
        return root;
    }

    // ---------- Scenes ----------

    public static void GlobalLight()
    {
        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
    }

    public static Camera MainCamera(Color background)
    {
        var cameraObject = new GameObject("Main Camera");
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);
        var camera = cameraObject.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5.625f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = background;
        cameraObject.AddComponent<AudioListener>();
        // Pixel art: 16 pixels per unit, a 320x180 view scaled up by whole numbers so pixels stay square.
        var pixelPerfect = cameraObject.AddComponent<PixelPerfectCamera>();
        pixelPerfect.assetsPPU = 16;
        pixelPerfect.refResolutionX = 320;
        pixelPerfect.refResolutionY = 180;
        pixelPerfect.gridSnapping = PixelPerfectCamera.GridSnapping.PixelSnapping;
        return camera;
    }

    public static void EventSystemObject()
    {
        var eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        var module = eventSystem.AddComponent<InputSystemUIInputModule>();
        // Adding the module in the editor usually assigns the default UI actions already.
        if (module.actionsAsset == null)
        {
            try { module.AssignDefaultActions(); }
            catch (System.InvalidOperationException) { }
        }
    }

    // ---------- UI ----------

    public static RectTransform Canvas(string name, int order)
    {
        var canvasObject = new GameObject(name, typeof(RectTransform));
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = order;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();
        return (RectTransform)canvasObject.transform;
    }

    public static RectTransform UiRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    public static TextMeshProUGUI Text(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size, string content, float fontSize, TextAlignmentOptions alignment)
    {
        var rect = UiRect(name, parent, anchor, anchor, position, size);
        var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = Color.white;
        return text;
    }

    // A see-through dark box, so white text can be read over any background.
    public static RectTransform Panel(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = UiRect(name, parent, anchor, anchor, position, size);
        var image = rect.gameObject.AddComponent<Image>();
        image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        image.type = Image.Type.Sliced;
        image.color = new Color(0.08f, 0.1f, 0.18f, 0.75f);
        return rect;
    }

    static TMP_DefaultControls.Resources TmpResources()
    {
        var resources = new TMP_DefaultControls.Resources();
        resources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        resources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        return resources;
    }

    public static Button Button(Transform parent, string label, Vector2 position, Component target, UnityAction action)
    {
        var buttonObject = TMP_DefaultControls.CreateButton(TmpResources());
        buttonObject.name = label + " Button";
        var rect = buttonObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(360, 80);
        var text = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 40;
        text.color = new Color32(40, 40, 40, 255);
        var button = buttonObject.GetComponent<Button>();
        UnityEventTools.AddPersistentListener(button.onClick, action);
        return button;
    }

    public static Slider VolumeSlider(Transform parent, string label, Vector2 position, System.Type sliderScript, string parameter)
    {
        Text(label + " Label", parent, new Vector2(0.5f, 0.5f), position + new Vector2(-260, 0), new Vector2(280, 60), label, 36, TextAlignmentOptions.Right);
        var resources = new DefaultControls.Resources();
        resources.standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        resources.background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd");
        resources.knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
        var sliderObject = DefaultControls.CreateSlider(resources);
        sliderObject.name = label + " Slider";
        var rect = sliderObject.GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position + new Vector2(80, 0);
        rect.sizeDelta = new Vector2(360, 30);
        var slider = sliderObject.GetComponent<Slider>();
        slider.minValue = 0f;
        slider.maxValue = 1f;
        slider.value = 1f;
        var script = sliderObject.AddComponent(sliderScript);
        SetString(script, "exposedParameter", parameter);
        return slider;
    }

    // ---------- Export ----------

    public static string Export(string folder, string outputPath)
    {
        // Anything the folder uses from outside Assets/<folder> that a student project might not have.
        var assets = AssetDatabase.FindAssets("", new[] { folder }).Select(AssetDatabase.GUIDToAssetPath).ToArray();
        var outside = AssetDatabase.GetDependencies(assets, true)
            .Where(p => !p.StartsWith(folder + "/") && p.StartsWith("Assets/")).ToList();
        // Only the kit's own files: a folder export with Recurse can pick up stray assets from elsewhere
        // (it once included Assets/MobileDependencyResolver from the ads package).
        var files = new List<string> { folder };
        files.AddRange(assets);
        AssetDatabase.ExportPackage(files.ToArray(), outputPath, ExportPackageOptions.Default);
        string report = "Exported " + folder + " to " + outputPath + " (" + new FileInfo(outputPath).Length / 1024 + " KB)";
        if (outside.Count > 0) report += "\n  Uses from outside the folder (not exported): " + string.Join(", ", outside);
        return report;
    }
}
