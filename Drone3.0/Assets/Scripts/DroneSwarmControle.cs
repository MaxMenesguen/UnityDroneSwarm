using System.Collections;
using System.Collections.Generic;

using UnityEngine;
using static BoundaryBoxManager;
using static DroneBehavior;
using static UnityEngine.UI.GridLayoutGroup;

public class DroneSwarmControle : MonoBehaviour
{
    //[SerializeField] private GameObject DronePrefab; // Référence au prefab du drone
    public BoidController boidPrefab;
    [SerializeField] private GameObject ClientAndServerPrefab;
    [SerializeField] private GameObject IRLClient;
    [SerializeField] private GameObject BoidBoundingBox;
    [SerializeField] private GameObject SphereObstaclePrefab;
    [SerializeField] private GameObject AtractionObjectPrefab;
    [SerializeField] private bool droneSimulation = false;
    [SerializeField] private bool droneIRL = false;
    [SerializeField] public bool Controller = false;
    [SerializeField] public bool takeOff = false;
    [SerializeField] public bool land = false;


    [SerializeField] private int numberOfObstacle = 10; // Number of obstacles to create
    [SerializeField] private bool CreateAtractionObject = false; // Create an object that attracts the boids

    private BoundaryBoxManager boundaryBoxManager;
    private float sizeOfBoidBoundingBox; // size of the default bounding box for the boids
    private Vector3[] corners ; // Array to store the corner points of the custom area
    private float height ; // Height of the custom area


    // Public property to access sizeOfBoidBoundingBox
    public float SizeOfBoidBoundingBox
    {
        get { return sizeOfBoidBoundingBox; }
        //set { sizeOfBoidBoundingBox = value; } // Make it readonly if you don't want it to be changed from other scripts
    }

    [SerializeField] private int numberOfSimuDrones = 0; // Nombre de drones SIMU à créer
    public int NumberOfSimuDrones
    {
        get { return numberOfSimuDrones; }
        //set { numberOfDrones = value; }
    }
    

    private List<BoidController> _droneGameObject; // Liste pour stocker les GameObjects des drones
    //private List<DroneBehavior> droneObjects = new List<DroneBehavior>(); // Liste pour stocker les scripts des drones

    public static bool droneConected = false;
    public static List<DroneInformation> droneInformation = null; // Liste pour stocker les informations des drones
    [HideInInspector]
    public static int selectedDroneIndex = -1; // -1 means no selection
    public static DroneInformation selectedDrone = null; // This will be set to the selected drone

    public static bool droneInitialized = false;
    private bool serverUpAndRunning = false;

    #region coroutine variables
    public static bool isCoroutineCheckDroneConnectionRunning = false;
    public static bool isCoroutineGetFromAPIRunning = false;
    public static bool isCoroutineTakeOffRunning = false;
    public static bool isCoroutineLandRunning = false;
    public static bool isCoroutineCloseLinksRunning = false;
    public static bool isCoroutineSetVelocity = false;
    #endregion

    //maxspeed of drone
    public float maxSpeed = 0.2f;
    public float closeEnoughDistance = 0.2f;

    public void Start()
    {
        // Get the BoundaryBoxManager component on the same GameObject
        boundaryBoxManager = GetComponent<BoundaryBoxManager>();
        if (boundaryBoxManager == null)
        {
            Debug.LogError("BoundaryBoxManager not found on the same GameObject!");
            return;
        }

        else
        {
            // Get the size of the BoidBoundingBox from the BoundaryBoxManager
            sizeOfBoidBoundingBox = boundaryBoxManager.sizeOfBoidBoundingBox;
            // Perform the initial setup based on the boundary mode
            PerformActionBasedOnBoundaryMode();
        }

        
    }

