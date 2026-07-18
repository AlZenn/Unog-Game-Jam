using UnityEngine;

// UI seslerinin merkezi: buton hover ve click sesleri tek yerden atanır.
// Kendi AudioSource'unu runtime'da oluşturur (diyalog blip'iyle karışmasın diye).
public class UISoundManager : MonoBehaviour
{
    public static UISoundManager Instance { get; private set; }

    [Header("Sesler (buraya sürükle)")]
    public AudioClip hoverClip;
    public AudioClip clickClip;

    [Header("Ayarlar")]
    [Range(0f, 1f)] public float hoverVolume = 0.5f;
    [Range(0f, 1f)] public float clickVolume = 0.8f;
    [Tooltip("Her çalışta pitch bu kadar rastgele sapar (0 = hep aynı)")]
    public float pitchVariance = 0.05f;

    AudioSource source;

    void Awake()
    {
        Instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
    }

    public void PlayHover() => Play(hoverClip, hoverVolume);
    public void PlayClick() => Play(clickClip, clickVolume);

    void Play(AudioClip clip, float volume)
    {
        if (clip == null || source == null) return;
        source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
        source.PlayOneShot(clip, volume);
    }
}
