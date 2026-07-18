using System.Collections;
using UnityEngine;
using UnityEngine.UI;

// Dikey stat slider'ı: mouse ile basılı tutup sürükleyerek değiştirilir (uGUI Slider'ın
// kendi drag davranışı), ancak değer StatManager'ın daralan min/max sınırlarına clamp'lenir.
// Kilitli bölgeler koyu overlay ile gösterilir. Diyalog sırasında slider kilitlenir.
// Feel: cevap etkisiyle değer değişince value yazısı punch yapar; aralık daralınca
// ilgili kilit bölgesi kırmızı flaş verir.
public class DraggableStatSlider : MonoBehaviour
{
    public StatType statType;
    public Slider slider;
    public Text valueLabel;
    public RectTransform bottomLockZone;
    public RectTransform topLockZone;

    [Header("Feel")]
    public float punchScale = 1.35f;
    public float punchDuration = 0.25f;
    public Color lockFlashColor = new Color(0.9f, 0.15f, 0.15f, 0.85f);
    public float lockFlashDuration = 0.5f;

    bool syncing;
    bool selfChange;
    float lastMinLimit = -1f;
    float lastMaxLimit = -1f;
    Image bottomLockImage;
    Image topLockImage;
    Color lockBaseColor = new Color(0f, 0f, 0f, 0.6f);
    Coroutine punchRoutine;
    Coroutine bottomFlashRoutine;
    Coroutine topFlashRoutine;

    StatManager Stats => GameManager.Instance != null ? GameManager.Instance.stats : null;

    void Awake()
    {
        if (slider == null) slider = GetComponent<Slider>();
        if (bottomLockZone != null) bottomLockImage = bottomLockZone.GetComponent<Image>();
        if (topLockZone != null) topLockImage = topLockZone.GetComponent<Image>();
        if (bottomLockImage != null) lockBaseColor = bottomLockImage.color;
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
        lastMinLimit = stats.GetMinLimit(statType);
        lastMaxLimit = stats.GetMaxLimit(statType);

        // Alt etiket: stat'ın zıttı yazılır (slider'ın alt ucu bu özelliği temsil eder).
        if (valueLabel != null) valueLabel.text = OppositeName(statType);
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

        selfChange = true;
        stats.SetValue(statType, clamped);
        selfChange = false;

        UpdateLabel(clamped);
    }

    void Refresh()
    {
        var stats = Stats;
        if (stats == null) return;

        float newValue = stats.GetValue(statType);
        bool valueChanged = !Mathf.Approximately(slider.value, newValue);

        syncing = true;
        slider.value = newValue;
        syncing = false;

        UpdateLabel(newValue);
        UpdateLockZones(stats);

        // Dışarıdan (cevap etkisi) gelen değer değişiminde punch.
        if (!selfChange && valueChanged) PunchValueLabel();

        // Aralık daraldıysa ilgili kilit bölgesi flaş verir.
        float newMin = stats.GetMinLimit(statType);
        float newMax = stats.GetMaxLimit(statType);
        if (lastMinLimit >= 0f && isActiveAndEnabled)
        {
            if (newMin > lastMinLimit + 0.01f)
                bottomFlashRoutine = RestartFlash(bottomFlashRoutine, bottomLockImage);
            if (newMax < lastMaxLimit - 0.01f)
                topFlashRoutine = RestartFlash(topFlashRoutine, topLockImage);
        }
        lastMinLimit = newMin;
        lastMaxLimit = newMax;
    }

    // Alt etiket sayı değil, stat'ın zıttını gösterir — değer değişiminde metin değişmez
    // (punch animasyonu yine oynar).
    void UpdateLabel(float value) { }

    static string OppositeName(StatType stat)
    {
        switch (stat)
        {
            case StatType.Ofke: return "SAKİNLİK";
            case StatType.Durustluk: return "İKİYÜZLÜLÜK";
            case StatType.Cikar: return "FEDAKARLIK";
            default: return "";
        }
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

    // ---------------------------------------------------------------- feel

    void PunchValueLabel()
    {
        if (valueLabel == null || !isActiveAndEnabled) return;
        if (punchRoutine != null) StopCoroutine(punchRoutine);
        punchRoutine = StartCoroutine(PunchRoutine(valueLabel.transform));
    }

    IEnumerator PunchRoutine(Transform target)
    {
        float t = 0f;
        while (t < punchDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / punchDuration);
            target.localScale = Vector3.one * Mathf.Lerp(punchScale, 1f, 1f - Mathf.Pow(1f - p, 2f));
            yield return null;
        }
        target.localScale = Vector3.one;
    }

    Coroutine RestartFlash(Coroutine current, Image image)
    {
        if (image == null) return null;
        if (current != null) StopCoroutine(current);
        return StartCoroutine(FlashRoutine(image));
    }

    IEnumerator FlashRoutine(Image image)
    {
        float t = 0f;
        while (t < lockFlashDuration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / lockFlashDuration);
            image.color = Color.Lerp(lockFlashColor, lockBaseColor, p);
            yield return null;
        }
        image.color = lockBaseColor;
    }
}
