using UnityEngine;

// Kısa sönümlü kamera sarsıntısı (örn. olumsuz diyalog cevabında).
public class CameraShake : MonoBehaviour
{
    public static CameraShake Instance { get; private set; }

    public float defaultDuration = 0.3f;
    public float defaultMagnitude = 0.15f;

    [Header("Ses")]
    [Tooltip("Sarsıntı tetiklenince çalınacak ses (örn. shake.mp3)")]
    public AudioClip shakeClip;
    [Range(0f, 1f)] public float shakeVolume = 0.8f;

    Vector3 basePos;
    float timer;
    float duration;
    float magnitude;
    AudioSource audioSource;

    void Awake()
    {
        Instance = this;
        basePos = transform.localPosition;
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    public void Shake() => Shake(defaultDuration, defaultMagnitude);

    public void Shake(float shakeDuration, float shakeMagnitude)
    {
        duration = Mathf.Max(0.01f, shakeDuration);
        magnitude = shakeMagnitude;
        timer = duration;

        if (shakeClip != null && audioSource != null)
            audioSource.PlayOneShot(shakeClip, shakeVolume);
    }

    void LateUpdate()
    {
        if (timer <= 0f) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            transform.localPosition = basePos;
            return;
        }

        float damper = timer / duration;
        Vector2 offset = Random.insideUnitCircle * magnitude * damper;
        transform.localPosition = basePos + new Vector3(offset.x, offset.y, 0f);
    }
}
