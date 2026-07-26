using UnityEngine;

// Anything the player can trigger by looking at it and pressing the interact key.
// ScaleTool already raycasts from the crosshair every frame, so it does the aiming
// and an interactable only has to say what happens.
public abstract class Interactable : MonoBehaviour
{
    public abstract void Interact();
}
