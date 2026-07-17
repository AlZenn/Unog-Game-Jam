using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Tek tıkla tüm oyun sahnesini kurar: kamera, ana karakter, 6 NPC, kapı,
// stat slider'ları, diyalog paneli, fade ekranı ve tüm referans bağlantıları.
public static class SceneSetupTool
{
    const string SpriteFolder = "Assets/Sprites";
    const string SquareSpritePath = SpriteFolder + "/Square.png";
    const string ScenePath = "Assets/Scenes/GameScene.unity";

    struct NpcDef
    {
        public string objName;
        public string displayName;
        public StatType stat;
        public Color color;
    }

    static readonly NpcDef[] Npcs =
    {
        new NpcDef { objName = "Kaos",     displayName = "Kaos",     stat = StatType.Ofke,      color = new Color(0.85f, 0.20f, 0.20f) },
        new NpcDef { objName = "Merhamet", displayName = "Merhamet", stat = StatType.Durustluk, color = new Color(0.95f, 0.55f, 0.75f) },
        new NpcDef { objName = "Utangac",  displayName = "Utangaç",  stat = StatType.Durustluk, color = new Color(0.55f, 0.60f, 0.90f) },
        new NpcDef { objName = "Heyecan",  displayName = "Heyecan",  stat = StatType.Ofke,      color = new Color(0.95f, 0.70f, 0.20f) },
        new NpcDef { objName = "Haz",      displayName = "Haz",      stat = StatType.Cikar,     color = new Color(0.70f, 0.35f, 0.85f) },
        new NpcDef { objName = "Acgozlu",  displayName = "Açgözlü",  stat = StatType.Cikar,     color = new Color(0.30f, 0.75f, 0.35f) },
    };

    static readonly (StatType stat, string label)[] SliderDefs =
    {
        (StatType.Ofke, "ÖFKE"),
        (StatType.Durustluk, "DÜRÜSTLÜK"),
        (StatType.Cikar, "ÇIKAR"),
    };

    static Font UIFont => Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

