using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using TMPro;
using System.IO;

// setup script for the scene. i kept it here because hand placing all this again sucks
public class BuildArena
{
    const string PREFAB_DIR = "Assets/Prefabs";
    const string ART_DIR = "Assets/Art";
    const string LOOK_VERSION = "_RecoilLook_v4";

    static readonly Color INK      = new Color(0.055f, 0.071f, 0.125f);
    static readonly Color FLOOR    = new Color(0.086f, 0.110f, 0.180f);
    static readonly Color GRIDLINE = new Color(0.135f, 0.17f, 0.28f, 0.58f);
    static readonly Color FRAME    = new Color(0.20f, 0.25f, 0.40f, 0.9f);
    static readonly Color AMBER    = new Color(0.949f, 0.757f, 0.306f);
    static readonly Color PALE     = new Color(1f, 0.914f, 0.659f);
    static readonly Color RED      = new Color(0.898f, 0.282f, 0.302f);
    static readonly Color VIOLET   = new Color(0.545f, 0.424f, 0.910f);
    static readonly Color TEAL     = new Color(0.169f, 0.702f, 0.639f);
    static readonly Color ICE      = new Color(0.416f, 0.843f, 1f);
    static readonly Color PAPER    = new Color(0.910f, 0.902f, 0.875f);
    static readonly Color DIM      = new Color(0.486f, 0.525f, 0.635f);

    static Material spriteMat;
    static Material lineMat;
    static Sprite playerSprite, chaserSprite, tankSprite, splitterSprite, ammoSprite, bulletSprite, sparkSprite, glowSprite, pixelSprite;

    [MenuItem("Tools/Recoil/Rebuild Game Look")]
    public static void Build()
    {
        if (!EditorUtility.DisplayDialog("Rebuild Recoil?", "This rebuilds the game objects in the open scene.", "yeah", "cancel")) return;
        BuildScene(false);
    }

    public static void BuildScene(bool saveWhenDone)
    {
        MakeSureFoldersExist();
        ImportArt();
        MakeMaterials();
        ClearOldStuff();
        SetUpCamera();
        MakePostFX();
        MakeBackground();

        GameObject sparkPf  = MakeSparkPrefab();
        GameObject bulletPf = MakeBulletPrefab();
        GameObject cellPf   = MakeCellPrefab();
        GameObject chaserPf   = MakeEnemyPrefab("Chaser", Enemy.Kind.Chaser, 1, 1.65f, 10, RED, 0.62f, chaserSprite, false);
        GameObject tankPf     = MakeEnemyPrefab("Tank", Enemy.Kind.Tank, 3, 0.90f, 25, VIOLET, 0.95f, tankSprite, true);
        GameObject splitterPf = MakeEnemyPrefab("Splitter", Enemy.Kind.Splitter, 1, 1.40f, 15, TEAL, 0.72f, splitterSprite, false);

        Enemy splitScript = splitterPf.GetComponent<Enemy>();
        splitScript.splitInto = splitterPf.GetComponent<Enemy>();
        EditorUtility.SetDirty(splitterPf);

        Shooter player = MakePlayer(bulletPf.GetComponent<Bullet>());
        GameBrain brain = MakeBrainAndUI();
        brain.chaserPrefab = chaserPf.GetComponent<Enemy>();
        brain.tankPrefab = tankPf.GetComponent<Enemy>();
        brain.splitterPrefab = splitterPf.GetComponent<Enemy>();
        brain.cellPrefab = cellPf.GetComponent<AmmoCell>();
        brain.sparkPrefab = sparkPf;
        EditorUtility.SetDirty(brain);

        new GameObject(LOOK_VERSION);

        AssetDatabase.SaveAssets();
        if (saveWhenDone) EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
        Debug.Log("Recoil scene rebuilt.");
        Selection.activeGameObject = brain.gameObject;
    }

    public static bool SceneHasCurrentLook()
    {
        return GameObject.Find(LOOK_VERSION) != null;
    }

    static void MakeSureFoldersExist()
    {
        if (!Directory.Exists(PREFAB_DIR)) AssetDatabase.CreateFolder("Assets", "Prefabs");
        if (!Directory.Exists(ART_DIR)) AssetDatabase.CreateFolder("Assets", "Art");
    }

