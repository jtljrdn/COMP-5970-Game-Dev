using UnityEngine;

public class Door : MonoBehaviour
{
    public GameObject door;
    public BoxCollider doorTrigger;
    public Material closedMaterial;
    public Material openMaterial;

    private Renderer doorRenderer;

    public void OpenDoor()
    {
        doorRenderer.material = openMaterial;
        doorTrigger.enabled = true;
    }

    public void CloseDoor()
    {
        doorRenderer.material = closedMaterial;
        doorTrigger.enabled = false;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        doorRenderer = door.GetComponent<Renderer>();
        doorTrigger.enabled = false;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.GameOver();
            }
            Debug.Log("Player entered the door trigger!");
        }
    }
}
