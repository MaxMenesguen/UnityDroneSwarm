using UnityEngine;
using System.IO;

public class BoundaryBoxManager : MonoBehaviour
{
    public enum BoundaryMode { SimpleCube, CustomArea, AreaCreation }

    [SerializeField]
    public BoundaryMode boundaryMode = BoundaryMode.SimpleCube;

    [Header("Simple Cube Settings")]
    public float sizeOfBoidBoundingBox = 2.5f;

    [Header("Custom Area Settings")]
    public float customHeight = 5f;
    public Vector3[] cornerPoints = new Vector3[4]; // Four corners for custom area

    [Header("Area Creation Tools")]
    [SerializeField] public string saveFileName = "CustomArea.json"; // File name for saving/loading

    private const string SaveDirectory = "Assets/Scripts/SavedAreas/";

    private void Awake()
    {
        if (boundaryMode == BoundaryMode.CustomArea || boundaryMode == BoundaryMode.AreaCreation)
        {
            LoadCustomArea();
        }
    }

    public void SaveCustomArea()
    {
        if (!Directory.Exists(SaveDirectory))
        {
            Directory.CreateDirectory(SaveDirectory);
        }

        CustomAreaData data = new CustomAreaData
        {
            cornerPoints = cornerPoints,
            customHeight = customHeight
        };

        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(Path.Combine(SaveDirectory, saveFileName), json);
        Debug.Log($"Custom area saved to {SaveDirectory}{saveFileName}");
    }

    public void LoadCustomArea()
    {
        string filePath = Path.Combine(SaveDirectory, saveFileName);

        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            CustomAreaData data = JsonUtility.FromJson<CustomAreaData>(json);
            cornerPoints = data.cornerPoints;
            customHeight = data.customHeight;
            Debug.Log("Custom area loaded from JSON.");
        }
        else
        {
            Debug.LogWarning($"Custom area file not found at {filePath}");
        }
    }

    [System.Serializable]
    public class CustomAreaData
    {
        public Vector3[] cornerPoints;
        public float customHeight;
    }
}