    private void Update() 
    {
        if (droneIRL)
        {
            droneSimulation = false;
        }
        else if (droneSimulation)
        {
            droneIRL = false;
        }


        #region IRL Drone Controle
        if (droneIRL)
        {
            if (serverUpAndRunning == false)
            {
                serverUpAndRunning = true;

                GameObject clientAndServer = Instantiate(IRLClient);

            }
            if (DroneIRLClient.droneClientCreated)
            {
                droneInformation = DroneIRLClient.droneInformationClient;
            }
            if (droneInformation != null && !droneInitialized)
            {
                InitialiserDrone(droneInformation);
                Debug.Log("Drone initialized");

            }
            else if (droneInformation != null && droneInitialized)
            {
                foreach (BoidController boid in _droneGameObject)
                {
                    boid.transform.position = new Vector3(droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.positionDroneX,
                        droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.positionDroneY,
                        droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.positionDroneZ);
                    boid.transform.rotation = Quaternion.Euler(droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.rotationDroneRoll,
                        droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.rotationDroneYaw,
                        droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.rotationDronePitch);
                }

                if (droneInformation[0].takeoff == true && Controller)
                {
                    foreach (BoidController boid in _droneGameObject)
                    {
                        //could change to a scrolable list between all the simulations so that is could be changed in the inspector !!! 
                        List<float> simulationInformations = boid.SimulateMovement(_droneGameObject, corners, height, Time.deltaTime);
                        if (simulationInformations != null)
                        {
                            droneInformation[_droneGameObject.IndexOf(boid)].droneVelocity.vitesseDroneX = simulationInformations[0];
                            droneInformation[_droneGameObject.IndexOf(boid)].droneVelocity.vitesseDroneY = simulationInformations[1];
                            droneInformation[_droneGameObject.IndexOf(boid)].droneVelocity.vitesseDroneZ = simulationInformations[2];
                            droneInformation[_droneGameObject.IndexOf(boid)].droneVelocity.vitesseDroneYaw = simulationInformations[3];
                            droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.rotationDroneRoll = simulationInformations[4];
                            droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.rotationDronePitch = simulationInformations[5];

                        }
                    }
                }
                else
                {
                    Controller = false;
                }


            }


        }

        #endregion
        if (droneSimulation)
        {

            if (numberOfSimuDrones != 0)
            {
                if (serverUpAndRunning == false)
                {
                    serverUpAndRunning = true;
                    
                    GameObject clientAndServer = Instantiate(ClientAndServerPrefab);
                    
                }
                if (DroneSimulationClient.droneClientCreated)
                {
                    droneInformation = DroneSimulationClient.droneInformationClient;
                }
                if (droneInformation != null && !droneInitialized)
                {
                    InitialiserDrone(droneInformation);
                    Debug.Log("Drone initialized");

                }
                else if (droneInformation != null && droneInitialized)
                {
                    //make boid simulation
                    //get the speed of the drone
                    foreach (BoidController boid in _droneGameObject)
                    {
                        List<float> simulationInformations = boid.SimulateMovement(_droneGameObject, corners, height, Time.deltaTime);
                        if (simulationInformations != null)
                        {
                            droneInformation[_droneGameObject.IndexOf(boid)].droneVelocity.vitesseDroneX = simulationInformations[0];
                            droneInformation[_droneGameObject.IndexOf(boid)].droneVelocity.vitesseDroneY = simulationInformations[1];
                            droneInformation[_droneGameObject.IndexOf(boid)].droneVelocity.vitesseDroneZ = simulationInformations[2];
                            droneInformation[_droneGameObject.IndexOf(boid)].droneVelocity.vitesseDroneYaw = simulationInformations[3];
                            droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.rotationDroneRoll = simulationInformations[4];
                            droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.rotationDronePitch = simulationInformations[5];
                        }
                    }
                    
                    
                    foreach (BoidController boid in _droneGameObject)
                    {
                        boid.transform.position = new Vector3(droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.positionDroneX, 
                            droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.positionDroneY, 
                            droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.positionDroneZ);
                        boid.transform.rotation = Quaternion.Euler(droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.rotationDroneRoll, 
                            droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.rotationDroneYaw, 
                            droneInformation[_droneGameObject.IndexOf(boid)].dronePosition.rotationDronePitch);
                    }

                    //do the reception of the server 
                    if (CreateAtractionObject && GameObject.FindWithTag("AtractionGameObject") ==null)
                    {
                        Instantiate(AtractionObjectPrefab);
                    }
                    else if (!CreateAtractionObject)
                    {
                        GameObject myObject = GameObject.FindWithTag("AtractionGameObject");
                        if (myObject != null)
                        {
                            Destroy(myObject);
                            
                        }
                        
                    }
                }
                
            }
            else
            {
                Debug.LogWarning("Put the desired amout of drone you want to simulate");
                
            }

        }

    }

