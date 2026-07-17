using System;
using System.Collections.Generic;
using UnityEngine;

// 3 ana stat'ın değerlerini ve kalıcı olarak daralan min/max sınırlarını tutar.
// Cevap etkileri aralığı daraltır: pozitif etki tabanı yükseltir, negatif etki tavanı düşürür.
// Böylece oyun ilerledikçe sliderlar "değişemeyecek duruma" gelir ve kötü son tetiklenir.
public class StatManager : MonoBehaviour
{
    [Serializable]
    public class StatState
    {
        public StatType type;
        public float currentValue;
        public float minLimit;
        public float maxLimit = 100f;
    }

    public float statMax = 100f;
    public StatState[] stats;

    public event Action OnStatsChanged;

    void Awake()
    {
        var types = (StatType[])Enum.GetValues(typeof(StatType));
        stats = new StatState[types.Length];
        for (int i = 0; i < types.Length; i++)
        {
            stats[i] = new StatState
            {
                type = types[i],
                minLimit = 0f,
                maxLimit = statMax,
                currentValue = statMax * 0.5f // default değer her zaman max'ın yarısı
            };
        }
    }

    StatState Get(StatType type)
    {
        foreach (var s in stats)
            if (s.type == type) return s;
        return null;
    }

    public float GetValue(StatType type) => Get(type).currentValue;
    public float GetMinLimit(StatType type) => Get(type).minLimit;
    public float GetMaxLimit(StatType type) => Get(type).maxLimit;

    // Slider sürüklenirken çağrılır; değer kalan aralığa clamp'lenir.
    public void SetValue(StatType type, float value)
    {
        var s = Get(type);
        float clamped = Mathf.Clamp(value, s.minLimit, s.maxLimit);
        if (Mathf.Approximately(clamped, s.currentValue)) return;
        s.currentValue = clamped;
        OnStatsChanged?.Invoke();
    }

    // Cevap etkilerini uygular: değeri kaydırır VE aralığı kalıcı daraltır.
    public void ApplyEffects(List<StatEffect> effects)
    {
        if (effects == null || effects.Count == 0) return;
        foreach (var e in effects) ApplyEffectInternal(e);
        OnStatsChanged?.Invoke();
    }

    void ApplyEffectInternal(StatEffect e)
    {
        var s = Get(e.stat);
        if (s == null || Mathf.Approximately(e.amount, 0f)) return;

        s.currentValue += e.amount;
        if (e.amount > 0f)
            s.minLimit = Mathf.Min(s.minLimit + e.amount, s.maxLimit); // taban yükselir
        else
            s.maxLimit = Mathf.Max(s.maxLimit + e.amount, s.minLimit); // tavan düşer

        s.currentValue = Mathf.Clamp(s.currentValue, s.minLimit, s.maxLimit);
    }
}
