using System;
using System.Collections.Generic;
using UnityEngine;

// Tek bir diyalog satırı: konuşan foto, sağ/sol toggle, metin.
[Serializable]
public class DialogueLine
{
    public Sprite portrait;
    [Tooltip("Açık: portre solda gösterilir. Kapalı: sağda.")]
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
