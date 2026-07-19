using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class PressurePlate : MonoBehaviour
{
    public GameObject door;
    public Transform buttonObject;

    public float pressDepth = 0.05f;

    public float moveSpeed = 0.5f;

    public float releaseDelay = 1.0f;

    [Tooltip("Total mass that must rest on the plate before it presses. Set to 0 to ignore weight entirely - anything at all will press it.")]
    public float requiredMass = 1.0f;

    [Tooltip("Mass the player counts for. The player is a CharacterController, so it has no Rigidbody to read a real mass from.")]
    public float playerMass = 70f;

    private Vector3 restPosition;
    private Vector3 pressedPosition;
    private Vector3 targetPosition;
    private Coroutine resetRoutine;

    // Everything currently sitting on the plate. Masses are summed fresh every
    // FixedUpdate rather than sampled on entry, so scaling an object while it
    // rests here updates the plate immediately.
    private readonly HashSet<Rigidbody> occupants = new();
    private int playerCount;
    private bool isPressed;

    void Start()
    {
        // Capture the button's up position once, and derive the down position from it.
        restPosition = buttonObject.localPosition;
        pressedPosition = restPosition + Vector3.down * pressDepth;
        targetPosition = restPosition;
    }

    void Update()
    {
        // Ease toward whichever position is currently the target.
        buttonObject.localPosition = Vector3.MoveTowards(
            buttonObject.localPosition, targetPosition, moveSpeed * Time.deltaTime);
    }

    private void FixedUpdate()
    {
        bool shouldBePressed = ShouldBePressed();
        if (shouldBePressed == isPressed)
        {
            return;
        }

        isPressed = shouldBePressed;
        if (isPressed)
        {
            Press();
        }
        else
        {
            Release();
        }
    }

    private bool ShouldBePressed()
    {
        // Objects can be destroyed without ever firing OnTriggerExit.
        occupants.RemoveWhere(body => body == null);

        bool occupied = playerCount > 0 || occupants.Count > 0;
        if (!occupied)
        {
            return false;
        }

        // requiredMass 0 switches weight detection off: the plate is a plain
        // "is something on me" button, whatever size that something happens to be.
        if (requiredMass <= 0f)
        {
            return true;
        }

        return CurrentMass() >= requiredMass;
    }

    // Sum of everything resting on the plate right now.
    private float CurrentMass()
    {
        float total = playerCount > 0 ? playerMass : 0f;
        foreach (Rigidbody body in occupants)
        {
            total += body.mass;
        }

        return total;
    }

    private void Press()
    {
        if (resetRoutine != null)
        {
            StopCoroutine(resetRoutine);
            resetRoutine = null;
        }

        targetPosition = pressedPosition;
        door.GetComponent<Door>().OpenDoor();
        Debug.Log("Pressure plate activated!");
    }

    private void Release()
    {
        resetRoutine = StartCoroutine(ResetButtonPosition());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerCount++;
            return;
        }

        if (!other.CompareTag("Physics"))
        {
            return;
        }

        // The collider is often on a child of the object that owns the Rigidbody.
        Rigidbody body = other.GetComponentInParent<Rigidbody>();
        if (body != null)
        {
            occupants.Add(body);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerCount = Mathf.Max(0, playerCount - 1);
            return;
        }

        if (!other.CompareTag("Physics"))
        {
            return;
        }

        Rigidbody body = other.GetComponentInParent<Rigidbody>();
        if (body != null)
        {
            occupants.Remove(body);
        }
    }

    private IEnumerator ResetButtonPosition()
    {
        yield return new WaitForSeconds(releaseDelay);
        targetPosition = restPosition;
        resetRoutine = null;
        door.GetComponent<Door>().CloseDoor();
        Debug.Log("Pressure plate deactivated!");
    }
}
