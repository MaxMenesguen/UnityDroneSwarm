using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestDroneController : MonoBehaviour
{
    public string droneIP { get; set; }

    public Vector3 pointA = new Vector3(0, 1, 0); // Starting point
    public Vector3 pointB = new Vector3(1, 1, 0); // Target point
    public float speed = 0.1f; // Movement speed in units/second
    public float thresholdRadius = 0.1f; // Radius around the target to consider "reached"
    public float SteeringSpeed = 100f;

    private Vector3 currentTarget; // Persistent target
    private bool movingToB = false; // State: Moving to Point B or Point A

    void Start()
    {
        // Set the initial target to point A
        currentTarget = pointA;
    }

    public List<float> SimulateMovement(List<BoidController> other, float sizeOfBoidBoundingBox, float time)
    {
        List<float> returnVariables;
        Vector3 velocity = Vector3.zero; // To store velocity
        float angularVelocity = 0f; // To store angular velocity

        // Check if the drone is close enough to the current target
        if (Vector3.Distance(transform.position, currentTarget) < thresholdRadius)
        {
            if (!movingToB)
            {
                Debug.Log("Reached Point A, moving to Point B");
                currentTarget = pointB;
                movingToB = true;
            }
            else
            {
                Debug.Log("Reached Point B, stopping");
                return new List<float> { 0, 0, 0, 0, transform.rotation.eulerAngles.x, transform.rotation.eulerAngles.z }; // Stop movement
            }
        }

        // Calculate direction to the target
        Vector3 direction = (currentTarget - transform.position).normalized;

        // Adjust rotation towards the target
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion previousRotation = transform.rotation;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, SteeringSpeed * time);

            // Calculate angular velocity
            Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(previousRotation);
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            angularVelocity = angle / time;
        }

        // Update position towards the target
        velocity = direction * speed * time;
        transform.position += velocity;

        // Return calculated values
        returnVariables = new List<float>
        {
            velocity.x,
            velocity.y,
            velocity.z,
            angularVelocity,
            transform.rotation.eulerAngles.x,
            transform.rotation.eulerAngles.z
        };

        return returnVariables;
    }
}
