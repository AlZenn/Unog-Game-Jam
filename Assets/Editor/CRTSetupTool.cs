using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using BrewedInk.CRT;

// ============================================================
// CRTSetupTool — URP (Universal Render Pipeline) uyumlu versiyon
//
// Bu tool:
//   1. Sahnede 'Main Camera' nesnesine CRTCameraBehaviour ekler ve seçilen preset'i bağlar.
//   2. Renderer2D.asset'e URPCRTRendererFeature ekler ve URP uyumlu materyali bağlar.
//   3. CRTCameraBehaviour üzerindeki değerler değiştirildikçe URP feature dinamik olarak bunları ekrana yansıtır.
//
// Kullanım: Tools > UnoG > CRT (URP) Efektini Kur
// ============================================================
public static class CRTSetupTool
{
    // ---------------------------------------------------------------- yollar
    const string RendererAssetPath    = "Assets/Settings/Renderer2D.asset";
    const string URPShaderPath        = "Assets/CRT-Free/Materials/CRTUnlitURP.shader";
    const string URPMaterialSavePath  = "Assets/CRT-Free/Materials/CRTMaterialURP.mat";
    const string OriginalSettingsPath = "Assets/CRT-Free/CRTRenderSettings.asset";

    static readonly (string label, string path)[] Presets =
    {
        ("Subtle (hafif)",             "Assets/CRT-Free/CRTs/Subtle.asset"),
        ("Gloss (parlak)",             "Assets/CRT-Free/CRTs/Gloss.asset"),
        ("Grey (gri)",                 "Assets/CRT-Free/CRTs/Grey.asset"),
        ("Moon (ay)",                  "Assets/CRT-Free/CRTs/Moon.asset"),
        ("RetroBlue (mavi retro)",     "Assets/CRT-Free/CRTs/RetroBlue.asset"),
        ("RetroRed (kırmızı retro)",   "Assets/CRT-Free/CRTs/RetroRed.asset"),
        ("RetroRed 1 (koyu kırmızı)", "Assets/CRT-Free/CRTs/RetroRed 1.asset"),
    };

    // ---------------------------------------------------------------- menü

    [MenuItem("Tools/UnoG/CRT (URP) Efektini Kur", priority = 20)]
    public static void SetupCRT()
    {
        int presetIndex = ShowPresetDialog();
        if (presetIndex < 0) return;

        // 1. Renderer2D asset'ini bul
        var renderer2D = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererAssetPath);
        if (renderer2D == null)
        {
            EditorUtility.DisplayDialog("Hata",
                $"Renderer2D.asset bulunamadı:\n{RendererAssetPath}\n\n" +
                "Lütfen 'Assets/Settings/Renderer2D.asset' yolunu kontrol edin.",
                "Tamam");
            return;
        }

        // 2. URP Shader'ını yükle
        var crtShader = AssetDatabase.LoadAssetAtPath<Shader>(URPShaderPath);
        if (crtShader == null)
        {
            EditorUtility.DisplayDialog("Hata",
                $"CRTUnlitURP.shader bulunamadı:\n{URPShaderPath}\n\n" +
                "Assets/CRT-Free/Materials/ klasörünü kontrol edin.",
                "Tamam");
            return;
        }

        // 3. Main Camera'yı bul
        Camera cam = FindMainCamera();
        if (cam == null) return;

        // 4. URP Material oluştur / güncelle
        Material crtMat = EnsureURPMaterial(crtShader);
        if (crtMat == null) return;

        // 5. Seçilen preset'i kameraya bağla
        SetupCameraComponent(cam.gameObject, presetIndex);

        // 6. URPCRTRendererFeature'ı Renderer2D'ye ekle
        bool added = EnsureRendererFeature(renderer2D, crtMat);

        EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
        EditorUtility.SetDirty(cam.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string resultMsg = added
            ? "URPCRTRendererFeature Renderer2D'ye EKLENDİ ve kameraya CRTCameraBehaviour yerleştirildi."
            : "Renderer Feature zaten mevcuttu, kameradaki CRT ayarları güncellendi.";

        Debug.Log($"[CRTSetupTool] ✓ {resultMsg} | Preset: {Presets[presetIndex].label}");

        EditorUtility.DisplayDialog("CRT Kurulumu Tamamlandı",
            $"✅ {resultMsg}\n\n" +
            $"Seçilen preset: {Presets[presetIndex].label}\n\n" +
            "Artık kameradaki 'CRTCameraBehaviour' veya ona bağlı 'startConfig' (preset) dosyasından\n" +
            "değerleri değiştirdiğinizde anında ekran güncellenecektir.",
            "Tamam");
    }

    [MenuItem("Tools/UnoG/CRT (URP) Efektini Kaldır", priority = 21)]
    public static void RemoveCRT()
    {
        // 1. Renderer feature kaldır
        var renderer2D = AssetDatabase.LoadAssetAtPath<Renderer2DData>(RendererAssetPath);
        if (renderer2D != null)
        {
            RemoveRendererFeature(renderer2D);
            AssetDatabase.SaveAssets();
        }

        // 2. Kamera bileşenini kaldır
        Camera cam = FindMainCameraSilent();
        if (cam != null)
        {
            var existing = cam.GetComponent<CRTCameraBehaviour>();
            if (existing != null)
            {
                Undo.DestroyObjectImmediate(existing);
                EditorSceneManager.MarkSceneDirty(cam.gameObject.scene);
            }
        }

        Debug.Log("[CRTSetupTool] URPCRTRendererFeature ve CRTCameraBehaviour kaldırıldı.");
    }

    [MenuItem("Tools/UnoG/CRT (URP) Efektini Kur", validate = true)]
    static bool ValidateSetup() => FindMainCameraSilent() != null;

    [MenuItem("Tools/UnoG/CRT (URP) Efektini Kaldır", validate = true)]
    static bool ValidateRemove() => FindMainCameraSilent() != null;

    // ---------------------------------------------------------------- camera component

    static void SetupCameraComponent(GameObject cameraGO, int presetIndex)
    {
        var crt = cameraGO.GetComponent<CRTCameraBehaviour>();
        if (crt == null)
        {
            crt = Undo.AddComponent<CRTCameraBehaviour>(cameraGO);
        }

        // Preset bağla
        string presetPath = Presets[presetIndex].path;
        var preset = AssetDatabase.LoadAssetAtPath<CRTDataObject>(presetPath);
        crt.startConfig = preset;

        // Orijinal render settings bağla (Gerekli değil ama hata vermesin diye)
        var settings = AssetDatabase.LoadAssetAtPath<CRTRenderSettingsObject>(OriginalSettingsPath);
        crt.crtRenderSettings = settings;

        // Zorla ilk kopyalamayı tetikle
        crt.ResetMaterial();

        EditorUtility.SetDirty(crt);
    }

    // ---------------------------------------------------------------- material

    static Material EnsureURPMaterial(Shader shader)
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(URPMaterialSavePath);
        if (existing != null)
        {
            existing.shader = shader;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        var mat = new Material(shader) { name = "CRTMaterialURP" };
        AssetDatabase.CreateAsset(mat, URPMaterialSavePath);
        return mat;
    }

    // ---------------------------------------------------------------- renderer feature

