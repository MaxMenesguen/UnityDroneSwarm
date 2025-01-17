using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidRotationLock : MonoBehaviour
{
    public float rotationSpeed = 100f; // Adjust as needed for smooth rotation
    
    void Update()
    {
        // Get parent's rotation in Euler angles for easy manipulation
        Vector3 parentRotationEuler = transform.parent.rotation.eulerAngles;

        // Create a target rotation that matches the parent's yaw but keeps pitch and roll at 0
        Quaternion targetRotation = Quaternion.Euler(20, parentRotationEuler.y , 0);

        transform.rotation = targetRotation;

        // Option 1: Smoothly interpolate towards the target rotation using Lerp
        //transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);

        // Option 2: Rotate towards the target rotation at a fixed step
        // transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);



    }
}
