using System.Collections;
using UnityEngine;
using UnityEngine.Events;

// A push button on a pedestal. The cap sinks into the pedestal on press, fires
// whatever it is wired to once it bottoms out, then springs back so it can be
// pressed again.
public class ButtonPedestal : Interactable
{
    [Tooltip("The cap that sinks into the pedestal. Leave empty to use the child named Button.")]
    public Transform button;

    [Tooltip("How far the cap sinks, in the pedestal's local units.")]
    public float pressDepth = 0.06f;

    [Tooltip("How fast the cap travels, in local units per second.")]
    public float moveSpeed = 0.4f;

    [Tooltip("Seconds the cap stays down before springing back. The button ignores presses until it is back up.")]
    public float holdTime = 0.35f;

    [Header("Output")]
    [Tooltip("Dropper that dispenses a fresh object on every press.")]
    public ObjectDropper dropper;

    [Tooltip("Anything else that should fire when the button bottoms out.")]
    public UnityEvent onPressed;

    [Header("Audio")]
    [Tooltip("Leave empty to use the AudioSource on this object. Clips are optional - the button works silently without them.")]
    public AudioSource audioSource;

    [Tooltip("Played as the cap starts sinking.")]
    public AudioClip pressClip;

    [Tooltip("Played as the cap starts springing back.")]
    public AudioClip releaseClip;

    private Vector3 restPosition;
    private Vector3 pressedPosition;

    // True from the moment the cap starts sinking until it is fully back up.
    private bool busy;

    private void Awake()
    {
        if (button == null)
        {
            button = transform.Find("Button");
        }

        if (button == null)
        {
            Debug.LogError($"{name}: no button cap assigned, so nothing can move. Disabling.", this);
            enabled = false;
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        // The cap's authored position is the up position; down is derived from it.
        restPosition = button.localPosition;
        pressedPosition = restPosition + Vector3.down * pressDepth;
    }

    public override void Interact()
    {
        if (busy || !enabled)
        {
            return;
        }

        StartCoroutine(PressRoutine());
    }

    private IEnumerator PressRoutine()
    {
        busy = true;

        Play(pressClip);
        yield return MoveCap(pressedPosition);

        // Firing at the bottom of the travel rather than on keypress keeps the
        // sound, the animation and the drop reading as one event.
        if (dropper != null)
        {
            dropper.Dispense();
        }

        onPressed?.Invoke();

        yield return new WaitForSeconds(holdTime);

        Play(releaseClip);
        yield return MoveCap(restPosition);

        busy = false;
    }

    private IEnumerator MoveCap(Vector3 target)
    {
        while (button.localPosition != target)
        {
            button.localPosition = Vector3.MoveTowards(
                button.localPosition, target, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }

    private void Play(AudioClip clip)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip);
    }
}
