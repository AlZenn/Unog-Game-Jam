using UnityEngine;

// Dünya objeleri (NPC, kapı) için hover feel: OutlineFx outline'ı belirir
// ve görsel hafifçe büyür. Büyüme miktarı ve hız Inspector'dan ayarlanır.
public class HoverHighlight : MonoBehaviour
{
    [Tooltip("Hover'da büyüme oranı (0.05 = %5)")]
    [Range(0f, 0.5f)] public float scaleAmount = 0.05f;
    [Tooltip("Scale geçiş hızı (lerp)")]
    public float animSpeed = 10f;
    public OutlineFx.OutlineFx outline;
    public Color outlineColor = Color.white;

    Vector3 baseScale;
    bool hovered;

    void Awake()
    {
        baseScale = transform.localScale;
        if (outline == null) outline = GetComponent<OutlineFx.OutlineFx>();
        if (outline != null)
        {
            outline._color = outlineColor;
            outline.enabled = false;
        }
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
        transform.localScale = baseScale;
        if (outline != null) outline.enabled = false;
    }

    void Update()
    {
        Vector3 target = hovered ? baseScale * (1f + scaleAmount) : baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * animSpeed);
    }
}