    static bool EnsureRendererFeature(Renderer2DData renderer2D, Material crtMat)
    {
        var existing = renderer2D.rendererFeatures
            .OfType<URPCRTRendererFeature>()
            .FirstOrDefault();

        if (existing != null)
        {
            existing.crtMaterial = crtMat;
            EditorUtility.SetDirty(renderer2D);
            return false;
        }

        var feature = ScriptableObject.CreateInstance<URPCRTRendererFeature>();
        feature.name       = "CRT Effect";
        feature.crtMaterial = crtMat;

        var so = new SerializedObject(renderer2D);
        var featuresProp = so.FindProperty("m_RendererFeatures");

        AssetDatabase.AddObjectToAsset(feature, RendererAssetPath);
        featuresProp.InsertArrayElementAtIndex(featuresProp.arraySize);
        featuresProp.GetArrayElementAtIndex(featuresProp.arraySize - 1).objectReferenceValue = feature;

        var featureMapProp = so.FindProperty("m_RendererFeatureMap");
        if (featureMapProp != null)
        {
            featureMapProp.InsertArrayElementAtIndex(featureMapProp.arraySize);
            featureMapProp.GetArrayElementAtIndex(featureMapProp.arraySize - 1).longValue =
                (long)feature.GetInstanceID();
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(renderer2D);
        return true;
    }

    static void RemoveRendererFeature(Renderer2DData renderer2D)
    {
        var so = new SerializedObject(renderer2D);
        var featuresProp = so.FindProperty("m_RendererFeatures");

        for (int i = featuresProp.arraySize - 1; i >= 0; i--)
        {
            var elem = featuresProp.GetArrayElementAtIndex(i).objectReferenceValue;
            if (elem is URPCRTRendererFeature)
            {
                featuresProp.DeleteArrayElementAtIndex(i);
                AssetDatabase.RemoveObjectFromAsset(elem);
                Object.DestroyImmediate(elem, true);
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(renderer2D);
    }

    // ---------------------------------------------------------------- helpers

    static Camera FindMainCamera()
    {
        Camera cam = FindMainCameraSilent();
        if (cam == null)
        {
            EditorUtility.DisplayDialog(
                "Kamera Bulunamadı",
                "Aktif sahnede 'MainCamera' etiketli bir kamera bulunamadı.",
                "Tamam");
        }
        return cam;
    }

    static Camera FindMainCameraSilent()
    {
        var tagged = GameObject.FindGameObjectWithTag("MainCamera");
        if (tagged != null)
        {
            var cam = tagged.GetComponent<Camera>();
            if (cam != null) return cam;
        }

        var byName = GameObject.Find("Main Camera");
        if (byName != null)
        {
            var cam = byName.GetComponent<Camera>();
            if (cam != null) return cam;
        }

        return null;
    }

    static int ShowPresetDialog()
    {
        int choice = EditorUtility.DisplayDialogComplex(
            "CRT Preset Seç",
            "Kameraya uygulanacak CRT preset'ini seçin:\n\n" +
            "• Subtle   → çok hafif efekt (önerilen başlangıç)\n" +
            "• Gloss    → parlak, cam yansımalı\n" +
            "• Grey     → gri tonlama\n" +
            "• Moon     → soğuk mavi ton\n" +
            "• RetroBlue → mavi retro\n" +
            "• RetroRed  → kırmızı retro",
            "Subtle (önerilen)",
            "İptal",
            "Diğer preset..."
        );

        if (choice == 1) return -1;
        if (choice == 0) return 0; // Subtle

        return ShowFullPresetMenu();
    }

    static int ShowFullPresetMenu()
    {
        int selected = EditorUtility.DisplayDialogComplex(
            "Preset Grubu",
            "Hangi grubu tercih edersiniz?",
            "Gloss / Grey / Moon",
            "RetroBlue / RetroRed",
            "İptal"
        );

        if (selected == 2) return -1;

        if (selected == 0)
        {
            int sub = EditorUtility.DisplayDialogComplex("Preset", "Gloss, Grey veya Moon?",
                "Gloss", "Grey", "Moon");
            return sub == 2 ? 3 : (sub == 1 ? 2 : 1);
        }
        else
        {
            int sub = EditorUtility.DisplayDialogComplex("Preset", "RetroBlue, RetroRed veya RetroRed 1?",
                "RetroBlue", "RetroRed", "RetroRed 1");
            return sub == 2 ? 6 : (sub == 1 ? 5 : 4);
        }
    }
}
