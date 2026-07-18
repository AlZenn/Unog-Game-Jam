using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Ana menüyü KENDİ canvas'ıyla (Screen Space - Camera) sıfırdan kurar.
// Oyun UI canvas'ına dokunmaz. Firefly partikülleri sorting order ile
// menü panelinin önünde, MenuFade karartması ise en önde render edilir.
//
// Katmanlar (Default sorting layer):
//   dünya sprite'ları 0 < MenuCanvas 50 < firefly 60 < MenuFade 100
public static class MainMenuSetupTool
{
    const int MenuCanvasOrder = 50;
    const int FireflyOrder = 60;
    const int MenuFadeOrder = 100;

    static readonly string[] FireflyPrefabPaths =
    {
        "Assets/COMICOMI/VFX_FirelyFlare/Art/Prefabs/Fx_FirelyFire_01.prefab",
        "Assets/COMICOMI/VFX_FirelyFlare/Art/Prefabs/Fx_FirelyFire_02.prefab",
        "Assets/COMICOMI/VFX_FirelyFlare/Art/Prefabs/Fx_FirelyFire_03.prefab",
    };

    [MenuItem("Tools/UnoG/Ana Menüyü Ekle", priority = 15)]
    public static void AddMainMenu()
    {
        // Eski kurulumları temizle (idempotent).
        // Controller hangi objedeyse (eski "MainMenu" root'u ya da yeni "MenuCanvas") onu sil.
        var oldController = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (oldController != null) Object.DestroyImmediate(oldController.gameObject);
        var oldCanvas = GameObject.Find("MenuCanvas");
        if (oldCanvas != null) Object.DestroyImmediate(oldCanvas);

        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("UnoG: Sahnede Main Camera yok. Önce 'Sahneyi Kur' çalıştır.");
            return;
        }

        var res = SceneSetupTool.GetUIResources();

        // Menüye özel canvas: Screen Space - Camera → firefly'lar sorting ile öne geçebilir.
        var canvasGo = new GameObject("MenuCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = cam;
        canvas.planeDistance = 10f;
        canvas.sortingOrder = MenuCanvasOrder;
        var scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        var controller = canvasGo.AddComponent<MainMenuController>();

        // Fullscreen koyu panel
        var panel = new GameObject("MenuPanel", typeof(RectTransform));
        var panelRt = (RectTransform)panel.transform;
        panelRt.SetParent(canvasGo.transform, false);
        Stretch(panelRt);
        var panelImg = panel.AddComponent<Image>();
        panelImg.color = new Color(0.06f, 0.06f, 0.10f, 1f);
        panelImg.raycastTarget = true; // arkadaki oyuna tıklamayı engeller

        // Başlık
        var title = SceneSetupTool.CreateUIText(panelRt, "Title", "UNOG", 96, FontStyle.Bold);
        var titleRt = (RectTransform)title.transform;
        titleRt.anchorMin = new Vector2(0.5f, 1f);
        titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -140f);
        titleRt.sizeDelta = new Vector2(900f, 120f);

        // Butonlar
        var playButton = CreateMenuButton(panelRt, res, "PlayButton", "OYNA", new Vector2(0f, 20f));
        var quitButton = CreateMenuButton(panelRt, res, "QuitButton", "ÇIKIŞ", new Vector2(0f, -90f));

        // İmza metni (en altta)
        var credit = SceneSetupTool.CreateUIText(panelRt, "CreditText", "alzenn", 24, FontStyle.Italic);
        var creditRt = (RectTransform)credit.transform;
        creditRt.anchorMin = new Vector2(0.5f, 0f);
        creditRt.anchorMax = new Vector2(0.5f, 0f);
        creditRt.pivot = new Vector2(0.5f, 0f);
        creditRt.anchoredPosition = new Vector2(0f, 30f);
        creditRt.sizeDelta = new Vector2(600f, 40f);
        credit.GetComponent<Text>().color = new Color(1f, 1f, 1f, 0.6f);

