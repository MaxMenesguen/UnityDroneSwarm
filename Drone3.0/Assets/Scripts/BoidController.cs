using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoidController : MonoBehaviour
{
    public string droneIP { get; set; }
    public float NoClumpingRadius = 0.30f;
    public float LocalAreaRadius = 0.7f;
    public float Speed = 0.01f;
    public float SteeringSpeed = 100f;
    public float distanceToObstacleDetection = 0.3f;
    public float getBackTOCenterTolerance = 0.05f;

    Vector3 avoidanceDirection;
    private Vector3 perlinNoiseSeed;
    private bool isOutOfBound = false;
    Vector3 outOfBound = Vector3.zero;

    //Tag : AtractionGameObject
    void Start()
    {
        // Initialize Perlin noise seed with a random value for each boid
        perlinNoiseSeed = new Vector3(Random.value * 100, Random.value * 100, Random.value * 100);
        // Set the initial target to point A
        currentTarget = pointA;
    }
    public List<float> SimulateMovement(List<BoidController> other, Vector3[] corners, float height, float time)
    {
        List<float> returnVariables;
        //default vars
        var steering = Vector3.zero;
        //separation vars
        Vector3 separationDirection = Vector3.zero;
        Vector3 alignmentDirection = Vector3.zero;
        Vector3 cohesionDirection = Vector3.zero;
        avoidanceDirection = Vector3.zero;
        Vector3 outOfBound = Vector3.zero;
        Vector3 correctionVector = Vector3.zero;


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
            if (Physics.Raycast(transform.position, dir, out hitInfo, distanceToObstacleDetection, LayerMask.GetMask("ObstacleLayer")) && !isOutOfBound)
            {
                Vector3 hitNormal = hitInfo.normal;
                //Debug.DrawLine(hitInfo.point, hitInfo.point + hitNormal * 2, Color.green, 2f);
                avoidanceDirection += hitNormal;
                contactCount++;
            }
        }
        if (contactCount > 0)
            avoidanceDirection /= contactCount;

        // Check if the drone is within the horizontal bounds of the polygon
        bool isInsidePolygon = IsPointInsidePolygon(corners, transform.position);

        // Check if the drone is within the vertical height bounds
        bool isInsideHeight = transform.position.y >= 0 && transform.position.y <= height;

        // Determine if the drone is out of bounds
        if (!isInsidePolygon || !isInsideHeight)
        {
            isOutOfBound = true;

            // Calculate the correction vector using the closest boundary point
            correctionVector = CalculateCorrectionVector(corners, transform.position);
            outOfBound = correctionVector;

            Debug.Log("Drone out of bounds! Calculating correction vector...");
        }
        else
        {
            isOutOfBound = false;
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
        steering += outOfBound.normalized;
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

        //atraction to AtractionObject
        GameObject myObject = GameObject.FindWithTag("AtractionGameObject");
        if (myObject != null)
        {
            // Access the object's location
            Vector3 location = myObject.transform.position;
            // Calculate the direction to the object
            Vector3 directionToObject = location - transform.position;
            // Normalize the direction
            directionToObject.Normalize();
            // Add the direction to the steering
            steering += directionToObject * 0.4f;
        }

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
        if (axis.y < 0) angle = -angle; // If axis points down, reverse the angle
        angle = angle > 180 ? angle - 360 : (angle < -180 ? angle + 360 : angle); // Normalize angle to -180 to 180
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
        //Debug.Log($"Velocity: {velocity}, Yaw Rate: {angularVelocity}");

        //Debug.Log("Index of boid: " + droneIP);
        returnVariables = new List<float> { velocity.x, velocity.y, velocity.z, angularVelocity, currentRotation.x, currentRotation.z };
        return returnVariables;

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
        float x = Mathf.PerlinNoise(time / 2 + perlinNoiseSeed.x, perlinNoiseSeed.x) - 0.5f;
        float y = Mathf.PerlinNoise(time / 2 + perlinNoiseSeed.y, perlinNoiseSeed.y) - 0.5f;
        float z = Mathf.PerlinNoise(time / 2 + perlinNoiseSeed.z, perlinNoiseSeed.z) - 0.5f;

        // Normalize to ensure consistent movement speed in all directions
        return new Vector3(x, y, z).normalized;
    }

    #region Find if the drone is out of bound and calculate the correction vector

    private bool IsPointInsidePolygon(Vector3[] polygonCorners, Vector3 point)
    {
        int crossings = 0;
        for (int i = 0; i < polygonCorners.Length; i++)
        {
            Vector3 start = polygonCorners[i];
            Vector3 end = polygonCorners[(i + 1) % polygonCorners.Length];

            // Check if the point is within the y-range of the edge
            if ((start.z <= point.z && end.z > point.z) || (start.z > point.z && end.z <= point.z))
            {
                float t = (point.z - start.z) / (end.z - start.z);
                float xIntersection = start.x + t * (end.x - start.x);

                // Check if the intersection is to the right of the point
                if (xIntersection > point.x)
                {
                    crossings++;
                }
            }
        }

        // If crossings are odd, the point is inside
        return (crossings % 2 != 0);
    }

    private bool IsOutOfCustomArea(Vector3[] cornerPoints, float height, Vector3 dronePosition)
    {
        // Check if the point is inside the polygon (XZ plane)
        bool insidePolygon = IsPointInsidePolygon(cornerPoints, dronePosition);

        // Check if the drone is within the height range
        bool withinHeight = dronePosition.y >= 0 && dronePosition.y <= height;

        // Return true if the drone is outside either the polygon or the height range
        return !insidePolygon || !withinHeight;
    }

    private Vector3 CalculateCorrectionVector(Vector3[] cornerPoints, Vector3 dronePosition)
    {
        Vector3 closestPoint = Vector3.zero;
        float closestDistance = float.MaxValue;

        // Iterate over all edges of the polygon
        for (int i = 0; i < cornerPoints.Length; i++)
        {
            Vector3 start = cornerPoints[i];
            Vector3 end = cornerPoints[(i + 1) % cornerPoints.Length];

            // Find the closest point on this edge
            Vector3 projectedPoint = ProjectPointOntoLineSegment(start, end, dronePosition);

            // Calculate distance to the projected point
            float distance = Vector3.Distance(dronePosition, projectedPoint);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPoint = projectedPoint;
            }
        }

        // Return the direction vector from the drone to the closest point
        return (closestPoint - dronePosition).normalized;
    }

    private Vector3 ProjectPointOntoLineSegment(Vector3 start, Vector3 end, Vector3 point)
    {
        Vector3 lineDirection = (end - start).normalized;
        float lineLength = Vector3.Distance(start, end);

        // Project the point onto the line
        float t = Vector3.Dot((point - start), lineDirection) / lineLength;

        // Clamp t to [0, 1] to restrict to the line segment
        t = Mathf.Clamp01(t);

        // Calculate the projection point
        return start + t * (end - start);
    }


    #endregion




    public Vector3 pointA = new Vector3(0, 1, 0); // Starting point
    public Vector3 pointB = new Vector3(1, 1, 0); // Target point
    public float speed = 0.1f; // Movement speed in units/second
    public float thresholdRadius = 0.1f; // Radius around the target to consider "reached"

    private Vector3 currentTarget; // Persistent target
    private bool movingToB = false; // State: Moving to Point B or Point A


    public List<float> SimulateMovement2(List<BoidController> other, float sizeOfBoidBoundingBox, float time)
    {
        List<float> returnVariables;
        Vector3 velocity = Vector3.zero; // To store velocity
        //float angularVelocity = 0f; // To store angular velocity

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
            /*Quaternion targetRotation = Quaternion.LookRotation(direction);
            Quaternion previousRotation = transform.rotation;
            transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, SteeringSpeed * time);

            // Calculate angular velocity
            Quaternion deltaRotation = transform.rotation * Quaternion.Inverse(previousRotation);
            deltaRotation.ToAngleAxis(out float angle, out Vector3 axis);
            angularVelocity = angle / time;
            transform.rotation = previousRotation; // Revert rotation !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!*/
        }

        // Update position towards the target
        velocity = direction * speed;
        Debug.Log("Velocity: " + velocity);
        //transform.position += velocity;

        // Return calculated values
        returnVariables = new List<float>
        {
            velocity.x,
            velocity.y,
            velocity.z,
            0.0f,
            transform.rotation.eulerAngles.x,
            transform.rotation.eulerAngles.z
        };

        return returnVariables;
    }


    // Inspector variables for direct control of velocity and yaw rate
    [SerializeField] public float vx = 0.0f; // Velocity in the X direction
    [SerializeField] public float vy = 0.0f; // Velocity in the Y direction
    [SerializeField] public float vz = 0.0f; // Velocity in the Z direction
    [SerializeField] public float yawRate = 0.0f; // Yaw rate in degrees per second

    // Input parameters for the simulation function
    public List<float> SimulateMovement3(List<BoidController> other, float sizeOfBoidBoundingBox, float time)
    {
        List<float> returnVariables;

        // Calculate velocity based on inspector values
        Vector3 velocity = new Vector3(vx, vy, vz);

        // Update position based on velocity
        Vector3 previousPosition = transform.position;
        transform.position += velocity * time;

        // Calculate yaw rotation based on yawRate
        float yawDelta = yawRate * time; // Change in yaw angle
        transform.Rotate(0, yawDelta, 0);

        // Debugging: Display velocity and rotation in the console
        Debug.Log($"Velocity: {velocity}, Yaw Rate: {yawRate}");

        // Return calculated values
        returnVariables = new List<float>
        {
            velocity.x,
            velocity.y,
            velocity.z,
            yawRate,
            transform.rotation.eulerAngles.x,
            transform.rotation.eulerAngles.z
        };

        return returnVariables;
    }

}