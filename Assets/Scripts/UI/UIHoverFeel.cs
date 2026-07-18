using UnityEngine;
using UnityEngine.EventSystems;

// UI objeleri (slider, buton, Image+Button yapısındaki karakterler) için hover feel:
// hafif scale büyümesi + isteğe bağlı UI outline. OutlineFx shader'ı CanvasRenderer'a
// etki edemediği için UI'da Unity'nin yerleşik UnityEngine.UI.Outline efekti kullanılır.
public class UIHoverFeel : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Hover'da büyüme oranı (0.05 = %5)")]
    [Range(0f, 0.5f)] public float scaleAmount = 0.05f;
    [Tooltip("Scale geçiş hızı (lerp)")]
    public float animSpeed = 10f;

    [Header("UI Outline (Image tabanlı objeler için)")]
    public bool useOutline = false;
    public Color outlineColor = Color.white;
    public Vector2 outlineDistance = new Vector2(4f, 4f);
    [Tooltip("Outline'ın ekleneceği obje; boşsa bu obje kullanılır")]
    public GameObject outlineTarget;

    UnityEngine.UI.Outline uiOutline;
    Vector3 baseScale;
    bool hovered;

    void Awake()
    {
        baseScale = transform.localScale;
        if (useOutline)
        {
            var target = outlineTarget != null ? outlineTarget : gameObject;
            uiOutline = target.GetComponent<UnityEngine.UI.Outline>();
            if (uiOutline == null) uiOutline = target.AddComponent<UnityEngine.UI.Outline>();
            uiOutline.effectColor = outlineColor;
            uiOutline.effectDistance = outlineDistance;
            uiOutline.enabled = false;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) => SetHovered(true);
    public void OnPointerExit(PointerEventData eventData) => SetHovered(false);

    void SetHovered(bool value)
    {
        hovered = value;
        if (uiOutline != null) uiOutline.enabled = value;
    }

    void OnDisable()
    {
        hovered = false;
        transform.localScale = baseScale;
        if (uiOutline != null) uiOutline.enabled = false;
    }

    void Update()
    {
        Vector3 target = hovered ? baseScale * (1f + scaleAmount) : baseScale;
        transform.localScale = Vector3.Lerp(transform.localScale, target, Time.deltaTime * animSpeed);
    }
}
