using UnityEngine;

public class Scalable : MonoBehaviour
{
    [Tooltip("Smallest allowed size, as a multiple of the object's starting scale.")]
    public float minFactor = 0.3f;

    [Tooltip("Largest allowed size, as a multiple of the object's starting scale.")]
    public float maxFactor = 3f;

    private Vector3 baseScale;
    private float factor = 1f;

    private void Awake()
    {
        baseScale = transform.localScale;
    }

    // Grows (positive) or shrinks (negative) the object, clamped to its limits.
    // Returns the direction actually applied: +1 grew, -1 shrank, 0 hit a limit.
    public int ApplyScale(float delta)
    {
        float previous = factor;
        factor = Mathf.Clamp(factor + delta, minFactor, maxFactor);
        transform.localScale = baseScale * factor;

        float applied = factor - previous;
        if (Mathf.Approximately(applied, 0f))
        {
            return 0;
        }

        return applied > 0f ? 1 : -1;
    }
}
