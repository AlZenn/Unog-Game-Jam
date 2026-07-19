using UnityEngine;

// Dünya objeleri (NPC, kapı) için feel: hover'da outline + büyüme,
// sürekli idle nefes salınımı ve tıklamada squash & stretch punch.
// İsim etiketi (Label child) her zaman görünürdür — bu script dokunmaz.
// localScale'i tek yerden yazar: baseScale × hover × nefes × punch.
public class HoverHighlight : MonoBehaviour
{
    [Header("Hover")]
    [Tooltip("Hover'da büyüme oranı (0.05 = %5)")]
    [Range(0f, 0.5f)] public float scaleAmount = 0.05f;
    [Tooltip("Geçiş hızı (lerp)")]
    public float animSpeed = 10f;
    public OutlineFx.OutlineFx outline;
    public Color outlineColor = Color.white;

    [Header("Idle Nefes")]
    public bool breatheEnabled = true;
    [Range(0f, 0.2f)] public float breatheAmount = 0.02f;
    public float breatheSpeed = 2f;

    [Header("Tıklama Punch (squash & stretch)")]
    [Range(0f, 0.5f)] public float punchAmount = 0.15f;
    public float punchDuration = 0.25f;

    Vector3 baseScale;
    bool hovered;
    float progress;     // 0 = normal, 1 = tam hover
    float breathePhase; // obje başına rastgele faz — hepsi aynı anda nefes almasın
    float punchTimer = -1f;

    void Awake()
    {
        baseScale = transform.localScale;
        breathePhase = Random.Range(0f, Mathf.PI * 2f);

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

    public void TriggerClickPunch()
    {
        punchTimer = 0f;
    }

    void OnDisable()
    {
        hovered = false;
        progress = 0f;
        punchTimer = -1f;
        transform.localScale = baseScale;
        if (outline != null) outline.enabled = false;
    }

    void Update()
    {
        progress = Mathf.Lerp(progress, hovered ? 1f : 0f, Time.deltaTime * animSpeed);
        float hoverFactor = 1f + scaleAmount * progress;

        float breatheFactor = 1f;
        if (breatheEnabled)
            breatheFactor = 1f + Mathf.Sin(Time.time * breatheSpeed + breathePhase) * breatheAmount;

        float squash = 0f;
        if (punchTimer >= 0f)
        {
            punchTimer += Time.deltaTime;
            if (punchTimer >= punchDuration)
                punchTimer = -1f;
            else
            {
                float t = punchTimer / punchDuration;
                squash = Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) * punchAmount;
            }
        }

        transform.localScale = new Vector3(
            baseScale.x * hoverFactor * breatheFactor * (1f + squash),
            baseScale.y * hoverFactor * breatheFactor * (1f - squash),
            baseScale.z);
    }
}
