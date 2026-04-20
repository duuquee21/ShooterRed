using UnityEngine;

// Muestra un aro rojo en el suelo que se expande y desvanece al explotar la granada.
// Sin prefab: se crea todo por código. Llama a Spawn() desde GrenadeVisual.
public class ExplosionVisual : MonoBehaviour
{
    private LineRenderer _lr;
    private float _maxRadius;
    private float _duration;
    private float _elapsed;

    private const int Segments = 64;

    // Crea el efecto en la posición indicada con el radio de la explosión
    public static void Spawn(Vector3 position, float radius, float duration = 0.8f)
    {
        GameObject go = new GameObject("ExplosionRing");
        go.transform.position = position + Vector3.up * 0.05f; // ligeramente sobre el suelo

        ExplosionVisual ev = go.AddComponent<ExplosionVisual>();
        ev._maxRadius = radius;
        ev._duration  = duration;

        LineRenderer lr = go.AddComponent<LineRenderer>();
        lr.loop           = true;
        lr.positionCount  = Segments;
        lr.useWorldSpace  = false;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;

        // Material unlit para que no le afecten las luces
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.startColor = new Color(1f, 0.1f, 0.1f, 1f);
        lr.endColor   = new Color(1f, 0.5f, 0.0f, 1f);
        lr.startWidth = 0.18f;
        lr.endWidth   = 0.18f;

        ev._lr = lr;
        ev.UpdateRing(0f);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = Mathf.Clamp01(_elapsed / _duration);

        UpdateRing(t);

        // Fade: se va volviendo transparente al final
        float alpha = 1f - t;
        _lr.startColor = new Color(1f, 0.1f * t, 0f, alpha);
        _lr.endColor   = new Color(1f, 0.5f,      0f, alpha);

        if (t >= 1f) Destroy(gameObject);
    }

    private void UpdateRing(float t)
    {
        // Expansión rápida al principio, luego se frena (ease-out)
        float r = Mathf.Lerp(0f, _maxRadius, 1f - Mathf.Pow(1f - t, 2f));

        for (int i = 0; i < Segments; i++)
        {
            float angle = (i / (float)Segments) * Mathf.PI * 2f;
            _lr.SetPosition(i, new Vector3(Mathf.Cos(angle) * r, 0f, Mathf.Sin(angle) * r));
        }
    }
}
