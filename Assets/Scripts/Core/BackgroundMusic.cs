using UnityEngine;

// Arka plan müziği: Play tuşuna basılınca başlar, loop olarak sürekli çalar.
// Diyalog sırasında ses otomatik kısılır (ducking), diyalog bitince geri yükselir.
// Tüm geçişler yumuşaktır. Klibi Inspector'dan ata (Managers objesinde).
public class BackgroundMusic : MonoBehaviour
{
    public static BackgroundMusic Instance { get; private set; }

    [Tooltip("Loop çalacak müzik dosyası")]
    public AudioClip musicClip;
    [Range(0f, 1f)] public float volume = 0.5f;
    [Tooltip("Diyalog sırasında müziğin kısılacağı seviye")]
    [Range(0f, 1f)] public float duckedVolume = 0.2f;
    [Tooltip("Ses geçiş hızı (birim/saniye) — hem fade-in hem ducking için")]
    public float volumeChangeSpeed = 0.35f;

    AudioSource source;
    bool started;

    void Awake()
    {
        Instance = this;
        source = gameObject.AddComponent<AudioSource>();
        source.loop = true;
        source.playOnAwake = false;
    }

    public void Play()
    {
        if (musicClip == null || source == null || started) return;

        started = true;
        source.clip = musicClip;
        source.volume = 0f; // fade-in: Update hedefe doğru yükseltir
        source.Play();
    }

    void Update()
    {
        if (!started || source == null) return;

        var gm = GameManager.Instance;
        bool inDialogue = gm != null && gm.State == GameState.InDialogue;
        float target = inDialogue ? duckedVolume : volume;

        source.volume = Mathf.MoveTowards(source.volume, target,
            Time.deltaTime * volumeChangeSpeed);
    }
}
