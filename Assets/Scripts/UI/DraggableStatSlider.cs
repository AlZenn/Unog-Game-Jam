using UnityEngine;
using UnityEngine.UI;

// Dikey stat slider'ı: mouse ile basılı tutup sürükleyerek değiştirilir (uGUI Slider'ın
// kendi drag davranışı), ancak değer StatManager'ın daralan min/max sınırlarına clamp'lenir.
// Kilitli bölgeler koyu overlay ile gösterilir. Diyalog sırasında slider kilitlenir.
public class DraggableStatSlider : MonoBehaviour
{
    public StatType statType;
    public Slider slider;
    public Text valueLabel;
    public RectTransform bottomLockZone;
    public RectTransform topLockZone;

    bool syncing;

    StatManager Stats => GameManager.Instance != null ? GameManager.Instance.stats : null;

    void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
    }

    void Start()
    {
        var stats = Stats;
        if (stats == null) return;

        slider.minValue = 0f;
        slider.maxValue = stats.statMax;
        slider.onValueChanged.AddListener(OnSliderChanged);
        stats.OnStatsChanged += Refresh;
        GameManager.Instance.OnStateChanged += OnGameStateChanged;
        Refresh();
    }

    void OnDestroy()
    {
        if (Stats != null) Stats.OnStatsChanged -= Refresh;
        if (GameManager.Instance != null) GameManager.Instance.OnStateChanged -= OnGameStateChanged;
    }

    void OnGameStateChanged(GameState state)
    {
        slider.interactable = state == GameState.Exploring;
    }

    void OnSliderChanged(float value)
    {
        if (syncing) return;
        var stats = Stats;
        if (stats == null) return;

        float clamped = Mathf.Clamp(value, stats.GetMinLimit(statType), stats.GetMaxLimit(statType));
        if (!Mathf.Approximately(clamped, value))
        {
            syncing = true;
            slider.value = clamped;
            syncing = false;
        }
        stats.SetValue(statType, clamped);
        UpdateLabel(clamped);
    }

    void Refresh()
    {
        var stats = Stats;
        if (stats == null) return;

        syncing = true;
        slider.value = stats.GetValue(statType);
        syncing = false;

        UpdateLabel(slider.value);
        UpdateLockZones(stats);
    }

    void UpdateLabel(float value)
    {
        if (valueLabel != null) valueLabel.text = Mathf.RoundToInt(value).ToString();
    }

    // Kilitli bölgeler: alt overlay [0, minLimit], üst overlay [maxLimit, max].
    void UpdateLockZones(StatManager stats)
    {
        float max = stats.statMax;
        if (bottomLockZone != null)
        {
            bottomLockZone.anchorMin = new Vector2(0f, 0f);
            bottomLockZone.anchorMax = new Vector2(1f, stats.GetMinLimit(statType) / max);
            bottomLockZone.offsetMin = Vector2.zero;
            bottomLockZone.offsetMax = Vector2.zero;
        }
        if (topLockZone != null)
        {
            topLockZone.anchorMin = new Vector2(0f, stats.GetMaxLimit(statType) / max);
            topLockZone.anchorMax = new Vector2(1f, 1f);
            topLockZone.offsetMin = Vector2.zero;
            topLockZone.offsetMax = Vector2.zero;
        }
    }
}