    static void ImportArt()
    {
        SetSprite("player.png", 100);
        SetSprite("chaser.png", 100);
        SetSprite("tank.png", 100);
        SetSprite("splitter.png", 100);
        SetSprite("ammo.png", 100);
        SetSprite("bullet.png", 100);
        SetSprite("spark.png", 100);
        SetSprite("glow.png", 100);
        SetSprite("pixel.png", 16);

        playerSprite   = AssetDatabase.LoadAssetAtPath<Sprite>(ART_DIR + "/player.png");
        chaserSprite   = AssetDatabase.LoadAssetAtPath<Sprite>(ART_DIR + "/chaser.png");
        tankSprite     = AssetDatabase.LoadAssetAtPath<Sprite>(ART_DIR + "/tank.png");
        splitterSprite = AssetDatabase.LoadAssetAtPath<Sprite>(ART_DIR + "/splitter.png");
        ammoSprite     = AssetDatabase.LoadAssetAtPath<Sprite>(ART_DIR + "/ammo.png");
        bulletSprite   = AssetDatabase.LoadAssetAtPath<Sprite>(ART_DIR + "/bullet.png");
        sparkSprite    = AssetDatabase.LoadAssetAtPath<Sprite>(ART_DIR + "/spark.png");
        glowSprite     = AssetDatabase.LoadAssetAtPath<Sprite>(ART_DIR + "/glow.png");
        pixelSprite    = AssetDatabase.LoadAssetAtPath<Sprite>(ART_DIR + "/pixel.png");
    }

    static void SetSprite(string file, float ppu)
    {
        string path = ART_DIR + "/" + file;
        TextureImporter imp = AssetImporter.GetAtPath(path) as TextureImporter;
        if (imp == null) return;
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spritePixelsPerUnit = ppu;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.filterMode = FilterMode.Bilinear;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.SaveAndReimport();
    }

    static void MakeMaterials()
    {
        spriteMat = AssetDatabase.LoadAssetAtPath<Material>(ART_DIR + "/RecoilSprite.mat");
        if (spriteMat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (s == null) s = Shader.Find("Sprites/Default");
            spriteMat = new Material(s);
            AssetDatabase.CreateAsset(spriteMat, ART_DIR + "/RecoilSprite.mat");
        }

        lineMat = AssetDatabase.LoadAssetAtPath<Material>(ART_DIR + "/RecoilLine.mat");
        if (lineMat == null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Sprites/Default");
            lineMat = new Material(s);
            AssetDatabase.CreateAsset(lineMat, ART_DIR + "/RecoilLine.mat");
        }
    }

    static void ClearOldStuff()
    {
        string[] names = { "Background", "Player", "UI", "Canvas", "GameBrain", "Post FX", LOOK_VERSION };
        foreach (string n in names)
        {
            GameObject g = GameObject.Find(n);
            if (g != null) Object.DestroyImmediate(g);
        }
    }

