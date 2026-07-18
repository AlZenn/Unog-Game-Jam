using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Gerilim vignette'i: art arda olumsuz cevap sayacı arttıkça ekran kenarları
// koyulaşır ve kızarır — kötü son yaklaşıyor hissi. Sayaç sıfırlanınca
// yumuşakça normale döner; GameOver'da maksimuma gider.
// Volume + profil runtime'da kurulur, sahnede ekstra obje gerekmez.
public class TensionVignette : MonoBehaviour
{
    [Range(0f, 1f)] public float baseIntensity = 0.15f;
    [Range(0f, 1f)] public float maxExtraIntensity = 0.35f;
    public float lerpSpeed = 2f;
    public Color calmColor = Color.black;
    public Color tenseColor = new Color(0.35f, 0f, 0f);

    Vignette vignette;

    void Awake()
    {
        var volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;

        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        vignette = profile.Add<Vignette>();
        vignette.active = true;
        vignette.intensity.Override(baseIntensity);
        vignette.color.Override(calmColor);
        vignette.smoothness.Override(0.5f);
        volume.profile = profile;
    }

    void Update()
    {
        var gm = GameManager.Instance;
        if (gm == null || vignette == null) return;

        float tension = gm.State == GameState.GameOver
            ? 1f
            : Mathf.Clamp01((float)gm.ConsecutiveNegativeCount /
                            Mathf.Max(1, gm.consecutiveNegativeLimit));

        float targetIntensity = baseIntensity + maxExtraIntensity * tension;
        Color targetColor = Color.Lerp(calmColor, tenseColor, tension);

        float step = Time.deltaTime * lerpSpeed;
        vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, step);
        vignette.color.value = Color.Lerp(vignette.color.value, targetColor, step);
    }
}
