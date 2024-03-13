using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidController : MonoBehaviour
{
    public int SwarmIndex { get; set; }
    public float NoClumpingRadius;
    public float LocalAreaRadius;
    public float Speed;
    public float SteeringSpeed;
    public float distanceToObstacleDetection;
    
    Vector3 avoidanceDirection;
    

    public void SimulateMovement(List<BoidController> other, float time)
    {
        //default vars
        var steering = Vector3.zero;
        //separation vars
        Vector3 separationDirection = Vector3.zero;
        Vector3 alignmentDirection = Vector3.zero;
        Vector3 cohesionDirection = Vector3.zero;
        avoidanceDirection = Vector3.zero;

        int separationCount = 0;
        int alignmentCount = 0;
        int cohesionCount = 0;
        int contactCount = 0;
        



        foreach (BoidController boid in other)
        {
            //skip self
            if (boid == this)
                continue;

            var distance = Vector3.Distance(boid.transform.position, this.transform.position);

            //identify local neighbour
            if (distance < NoClumpingRadius)
            {
                separationDirection += boid.transform.position - transform.position;
                separationCount++;
            }
            //identify local neighbour
            if (distance < LocalAreaRadius)
            {
                alignmentDirection += boid.transform.forward;
                alignmentCount++;
            }
            if (distance < LocalAreaRadius)
            {
                cohesionDirection += boid.transform.position - transform.position;
                cohesionCount++;
            }
        }
        Vector3[] directions = { transform.forward, transform.up, transform.right, -transform.right, -transform.up };
        RaycastHit hitInfo;

        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(transform.position, dir, out hitInfo, distanceToObstacleDetection, LayerMask.GetMask("ObstacleLayer")))
            {
                Vector3 hitNormal = hitInfo.normal;
                //Debug.DrawLine(hitInfo.point, hitInfo.point + hitNormal * 2, Color.green, 2f);
                avoidanceDirection += hitNormal;
                contactCount++;
            }
        }
        if (contactCount > 0)
            avoidanceDirection /= contactCount;
        
        
        

        //calculate average
        if (separationCount > 0)
            separationDirection /= separationCount;
        if (alignmentCount > 0)
            alignmentDirection /= alignmentCount;
        if (cohesionCount > 0)
            cohesionDirection /= cohesionCount;

        //flip and normalize
        /*separationDirection = -separationDirection.normalized;
        alignmentDirection = alignmentDirection.normalized;
        cohesionDirection = cohesionDirection.normalized;*/
        //apply to steering
        

        if (avoidanceDirection != Vector3.zero)
        {
            
            steering += avoidanceDirection ;
            steering += -separationDirection.normalized * 0.5f;
            steering += alignmentDirection.normalized * 0.34f;
            steering += cohesionDirection.normalized * 0.16f;
        }
        else
        {
            steering += -separationDirection.normalized * 0.5f;
            steering += alignmentDirection.normalized * 0.34f;
            steering += cohesionDirection.normalized * 0.16f;
        }
        //apply steering
        if (steering != Vector3.zero)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(steering), SteeringSpeed * time);
            
        }




        //move 
        transform.position += transform.TransformDirection(new Vector3(0, 0, Speed)) * time;//transfor.front * time;?
        //debug
        /*Debug.DrawLine(transform.position, transform.position + transform.forward * distanceToObstacleDetection, Color.red);
        Debug.DrawLine(transform.position, transform.position + transform.up * distanceToObstacleDetection, Color.blue);
        Debug.DrawLine(transform.position, transform.position + transform.right * distanceToObstacleDetection, Color.blue);
        Debug.DrawLine(transform.position, transform.position - transform.up * distanceToObstacleDetection, Color.blue);
        Debug.DrawLine(transform.position, transform.position - transform.right * distanceToObstacleDetection, Color.blue);
*/

    }
}