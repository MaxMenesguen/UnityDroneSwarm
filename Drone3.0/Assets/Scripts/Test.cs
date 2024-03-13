using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    [SerializeField] private Vector3 lookTo;
    [SerializeField] private float SteeringSpeed = 100;
    private Vector3 avoidanceDirection= Vector3.zero;
    bool inContact = false;
    

    // Update is called once per frame
    void Update()
    {
        //avoidanceDirection= Vector3.zero;
        //inContact = false;
        //lookTo = transform.forward;
        RaycastHit hitInfo;
        if (Physics.Raycast(transform.position, transform.forward, out hitInfo, 5f, LayerMask.GetMask("ObstacleLayer")))
        {
            if (!inContact)
            {
                // Invert the direction away from the obstacle
                avoidanceDirection = -(hitInfo.point - transform.position);//normalized
                avoidanceDirection = avoidanceDirection.normalized;
                Debug.Log("hit");
                inContact = true;

            }
            

        }
        else
        {
            avoidanceDirection = Vector3.zero;
            inContact = false;
        }
        Debug.Log("is  Touching ? : " + inContact);
        
        //Debug.Log("Transform rotation: " + transform.rotation);
        //Debug.Log("Transform rotation euler: " + transform.rotation.eulerAngles);
        if (avoidanceDirection != Vector3.zero)
        {
            
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(avoidanceDirection), SteeringSpeed * Time.deltaTime);
            //Debug.Log("Avoidance direction: " + avoidanceDirection);
        }
        else if (lookTo != Vector3.zero)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(lookTo), SteeringSpeed * Time.deltaTime);
        }
        
        Debug.DrawLine(transform.position, transform.position + transform.forward * 5f, Color.red);
    }
}
