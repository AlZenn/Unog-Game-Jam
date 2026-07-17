using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "YeniDiyalog", menuName = "UnoG/Diyalog", order = 0)]
public class DialogueData : ScriptableObject
{
    [Header("Satırlar (sırayla oynar)")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    [Header("Olumsuz havuz diyaloğu mu? (koşulsuz, cevapsız, stat etkisiz)")]
    public bool isNegative;

    [Header("Açılma koşulları (tümü sağlanmalı — olumlu diyaloglar için)")]
    public List<StatRequirement> requirements = new List<StatRequirement>();

    [Header("Cevap seçenekleri (yalnızca olumlu diyaloglarda)")]
    public DialogueAnswer answerA = new DialogueAnswer();
    public DialogueAnswer answerB = new DialogueAnswer();

    // Mevcut slider DEĞERLERİ tüm koşulları sağlıyor mu?
    public bool MeetsRequirements(StatManager stats)
    {
        foreach (var req in requirements)
        {
            float value = stats.GetValue(req.stat);
            if (value < req.min || value > req.max) return false;
        }
        return true;
    }

    // Slider'ların kalan (daralmış) ARALIKLARI ile bu koşullar hâlâ sağlanabilir mi?
    // Kötü son kontrolü bunun üzerinden yapılır.
    public bool CanEverBeMet(StatManager stats)
    {
        foreach (var req in requirements)
        {
            float minLimit = stats.GetMinLimit(req.stat);
            float maxLimit = stats.GetMaxLimit(req.stat);
            bool intersects = req.min <= maxLimit && req.max >= minLimit;
            if (!intersects) return false;
        }
        return true;
    }
}
