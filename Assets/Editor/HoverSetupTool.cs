using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// Hover efektlerini kurar:
// 1) OutlineFxFeature'ı Renderer2D.asset'e ekler (yoksa)
// 2) NPC + kapı objelerine OutlineFx + HoverHighlight
// 3) Slider ve butonlara UIHoverFeel
public static class HoverSetupTool
{
    const string RendererPath = "Assets/Settings/Renderer2D.asset";

    [MenuItem("Tools/UnoG/Hover Efektlerini Kur", priority = 17)]
    public static void Setup()
    {
        EnsureOutlineFeature();
        int worldCount = SetupWorldObjects();
        int uiCount = SetupUI();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"UnoG: Hover efektleri kuruldu — {worldCount} dünya objesi, {uiCount} UI objesi.");
    }

    // ---------------------------------------------------------------- renderer feature

    static void EnsureOutlineFeature()
    {
        var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RendererPath);
        if (rendererData == null)
        {
            Debug.LogWarning("UnoG: " + RendererPath + " bulunamadı — OutlineFxFeature'ı " +
                "Renderer2D Inspector'ından elle ekle (Add Renderer Feature > Outline Fx Feature).");
            return;
        }

        bool exists = AssetDatabase.LoadAllAssetsAtPath(RendererPath)
            .OfType<OutlineFx.OutlineFxFeature>().Any();
        if (exists) return;

        var feature = ScriptableObject.CreateInstance<OutlineFx.OutlineFxFeature>();
        feature.name = "OutlineFx";
        feature._thickness = 0.004f;
        AssetDatabase.AddObjectToAsset(feature, rendererData);

        // URP'nin ScriptableRendererDataEditor.AddComponent pattern'i:
        // m_RendererFeatures listesine referans + m_RendererFeatureMap'e local id eklenir.
        var so = new SerializedObject(rendererData);
        var features = so.FindProperty("m_RendererFeatures");
        var map = so.FindProperty("m_RendererFeatureMap");
        if (features == null || map == null)
        {
            Object.DestroyImmediate(feature, true);
            Debug.LogWarning("UnoG: Renderer feature listesi bulunamadı — OutlineFxFeature'ı " +
                "Renderer2D Inspector'ından elle ekle.");
            return;
        }

        features.arraySize++;
        features.GetArrayElementAtIndex(features.arraySize - 1).objectReferenceValue = feature;

        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out _, out long localId);
        map.arraySize++;
        map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;

        so.ApplyModifiedProperties();
        EditorUtility.SetDirty(rendererData);
        Debug.Log("UnoG: OutlineFxFeature Renderer2D'ye eklendi (thickness 0.004).");
    }

    // ---------------------------------------------------------------- dünya objeleri

    static int SetupWorldObjects()
    {
        int count = 0;
        foreach (var character in Object.FindObjectsByType<ClickableCharacter>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ApplyWorldHover(character.gameObject)) count++;

        foreach (var door in Object.FindObjectsByType<ExitDoor>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ApplyWorldHover(door.gameObject)) count++;

        return count;
    }

    static bool ApplyWorldHover(GameObject go)
    {
        var outline = go.GetComponent<OutlineFx.OutlineFx>();
        if (outline == null) outline = go.AddComponent<OutlineFx.OutlineFx>();
        outline._color = Color.white;
        outline.enabled = false;

        var hover = go.GetComponent<HoverHighlight>();
        if (hover == null) hover = go.AddComponent<HoverHighlight>();
        hover.outline = outline;

        EditorUtility.SetDirty(go);
        return true;
    }

    // ---------------------------------------------------------------- UI

    static int SetupUI()
    {
        int count = 0;

        // Stat sliderları: yalnızca scale (outline slider görselini bozar)
        foreach (var slider in Object.FindObjectsByType<DraggableStatSlider>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
            if (ApplyUIFeel(slider.gameObject, useOutline: false)) count++;

        // Diyalog cevap butonları
        var dialogueManager = Object.FindFirstObjectByType<DialogueManager>(FindObjectsInactive.Include);
        if (dialogueManager != null)
        {
            if (dialogueManager.answerButtonA != null &&
                ApplyUIFeel(dialogueManager.answerButtonA.gameObject, useOutline: true)) count++;
            if (dialogueManager.answerButtonB != null &&
                ApplyUIFeel(dialogueManager.answerButtonB.gameObject, useOutline: true)) count++;
        }

        // Ana menü butonları
        var menu = Object.FindFirstObjectByType<MainMenuController>(FindObjectsInactive.Include);
        if (menu != null)
        {
            if (menu.playButton != null &&
                ApplyUIFeel(menu.playButton.gameObject, useOutline: true)) count++;
            if (menu.quitButton != null &&
                ApplyUIFeel(menu.quitButton.gameObject, useOutline: true)) count++;
        }

        return count;
    }

    static bool ApplyUIFeel(GameObject go, bool useOutline)
    {
        var feel = go.GetComponent<UIHoverFeel>();
        if (feel == null) feel = go.AddComponent<UIHoverFeel>();
        feel.useOutline = useOutline;
        EditorUtility.SetDirty(go);
        return true;
    }
}
