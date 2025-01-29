using UnityEngine;

[ExecuteInEditMode]
public class AttractionObjectController : MonoBehaviour
{
    [Header("Attraction Object Settings")]
    public GameObject attractionObjectPrefab; // Prefab for the attraction object
    private GameObject attractionObject; // Active attraction object

    public float moveSpeed = 5f; // Speed for movement (forward/backward/sideways)
    public float altitudeSpeed = 2f; // Speed for altitude adjustment (up/down)
    public float rotationSpeed = 100f; // Speed for yaw rotation

    public KeyCode toggleKey = KeyCode.Space; // Key to toggle control mode
    private bool controlMode = false; // Whether the attraction object control is active

    void Update()
    {
        if (!Application.isPlaying) // Edit mode logic
        {
            HandleEditMode();
            return;
        }

        // Play mode logic
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleControlMode();
        }

        if (controlMode && attractionObject != null)
        {
            HandleControllerInput();
        }
    }

    private void ToggleControlMode()
    {
        controlMode = !controlMode;

        if (controlMode)
        {
            Debug.Log("Control Mode Enabled");

            if (attractionObject == null)
            {
                attractionObject = Instantiate(attractionObjectPrefab);
                attractionObject.transform.position = new Vector3(0, 1, 0); // Initial position
            }
        }
        else
        {
            Debug.Log("Control Mode Disabled");

            if (attractionObject != null)
            {
                DestroyImmediate(attractionObject); // Destroy object in both play and edit mode
                attractionObject = null;
            }
        }
    }

    private void HandleControllerInput()
    {
        float leftJoystickVertical = Input.GetAxis("Vertical");
        float leftJoystickHorizontal = Input.GetAxis("Horizontal");

        float rightJoystickVertical = Input.GetAxis("RightStickVertical");
        float rightJoystickHorizontal = Input.GetAxis("RightStickHorizontal");

        Vector3 altitudeChange = Vector3.up * leftJoystickVertical * altitudeSpeed * Time.deltaTime;
        float yawChange = leftJoystickHorizontal * rotationSpeed * Time.deltaTime;

        Vector3 forwardBackward = attractionObject.transform.forward * rightJoystickVertical * moveSpeed * Time.deltaTime;
        Vector3 sideMovement = attractionObject.transform.right * rightJoystickHorizontal * moveSpeed * Time.deltaTime;

        attractionObject.transform.position += altitudeChange + forwardBackward + sideMovement;
        attractionObject.transform.Rotate(0, yawChange, 0);
    }

    private void HandleEditMode()
    {
        // Logic for handling the attraction object in Scene View
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleControlMode();
        }

        if (controlMode && attractionObject != null)
        {
            // Handle movement and rotation in edit mode
            HandleControllerInput();
        }
    }

    private void OnDrawGizmos()
    {
        if (attractionObject != null)
        {
            Gizmos.color = Color.green;
            Vector3 position = attractionObject.transform.position;
            Vector3 forward = attractionObject.transform.TransformDirection(Vector3.right);
            Gizmos.DrawLine(position, position + forward * 0.5f);
        }
    }
}
