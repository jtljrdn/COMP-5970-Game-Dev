using UnityEngine;

public class CursorLock : MonoBehaviour
{
   public bool lockOnStart = true;

    private void Start()
    {
        if (lockOnStart)
        {
            SetLocked(true);
        }
    }
    public void SetLocked(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