    void InitialiserDrone(List<DroneInformation> droneInformation)
    {
        droneInitialized = true;
         int numberOfDrones = droneInformation.Count;
        _droneGameObject = new List<BoidController>();
        for (int i = 0; i < numberOfDrones; i++)
        {
            Vector3 posDrone = new Vector3(droneInformation[i].dronePosition.positionDroneX, droneInformation[i].dronePosition.positionDroneZ, droneInformation[i].dronePosition.positionDroneY);
            Vector3 rotDrone = new Vector3(0, droneInformation[i].dronePosition.rotationDroneYaw, 0);

            Quaternion rotationQuaternion = Quaternion.Euler(rotDrone); // Convertit les angles Euler en Quaternion

            GameObject droneInstance = Instantiate(boidPrefab.gameObject, posDrone, rotationQuaternion);

            _droneGameObject.Add(droneInstance.GetComponent<BoidController>());
            //droneInstance.GetComponent<DroneBehavior>().Initialiser(droneInformation[i]); what is this for ?
            droneInstance.GetComponent<BoidController>().droneIP = droneInformation[i].droneIP;
        }
    }

    public void PerformActionBasedOnBoundaryMode()
    {
        if (boundaryBoxManager != null)
        {
            // Clear any existing bounding boxes (if needed)
            //ClearExistingBoundingBoxes();

            if (boundaryBoxManager.boundaryMode == BoundaryBoxManager.BoundaryMode.SimpleCube)
            {
                Debug.Log("Handling SimpleCube Mode...");
                CreateSimpleCubeBoundary(boundaryBoxManager.sizeOfBoidBoundingBox);
            }
            else if (boundaryBoxManager.boundaryMode == BoundaryBoxManager.BoundaryMode.CustomArea)
            {
                Debug.Log("Handling CustomArea Mode...");
                CreateCustomAreaBoundary(boundaryBoxManager.cornerPoints, boundaryBoxManager.customHeight);
            }
            else if (boundaryBoxManager.boundaryMode == BoundaryBoxManager.BoundaryMode.AreaCreation)
            {
                Debug.Log("Handling AreaCreation Mode...");
                CreateCustomAreaBoundary(boundaryBoxManager.cornerPoints, boundaryBoxManager.customHeight);
                // Additional tools for user interaction can be added here
            }
        }
    }

    private void CreateSimpleCubeBoundary(float sizeOfBoidBoundingBox)
    {
        

        // Set the scale of the BoidBoundingBox
        BoidBoundingBox.transform.localScale = new Vector3(sizeOfBoidBoundingBox, sizeOfBoidBoundingBox, sizeOfBoidBoundingBox);

        // Offset distance to position the outer cubes
        float halfSize = sizeOfBoidBoundingBox / 2;

        //variables for the simulation : corners and height
        corners = new Vector3[]
        {
            new Vector3(-halfSize, 0, -halfSize), // Bottom-left-back
            new Vector3(-halfSize, 0, halfSize),  // Bottom-left-front
            new Vector3(halfSize, 0, halfSize),   // Bottom-right-front
            new Vector3(halfSize, 0, -halfSize)   // Bottom-right-back
        };

        height = sizeOfBoidBoundingBox;

        // Array of positions for bounding boxes on each face of the inner cube
        Vector3[] positions = new Vector3[]
        {
            new Vector3(sizeOfBoidBoundingBox, halfSize, 0),  // Right face
            new Vector3(-sizeOfBoidBoundingBox, halfSize, 0), // Left face
            new Vector3(0, halfSize+ sizeOfBoidBoundingBox, 0),  // Top face
            new Vector3(0, -halfSize, 0), // Bottom face
            new Vector3(0, halfSize, sizeOfBoidBoundingBox),  // Front face
            new Vector3(0, halfSize, -sizeOfBoidBoundingBox)  // Back face
        };

        // Instantiate bounding boxes at each position
        foreach (Vector3 position in positions)
        {
            Instantiate(BoidBoundingBox, position, Quaternion.identity);
        }

        for (int i = 0; i < numberOfObstacle; i++)
        {
            var obstacleInstance = Instantiate(SphereObstaclePrefab);
            float sizeObstacle = Mathf.Pow(Random.Range(0f, 1f), 3) * (sizeOfBoidBoundingBox) / 4;
            obstacleInstance.transform.localScale = new Vector3(sizeObstacle, sizeObstacle, sizeObstacle);
            obstacleInstance.transform.localPosition += new Vector3(Random.Range(-sizeOfBoidBoundingBox / 2, sizeOfBoidBoundingBox / 2),
                Random.Range(-sizeOfBoidBoundingBox / 2, sizeOfBoidBoundingBox / 2),
                Random.Range(-sizeOfBoidBoundingBox / 2, sizeOfBoidBoundingBox / 2));
        }
    }