        // Menü fade'i: nested canvas + override sorting → firefly'lar dahil her şeyi örter.
        var fadeGo = new GameObject("MenuFade", typeof(RectTransform));
        var fadeRt = (RectTransform)fadeGo.transform;
        fadeRt.SetParent(canvasGo.transform, false);
        Stretch(fadeRt);
        var fadeCanvas = fadeGo.AddComponent<Canvas>();
        fadeCanvas.overrideSorting = true;
        fadeCanvas.sortingOrder = MenuFadeOrder;
        fadeGo.AddComponent<GraphicRaycaster>();
        var fadeImg = fadeGo.AddComponent<Image>();
        fadeImg.color = new Color(0f, 0f, 0f, 0f);
        fadeImg.raycastTarget = true;
        fadeGo.SetActive(false);

        // Referanslar
        controller.menuRoot = panel;
        controller.menuFadeImage = fadeImg;
        controller.playButton = playButton;
        controller.quitButton = quitButton;

        // Menü açıkken gizlenecek oyun HUD'ı (Overlay canvas menünün üstüne çizildiği için).
        var statSliders = GameObject.Find("StatSliders");
        if (statSliders != null)
            controller.hideWhileMenuOpen = new[] { statSliders };
        else
            Debug.LogWarning("UnoG: 'StatSliders' bulunamadı — menü açıkken gizlenecek UI'ları " +
                "MainMenuController.hideWhileMenuOpen alanına elle bağla.");

        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(canvasGo.scene);
        Selection.activeGameObject = canvasGo;
        Debug.Log("UnoG: Ana menü kendi canvas'ıyla (Screen Space - Camera) kuruldu. " +
            "Firefly için: Tools > UnoG > Ana Menüye Firefly Ekle");
    }

    [MenuItem("Tools/UnoG/Ana Menüye Firefly Ekle", priority = 16)]
    public static void AddFireflies()
    {
        var controller = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (controller == null)
        {
            Debug.LogError("UnoG: Önce 'Ana Menüyü Ekle' çalıştır.");
            return;
        }

        var old = GameObject.Find("MenuFireflies");
        if (old != null) Object.DestroyImmediate(old);

        var root = new GameObject("MenuFireflies");
        // Ekranın sol/orta/sağ bölgeleri (dünya uzayı, kamera 1080p'de ~19.2x10.8 birim görür)
        Vector3[] positions =
        {
            new Vector3(-6f, 1.5f, 0f),
            new Vector3(0f, -2f, 0f),
            new Vector3(6f, 2f, 0f),
        };

        int placed = 0;
        for (int i = 0; i < FireflyPrefabPaths.Length; i++)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(FireflyPrefabPaths[i]);
            if (prefab == null)
            {
                Debug.LogWarning("UnoG: Firefly prefab bulunamadı: " + FireflyPrefabPaths[i]);
                continue;
            }
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            instance.transform.SetParent(root.transform, false);
            instance.transform.position = positions[i % positions.Length];

            // Menü panelinin (canvas order 50) önünde render edilsinler.
            foreach (var renderer in instance.GetComponentsInChildren<ParticleSystemRenderer>(true))
                renderer.sortingOrder = FireflyOrder;
            placed++;
        }

        if (placed == 0)
        {
            Object.DestroyImmediate(root);
            Debug.LogError("UnoG: Hiçbir firefly prefab'ı yüklenemedi, kurulum iptal.");
            return;
        }

        controller.firefliesRoot = root;
        EditorUtility.SetDirty(controller);
        EditorSceneManager.MarkSceneDirty(root.scene);
        Debug.Log("UnoG: " + placed + " firefly efekti menünün önüne yerleştirildi. " +
            "Pembe görünürlerse: Edit > Rendering > Materials > Convert Selected Built-in Materials to URP");
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    static Button CreateMenuButton(RectTransform parent, DefaultControls.Resources res,
        string name, string label, Vector2 position)
    {
        var go = DefaultControls.CreateButton(res);
        go.name = name;
        var rt = (RectTransform)go.transform;
        rt.SetParent(parent, false);
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = position;
        rt.sizeDelta = new Vector2(320f, 80f);

        var text = go.GetComponentInChildren<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 32;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.black;
        text.text = label;
        return go.GetComponent<Button>();
    }
}
