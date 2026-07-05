using UnityEngine;
using UnityEngine.InputSystem;

// The core mechanic. Put this on the player. Every frame it casts a ray from the
// centre of the camera; if it hits a Scalable object within range, holding the
// grow/shrink keys (or scrolling the mouse wheel) resizes it.
//
// Input is handled through the new Input System via InputActions defined in code,
// so it needs no wiring in the Input Actions editor and stays independent of the
// shared StarterAssets input map.
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

    [Tooltip("How snappily a held object follows the view. Higher = tighter, lower = floatier.")]
    public float followSharpness = 15f;

    [Tooltip("Gentle forward toss applied when dropping (only if the object uses gravity).")]
    public float dropForce = 2f;

    // The Scalable currently under the crosshair (null if none).
    public Scalable Target { get; private set; }

    // Grow/shrink axis (+1 grow, -1 shrink) and the mouse wheel, as Input System actions.
    private InputAction scaleAction;
    private InputAction scrollAction;

    private InputAction pickupAction;

    // Pickup state.
    private Scalable heldObject;
    private Rigidbody heldBody;
    private bool heldOriginalKinematic;
    private bool heldOriginalGravity;

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

        Ray ray = aimCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
        if (Physics.Raycast(ray, out RaycastHit hit, range, hitMask, QueryTriggerInteraction.Ignore))
        {
            found = hit.collider.GetComponentInParent<Scalable>();
        }

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
        // F toggles: drop what we're holding, otherwise pick up what we're aiming at.
        if (pickupAction.triggered)
        {
            if (IsHoldingObject)
            {
                Drop();
            }
            else if (Target != null)
            {
                PickUp(Target);
            }
        }

        if (IsHoldingObject)
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
            heldBody.isKinematic = true;
            heldBody.useGravity = false;
        }

        Debug.Log("Picked up: " + obj.gameObject.name);
    }

    private void MoveHeldObject()
    {
        Transform cam = aimCamera.transform;
        Vector3 targetPosition = cam.position + cam.forward * holdDistance;

        float t = 1f - Mathf.Exp(-followSharpness * Time.deltaTime);
        heldObject.transform.position = Vector3.Lerp(heldObject.transform.position, targetPosition, t);
        heldObject.transform.rotation = Quaternion.Slerp(heldObject.transform.rotation, cam.rotation, t);
    }

    private void Drop()
    {
        if (heldBody != null)
        {
            heldBody.isKinematic = heldOriginalKinematic;
            heldBody.useGravity = heldOriginalGravity;

            // Give it a gentle push forward if it's a normal physics object.
            if (!heldBody.isKinematic)
            {
                heldBody.AddForce(aimCamera.transform.forward * dropForce, ForceMode.VelocityChange);
            }
        }

        Debug.Log("Dropped: " + heldObject.gameObject.name);
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
