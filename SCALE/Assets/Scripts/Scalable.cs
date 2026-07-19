using UnityEngine;
using TMPro;

[RequireComponent(typeof(Rigidbody))]
public class Scalable : MonoBehaviour
{
    [Tooltip("Smallest allowed size, as a multiple of the object's starting scale.")]
    public float minFactor = 0.3f;

    [Tooltip("Largest allowed size, as a multiple of the object's starting scale.")]
    public float maxFactor = 3f;

    [Header("Growth Blocking")]
    [Tooltip("Layers that can stop this object from growing. Set to Everything if unsure.")]
    public LayerMask blockingMask = ~0;

    [Tooltip("Slack allowed when testing for room to grow. Stops resting contact with the floor from counting as a blockage.")]
    public float growthSkin = 0.03f;

    public TextMeshPro debugText;

    private Rigidbody rb;
    private Collider bodyCollider;
    private Vector3 baseScale;
    private float baseMass;
    private float factor = 1f;

    private readonly Collider[] overlapBuffer = new Collider[16];

    public bool IsGrowthBlocked { get; private set; }

    private void Awake()
    {
        baseScale = transform.localScale;
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponentInChildren<Collider>();
        if (rb != null)
        {
            baseMass = rb.mass;
        }

        UpdateDebugText();
    }

    public int ApplyScale(float delta)
    {
        float previous = factor;
        float desired = Mathf.Clamp(factor + delta, minFactor, maxFactor);

        // Shrinking can never create a new overlap, so only growth is tested.
        IsGrowthBlocked = desired > factor && !HasRoomFor(desired);
        if (IsGrowthBlocked)
        {
            return 0;
        }

        factor = desired;
        transform.localScale = baseScale * factor;

        if (rb != null)
        {
            rb.mass = baseMass * factor;
        }

        float applied = factor - previous;
        if (Mathf.Approximately(applied, 0f))
        {
            return 0;
        }

        UpdateDebugText();

        return applied > 0f ? 1 : -1;
    }

    private bool HasRoomFor(float newFactor)
    {
        if (bodyCollider == null)
        {
            return true;
        }

        float ratio = newFactor / factor;
        Bounds bounds = bodyCollider.bounds;

        Vector3 center = transform.position + (bounds.center - transform.position) * ratio;
        Vector3 half = bounds.extents * ratio - Vector3.one * growthSkin;
        half = Vector3.Max(half, Vector3.one * 0.001f);

        int count = Physics.OverlapBoxNonAlloc(
            center, half, overlapBuffer, Quaternion.identity, blockingMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapBuffer[i];

            if (hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.CompareTag("Player"))
            {
                continue;
            }

            return false;
        }

        return true;
    }

    private void UpdateDebugText()
    {
        if (debugText == null)
        {
            return;
        }

        float mass = rb != null ? rb.mass : 0f;
        debugText.text = $"debug\nsize: {transform.localScale:F2}\nmass: {mass:F2}";
    }
}
