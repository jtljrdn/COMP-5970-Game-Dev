using UnityEngine;
using UnityEngine.InputSystem;

public class ScaleTool : MonoBehaviour
{
    [Header("Aiming")]
    [Tooltip("Camera to aim from. Leave empty to use the main camera.")]
    public Camera aimCamera;

    [Tooltip("How far the player can reach to scale an object.")]
    public float range = 8f;

    [Tooltip("Layers the ray can hit. Set to Everything if unsure.")]
    public LayerMask hitMask = ~0;

    [Header("Scaling")]
    [Tooltip("How fast objects grow/shrink while holding a key (per second).")]
    public float scaleSpeed = 1.5f;

    [Tooltip("How much one mouse-wheel notch changes the size.")]
    public float scrollStep = 0.25f;

    [Header("Feedback")]
    [Tooltip("Optional: played once per scale direction change. Pitch shifts up when growing, down when shrinking.")]
    public AudioSource scaleAudio;

    [Tooltip("Optional: target object is tinted this colour while aimed at.")]
    public Color highlightColor = new(1f, 0.85f, 0.3f);

    [Header("Pickup")]
    [Tooltip("How far in front of the camera a held object floats.")]
    public float holdDistance = 2.5f;

    [Tooltip("Closest a held object is allowed to come when something blocks the hold point.")]
    public float minHoldDistance = 1f;

    [Tooltip("How snappily a held object follows the view. Higher = tighter, lower = floatier.")]
    public float followSharpness = 15f;

    [Tooltip("Speed cap while carrying. Keeps a held object from being flung through thin geometry.")]
    public float maxHoldSpeed = 12f;

    [Tooltip("Turn-rate cap while carrying, in radians per second.")]
    public float maxHoldAngularSpeed = 20f;

    [Tooltip("Gentle forward toss applied when dropping (only if the object uses gravity).")]
    public float dropForce = 2f;

    [Header("Interaction")]
    [Tooltip("How close the player must be to press a button. Shorter than the scaling range so buttons can't be hit across the room.")]
    public float interactRange = 3.5f;

    // The Scalable currently under the crosshair (null if none).
    public Scalable Target { get; private set; }

    // The Interactable currently under the crosshair and within reach (null if none).
    public Interactable InteractTarget { get; private set; }

    // Grow/shrink axis (+1 grow, -1 shrink) and the mouse wheel, as Input System actions.
    private InputAction scaleAction;
    private InputAction scrollAction;

    private InputAction pickupAction;


    private Scalable heldObject;
    private Rigidbody heldBody;
    private bool heldOriginalKinematic;
    private bool heldOriginalGravity;
    private CollisionDetectionMode heldOriginalDetection;
    private RigidbodyInterpolation heldOriginalInterpolation;

    private Collider[] playerColliders;

    private readonly RaycastHit[] holdHits = new RaycastHit[8];

    public bool IsHoldingObject => heldObject != null;

    private Scalable highlighted;
    private Color highlightBaseColor;
    private Renderer highlightRenderer;
    private int lastDirection;

    private void Awake()
    {
        if (aimCamera == null)
        {
            aimCamera = Camera.main;
        }

        playerColliders = transform.root.GetComponentsInChildren<Collider>();

        // Hold E to grow, Q to shrink, resolved as a -1..1 axis.
        scaleAction = new InputAction("Scale", InputActionType.Value);
        scaleAction.AddCompositeBinding("1DAxis")
            .With("Positive", "<Keyboard>/e")
            .With("Negative", "<Keyboard>/q");

        // Mouse wheel for discrete notch adjustments.
        scrollAction = new InputAction("ScaleScroll", InputActionType.Value, "<Mouse>/scroll/y");

        pickupAction = new InputAction("Pickup", InputActionType.Button, "<Keyboard>/f");
    }

    private void OnEnable()
    {
        scaleAction.Enable();
        scrollAction.Enable();
        pickupAction.Enable();
    }

    private void OnDisable()
    {
        scaleAction.Disable();
        scrollAction.Disable();
        pickupAction.Disable();
    }