    [MenuItem("Tools/UnoG/Sahneyi Kur (Tam Kurulum)", priority = 10)]
    public static void SetupFullScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Sprite square = EnsureSquareSprite();
        var dialogues = SampleDialogueGenerator.EnsureAll();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildScene(square, dialogues);

        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log("UnoG: Sahne kuruldu → " + ScenePath);
    }

    // ---------------------------------------------------------------- sprite

    static Sprite EnsureSquareSprite()
    {
        if (!AssetDatabase.IsValidFolder(SpriteFolder))
            AssetDatabase.CreateFolder("Assets", "Sprites");

        if (AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath) == null)
        {
            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false);
            var pixels = new Color32[64 * 64];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(pixels);
            tex.Apply();
            File.WriteAllBytes(SquareSpritePath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(SquareSpritePath);

            var importer = (TextureImporter)AssetImporter.GetAtPath(SquareSpritePath);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64f; // 1 kare = 1 dünya birimi
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(SquareSpritePath);
    }

    // ---------------------------------------------------------------- scene

    static void BuildScene(Sprite square, SampleDialogueGenerator.DialogueLibrary dialogues)
    {
        // Kamera (1920x1080, ortho yükseklik 10.8 birim)
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 5.4f;
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.09f, 0.09f, 0.13f);
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();

        // URP 2D Renderer'da sprite'lar Sprite-Lit materyali kullanır;
        // global ışık olmadan siyah görünürler.
        var lightGo = new GameObject("Global Light 2D");
        var globalLight = lightGo.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        var lightSo = new SerializedObject(globalLight);
        lightSo.FindProperty("m_LightType").intValue =
            (int)UnityEngine.Rendering.Universal.Light2D.LightType.Global;
        lightSo.ApplyModifiedPropertiesWithoutUndo();
        globalLight.intensity = 1f;

        // Manager objesi
        var managers = new GameObject("Managers");
        var gm = managers.AddComponent<GameManager>();
        var stats = managers.AddComponent<StatManager>();
        var clickManager = managers.AddComponent<ClickManager>();
        var dialogueManager = managers.AddComponent<DialogueManager>();
        var endController = managers.AddComponent<GameEndController>();
        clickManager.worldCamera = cam;

        // Dünya objeleri
        var world = new GameObject("World");
        CreateSquare(world.transform, "Player", square, new Color(0.9f, 0.9f, 0.95f),
            new Vector3(0f, -0.7f, 0f), new Vector3(1.4f, 1.9f, 1f), false, "SEN");

        var npcCharacters = new List<ClickableCharacter>();
        const float radius = 3.9f;
        for (int i = 0; i < Npcs.Length; i++)
        {
            float angle = (90f - i * 60f) * Mathf.Deg2Rad; // tepeden başlayıp saat yönünde
            var pos = new Vector3(Mathf.Cos(angle) * radius * 1.35f, Mathf.Sin(angle) * radius * 0.85f, 0f);
            var npcGo = CreateSquare(world.transform, Npcs[i].objName, square, Npcs[i].color,
                pos, new Vector3(1.1f, 1.5f, 1f), true, Npcs[i].displayName);

            var character = npcGo.AddComponent<ClickableCharacter>();
            character.characterName = Npcs[i].displayName;
            character.specialStat = Npcs[i].stat;
            character.specialValue = 100f;
            character.specialDecreasePerDialogue = 10f;
            if (dialogues.positivesByCharacter.TryGetValue(Npcs[i].objName, out var positives))
                character.positiveDialogues = new List<DialogueData>(positives);
            npcCharacters.Add(character);
        }

        // Çıkış kapısı (sağ kenar)
        var doorGo = CreateSquare(world.transform, "ExitDoor", square, new Color(0.45f, 0.30f, 0.15f),
            new Vector3(8.0f, -0.4f, 0f), new Vector3(1.6f, 2.8f, 1f), true, "KAPI");
        doorGo.AddComponent<ExitDoor>();

        // UI
        var uiResources = GetUIResources();
        var canvas = CreateCanvas();
        CreateEventSystem();
        CreateStatSliders(canvas.transform, uiResources, gm);
        WireDialoguePanel(canvas.transform, uiResources, dialogueManager);
        WireEndScreen(canvas.transform, endController);

        // GameManager bağlantıları
        gm.stats = stats;
        gm.dialogueManager = dialogueManager;
        gm.endController = endController;
        gm.characters = npcCharacters;
        gm.negativeDialogues = new List<DialogueData>(dialogues.negatives);
        gm.doorDialogue = dialogues.door;

        EditorUtility.SetDirty(gm);
    }

    static GameObject CreateSquare(Transform parent, string name, Sprite sprite, Color color,
        Vector3 pos, Vector3 scale, bool clickable, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        go.transform.localScale = scale;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.color = color;

        if (clickable) go.AddComponent<BoxCollider2D>();

        if (!string.IsNullOrEmpty(label))
        {
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            // Parent scale'i etkisiz kılmak için ters ölçek
            labelGo.transform.localScale = new Vector3(1f / scale.x, 1f / scale.y, 1f);
            labelGo.transform.localPosition = new Vector3(0f, 0.5f + 0.35f / scale.y, 0f);

            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = label;
            tm.font = UIFont;
            tm.fontSize = 48;
            tm.characterSize = 0.09f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = Color.white;
            labelGo.GetComponent<MeshRenderer>().sharedMaterial = UIFont.material;
        }
        return go;
    }

    // ---------------------------------------------------------------- UI

    static DefaultControls.Resources GetUIResources()
    {
        return new DefaultControls.Resources
        {
            standard = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd"),
            background = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Background.psd"),
            inputField = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/InputFieldBackground.psd"),
            knob = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd"),
            checkmark = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Checkmark.psd"),
            dropdown = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/DropdownArrow.psd"),
            mask = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UIMask.psd"),
        };
    }

    static Canvas CreateCanvas()
    {
        var go = new GameObject("Canvas");
        var canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    static void CreateEventSystem()
    {
        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    static void CreateStatSliders(Transform canvas, DefaultControls.Resources res, GameManager gm)
    {
        var container = new GameObject("StatSliders", typeof(RectTransform));
        var containerRt = (RectTransform)container.transform;
        containerRt.SetParent(canvas, false);
        containerRt.anchorMin = new Vector2(0f, 0.5f);
        containerRt.anchorMax = new Vector2(0f, 0.5f);
        containerRt.pivot = new Vector2(0f, 0.5f);
        containerRt.anchoredPosition = new Vector2(40f, 60f);
        containerRt.sizeDelta = new Vector2(360f, 640f);

        for (int i = 0; i < SliderDefs.Length; i++)
        {
            var sliderGo = DefaultControls.CreateSlider(res);
            sliderGo.name = "Slider_" + SliderDefs[i].stat;
            var rt = (RectTransform)sliderGo.transform;
            rt.SetParent(containerRt, false);
            rt.anchorMin = new Vector2(0f, 0.5f);
            rt.anchorMax = new Vector2(0f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(60f + i * 120f, 0f);
            rt.sizeDelta = new Vector2(42f, 520f);

            var slider = sliderGo.GetComponent<Slider>();
            slider.SetDirection(Slider.Direction.BottomToTop, true);
            slider.minValue = 0f;
            slider.maxValue = 100f;
            slider.value = 50f;

            // Kilitli bölge overlay'leri (Handle'ın altına, Fill'in üstüne)
            var handleArea = sliderGo.transform.Find("Handle Slide Area");
            var bottomLock = CreateLockZone(rt, "LockBottom");
            var topLock = CreateLockZone(rt, "LockTop");
            if (handleArea != null)
            {
                int handleIndex = handleArea.GetSiblingIndex();
                bottomLock.transform.SetSiblingIndex(handleIndex);
                topLock.transform.SetSiblingIndex(handleIndex + 1);
            }

            // Başlık ve değer etiketi
            var title = CreateUIText(rt, "Title", SliderDefs[i].label, 22, FontStyle.Bold);
            var titleRt = (RectTransform)title.transform;
            titleRt.anchorMin = new Vector2(0.5f, 1f);
            titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 0f);
            titleRt.anchoredPosition = new Vector2(0f, 14f);
            titleRt.sizeDelta = new Vector2(160f, 30f);

            var value = CreateUIText(rt, "Value", "50", 22, FontStyle.Normal);
            var valueRt = (RectTransform)value.transform;
            valueRt.anchorMin = new Vector2(0.5f, 0f);
            valueRt.anchorMax = new Vector2(0.5f, 0f);
            valueRt.pivot = new Vector2(0.5f, 1f);
            valueRt.anchoredPosition = new Vector2(0f, -14f);
            valueRt.sizeDelta = new Vector2(120f, 30f);

            var statSlider = sliderGo.AddComponent<DraggableStatSlider>();
            statSlider.statType = SliderDefs[i].stat;
            statSlider.slider = slider;
            statSlider.valueLabel = value.GetComponent<Text>();
            statSlider.bottomLockZone = (RectTransform)bottomLock.transform;
            statSlider.topLockZone = (RectTransform)topLock.transform;
        }
    }

    static GameObject CreateLockZone(RectTransform parent, string name)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.zero;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.6f);
        img.raycastTarget = false;
        return go;
    }

    static void WireDialoguePanel(Transform canvas, DefaultControls.Resources res, DialogueManager dm)
    {
        // Panel arka planı (alt bant)
        var panel = new GameObject("DialoguePanel", typeof(RectTransform));
        var panelRt = (RectTransform)panel.transform;
        panelRt.SetParent(canvas, false);
        panelRt.anchorMin = new Vector2(0f, 0f);
        panelRt.anchorMax = new Vector2(1f, 0f);
        panelRt.pivot = new Vector2(0.5f, 0f);
        panelRt.anchoredPosition = new Vector2(0f, 20f);
        panelRt.sizeDelta = new Vector2(-460f, 250f);
        var panelImg = panel.AddComponent<Image>();
        panelImg.sprite = res.background;
        panelImg.type = Image.Type.Sliced;
        panelImg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        // Portreler (sağ / sol)
        var portraitLeft = CreatePortrait(panelRt, "PortraitLeft", new Vector2(0f, 0.5f), new Vector2(115f, 30f));
        var portraitRight = CreatePortrait(panelRt, "PortraitRight", new Vector2(1f, 0.5f), new Vector2(-115f, 30f));

        // Diyalog metni
        var textGo = CreateUIText(panelRt, "DialogueText", "", 28, FontStyle.Normal);
        var textRt = (RectTransform)textGo.transform;
        textRt.anchorMin = new Vector2(0f, 0f);
        textRt.anchorMax = new Vector2(1f, 1f);
        textRt.offsetMin = new Vector2(240f, 90f);
        textRt.offsetMax = new Vector2(-240f, -25f);
        var text = textGo.GetComponent<Text>();
        text.alignment = TextAnchor.UpperLeft;

        // Cevap butonları
        var answerRoot = new GameObject("Answers", typeof(RectTransform));
        var answerRt = (RectTransform)answerRoot.transform;
        answerRt.SetParent(panelRt, false);
        answerRt.anchorMin = new Vector2(0.5f, 0f);
        answerRt.anchorMax = new Vector2(0.5f, 0f);
        answerRt.pivot = new Vector2(0.5f, 0f);
        answerRt.anchoredPosition = new Vector2(0f, 15f);
        answerRt.sizeDelta = new Vector2(900f, 64f);

        var buttonA = CreateAnswerButton(answerRt, res, "AnswerA", new Vector2(-230f, 0f));
        var buttonB = CreateAnswerButton(answerRt, res, "AnswerB", new Vector2(230f, 0f));

        dm.panelRoot = panel;
        dm.portraitLeft = portraitLeft;
        dm.portraitRight = portraitRight;
        dm.dialogueText = text;
        dm.answerRoot = answerRoot;
        dm.answerButtonA = buttonA;
        dm.answerButtonB = buttonB;
        dm.answerTextA = buttonA.GetComponentInChildren<Text>();
        dm.answerTextB = buttonB.GetComponentInChildren<Text>();

        panel.SetActive(false);
        answerRoot.SetActive(false);
    }

    static Image CreatePortrait(RectTransform parent, string name, Vector2 anchor, Vector2 offset)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = offset;
        rt.sizeDelta = new Vector2(190f, 190f);
        var img = go.AddComponent<Image>();
        img.preserveAspect = true;
        img.raycastTarget = false;
        go.SetActive(false);
        return img;
    }

    static Button CreateAnswerButton(RectTransform parent, DefaultControls.Resources res,
        string name, Vector2 position)
    {
        var go = DefaultControls.CreateButton(res);
        go.name = name;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(430f, 60f);

        var label = go.GetComponentInChildren<Text>();
        label.font = UIFont;
        label.fontSize = 22;
        label.color = Color.black;
        label.text = "";
        return go.GetComponent<Button>();
    }

    static void WireEndScreen(Transform canvas, GameEndController endController)
    {
        var fadeGo = new GameObject("FadeScreen", typeof(RectTransform));
        var fadeRt = (RectTransform)fadeGo.transform;
        fadeRt.SetParent(canvas, false);
        fadeRt.anchorMin = Vector2.zero;
        fadeRt.anchorMax = Vector2.one;
        fadeRt.offsetMin = Vector2.zero;
        fadeRt.offsetMax = Vector2.zero;
        var fadeImg = fadeGo.AddComponent<Image>();
        fadeImg.color = new Color(0f, 0f, 0f, 0f);
        fadeImg.raycastTarget = true;

        var endTextGo = CreateUIText(fadeRt, "EndText", "", 40, FontStyle.Bold);
        var endRt = (RectTransform)endTextGo.transform;
        endRt.anchorMin = new Vector2(0.5f, 0.5f);
        endRt.anchorMax = new Vector2(0.5f, 0.5f);
        endRt.pivot = new Vector2(0.5f, 0.5f);
        endRt.anchoredPosition = Vector2.zero;
        endRt.sizeDelta = new Vector2(1400f, 400f);
        var endText = endTextGo.GetComponent<Text>();
        endText.alignment = TextAnchor.MiddleCenter;

        endController.fadeImage = fadeImg;
        endController.endTextLabel = endText;

        fadeGo.SetActive(false);
        endTextGo.SetActive(false);
    }

    static GameObject CreateUIText(RectTransform parent, string name, string content,
        int fontSize, FontStyle style)
    {
        var go = new GameObject(name, typeof(RectTransform));
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        var text = go.AddComponent<Text>();
        text.font = UIFont;
        text.text = content;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;
        return go;
    }
}
