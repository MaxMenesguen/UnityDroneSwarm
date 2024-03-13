using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SceneController : MonoBehaviour
{
    public BoidController boidPrefab;
    //implement later
    //public GameObject obstaclePrefab;
    public GameObject BoidBoundingBox;
    public int sizeOfBoidBoundingBox = 40;
    public int spawnBoids = 100;

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

        for (int i = 0; i < spawnBoids; i++)
        {
            SpawnBoid(boidPrefab.gameObject, 0);
        }
    }

    private void Update()
    {
        foreach (BoidController boid in _boids)
        {
            boid.SimulateMovement(_boids, Time.deltaTime);
            if (boid.transform.position.x > sizeOfBoidBoundingBox +20 || boid.transform.position.x < -(sizeOfBoidBoundingBox + 20) || boid.transform.position.y > sizeOfBoidBoundingBox + 20 || boid.transform.position.y < -(sizeOfBoidBoundingBox + 20) || boid.transform.position.z > sizeOfBoidBoundingBox + 20 || boid.transform.position.z < -(sizeOfBoidBoundingBox + 20))
            {
                boid.transform.position = new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));
                //debugging
                //boid.transform.position = new Vector3(0, 0, 0);
            }
            

        }
    }

    private void SpawnBoid(GameObject prefab, int swarmIndex)
    {
        var boidInstance = Instantiate(prefab);
        boidInstance.transform.localPosition += new Vector3(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));
        //debugging
        //boidInstance.transform.localPosition += new Vector3(0,0,0);
        _boids.Add(boidInstance.GetComponent<BoidController>());
    }

}
