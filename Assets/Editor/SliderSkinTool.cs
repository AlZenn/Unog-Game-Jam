using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Stat slider'larına hazırlanan kılıç görsellerini uygular:
//   diş.png     → Background (gümüş kılıç çerçevesi)
//   handler.png → Handle (kabza)
//   kirmizi.png → Öfke fill, mavş.png → Dürüstlük fill, sari.png → Çıkar fill
// Ayrıca fill'in arka planın ÜSTÜNDE render edilmesi için sibling sırasını
// zorlar: Background(0) → Fill Area(1) → kilit bölgeleri → Handle (en üst).
public static class SliderSkinTool
{
    const string Folder = "Assets/characters/slider/";
    const string BackgroundPath = Folder + "diş.png";
    const string HandlePath = Folder + "handler.png";

    [MenuItem("Tools/UnoG/Slider Görsellerini Uygula", priority = 23)]
    public static void Apply()
    {
        var backgroundSprite = LoadSprite(BackgroundPath);
        var handleSprite = LoadSprite(HandlePath);
        var fillOfke = LoadSprite(Folder + "kirmizi.png");
        var fillDurustluk = LoadSprite(Folder + "mavş.png");
        var fillCikar = LoadSprite(Folder + "sari.png");

        if (backgroundSprite == null || handleSprite == null)
        {
            Debug.LogError("UnoG: Slider sprite'ları yüklenemedi (" + Folder + " kontrol et).");
            return;
        }

        int count = 0;
        foreach (var statSlider in Object.FindObjectsByType<DraggableStatSlider>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var slider = statSlider.slider != null
                ? statSlider.slider
                : statSlider.GetComponent<Slider>();
            if (slider == null) continue;

            Sprite fillSprite =
                statSlider.statType == StatType.Ofke ? fillOfke :
                statSlider.statType == StatType.Durustluk ? fillDurustluk : fillCikar;

            ApplyToSlider(slider, backgroundSprite, fillSprite, handleSprite);
            EditorUtility.SetDirty(slider.gameObject);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("UnoG: " + count + " slider'a kılıç görselleri uygulandı " +
            "(öfke kırmızı, dürüstlük mavi, çıkar sarı).");
    }

    static void ApplyToSlider(Slider slider, Sprite background, Sprite fill, Sprite handle)
    {
        var root = slider.transform;

        // Background
        var backgroundTr = root.Find("Background");
        if (backgroundTr != null)
        {
            var img = backgroundTr.GetComponent<Image>();
            img.sprite = background;
            img.type = Image.Type.Simple;
            img.color = Color.white;
            backgroundTr.SetSiblingIndex(0); // en arkada
        }

        // Fill — arka planın üstünde
        var fillArea = root.Find("Fill Area");
        if (fillArea != null)
        {
            fillArea.SetSiblingIndex(1);
            var fillTr = fillArea.Find("Fill");
            if (fillTr != null)
            {
                var img = fillTr.GetComponent<Image>();
                img.sprite = fill;
                img.type = Image.Type.Simple;
                img.color = Color.white; // sprite'lar zaten renkli
            }
        }

        // Kilit bölgeleri fill'in üstünde kalsın
        int nextIndex = 2;
        var lockBottom = root.Find("LockBottom");
        if (lockBottom != null) lockBottom.SetSiblingIndex(nextIndex++);
        var lockTop = root.Find("LockTop");
        if (lockTop != null) lockTop.SetSiblingIndex(nextIndex++);

        // Handle — en üstte, kabza sprite'ı geniş olduğu için boyutu ayarlanır
        var handleArea = root.Find("Handle Slide Area");
        if (handleArea != null)
        {
            handleArea.SetAsLastSibling();
            var handleTr = handleArea.Find("Handle");
            if (handleTr != null)
            {
                var img = handleTr.GetComponent<Image>();
                img.sprite = handle;
                img.type = Image.Type.Simple;
                img.color = Color.white;
                img.preserveAspect = true;

                var rt = (RectTransform)handleTr;
                rt.sizeDelta = new Vector2(96f, 36f); // track'ten taşan kabza
            }
        }
    }

    static Sprite LoadSprite(string path)
    {
        // Görselin Sprite olarak import edildiğinden emin ol
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.SaveAndReimport();
        }
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null) Debug.LogWarning("UnoG: Sprite bulunamadı: " + path);
        return sprite;
    }
}
