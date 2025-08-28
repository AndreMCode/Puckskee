using UnityEngine;

public class RotateObstacle : MonoBehaviour
{
    public float rotationSpeed = 45f; // Degrees per second

    void Start()
    {
        
    }

    void Update()
    {
        transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);

        if (transform.rotation.eulerAngles.y >= 360f)
        {
            transform.rotation = Quaternion.Euler(transform.rotation.eulerAngles.x, 0f, transform.rotation.eulerAngles.z);
        }
    }
}
