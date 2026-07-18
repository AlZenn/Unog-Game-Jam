using System;
using System.Collections.Generic;
using UnityEngine;

// Tüm portre sprite'larının merkezi: ana karakter + her NPC için bir portre.
// DialogueData'lar sprite referansı tutmaz; DialogueManager buradan çözer.
public class PortraitManager : MonoBehaviour
{
    public static PortraitManager Instance { get; private set; }

    [Serializable]
    public class Entry
    {
        public SpeakerCharacter character;
        public Sprite portrait;
    }

    [Tooltip("Ana karakterin portresi (diyalogda sağ tarafta gösterilir)")]
    public Sprite mainCharacterSprite;

    [Tooltip("NPC portreleri (diyalogda sol tarafta gösterilir)")]
    public List<Entry> portraits = new List<Entry>();

    void Awake()
    {
        Instance = this;
    }

    public Sprite GetPortrait(SpeakerCharacter character)
    {
        foreach (var entry in portraits)
            if (entry.character == character) return entry.portrait;
        return null;
    }
}
