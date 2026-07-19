using UnityEngine;

// Typewriter harf sesi. Inspector'dan bir klip atanırsa o kullanılır;
// boş bırakılırsa kodla üretilen prosedürel blip yedek olarak devreye girer.
// Her harfte rastgele pitch ile çalınır.
public class DialogueAudio : MonoBehaviour
{
    [Tooltip("Harf sesi; boşsa kodla üretilen blip kullanılır")]
    public AudioClip blipClip;
    [Range(0f, 1f)] public float volume = 0.35f;
    public float basePitch = 1f;
    [Tooltip("Her blip'te pitch bu kadar rastgele sapar")]
    public float pitchVariance = 0.12f;
    [Tooltip("Blip temel frekansı (Hz)")]
    public float blipFrequency = 620f;

    AudioSource source;
    AudioClip proceduralBlip;

    void Awake()
    {
        source = GetComponent<AudioSource>();
        if (source == null) source = gameObject.AddComponent<AudioSource>();
        source.playOnAwake = false;
        if (blipClip == null) proceduralBlip = CreateBlipClip();
    }

    public void PlayBlip()
    {
        var clip = blipClip != null ? blipClip : proceduralBlip;
        if (source == null || clip == null) return;
        source.pitch = basePitch + Random.Range(-pitchVariance, pitchVariance);
        source.PlayOneShot(clip, volume);
    }

    // ~0.045 sn, sinüs + hafif kare karışımı, lineer sönümlü zarf.
    AudioClip CreateBlipClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.045f;
        int count = (int)(sampleRate * duration);
        var samples = new float[count];

        for (int i = 0; i < count; i++)
        {
            float t = (float)i / sampleRate;
            float w = 2f * Mathf.PI * blipFrequency * t;
            float wave = 0.6f * Mathf.Sin(w) + 0.4f * Mathf.Sign(Mathf.Sin(w));
            float envelope = 1f - (float)i / count;
            samples[i] = wave * envelope * 0.8f;
        }

        var clip = AudioClip.Create("DialogueBlip", count, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
