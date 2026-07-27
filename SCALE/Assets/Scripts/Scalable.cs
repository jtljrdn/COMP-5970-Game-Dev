using System.Collections;
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

    [Tooltip("Penetration depth ignored when making room. Stops resting contact with the floor from counting as a blockage.")]
    public float growthSkin = 0.03f;

    [Tooltip("How far the object may be shoved out of surfaces to make room while growing. Growth is refused if it needs more than this.")]
    public float maxGrowthPush = 1.5f;

    [Tooltip("Passes used to shove the object clear after growing. More passes settle tight corners better.")]
    public int growthResolvePasses = 4;

    public TextMeshPro debugText;

    [Header("Tutorial")]
    public bool isTutorialObject = false;
    private bool isTutorialComplete = false;
    public TextMeshPro tutorialText;

    [Tooltip("Seconds the tutorial text stays fully visible after the tutorial is completed.")]
    public float tutorialHoldTime = 2f;

    [Tooltip("Seconds the tutorial text takes to fade from visible to invisible.")]
    public float tutorialFadeTime = 1f;

    private static readonly Vector3[] Directions =
    {
        Vector3.up, Vector3.down, Vector3.right, Vector3.left, Vector3.forward, Vector3.back,
    };

    private Rigidbody rb;
    private Collider bodyCollider;
    private Vector3 baseScale;
    private float baseMass;
    private float factor = 1f;

    private readonly Collider[] overlapBuffer = new Collider[16];
    private readonly Collider[] probeBuffer = new Collider[16];

    private bool separationImpossible;
    private Collider[] carrier;

    public bool IsGrowthBlocked { get; private set; }

    public void SetCarrier(Collider[] colliders)
    {
        carrier = colliders;
    }

    private void Awake()
    {
        baseScale = transform.localScale;
        rb = GetComponent<Rigidbody>();
        bodyCollider = GetComponentInChildren<Collider>();
        if (rb != null)
        {
            baseMass = rb.mass;
        }

        if (tutorialText != null)
        {
            tutorialText.gameObject.SetActive(isTutorialObject && !isTutorialComplete);
        }

        UpdateDebugText();
    }

    public int ApplyScale(float delta)
    {
        float previous = factor;
        float desired = Mathf.Clamp(factor + delta, minFactor, maxFactor);

        float applied = desired - previous;
        if (Mathf.Approximately(applied, 0f))
        {
            IsGrowthBlocked = false;
            return 0;
        }

        Vector3 previousScale = transform.localScale;
        Vector3 previousPosition = transform.position;

        factor = desired;
        transform.localScale = baseScale * factor;
        Physics.SyncTransforms();

        // Grow first and shove clear afterwards, so resting on the floor doesn't
        // count as being blocked. Shrinking can't create an overlap.
        if (applied > 0f && !ResolveOverlap())
        {
            factor = previous;
            transform.localScale = previousScale;
            transform.position = previousPosition;
            Physics.SyncTransforms();

            IsGrowthBlocked = true;
            return 0;
        }

        IsGrowthBlocked = false;

        if (rb != null)
        {
            rb.mass = baseMass * factor;

            if (!rb.isKinematic)
            {
                rb.WakeUp();
            }
        }

        UpdateDebugText();

        return applied > 0f ? 1 : -1;
    }

    private bool ResolveOverlap()
    {
        if (bodyCollider == null)
        {
            return true;
        }

        Vector3 start = transform.position;

        for (int pass = 0; pass < growthResolvePasses; pass++)
        {
            Vector3 push = ComputeSeparation();
            if (separationImpossible)
            {
                return false;
            }

            if (push == Vector3.zero)
            {
                return true;
            }

            transform.position += push;
            Physics.SyncTransforms();

            if ((transform.position - start).sqrMagnitude > maxGrowthPush * maxGrowthPush)
            {
                return false;
            }
        }

        return ComputeSeparation() == Vector3.zero && !separationImpossible;
    }

    private Vector3 ComputeSeparation()
    {
        separationImpossible = false;

        Bounds bounds = bodyCollider.bounds;
        Transform body = bodyCollider.transform;
        Vector3 push = Vector3.zero;

        int count = Physics.OverlapBoxNonAlloc(
            bounds.center, bounds.extents, overlapBuffer,
            Quaternion.identity, blockingMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapBuffer[i];

            if (hit == null || hit.transform.IsChildOf(transform) || hit.CompareTag("Player") || IsWater(hit))
            {
                continue;
            }

            // ComputePenetration can't take a concave mesh, which is what the
            // greybox level is built from.
            push += hit is MeshCollider { convex: false } ? EscapeCollider(hit) : Penetration(hit, body);
        }

        // The carrier's collision with this is switched off and its layer is
        // outside blockingMask, so it never shows up in the query above.
        if (carrier != null)
        {
            foreach (Collider held in carrier)
            {
                if (held != null && held is not CharacterController && !held.transform.IsChildOf(transform))
                {
                    push += Penetration(held, body);
                }
            }
        }

        return push;
    }

    private Vector3 Penetration(Collider other, Transform body)
    {
        if (Physics.ComputePenetration(
                bodyCollider, body.position, body.rotation,
                other, other.transform.position, other.transform.rotation,
                out Vector3 direction, out float distance)
            && distance > growthSkin)
        {
            return direction * (distance - growthSkin);
        }

        return Vector3.zero;
    }

    // A concave mesh's bounding box is mostly fresh air - the chamber floor is one
    // 4.5m slab with the pool carved out of it - so the way out has to be measured
    // against the real triangles: step along each axis and keep the shortest move
    // that comes free.
    private Vector3 EscapeCollider(Collider hit)
    {
        Bounds bounds = bodyCollider.bounds;
        Vector3 probe = Vector3.Max(bounds.extents - Vector3.one * growthSkin, Vector3.one * 0.001f);

        if (!Touches(bounds.center, probe, hit))
        {
            return Vector3.zero;
        }

        Vector3 best = Vector3.zero;
        float shortest = float.MaxValue;

        foreach (Vector3 direction in Directions)
        {
            if (Touches(bounds.center + direction * maxGrowthPush, probe, hit))
            {
                continue;
            }

            float stuck = 0f;
            float free = maxGrowthPush;
            for (int step = 0; step < 6; step++)
            {
                float middle = (stuck + free) * 0.5f;
                if (Touches(bounds.center + direction * middle, probe, hit))
                {
                    stuck = middle;
                }
                else
                {
                    free = middle;
                }
            }

            if (free < shortest)
            {
                shortest = free;
                best = direction * free;
            }
        }

        if (shortest == float.MaxValue)
        {
            separationImpossible = true;
        }

        return best;
    }

    private bool Touches(Vector3 centre, Vector3 extents, Collider collider)
    {
        int count = Physics.OverlapBoxNonAlloc(
            centre, extents, probeBuffer,
            Quaternion.identity, blockingMask, QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            if (probeBuffer[i] == collider)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsWater(Collider collider)
    {
        return collider.GetComponentInParent<Water>(true) != null
            || collider.gameObject.layer == LayerMask.NameToLayer("Water");
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

    public void CompleteTutorial()
    {
        if (isTutorialObject && !isTutorialComplete)
        {
            if (tutorialText != null)
            {
                StartCoroutine(FadeOutTutorialText());
            }
            isTutorialComplete = true;
        }
    }

    private IEnumerator FadeOutTutorialText()
    {
        yield return new WaitForSeconds(tutorialHoldTime);

        tutorialText.text = "Scroll to SCALE";

        yield return new WaitForSeconds(tutorialHoldTime);

        float startAlpha = tutorialText.alpha;
        float elapsed = 0f;

        while (elapsed < tutorialFadeTime)
        {
            elapsed += Time.deltaTime;
            tutorialText.alpha = Mathf.Lerp(startAlpha, 0f, elapsed / tutorialFadeTime);
            yield return null;
        }

        tutorialText.alpha = 0f;
        tutorialText.gameObject.SetActive(false);
    }
}