    private void CreateCustomAreaBoundary(Vector3[] cornerPoints, float height)
    {
        // Ensure corner points are set
        if (cornerPoints.Length != 4)
        {
            Debug.LogError("Custom area requires exactly 4 corner points!");
            return;
        }

        //Define half the thickness of the wall (u1)
        float wallThickness = 0.1f;// Adjust as needed
        
        //variables for the simulation : corners and height
        corners = cornerPoints;
        this.height = height;

        // Create the vertical walls and connect corners
        for (int i = 0; i < cornerPoints.Length; i++)
        {
            Vector3 start = cornerPoints[i];
            Vector3 end = cornerPoints[(i + 1) % cornerPoints.Length]; // Wrap around to the first corner

            // Step 1: Find the midpoint between the two corners (x1, y1, z1)
            Vector3 midpoint = (start + end) / 2;

            // Step 2: Calculate the height offset (y2)
            float heightOffset = height / 2;

            // Step 3: Calculate the wall's center point (x1, y1+y2, z1+u1)
            // Since we want the wall to "stand" vertically and account for thickness, we only offset height
            Vector3 wallCenter = new Vector3(midpoint.x, midpoint.y + heightOffset, midpoint.z);

            // Step 4: Calculate the wall's rotation to align it with the vector between the two corners
            Vector3 wallDirection = (end - start).normalized;
            Quaternion wallRotation = Quaternion.LookRotation(wallDirection);

            // Step 5: Instantiate the boundary wall
            GameObject wall = Instantiate(BoidBoundingBox, wallCenter, wallRotation);

            // Set the scale of the wall (length, height, thickness)
            float wallLength = Vector3.Distance(start, end);
            wall.transform.localScale = new Vector3(wallThickness, height, wallLength);

            // Optional Debug: Draw lines for clarity
            Debug.DrawLine(start, end, Color.red, 5000);
        }
        //crate top and bottom walls

        // Step 1: Find the min and max of the coordinates
        float minX = float.MaxValue, maxX = float.MinValue;
        float minZ = float.MaxValue, maxZ = float.MinValue;

        foreach (var corner in cornerPoints)
        {
            minX = Mathf.Min(minX, corner.x);
            maxX = Mathf.Max(maxX, corner.x);
            minZ = Mathf.Min(minZ, corner.z);
            maxZ = Mathf.Max(maxZ, corner.z);
        }

        // Calculate the width and depth of the area
        float width = maxX - minX;
        float depth = maxZ - minZ;

        // Calculate the center of the area
        Vector3 center = new Vector3(minX + width / 2, 0, minZ + depth / 2);

        // Step 3: Create the bottom wall
        const float OffsetAdjustment = 0.001f;
        Vector3 bottomWallCenter = new Vector3(center.x, center.y - wallThickness/2 - OffsetAdjustment, center.z);
        GameObject bottomWall = Instantiate(BoidBoundingBox, bottomWallCenter, Quaternion.Euler(0, 0, 0));
        bottomWall.transform.localScale = new Vector3(width, wallThickness, depth); // Thin wall for the bottom

        // Step 4: Create the top wall
        Vector3 topWallCenter = new Vector3(center.x, center.y + height + wallThickness/2, center.z);
        
        GameObject topWall = Instantiate(BoidBoundingBox, topWallCenter, Quaternion.Euler(0, 0, 0));
        topWall.transform.localScale = new Vector3(width, wallThickness, depth); // Thin wall for the top

        // Optional Debug: Draw rectangles for clarity
        /*Debug.DrawLine(new Vector3(minX, center.y, minZ), new Vector3(maxX, center.y, minZ), Color.green, 5000);
        Debug.DrawLine(new Vector3(maxX, center.y, minZ), new Vector3(maxX, center.y, maxZ), Color.green, 5000);
        Debug.DrawLine(new Vector3(maxX, center.y, maxZ), new Vector3(minX, center.y, maxZ), Color.green, 5000);
        Debug.DrawLine(new Vector3(minX, center.y, maxZ), new Vector3(minX, center.y, minZ), Color.green, 5000);*/
    }




    /*private void ClearExistingBoundingBoxes()
    {
        // Find all instances of the BoidBoundingBox prefab in the scene and destroy them
        foreach (var obj in GameObject.FindGameObjectsWithTag("BoidBoundingBox"))
        {
            Destroy(obj);
        }
    }*/




}