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

        // Growing always digs the object into whatever it is resting on, so
        // refusing on contact would leave anything sat on the floor permanently
        // stuck. Instead we grow first and then shove the object clear, and only
        // give up when it is genuinely boxed in. Shrinking can never create a
        // new overlap, so it skips the whole dance.
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

            // A body that had already fallen asleep on the floor won't re-settle
            // against its new size on its own.
            if (!rb.isKinematic)
            {
                rb.WakeUp();
            }
        }

        UpdateDebugText();

        return applied > 0f ? 1 : -1;
    }

    // Pushes the object out of everything it currently intersects. Runs several
    // passes so that a corner (floor plus wall) settles instead of the two
    // surfaces fighting each other. Returns false if the object cannot be freed
    // within maxGrowthPush.
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

        return ComputeSeparation() == Vector3.zero;
    }

    // The offset that would separate this object from everything it overlaps.
    private Vector3 ComputeSeparation()
    {
        Bounds bounds = bodyCollider.bounds;
        int count = Physics.OverlapBoxNonAlloc(
            bounds.center, bounds.extents, overlapBuffer,
            Quaternion.identity, blockingMask, QueryTriggerInteraction.Ignore);

        Transform body = bodyCollider.transform;
        Vector3 push = Vector3.zero;

        for (int i = 0; i < count; i++)
        {
            Collider hit = overlapBuffer[i];

            if (hit == null || hit.transform.IsChildOf(transform) || hit.CompareTag("Player"))
            {
                continue;
            }

            if (SupportsPenetration(hit))
            {
                if (Physics.ComputePenetration(
                        bodyCollider, body.position, body.rotation,
                        hit, hit.transform.position, hit.transform.rotation,
                        out Vector3 direction, out float distance))
                {
                    // Resting bodies always sit a sliver inside the floor, so
                    // shallow contact is left alone and only real intersection
                    // gets pushed out.
                    float depth = distance - growthSkin;
                    if (depth > 0f)
                    {
                        push += direction * depth;
                    }
                }
            }
            else
            {
                push += SeparateBounds(bounds, hit.bounds);
            }
        }

        return push;
    }

    // ComputePenetration cannot handle non-convex mesh colliders, which is what
    // greybox level geometry is built from.
    private static bool SupportsPenetration(Collider collider)
    {
        return collider is not MeshCollider mesh || mesh.convex;
    }

    // Fallback for those mesh colliders: the shortest move that pulls one box
    // out of the other. Level geometry is axis-aligned slabs, so the shallowest
    // axis is reliably the one to back out along - a crate on a wide floor
    // overlaps it barely in Y and hugely in X and Z, so it gets pushed up.
    private Vector3 SeparateBounds(Bounds self, Bounds other)
    {
        Vector3 delta = self.center - other.center;
        Vector3 overlap = self.extents + other.extents
            - new Vector3(Mathf.Abs(delta.x), Mathf.Abs(delta.y), Mathf.Abs(delta.z))
            - Vector3.one * growthSkin;

        if (overlap.x <= 0f || overlap.y <= 0f || overlap.z <= 0f)
        {
            return Vector3.zero;
        }

        if (overlap.y <= overlap.x && overlap.y <= overlap.z)
        {
            return new Vector3(0f, Mathf.Sign(delta.y) * overlap.y, 0f);
        }

        if (overlap.x <= overlap.z)
        {
            return new Vector3(Mathf.Sign(delta.x) * overlap.x, 0f, 0f);
        }

        return new Vector3(0f, 0f, Mathf.Sign(delta.z) * overlap.z);
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
