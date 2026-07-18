using UnityEngine;

// Dünya objeleri (NPC, kapı) için hover feel: OutlineFx outline'ı belirir,
// görsel hafifçe büyür ve isim etiketi (Label) büyüme animasyonuyla birlikte
// yumuşakça görünür olur. Etiket normalde gizlidir.
public class HoverHighlight : MonoBehaviour
{
    [Tooltip("Hover'da büyüme oranı (0.05 = %5)")]
    [Range(0f, 0.5f)] public float scaleAmount = 0.05f;
    [Tooltip("Geçiş hızı (lerp)")]
    public float animSpeed = 10f;
    public OutlineFx.OutlineFx outline;
    public Color outlineColor = Color.white;

    [Tooltip("Hover'da görünecek isim etiketi; boşsa 'Label' adlı child aranır")]
    public TextMesh label;

    Vector3 baseScale;
    bool hovered;
    float progress; // 0 = normal, 1 = tam hover

    void Awake()
    {
        baseScale = transform.localScale;

        if (outline == null) outline = GetComponent<OutlineFx.OutlineFx>();
        if (outline != null)
        {
            outline._color = outlineColor;
            outline.enabled = false;
        }

        if (label == null)
        {
            var labelTransform = transform.Find("Label");
            if (labelTransform != null) label = labelTransform.GetComponent<TextMesh>();
        }
        SetLabelAlpha(0f);
    }

    public void SetHovered(bool value)
    {
        if (hovered == value) return;
        hovered = value;
        if (outline != null)
        {
            outline._color = outlineColor;
            outline.enabled = value;
        }
    }

    void OnDisable()
    {
        hovered = false;
        progress = 0f;
        transform.localScale = baseScale;
        if (outline != null) outline.enabled = false;
        SetLabelAlpha(0f);
    }

    void Update()
    {
        progress = Mathf.Lerp(progress, hovered ? 1f : 0f, Time.deltaTime * animSpeed);
        transform.localScale = Vector3.Lerp(baseScale, baseScale * (1f + scaleAmount), progress);
        SetLabelAlpha(progress);
    }

    void SetLabelAlpha(float alpha)
    {
        if (label == null) return;
        Color c = label.color;
        c.a = alpha;
        label.color = c;
    }
}
