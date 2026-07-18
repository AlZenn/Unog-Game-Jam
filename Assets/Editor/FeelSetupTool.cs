using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

// Feel efektlerini kurar: CameraShake, TensionVignette, DialogueAudio,
// Player idle nefesi ve ortam toz partikülleri. Idempotent.
public static class FeelSetupTool
{
    const string DustMaterialPath = "Assets/Sprites/DustParticle.mat";
    const string DustTexturePath =
        "Assets/COMICOMI/VFX_FirelyFlare/Art/Textures/tex_vfx-ult_particle_sprite_point-blurred.png";

    [MenuItem("Tools/UnoG/Feel Efektlerini Kur", priority = 18)]
    public static void Setup()
    {
        // Kamera: shake + post-processing (vignette için şart)
        var cam = Camera.main;
        if (cam != null)
        {
            if (cam.GetComponent<CameraShake>() == null) cam.gameObject.AddComponent<CameraShake>();
            var camData = cam.GetUniversalAdditionalCameraData();
            if (camData != null) camData.renderPostProcessing = true;
            EditorUtility.SetDirty(cam.gameObject);
        }
        else
        {
            Debug.LogWarning("UnoG: Main Camera bulunamadı — CameraShake eklenemedi.");
        }

        // Managers: vignette + diyalog sesi
        var gm = Object.FindFirstObjectByType<GameManager>();
        if (gm != null)
        {
            var go = gm.gameObject;
            if (go.GetComponent<TensionVignette>() == null) go.AddComponent<TensionVignette>();
            if (go.GetComponent<DialogueAudio>() == null) go.AddComponent<DialogueAudio>();
            var audioSource = go.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = go.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;

            // Arka plan müziği: adında music/bgm/müzik/theme geçen klibi otomatik bağla.
            var music = go.GetComponent<BackgroundMusic>();
            if (music == null) music = go.AddComponent<BackgroundMusic>();
            if (music.musicClip == null)
                music.musicClip = FindClipByKeywords("music", "bgm", "muzik", "müzik", "theme", "soundtrack");

            EditorUtility.SetDirty(go);
        }
        else
        {
            Debug.LogWarning("UnoG: GameManager bulunamadı — TensionVignette/DialogueAudio eklenemedi.");
        }

        // Player: idle nefes (HoverHighlight'ı olmayan objeler için ayrı component)
        var player = GameObject.Find("Player");
        if (player != null && player.GetComponent<HoverHighlight>() == null &&
            player.GetComponent<IdleBreathe>() == null)
        {
            player.AddComponent<IdleBreathe>();
            EditorUtility.SetDirty(player);
        }

        // Ortam tozu
        if (GameObject.Find("AmbientDust") == null)
            CreateAmbientDust();

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("UnoG: Feel efektleri kuruldu (shake, vignette, blip sesi, nefes, toz).");
    }

    static AudioClip FindClipByKeywords(params string[] keywords)
    {
        foreach (var guid in AssetDatabase.FindAssets("t:AudioClip"))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path.StartsWith("Packages/")) continue;
            string name = System.IO.Path.GetFileNameWithoutExtension(path).ToLowerInvariant();
            foreach (var keyword in keywords)
                if (name.Contains(keyword))
                    return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }
        return null;
    }

    // ---------------------------------------------------------------- toz

    static void CreateAmbientDust()
    {
        var material = EnsureDustMaterial();

        var go = new GameObject("AmbientDust");
        var ps = go.AddComponent<ParticleSystem>();
        go.transform.position = Vector3.zero;

        var main = ps.main;
        main.loop = true;
        main.startLifetime = new ParticleSystem.MinMaxCurve(8f, 14f);
        main.startSpeed = 0f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.14f);
        main.startColor = new Color(1f, 1f, 1f, 0.25f);
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.prewarm = true;

        var emission = ps.emission;
        emission.rateOverTime = 12f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20f, 12f, 1f);

        var velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-0.15f, 0.15f);
        velocity.y = new ParticleSystem.MinMaxCurve(0.03f, 0.18f);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);

        // Doğup ölürken yumuşak alpha (belirip kaybolma)
        var col = ps.colorOverLifetime;
        col.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 0.2f),
                new GradientAlphaKey(1f, 0.8f),
                new GradientAlphaKey(0f, 1f),
            });
        col.color = gradient;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.material = material;
        renderer.sortingOrder = 5; // sprite'ların önü, UI'ın arkası

        EditorUtility.SetDirty(go);
    }

    static Material EnsureDustMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(DustMaterialPath);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder("Assets/Sprites"))
            AssetDatabase.CreateFolder("Assets", "Sprites");

        // Sprites/Default: URP'de particle için sorunsuz, transparan, vertex-color'lı.
        var material = new Material(Shader.Find("Sprites/Default"));
        var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(DustTexturePath);
        if (texture != null) material.mainTexture = texture;
        AssetDatabase.CreateAsset(material, DustMaterialPath);
        return material;
    }
}
