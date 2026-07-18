using UnityEngine;

// HoverHighlight'ı OLMAYAN objeler için idle nefes salınımı (örn. ana karakter).
// HoverHighlight'lı objelerde nefes o component'in içindedir — ikisi birden
// aynı objeye eklenmemeli (localScale'i ezerler).
public class IdleBreathe : MonoBehaviour
{
    [Range(0f, 0.2f)] public float amount = 0.02f;
    public float speed = 2f;

    Vector3 baseScale;
    float phase;

    void Awake()
    {
        baseScale = transform.localScale;
        phase = Random.Range(0f, Mathf.PI * 2f);
    }

    void OnDisable()
    {
        transform.localScale = baseScale;
    }

    void Update()
    {
        float factor = 1f + Mathf.Sin(Time.time * speed + phase) * amount;
        transform.localScale = baseScale * factor;
    }
}
