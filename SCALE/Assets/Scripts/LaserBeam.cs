using UnityEngine;

public class LaserBeam : MonoBehaviour
{
    public float maxRange = 30f;

    public float beamRadius = 0.05f;

    public float originOffset = 0.2f;
    public LayerMask blockingMask = ~0;

    public bool killsPlayer = true;

    public float onDuration = 2f;

    [Tooltip("Seconds the beam stays off. Set to 0 for a beam that never blinks.")]
    public float offDuration = 0f;

    public LineRenderer beam;

    [Tooltip("Optional: glow moved to wherever the beam terminates.")]
    public Transform impactGlow;

    [Tooltip("Optional: looping hum, silenced while the beam is off.")]
    public AudioSource humAudio;

    [Tooltip("Optional: played once when the beam kills the player.")]
    public AudioClip zapClip;

    [Header("Editor")]
    public Color gizmoColor = new(1f, 0.2f, 0.2f, 0.8f);

    // Set false by a switch, plate, or anything else that should cut the beam.
    public bool IsPowered { get; private set; } = true;

    // True when the beam is actually emitting right now (powered and not mid-blink).
    public bool IsEmitting { get; private set; } = true;

    private float phaseTimer;

    private void Awake()
    {
        if (beam == null)
        {
            beam = GetComponentInChildren<LineRenderer>();
        }

        if (beam != null)
        {
            beam.useWorldSpace = true;
            beam.positionCount = 2;
        }

        phaseTimer = onDuration;
        IsEmitting = true;
    }

    private void FixedUpdate()
    {
        UpdateBlink();

        if (!IsEmitting)
        {
            ShowBeam(false);
            return;
        }

        Vector3 origin = transform.position + transform.forward * originOffset;
        Vector3 end = origin + transform.forward * maxRange;

        // Triggers must be ignored or the pressure plate and door volumes would
        // stop the beam dead in mid-air.
        bool blocked = beamRadius > 0f
            ? Physics.SphereCast(origin, beamRadius, transform.forward, out RaycastHit hit,
                maxRange, blockingMask, QueryTriggerInteraction.Ignore)
            : Physics.Raycast(origin, transform.forward, out hit,
                maxRange, blockingMask, QueryTriggerInteraction.Ignore);

        if (blocked)
        {
            // A spherecast that starts already overlapping reports distance 0 and a
            // meaningless point, so fall back to the geometric endpoint.
            end = hit.distance > 0f ? hit.point : origin;

            if (killsPlayer && hit.collider.CompareTag("Player"))
            {
                Kill();
            }
        }

        ShowBeam(true);
        if (beam != null)
        {
            beam.SetPosition(0, origin);
            beam.SetPosition(1, end);
        }

        if (impactGlow != null)
        {
            impactGlow.gameObject.SetActive(blocked);
            impactGlow.position = end;
        }
    }

    private void UpdateBlink()
    {
        if (!IsPowered)
        {
            IsEmitting = false;
            return;
        }

        // offDuration 0 means a beam that is simply always on.
        if (offDuration <= 0f)
        {
            IsEmitting = true;
            return;
        }

        phaseTimer -= Time.fixedDeltaTime;
        if (phaseTimer <= 0f)
        {
            IsEmitting = !IsEmitting;
            phaseTimer = IsEmitting ? onDuration : offDuration;
        }
    }

    private void ShowBeam(bool visible)
    {
        if (beam != null && beam.enabled != visible)
        {
            beam.enabled = visible;
        }

        if (impactGlow != null && !visible && impactGlow.gameObject.activeSelf)
        {
            impactGlow.gameObject.SetActive(false);
        }

        if (humAudio != null)
        {
            if (visible && !humAudio.isPlaying)
            {
                humAudio.Play();
            }
            else if (!visible && humAudio.isPlaying)
            {
                humAudio.Pause();
            }
        }
    }

    private void Kill()
    {
        if (GameManager.Instance == null || GameManager.Instance.IsGameOver)
        {
            return;
        }

        if (zapClip != null)
        {
            AudioSource.PlayClipAtPoint(zapClip, transform.position);
        }

        GameManager.Instance.PlayerDied();
    }

    // Hook a pressure plate or switch up to these to make lasers part of a puzzle.
    public void SetPowered(bool powered)
    {
        IsPowered = powered;
        if (!powered)
        {
            IsEmitting = false;
            ShowBeam(false);
        }
        else
        {
            phaseTimer = onDuration;
            IsEmitting = true;
        }
    }

    public void PowerOn() => SetPowered(true);

    public void PowerOff() => SetPowered(false);

    // Lets emitters be aimed in the scene view without entering play mode.
    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Vector3 origin = transform.position + transform.forward * originOffset;
        Vector3 end = origin + transform.forward * maxRange;

        Gizmos.DrawLine(origin, end);
        Gizmos.DrawWireSphere(origin, Mathf.Max(0.01f, beamRadius));
        Gizmos.DrawWireSphere(end, Mathf.Max(0.01f, beamRadius));
    }
}
