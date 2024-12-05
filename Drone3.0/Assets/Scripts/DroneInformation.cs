using System.Collections.Generic;
using static DroneCommandData;

#region Information of the drones region
public class DroneInformation  
{
    public string droneIP;
    public bool takeoff = false;
    //Position info
    public DronePosition dronePosition;
    //Speed info
    public DroneVelocity droneVelocity;

}

public class DronePosition
{
    //tells us if position has been sent by the API
    public bool positionInfo = false;
    //Position info
    public float positionDroneX = 0;
    public float positionDroneY = 0;
    public float positionDroneZ = 0;
    public float rotationDroneYaw = 0;
    public float rotationDroneRoll = 0;
    public float rotationDronePitch = 0;
}

public class DroneVelocity
{
    //Speed info
    public float vitesseDroneX = 0;
    public float vitesseDroneY = 0;
    public float vitesseDroneZ = 0;
    public float vitesseDroneYaw = 0;
}

#endregion

[System.Serializable]

public class DroneApiResponse// peut être utilliser les trucs de noé pour les stocker dans des game object ou jsp quoi
{
    public string[] URIS;
}

[System.Serializable]
public class DronePositionResponse
{
    public string type;  // The type of message, e.g., "Positions"
    public Dictionary<string, float[]> Positions;  // Position data
    public string additionalInformation;  // Optional field for future use
}

//this is for the post request 
//helps json to be created

[System.Serializable]
public class DroneInstruction
{
    public string type; // Command type: "speed", "takeoff", "land", etc.
    public DroneSpeedDataList droneSpeedDataList; // For "speed" commands
    public DroneCommandData commandData; // For "takeoff", "land", etc.
}

[System.Serializable]
public class DroneCommandData
{
    public string command; // Specific command (e.g., "takeoff", "land")
    public string targetDrone; // Drone ID for individual commands (optional)
    public Dictionary<string, object> parameters; // Additional parameters if needed
}
[System.Serializable]
public class DroneSpeedData
{
    
    public string droneIP;
    public float Vx;
    public float Vy;
    public float Vz;
    public float yaw_rate;
}

[System.Serializable]
public class DroneSpeedDataList
{
    public List<DroneSpeedData> Velocity = new List<DroneSpeedData>();
}