    static void SetUpCamera()
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            GameObject go = new GameObject("Main Camera");
            cam = go.AddComponent<Camera>();
            go.tag = "MainCamera";
            go.AddComponent<AudioListener>();
        }
        cam.orthographic = true;
        cam.orthographicSize = 6.6f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = INK;
        cam.allowHDR = true;
        cam.transform.position = new Vector3(0f, 0f, -10f);
        cam.transform.rotation = Quaternion.identity;

        UniversalAdditionalCameraData data = cam.GetComponent<UniversalAdditionalCameraData>();
        if (data == null) data = cam.gameObject.AddComponent<UniversalAdditionalCameraData>();
        data.renderPostProcessing = true;
    }

    static void MakePostFX()
    {
        const string path = "Assets/Settings/RecoilVolume.asset";
        VolumeProfile old = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
        if (old != null) AssetDatabase.DeleteAsset(path);

        VolumeProfile profile = ScriptableObject.CreateInstance<VolumeProfile>();
        Bloom bloom = profile.Add<Bloom>(true);
        bloom.intensity.Override(1.05f);
        bloom.threshold.Override(0.65f);
        bloom.scatter.Override(0.65f);

        Vignette vig = profile.Add<Vignette>(true);
        vig.intensity.Override(0.22f);
        vig.smoothness.Override(0.72f);

        ColorAdjustments color = profile.Add<ColorAdjustments>(true);
        color.contrast.Override(7f);
        color.saturation.Override(-4f);

        AssetDatabase.CreateAsset(profile, path);

        GameObject go = new GameObject("Post FX");
        Volume v = go.AddComponent<Volume>();
        v.isGlobal = true;
        v.priority = 10f;
        v.sharedProfile = profile;
    }

    static SpriteRenderer AddSprite(GameObject go, Sprite s, Color c, int order)
    {
        SpriteRenderer sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = s;
        sr.color = c;
        sr.sortingOrder = order;
        sr.material = spriteMat;
        return sr;
    }

    static void AddGlow(GameObject parent, Color c, float scale, int order)
    {
        GameObject g = new GameObject("Glow");
        g.transform.SetParent(parent.transform, false);
        g.transform.localPosition = Vector3.zero;
        g.transform.localScale = Vector3.one * scale;
        Color gc = c;
        gc.a = 0.50f;
        SpriteRenderer sr = AddSprite(g, glowSprite, gc, order);
        sr.material = spriteMat;
    }

    static void MakeBackground()
    {
        float h = 6.6f;
        float w = h * (16f / 9f);
        GameObject root = new GameObject("Background");

        GameObject floor = new GameObject("Floor");
        floor.transform.SetParent(root.transform);
        SpriteRenderer fsr = AddSprite(floor, pixelSprite, FLOOR, -100);
        floor.transform.localScale = new Vector3(w * 2.1f, h * 2.1f, 1f);

        float grid = 1.45f;
        for (float x = -w; x <= w; x += grid) MakeBar(root.transform, "vline", new Vector3(x, 0, 0), new Vector3(0.018f, h * 2.03f, 1), GRIDLINE, -98);
        for (float y = -h; y <= h; y += grid) MakeBar(root.transform, "hline", new Vector3(0, y, 0), new Vector3(w * 2.03f, 0.018f, 1), GRIDLINE, -98);

        // frame around the play area so it reads like the html box instead of an empty screen
        MakeBar(root.transform, "top", new Vector3(0, h - .05f, 0), new Vector3(w * 2f, .045f, 1), FRAME, -95);
        MakeBar(root.transform, "bottom", new Vector3(0, -h + .05f, 0), new Vector3(w * 2f, .045f, 1), FRAME, -95);
        MakeBar(root.transform, "left", new Vector3(-w + .05f, 0, 0), new Vector3(.045f, h * 2f, 1), FRAME, -95);
        MakeBar(root.transform, "right", new Vector3(w - .05f, 0, 0), new Vector3(.045f, h * 2f, 1), FRAME, -95);
    }

    static void MakeBar(Transform parent, string name, Vector3 pos, Vector3 scale, Color c, int order)
    {
        GameObject line = new GameObject(name);
        line.transform.SetParent(parent);
        AddSprite(line, pixelSprite, c, order);
        line.transform.position = pos;
        line.transform.localScale = scale;
    }

    static GameObject SaveAsPrefab(GameObject go, string name)
    {
        string path = PREFAB_DIR + "/" + name + ".prefab";
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(go, path);
        Object.DestroyImmediate(go);
        return saved;
    }

    static GameObject MakeSparkPrefab()
    {
        GameObject go = new GameObject("Spark");
        AddSprite(go, sparkSprite, PALE, 20);
        go.transform.localScale = Vector3.one * 0.22f;
        go.AddComponent<Spark>();
        return SaveAsPrefab(go, "Spark");
    }

    static GameObject MakeBulletPrefab()
    {
        GameObject go = new GameObject("Bullet");
        AddGlow(go, PALE, 1.9f, 9);
        AddSprite(go, bulletSprite, PALE, 10);
        go.transform.localScale = new Vector3(0.55f, 0.55f, 1f);

        TrailRenderer tr = go.AddComponent<TrailRenderer>();
        tr.material = lineMat;
        tr.time = 0.11f;
        tr.minVertexDistance = 0.025f;
        tr.startWidth = 0.13f;
        tr.endWidth = 0.01f;
        tr.startColor = new Color(PALE.r, PALE.g, PALE.b, 0.85f);
        tr.endColor = new Color(AMBER.r, AMBER.g, AMBER.b, 0f);
        tr.sortingOrder = 8;

        go.AddComponent<Bullet>();
        return SaveAsPrefab(go, "Bullet");
    }

    static GameObject MakeCellPrefab()
    {
        GameObject go = new GameObject("AmmoCell");
        AddGlow(go, ICE, 2.5f, 4);
        AddSprite(go, ammoSprite, ICE, 5);
        go.transform.localScale = Vector3.one * 0.62f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.48f;

        AmmoCell cell = go.AddComponent<AmmoCell>();
        cell.gives = 7;
        return SaveAsPrefab(go, "AmmoCell");
    }

    static GameObject MakeEnemyPrefab(string name, Enemy.Kind kind, int hp, float speed, int points, Color color, float size, Sprite shape, bool showHp)
    {
        GameObject go = new GameObject(name);
        AddGlow(go, color, kind == Enemy.Kind.Tank ? 2.35f : 2.05f, 1);
        SpriteRenderer body = AddSprite(go, shape, color, 2);
        go.transform.localScale = Vector3.one * size;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = kind == Enemy.Kind.Tank ? 0.50f : 0.44f;

        Enemy e = go.AddComponent<Enemy>();
        e.kind = kind;
        e.hp = hp;
        e.speed = speed;
        e.points = points;
        e.body = body;

        if (showHp)
        {
            GameObject label = new GameObject("HpLabel");
            label.transform.SetParent(go.transform, false);
            label.transform.localPosition = Vector3.zero;
            label.transform.localScale = Vector3.one * 0.045f;

            TextMeshPro t = label.AddComponent<TextMeshPro>();
            t.text = hp.ToString();
            t.fontSize = 40;
            t.fontStyle = FontStyles.Bold;
            t.alignment = TextAlignmentOptions.Center;
            t.color = INK;
            t.sortingOrder = 3;
            t.GetComponent<MeshRenderer>().sortingOrder = 3;
            e.hpLabel = t;
        }
        return SaveAsPrefab(go, name);
    }

    static Shooter MakePlayer(Bullet bulletPrefab)
    {
        GameObject go = new GameObject("Player");
        go.transform.position = Vector3.zero;
        AddGlow(go, AMBER, 2.35f, 6);
        SpriteRenderer sr = AddSprite(go, playerSprite, AMBER, 8);
        go.transform.localScale = Vector3.one * 0.92f;

        Rigidbody2D rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D col = go.AddComponent<CircleCollider2D>();
        col.isTrigger = true;
        col.radius = 0.42f;

        TrailRenderer tr = go.AddComponent<TrailRenderer>();
        tr.material = lineMat;
        tr.time = 0.14f;
        tr.minVertexDistance = 0.04f;
        tr.startWidth = 0.20f;
        tr.endWidth = 0f;
        tr.startColor = new Color(AMBER.r, AMBER.g, AMBER.b, 0.38f);
        tr.endColor = new Color(AMBER.r, AMBER.g, AMBER.b, 0f);
        tr.sortingOrder = 5;

        LineRenderer aim = go.AddComponent<LineRenderer>();
        aim.material = lineMat;
        aim.useWorldSpace = true;
        aim.positionCount = 2;
        aim.startWidth = 0.018f;
        aim.endWidth = 0.008f;
        aim.startColor = new Color(PAPER.r, PAPER.g, PAPER.b, 0.16f);
        aim.endColor = new Color(PAPER.r, PAPER.g, PAPER.b, 0.06f);
        aim.sortingOrder = 0;

        Shooter s = go.AddComponent<Shooter>();
        s.bulletPrefab = bulletPrefab;
        s.body = sr;
        s.aimLine = aim;
        s.startAmmo = 16;
        s.maxAmmo = 28;
        return s;
    }

    static GameBrain MakeBrainAndUI()
    {
        GameObject canvasGo = new GameObject("UI");
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        TMP_Text score = MakeLabel(canvasGo, "ScoreText", new Vector2(0f, 1f), new Vector2(52f, -45f), TextAlignmentOptions.TopLeft, 36, PAPER, new Vector2(600, 90));
        TMP_Text best = MakeLabel(canvasGo, "BestText", new Vector2(1f, 1f), new Vector2(-52f, -45f), TextAlignmentOptions.TopRight, 34, DIM, new Vector2(600, 90));
        TMP_Text wave = MakeLabel(canvasGo, "WaveText", new Vector2(0.5f, 1f), new Vector2(0f, -45f), TextAlignmentOptions.Top, 32, DIM, new Vector2(500, 90));
        TMP_Text ammo = MakeLabel(canvasGo, "AmmoText", new Vector2(0f, 0f), new Vector2(52f, 55f), TextAlignmentOptions.BottomLeft, 30, DIM, new Vector2(900, 70));

        // ammo bar
        Image barBack = MakePanel(canvasGo, "AmmoBarBack", new Color(0.035f, 0.047f, 0.085f, 0.98f));
        RectTransform br = barBack.rectTransform;
        br.anchorMin = new Vector2(0f, 0f); br.anchorMax = new Vector2(1f, 0f);
        br.pivot = new Vector2(0.5f, 0f); br.anchoredPosition = new Vector2(0f, 26f);
        br.offsetMin = new Vector2(52f, 0f); br.offsetMax = new Vector2(-52f, 14f);
        Outline outline = barBack.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(GRIDLINE.r, GRIDLINE.g, GRIDLINE.b, 0.9f);
        outline.effectDistance = new Vector2(2f, -2f);

        Image fill = MakePanel(barBack.gameObject, "AmmoBarFill", AMBER);
        RectTransform fr = fill.rectTransform;
        fr.anchorMin = Vector2.zero; fr.anchorMax = Vector2.one; fr.offsetMin = new Vector2(2f, 2f); fr.offsetMax = new Vector2(-2f, -2f);
        fill.type = Image.Type.Filled; fill.fillMethod = Image.FillMethod.Horizontal; fill.fillOrigin = 0; fill.fillAmount = 1f;

        // dark start/death overlay
        Image overlay = MakePanel(canvasGo, "Overlay", new Color(0.055f, 0.071f, 0.125f, 0.90f));
        RectTransform ort = overlay.rectTransform;
        ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one; ort.offsetMin = Vector2.zero; ort.offsetMax = Vector2.zero;

        TMP_Text big = MakeLabel(overlay.gameObject, "BigText", new Vector2(0.5f, 0.5f), Vector2.zero, TextAlignmentOptions.Center, 76, PAPER, new Vector2(1500f, 650f));
        big.fontStyle = FontStyles.Bold;

        GameObject brainGo = new GameObject("GameBrain");
        brainGo.AddComponent<AudioSource>().playOnAwake = false;
        GameBrain brain = brainGo.AddComponent<GameBrain>();
        brain.scoreText = score;
        brain.bestText = best;
        brain.waveText = wave;
        brain.ammoText = ammo;
        brain.bigText = big;
        brain.ammoFill = fill;
        brain.overlayPanel = overlay.gameObject;
        brain.gapBetweenSpawns = 1.05f;
        brain.breatherBetweenWaves = 2.0f;
        brain.dropChance = 0.72f;
        return brain;
    }

    static TMP_Text MakeLabel(GameObject parent, string name, Vector2 anchor, Vector2 offset, TextAlignmentOptions align, float size, Color c, Vector2 box)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        TextMeshProUGUI t = go.AddComponent<TextMeshProUGUI>();
        t.text = name;
        t.fontSize = size;
        t.fontStyle = FontStyles.Bold;
        t.color = c;
        t.alignment = align;
        t.enableWordWrapping = false;
        t.richText = true;
        t.characterSpacing = 1.5f;
        RectTransform rt = t.rectTransform;
        rt.anchorMin = anchor; rt.anchorMax = anchor; rt.pivot = anchor;
        rt.anchoredPosition = offset; rt.sizeDelta = box;
        return t;
    }

    static Image MakePanel(GameObject parent, string name, Color c)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent.transform, false);
        Image im = go.AddComponent<Image>();
        im.color = c;
        im.raycastTarget = false;
        return im;
    }

    [MenuItem("Tools/Recoil/Wipe High Score")]
    public static void WipeBest()
    {
        PlayerPrefs.DeleteKey("recoil_best");
        PlayerPrefs.Save();
    }
}
