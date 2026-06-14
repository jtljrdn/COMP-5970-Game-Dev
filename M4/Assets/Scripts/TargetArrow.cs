using UnityEngine;

public class TargetArrow : MonoBehaviour
{
    public Transform player;
    public Transform target;
    public Transform package;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (player == null || target == null)
        {
            return;
        }

        CarController carController = player.GetComponent<CarController>();
        Vector2 direction;

        if (carController.hasPackage && package != null)
        {
             direction = target.position - player.position;
        } else {
             direction = package.position - player.position;
        }

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }
}
