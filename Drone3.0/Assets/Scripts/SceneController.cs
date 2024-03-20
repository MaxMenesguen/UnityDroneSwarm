using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneController : MonoBehaviour
{
    public BoidController boidPrefab;
    //implement later
    //public GameObject obstaclePrefab;
    public GameObject BoidBoundingBox;
    public GameObject SphereObstaclePrefab;
    public float sizeOfBoidBoundingBox = 4f;
    public int spawnBoids = 100;
    public int numberOfObstacle = 10;
    public float resetBoidTolerancePurcentage = 0.1f;


    private List<BoidController> _boids;

    private void Start()
    {
        // Set the scale of the BoidBoundingBox
        BoidBoundingBox.transform.localScale = new Vector3(sizeOfBoidBoundingBox, sizeOfBoidBoundingBox, sizeOfBoidBoundingBox);

        // Array of positions for bounding boxes
        Vector3[] positions = new Vector3[]
        {
        new Vector3(sizeOfBoidBoundingBox, 0, 0),
        new Vector3(-sizeOfBoidBoundingBox, 0, 0),
        new Vector3(0, sizeOfBoidBoundingBox, 0),
        new Vector3(0, -sizeOfBoidBoundingBox, 0),
        new Vector3(0, 0, sizeOfBoidBoundingBox),
        new Vector3(0, 0, -sizeOfBoidBoundingBox)
        };

        // Instantiate bounding boxes at each position
        foreach (Vector3 position in positions)
        {
            Instantiate(BoidBoundingBox, position, Quaternion.identity);
        }
        _boids = new List<BoidController>();

        for (int i = 0; i < numberOfObstacle; i++)
        {
            var obstacleInstance = Instantiate(SphereObstaclePrefab);
            float sizeObstacle = Mathf.Pow(Random.Range(0f, 1f), 3) * (sizeOfBoidBoundingBox) / 4;
            obstacleInstance.transform.localScale = new Vector3(sizeObstacle, sizeObstacle, sizeObstacle);
            obstacleInstance.transform.localPosition += new Vector3(Random.Range(-sizeOfBoidBoundingBox/2, sizeOfBoidBoundingBox / 2), Random.Range(-sizeOfBoidBoundingBox / 2, sizeOfBoidBoundingBox / 2), Random.Range(-sizeOfBoidBoundingBox / 2, sizeOfBoidBoundingBox / 2));
        }

        for (int i = 0; i < spawnBoids; i++)
        {
            SpawnBoid(boidPrefab.gameObject, i);
        }
    }

    private void Update()
    {
        foreach (BoidController boid in _boids)
        {
            boid.SimulateMovement(_boids, sizeOfBoidBoundingBox, Time.deltaTime);
            if (boid.transform.position.x > sizeOfBoidBoundingBox/2+(sizeOfBoidBoundingBox*resetBoidTolerancePurcentage/2) 
                || boid.transform.position.x < -(sizeOfBoidBoundingBox/2 + (sizeOfBoidBoundingBox * resetBoidTolerancePurcentage/2)) 
                || boid.transform.position.y > sizeOfBoidBoundingBox/2 + (sizeOfBoidBoundingBox * resetBoidTolerancePurcentage/2) 
                || boid.transform.position.y < -(sizeOfBoidBoundingBox/2 + (sizeOfBoidBoundingBox * resetBoidTolerancePurcentage/2)) 
                || boid.transform.position.z > sizeOfBoidBoundingBox/2 + (sizeOfBoidBoundingBox * resetBoidTolerancePurcentage/2) 
                || boid.transform.position.z < -(sizeOfBoidBoundingBox/2 + (sizeOfBoidBoundingBox * resetBoidTolerancePurcentage/2)))
            {
                boid.transform.position = new Vector3(Random.Range(-sizeOfBoidBoundingBox / 2, sizeOfBoidBoundingBox / 2), Random.Range(-sizeOfBoidBoundingBox / 2, sizeOfBoidBoundingBox / 2), Random.Range(-sizeOfBoidBoundingBox / 2, sizeOfBoidBoundingBox / 2));
                Debug.Log("Boid reset");
            }
            

        }
    }

    private void SpawnBoid(GameObject prefab, int droneIP)
    {
        var boidInstance = Instantiate(prefab);
        boidInstance.transform.localPosition = new Vector3(Random.Range(-sizeOfBoidBoundingBox / 2, sizeOfBoidBoundingBox / 2), Random.Range(-sizeOfBoidBoundingBox / 2, sizeOfBoidBoundingBox / 2), Random.Range(-sizeOfBoidBoundingBox / 2, sizeOfBoidBoundingBox / 2));
        //debugging
        //boidInstance.transform.localPosition += new Vector3(0,0,0);
        _boids.Add(boidInstance.GetComponent<BoidController>());
        boidInstance.GetComponent<BoidController>().droneIP = droneIP.ToString();
    }

}