    private void OnDestroy()
    {
        scaleAction.Dispose();
        scrollAction.Dispose();
        pickupAction.Dispose();
    }

    private void Update()
    {
        if (aimCamera == null)
        {
            return;
        }

        UpdateTarget();
        HandleScaling();
        HandlePickup();
    }

    private void UpdateTarget()
    {
        Scalable found = null;
        Interactable interactable = null;

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            found = hit.collider.GetComponentInParent<Scalable>();

            // A button's collider is usually a child of the object holding the script.
            if (hit.distance <= interactRange)
            {
                interactable = hit.collider.GetComponentInParent<Interactable>();
            }
        }

        InteractTarget = interactable;

        if (found != highlighted)
        {
            ClearHighlight();
            SetHighlight(found);
        }

        Target = found;
    }

    private void HandleScaling()
    {
        if (Target == null)
        {
            lastDirection = 0;
            return;
        }

        // Held keys scale smoothly over time.
        float delta = scaleAction.ReadValue<float>() * scaleSpeed * Time.deltaTime;

        // The wheel adds discrete steps on top.
        float scroll = scrollAction.ReadValue<float>();
        if (scroll > 0f)
        {
            delta += scrollStep;
        }
        else if (scroll < 0f)
        {
            delta -= scrollStep;
        }

        if (Mathf.Approximately(delta, 0f))
        {
            lastDirection = 0;
            return;
        }

        int direction = Target.ApplyScale(delta);
        if (direction != 0 && direction != lastDirection)
        {
            PlayScaleSound(direction);
        }
        lastDirection = direction;
    }

    private void HandlePickup()
    {
        if (!pickupAction.triggered)
        {
            return;
        }

        // Aiming at something usable wins over carrying: pressing a button while
        // holding a cube shouldn't fling the cube on the floor.
        if (InteractTarget != null)
        {
            InteractTarget.Interact();
            return;
        }

        if (IsHoldingObject)
        {
            Drop();
        }
        else if (Target != null)
        {
            PickUp(Target);
            Target.CompleteTutorial();
        }
    }

    private void FixedUpdate()
    {
        // Carrying is driven through the physics engine, so it belongs on the
        // physics tick rather than the render frame.
        if (IsHoldingObject && heldBody != null)
        {
            MoveHeldObject();
        }
    }

    private void PickUp(Scalable obj)
    {
        heldObject = obj;
        heldBody = obj.GetComponent<Rigidbody>();

        if (heldBody != null)
        {
            // Remember the object's real physics settings so we can restore them on drop.
            heldOriginalKinematic = heldBody.isKinematic;
            heldOriginalGravity = heldBody.useGravity;
            heldOriginalDetection = heldBody.collisionDetectionMode;
            heldOriginalInterpolation = heldBody.interpolation;

            // The body stays dynamic while carried so the world still stops it.
            // Gravity is off so it hovers, and continuous detection keeps it from
            // tunnelling when swung quickly past thin geometry.
            heldBody.isKinematic = false;
            heldBody.useGravity = false;
            heldBody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            heldBody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        // A carried object shouldn't shove the person carrying it.
        SetPlayerCollision(obj, false);

        Debug.Log("Picked up: " + obj.gameObject.name);
    }

    private void MoveHeldObject()
    {
        Transform cam = aimCamera.transform;
        Vector3 targetPosition = cam.position + cam.forward * ClearHoldDistance(cam);

        // Steering by velocity rather than writing the transform is what makes the
        // object collide: the physics engine resolves the move instead of us
        // teleporting through whatever happens to be in the way.
        Vector3 toTarget = targetPosition - heldBody.position;
        heldBody.linearVelocity = Vector3.ClampMagnitude(toTarget * followSharpness, maxHoldSpeed);

        // Rotation is steered the same way, as angular velocity.
        Quaternion difference = cam.rotation * Quaternion.Inverse(heldBody.rotation);
        difference.ToAngleAxis(out float angle, out Vector3 axis);
        if (angle > 180f)
        {
            angle -= 360f;
        }

        if (Mathf.Abs(angle) > 0.01f && axis.sqrMagnitude > 0.0001f && !float.IsInfinity(axis.x))
        {
            Vector3 spin = axis.normalized * (angle * Mathf.Deg2Rad * followSharpness);
            heldBody.angularVelocity = Vector3.ClampMagnitude(spin, maxHoldAngularSpeed);
        }
        else
        {
            heldBody.angularVelocity = Vector3.zero;
        }
    }

    // Pulls the hold point in when something solid sits between the camera and it,
    // so walking up to a wall tucks the object in instead of grinding it against
    // the geometry.
    private float ClearHoldDistance(Transform cam)
    {
        float radius = HeldRadius();
        int count = Physics.SphereCastNonAlloc(
            cam.position, radius, cam.forward, holdHits, holdDistance, hitMask, QueryTriggerInteraction.Ignore);

        float nearest = holdDistance;
        for (int i = 0; i < count; i++)
        {
            Transform hit = holdHits[i].collider.transform;

            // The object we're carrying and the player carrying it aren't obstacles.
            if (hit.IsChildOf(heldObject.transform) || hit.IsChildOf(transform.root))
            {
                continue;
            }

            if (holdHits[i].distance < nearest)
            {
                nearest = holdHits[i].distance;
            }
        }

        return Mathf.Max(minHoldDistance, nearest);
    }

    // Half the held object's largest dimension - it rotates with the view, so the
    // widest axis is the one that has to clear.
    private float HeldRadius()
    {
        Collider collider = heldObject.GetComponentInChildren<Collider>();
        if (collider == null)
        {
            return 0.1f;
        }

        Vector3 extents = collider.bounds.extents;
        return Mathf.Max(0.05f, Mathf.Max(extents.x, Mathf.Max(extents.y, extents.z)));
    }

    private void SetPlayerCollision(Scalable obj, bool enabled)
    {
        if (playerColliders == null || obj == null)
        {
            return;
        }

        Collider[] objectColliders = obj.GetComponentsInChildren<Collider>();
        foreach (Collider playerCollider in playerColliders)
        {
            if (playerCollider == null)
            {
                continue;
            }

            foreach (Collider objectCollider in objectColliders)
            {
                if (objectCollider != null)
                {
                    Physics.IgnoreCollision(objectCollider, playerCollider, !enabled);
                }
            }
        }
    }

    private void Drop()
    {
        Scalable dropped = heldObject;

        if (heldBody != null)
        {
            // Detection mode has to go back before isKinematic: a kinematic body
            // can't be left on ContinuousDynamic.
            heldBody.collisionDetectionMode = heldOriginalDetection;
            heldBody.interpolation = heldOriginalInterpolation;
            heldBody.useGravity = heldOriginalGravity;
            heldBody.isKinematic = heldOriginalKinematic;

            // Give it a gentle push forward if it's a normal physics object.
            if (!heldBody.isKinematic)
            {
                heldBody.AddForce(aimCamera.transform.forward * dropForce, ForceMode.VelocityChange);
            }
        }

        SetPlayerCollision(dropped, true);

        Debug.Log("Dropped: " + dropped.gameObject.name);
        heldObject = null;
        heldBody = null;
    }

    private void PlayScaleSound(int direction)
    {
        if (scaleAudio == null)
        {
            return;
        }

        // Higher pitch when growing, lower when shrinking.
        scaleAudio.pitch = direction > 0 ? 1.15f : 0.85f;
        scaleAudio.Play();
    }

    private void SetHighlight(Scalable scalable)
    {
        highlighted = scalable;
        if (scalable == null)
        {
            return;
        }

        highlightRenderer = scalable.GetComponentInChildren<Renderer>();
        if (highlightRenderer != null && highlightRenderer.material.HasProperty("_BaseColor"))
        {
            highlightBaseColor = highlightRenderer.material.GetColor("_BaseColor");
            highlightRenderer.material.SetColor("_BaseColor", highlightBaseColor * highlightColor);
        }
    }

    private void ClearHighlight()
    {
        if (highlightRenderer != null && highlightRenderer.material.HasProperty("_BaseColor"))
        {
            highlightRenderer.material.SetColor("_BaseColor", highlightBaseColor);
        }

        highlighted = null;
        highlightRenderer = null;
    }
}
