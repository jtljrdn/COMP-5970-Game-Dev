using UnityEngine;

public class SawMover : MonoBehaviour
{
    public Transform pointA;
    public Transform pointB;
    public float speed = 1f;
    public float spinSpeed = 180f;
    public bool startAtPointA = true;

    void Update()
    {
        float t = (Mathf.Sin(Time.time * speed) + 1f) * 0.5f;
        if (!startAtPointA) t = 1f - t;
        transform.position = Vector3.Lerp(pointA.position, pointB.position, t);
        transform.Rotate(spinSpeed * Time.deltaTime * Vector3.forward);
    }
}