using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

// MEVCUT sahne için portre sistemini kurar (sahneyi yeniden kurmadan):
// Managers'a PortraitManager ekler ve NPC'lerin characterId'lerini
// obje/karakter adına göre otomatik eşler. Portre sprite'larını
// Managers > PortraitManager'a sen atarsın.
public static class PortraitSetupTool
{
    [MenuItem("Tools/UnoG/Portre Sistemini Kur", priority = 21)]
    public static void Setup()
    {
        // PortraitManager
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm == null)
        {
            Debug.LogError("UnoG: GameManager bulunamadı — önce sahneyi kur.");
            return;
        }
        var pm = gm.GetComponent<PortraitManager>();
        if (pm == null) pm = gm.gameObject.AddComponent<PortraitManager>();

        // Her karakter için boş slot hazırla (varsa dokunma)
        foreach (SpeakerCharacter id in System.Enum.GetValues(typeof(SpeakerCharacter)))
        {
            if (id == SpeakerCharacter.None) continue;
            bool exists = pm.portraits.Exists(e => e.character == id);
            if (!exists) pm.portraits.Add(new PortraitManager.Entry { character = id });
        }
        EditorUtility.SetDirty(gm.gameObject);

        // NPC characterId eşlemesi (obje adı veya characterName üzerinden)
        int matched = 0;
        foreach (var character in Object.FindObjectsByType<ClickableCharacter>(
            FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            var id = GuessId(character);
            if (id == SpeakerCharacter.None)
            {
                Debug.LogWarning("UnoG: characterId eşlenemedi: " + character.name +
                    " — Inspector'dan elle seç.");
                continue;
            }
            character.characterId = id;
            EditorUtility.SetDirty(character.gameObject);
            matched++;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log($"UnoG: Portre sistemi kuruldu — {matched} NPC eşlendi. " +
            "Portre sprite'larını Managers > PortraitManager'a ata " +
            "(Main Character Sprite + her NPC için bir slot).");
    }

    static SpeakerCharacter GuessId(ClickableCharacter character)
    {
        string name = (character.name + " " + character.characterName).ToLowerInvariant();
        if (name.Contains("kaos")) return SpeakerCharacter.Kaos;
        if (name.Contains("merhamet")) return SpeakerCharacter.Merhamet;
        if (name.Contains("utanga")) return SpeakerCharacter.Utangac;   // utangaç/utangac
        if (name.Contains("heyecan")) return SpeakerCharacter.Heyecan;
        if (name.Contains("haz")) return SpeakerCharacter.Haz;
        if (name.Contains("gözlü") || name.Contains("gozlu") || name.Contains("acg"))
            return SpeakerCharacter.Acgozlu;
        return SpeakerCharacter.None;
    }
}
