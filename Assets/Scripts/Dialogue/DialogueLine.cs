using System;
using System.Collections.Generic;
using UnityEngine;

// Diyalogların ait olduğu karakter. Portreler PortraitManager'dan çözülür.
public enum SpeakerCharacter
{
    None,      // konuşmacısız (örn. kapı) — portre gösterilmez
    Kaos,
    Merhamet,
    Utangac,
    Heyecan,
    Haz,
    Acgozlu
}

// Tek bir diyalog satırı. Portre referansı YOKTUR:
// sol = diyaloğun karakterinin portresi, sağ = ana karakterin portresi
// (sprite'lar PortraitManager'dan gelir).
[Serializable]
public class DialogueLine
{
    [Tooltip("Açık: karakterin portresi solda. Kapalı: ana karakterin portresi sağda.")]
    public bool isLeftSide = true;
    [TextArea(2, 5)] public string text;
}

// Diyaloğun açılması için gereken slider alt/üst sınırı (örn. Öfke min 50, max 100).
[Serializable]
public class StatRequirement
{
    public StatType stat;
    [Range(0f, 100f)] public float min = 0f;
    [Range(0f, 100f)] public float max = 100f;
}

// Bir cevabın ana slider'a etkisi (+10, -5 gibi).
[Serializable]
public class StatEffect
{
    public StatType stat;
    public float amount;
}

// Diyalog sonundaki cevap seçeneği.
[Serializable]
public class DialogueAnswer
{
    public string answerText;
    public List<StatEffect> effects = new List<StatEffect>();
}
