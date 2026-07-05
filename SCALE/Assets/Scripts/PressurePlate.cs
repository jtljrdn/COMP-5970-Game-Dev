using UnityEngine;
using System.Collections;

public class PressurePlate : MonoBehaviour
{
    public GameObject door;
    public Transform buttonObject;

    public float pressDepth = 0.05f;

    public float moveSpeed = 0.5f;

    public float releaseDelay = 1.0f;

    private Vector3 restPosition;
    private Vector3 pressedPosition;
    private Vector3 targetPosition;
    private Coroutine resetRoutine;

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

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Physics"))
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
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") || other.CompareTag("Physics"))
        {
            resetRoutine = StartCoroutine(ResetButtonPosition());
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
