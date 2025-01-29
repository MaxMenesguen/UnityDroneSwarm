using System.IO;
using UnityEngine;

public class ObstacleCreator : MonoBehaviour
{
    [Header("Obstacle Creation Settings")]
    public bool createObstacles = false; // Checkbox to activate obstacle creation
    public string jsonFilePath = "Assets/Scripts/Obstacles/obstacles.json"; // Path to the JSON file
    public GameObject cubePrefab; // Prefab for cubes
    public GameObject donutPrefab; // Prefab for donuts

    // Struct to match the JSON structure
    [System.Serializable]
    public class Obstacle
    {
        public string type;
        public Vector3 corner1; // For cubes
        public Vector3 corner2; // For cubes
        public float height;    // For cubes
        public Vector3 center;  // For donuts
        public float innerDiameter; // For donuts
        public float outerDiameter; // For donuts
        public Vector3 holeaxis; // For donuts
    }

    [System.Serializable]
    public class ObstacleData
    {
        public Obstacle[] obstacles;
    }

    void Update()
    {
        if (createObstacles)
        {
            //check if the object is already created
            GameObject[] obstacles = GameObject.FindGameObjectsWithTag("CustomObstacle");
            if (obstacles.Length > 0)
            {
                foreach (GameObject obstacle in obstacles)
                {
                    Destroy(obstacle);
                }
            }
            else
            {
                LoadAndCreateObstacles();
            }
            createObstacles = false;
        }
    }

    void LoadAndCreateObstacles()
    {
        if (!File.Exists(jsonFilePath))
        {
            Debug.LogError($"JSON file not found at {jsonFilePath}");
            return;
        }

        // Read and parse the JSON file
        string jsonContent = File.ReadAllText(jsonFilePath);
        ObstacleData obstacleData = JsonUtility.FromJson<ObstacleData>(jsonContent);

        foreach (var obstacle in obstacleData.obstacles)
        {
            if (obstacle.type == "cube")
            {
                CreateCubeObstacle(obstacle);
            }
            else if (obstacle.type == "donut")
            {
                CreateDonutObstacle(obstacle);
            }
            else
            {
                Debug.LogWarning($"Unknown obstacle type: {obstacle.type}");
            }
        }
    }

    void CreateCubeObstacle(Obstacle obstacle)
    {
        // Calculate the center and size of the cube
        Vector3 corner1 = new Vector3(obstacle.corner1.x, obstacle.corner1.z, obstacle.corner1.y); // Swap Y and Z for Unity
        Vector3 corner2 = new Vector3(obstacle.corner2.x, obstacle.corner2.z, obstacle.corner2.y);
        Vector3 center = (corner1 + corner2) / 2 + new Vector3(0,obstacle.height/2,0);
        Vector3 size = new Vector3(
            Mathf.Abs(corner1.x - corner2.x),
            obstacle.height,
            Mathf.Abs(corner1.z - corner2.z)
        );

        // Instantiate the cube
        GameObject cube = Instantiate(cubePrefab, center, Quaternion.identity);
        cube.transform.localScale = size;
        Debug.Log($"Created cube at {center} with size {size}");
    }

    void CreateDonutObstacle(Obstacle obstacle)
    {
        // Use the center, inner diameter, and outer diameter to create the donut
        Vector3 center = new Vector3(obstacle.center.x, obstacle.center.z, obstacle.center.y); // Swap Y and Z for Unity

        float outerDiameter = obstacle.outerDiameter;
        Vector3 scale = new Vector3(outerDiameter, outerDiameter, outerDiameter);

        // Get the hole axis and normalize it to ensure it's a direction vector
        Vector3 holeAxis = new Vector3(obstacle.holeaxis.x, obstacle.holeaxis.z, obstacle.holeaxis.y).normalized; // Swap Y and Z for Unity

        // Calculate the rotation using the hole axis
        Quaternion rotation;
        if (holeAxis != Vector3.zero)
        {

            rotation = Quaternion.Euler(0, 90, 0);
        }
        else
        {
            // Default rotation if the axis is invalid
            rotation = Quaternion.identity;
        }

        // Instantiate the donut with the calculated position and rotation
        GameObject donut = Instantiate(donutPrefab, center, rotation);
        //multiply the scale of the donut with the outer diameter
        donut.transform.localScale = Vector3.Scale(donut.transform.localScale, scale);

        Debug.Log($"Created donut at {center} with outer diameter {outerDiameter} and hole axis {holeAxis}");
    }

}
