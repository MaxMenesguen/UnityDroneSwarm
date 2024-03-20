using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidController : MonoBehaviour
{
    public string droneIP { get; set; }
    public float NoClumpingRadius =  0.30f;
    public float LocalAreaRadius = 0.7f;
    public float Speed = 0.01f;
    public float SteeringSpeed = 100f;
    public float distanceToObstacleDetection = 0.3f;
    public float getBackTOCenterTolerance = 0.05f;
    
    Vector3 avoidanceDirection;
    private Vector3 perlinNoiseSeed;
    private bool isOutOfBound = false;
    Vector3 outOfBound = Vector3.zero;
    void Start()
    {
        // Initialize Perlin noise seed with a random value for each boid
        perlinNoiseSeed = new Vector3(Random.value *100, Random.value * 100, Random.value * 100);
    }
    public List<float> SimulateMovement(List<BoidController> other,float sizeOfBoidBoundingBox, float time)
    {
        List<float> returnVariables ;
        //default vars
        var steering = Vector3.zero;
        //separation vars
        Vector3 separationDirection = Vector3.zero;
        Vector3 alignmentDirection = Vector3.zero;
        Vector3 cohesionDirection = Vector3.zero;
        avoidanceDirection = Vector3.zero;
        Vector3 outOfBound = Vector3.zero;
        

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
        Vector3[] directions = { transform.forward, transform.up, transform.right, -transform.right, -transform.up,
            (transform.forward + transform.up).normalized, (transform.forward - transform.up).normalized,
            (transform.forward + transform.right).normalized, (transform.forward - transform.right).normalized};
        
        RaycastHit hitInfo;

        foreach (Vector3 dir in directions)
        {
            if (Physics.Raycast(transform.position, dir, out hitInfo, distanceToObstacleDetection, LayerMask.GetMask("ObstacleLayer"))&&!isOutOfBound)
            {
                Vector3 hitNormal = hitInfo.normal;
                //Debug.DrawLine(hitInfo.point, hitInfo.point + hitNormal * 2, Color.green, 2f);
                avoidanceDirection += hitNormal;
                contactCount++;
            }
        }
        if (contactCount > 0)
            avoidanceDirection /= contactCount;
        
        if (transform.position.x > (sizeOfBoidBoundingBox/2 + (sizeOfBoidBoundingBox * getBackTOCenterTolerance / 2)) 
            || transform.position.x < -(sizeOfBoidBoundingBox / 2 + (sizeOfBoidBoundingBox * getBackTOCenterTolerance / 2)) 
            || transform.position.y > (sizeOfBoidBoundingBox / 2 + (sizeOfBoidBoundingBox * getBackTOCenterTolerance / 2)) 
            || transform.position.y < -(sizeOfBoidBoundingBox / 2 + (sizeOfBoidBoundingBox * getBackTOCenterTolerance / 2)) 
            || transform.position.z > (sizeOfBoidBoundingBox / 2 + (sizeOfBoidBoundingBox * getBackTOCenterTolerance / 2)) 
            || transform.position.z < -(sizeOfBoidBoundingBox / 2 + (sizeOfBoidBoundingBox * getBackTOCenterTolerance / 2)) )
        {
            outOfBound = -transform.position;
            isOutOfBound = true;
            Debug.Log("Out of bound");
        }
        if (isOutOfBound)
        {
            Debug.Log("Out of bound and true");
            outOfBound = -transform.position;
            if (transform.position.x < (sizeOfBoidBoundingBox/2 - (sizeOfBoidBoundingBox * getBackTOCenterTolerance / 2)) 
                && transform.position.x > -(sizeOfBoidBoundingBox/2 - (sizeOfBoidBoundingBox * getBackTOCenterTolerance / 2)) 
                && transform.position.y < (sizeOfBoidBoundingBox / 2 - (sizeOfBoidBoundingBox * getBackTOCenterTolerance / 2)) 
                && transform.position.y > -(sizeOfBoidBoundingBox / 2 - (sizeOfBoidBoundingBox * getBackTOCenterTolerance / 2)) 
                && transform.position.z < (sizeOfBoidBoundingBox / 2 - (sizeOfBoidBoundingBox * getBackTOCenterTolerance / 2)) 
                && transform.position.z > -(sizeOfBoidBoundingBox / 2 - (sizeOfBoidBoundingBox * getBackTOCenterTolerance / 2)))
            {
                isOutOfBound = false;
                outOfBound = Vector3.zero;
                Debug.Log("in bound and false");
            }

        }



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


        steering += avoidanceDirection;
        steering += -separationDirection.normalized * 0.5f;
        steering += alignmentDirection.normalized * 0.34f;
        steering += cohesionDirection.normalized * 0.16f;
        steering += outOfBound.normalized ;
        // Calculate Perlin noise based movement
        Vector3 perlinDirection = CalculatePerlinDirection();
        //ajust rotation speed
        Vector3 currentDirection = transform.forward; // The current forward direction of the drone
         // Calculate or set your target direction here

        float angleDifference = Vector3.Angle(currentDirection, steering);

        // Optionally, scale the rotationSpeed based on the angleDifference
        // For example, smaller angles could result in a smaller rotationSpeed
        float rotationSpeedModifier = (angleDifference >= 15) ? 1 : Mathf.InverseLerp(15, 0, angleDifference);
        // Base speed, adjust as needed
        float adjustedRotationSpeed = SteeringSpeed * rotationSpeedModifier;

        var previousRotation = transform.rotation;

        //apply steering
        if (!isOutOfBound)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(perlinDirection.normalized), (40 / (1 + 2 * cohesionCount)) * time);
        }
        
        if (steering != Vector3.zero)
        {
            transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(steering), adjustedRotationSpeed * time);

        }
        Vector3 currentRotation = transform.rotation.eulerAngles;
        Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(previousRotation);
        deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
        angle = angle > 180 ? angle - 360 : angle; // Convert angle to range -180 to 180
        float angularVelocity = angle / Time.deltaTime; // In degrees per second
        

        transform.rotation = previousRotation; // Revert rotation !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        //Debug.Log("Angular Velocity :"+angularVelocity);
        
        Vector3 previousPosition = transform.position;
        //move 
        transform.position += transform.TransformDirection(new Vector3(0, 0, Speed)) * time;

        // Calculate velocity
        Vector3 velocity = (transform.position - previousPosition) / Time.deltaTime;
        
        transform.position = previousPosition; // Revert position !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

        Debug.DrawLine(transform.position, transform.position + velocity * distanceToObstacleDetection, Color.green);

        //Debug.Log("Index of boid: " + droneIP);
        returnVariables = new List<float> { velocity.x, velocity.y, velocity.z, angularVelocity,currentRotation.x,currentRotation.z};
        return  returnVariables;

        //debug
        
        //steering = steering.normalized;
        /*Debug.DrawLine(transform.position, transform.position + transform.forward * distanceToObstacleDetection, Color.red);
        Debug.DrawLine(transform.position, transform.position + transform.up * distanceToObstacleDetection, Color.blue);
        Debug.DrawLine(transform.position, transform.position + transform.right * distanceToObstacleDetection, Color.blue);
        Debug.DrawLine(transform.position, transform.position - transform.up * distanceToObstacleDetection, Color.blue);
        Debug.DrawLine(transform.position, transform.position - transform.right * distanceToObstacleDetection, Color.blue);
        Debug.DrawLine(transform.position, transform.position + (transform.forward + transform.up).normalized * distanceToObstacleDetection, Color.blue);
        Debug.DrawLine(transform.position, transform.position + (transform.forward - transform.up).normalized * distanceToObstacleDetection, Color.blue);
        Debug.DrawLine(transform.position, transform.position + (transform.forward + transform.right).normalized * distanceToObstacleDetection, Color.blue);
        Debug.DrawLine(transform.position, transform.position + (transform.forward - transform.right).normalized * distanceToObstacleDetection, Color.blue);

        Debug.DrawLine(transform.position, transform.position + steering * distanceToObstacleDetection, Color.green);*/
    }


    Vector3 CalculatePerlinDirection()
    {
        float time = Time.time;
        // Generate Perlin noise based direction
        float x = Mathf.PerlinNoise(time/2 + perlinNoiseSeed.x, perlinNoiseSeed.x) - 0.5f;
        float y = Mathf.PerlinNoise(time/2 + perlinNoiseSeed.y, perlinNoiseSeed.y) - 0.5f;
        float z = Mathf.PerlinNoise(time/2 + perlinNoiseSeed.z, perlinNoiseSeed.z) - 0.5f;

        // Normalize to ensure consistent movement speed in all directions
        return new Vector3(x, y, z).normalized;
    }
}