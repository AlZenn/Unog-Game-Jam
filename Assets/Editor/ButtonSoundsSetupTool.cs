using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Buton seslerini kurar: Managers'a UISoundManager ekler, sahnedeki TÜM butonlara
// UIButtonSounds ekler. Projede adı "hover"/"click" içeren AudioClip bulursa
// otomatik atar; bulamazsa alanlar boş kalır, kullanıcı sürükleyip bırakır.
public static class ButtonSoundsSetupTool
{
    [MenuItem("Tools/UnoG/Buton Seslerini Kur", priority = 19)]
    public static void Setup()
    {
        // UISoundManager (Managers objesine)
        var gm = Object.FindFirstObjectByType<GameManager>();
        UISoundManager soundManager;
        if (gm != null)
        {
            soundManager = gm.GetComponent<UISoundManager>();
            if (soundManager == null) soundManager = gm.gameObject.AddComponent<UISoundManager>();
        }
        else
        {
            soundManager = Object.FindFirstObjectByType<UISoundManager>();
            if (soundManager == null)
            {
                var go = new GameObject("UISoundManager");
                soundManager = go.AddComponent<UISoundManager>();
                Debug.LogWarning("UnoG: GameManager bulunamadı, UISoundManager ayrı objede oluşturuldu.");
            }
        }

        // Ses dosyalarını isme göre otomatik bul (yalnızca alan boşsa)
        if (soundManager.hoverClip == null)
            soundManager.hoverClip = FindClip("hover");
        if (soundManager.clickClip == null)
            soundManager.clickClip = FindClip("click", "tik", "press");
        EditorUtility.SetDirty(soundManager.gameObject);

        // Sahnedeki tüm butonlara (inaktifler dahil: cevap butonları vb.)
        int count = 0;
        foreach (var button in Object.FindObjectsByType<Button>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (button.GetComponent<UIButtonSounds>() != null) continue;
            button.gameObject.AddComponent<UIButtonSounds>();
            EditorUtility.SetDirty(button.gameObject);
            count++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

        string hoverInfo = soundManager.hoverClip != null ? soundManager.hoverClip.name : "BOŞ — elle ata";
        string clickInfo = soundManager.clickClip != null ? soundManager.clickClip.name : "BOŞ — elle ata";
        Debug.Log($"UnoG: {count} butona ses eklendi. Hover: {hoverInfo} | Click: {clickInfo}\n" +
            "Sesleri değiştirmek için: Managers > UISoundManager component'i.");
    }

    // Adında verilen anahtar kelimelerden biri geçen ilk AudioClip'i döndürür.
    static AudioClip FindClip(params string[] keywords)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue;
            string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            foreach (var keyword in keywords)
                if (name.Contains(keyword))
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }
}
