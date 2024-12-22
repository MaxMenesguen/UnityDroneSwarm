using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;

using System.Threading.Tasks;
using System.Collections.Concurrent;
using UnityEngine.Rendering;
using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime;
using System.Threading;
using System.Linq;
using Newtonsoft.Json;
using UnityEditor.Experimental.GraphView;
using static UnityEngine.EventSystems.EventTrigger;



public class DroneIRLClient : MonoBehaviour
{
    #region variables
    public DroneSwarmControle referenceToDroneSwarmControle;
    
    private TcpClient client;
    private NetworkStream stream;
    //private int numberOfDrones = 0;
    //private float sizeOfBoidBoundingBox = 0f;
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    public static bool droneClientCreated = false;
    public static List<DroneInformation> droneInformationClient = new List<DroneInformation>();
    private ManualResetEventSlim messageAvailable = new ManualResetEventSlim(false);
    #endregion
    private string serverIP = "127.0.0.1";  // Your server IP
    //public string serverIP = "0.0.0.0";  // Your server IP
    //public int clientPort = 8080;

    private int serverPort = 8080;  // Your server port

    private ConcurrentQueue<string> messagesFromServerIRLQueue = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> messagesToServerIRLQueue = new ConcurrentQueue<string>();
    //private int numberOfDrones;

    void Start()
    {
        referenceToDroneSwarmControle = FindObjectOfType<DroneSwarmControle>();
        // Start the async operation without awaiting it
        Task.Run(async () =>
        {
            try
            {
                await ConnectToServer(cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        });
    }

    async Task ConnectToServer(CancellationToken token)
    {
        client = new TcpClient();

        try
        {
            client = new TcpClient();
            Debug.Log("Attempting to connect to the server...");

            await client.ConnectAsync(serverIP, serverPort);  // Explicit async connection
            Debug.Log("Connected to the server.");

            stream = client.GetStream();

            // Send initial message to the server
            /*string messageToSend = "This is Unity !";
            byte[] buffer = Encoding.ASCII.GetBytes(messageToSend);
            await stream.WriteAsync(buffer, 0, buffer.Length);
            Debug.Log("Sent: " + messageToSend);*/

            // Begin listening for messages from the server
            var receiveTask = ReceiveMessagesAsyncClient(token);
            var sendTask = SendMessagesAsyncClient(token);

            await Task.WhenAll(receiveTask, sendTask);  // Wait for tasks to complete
        }
        catch (Exception ex)
        {
            Debug.LogError("Error: " + ex.Message);
        }
    }

    async Task SendMessagesAsyncClient(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                if (messagesToServerIRLQueue.TryDequeue(out string message))
                {
                    byte[] buffer = Encoding.ASCII.GetBytes(message);
                    await stream.WriteAsync(buffer, 0, buffer.Length, token);
                    messageAvailable.Reset();
                }
                else
                {
                    messageAvailable.Wait(token);  // Wait until a message is available
                }
            }
            Debug.Log("Close SendMessagesAsyncClient");
        }
        catch (OperationCanceledException) { /* Graceful shutdown */ }
        catch (Exception ex) { Debug.LogError($"Send error: {ex.Message}"); }
    }

    async Task ReceiveMessagesAsyncClient(CancellationToken token)
    {
        IPEndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, serverPort);
        try
        {
            var stream = client.GetStream();
            byte[] buffer = new byte[2048 * 16];
            while (!token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead == 0) break; // Connection closed
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                messagesFromServerIRLQueue.Enqueue(message);
            }
        }
        catch (SocketException ex)
        {
            Debug.LogError($"Receive error: {ex.Message}");
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Read operation was canceled.");
        }
    }

    #region Commands
    public void SendCommand(string commandType, Dictionary<string, object> parameters = null, string targetDrone = null)
    {
        // Create a new DroneCommandData instance
        DroneCommandData commandData = new DroneCommandData
        {
            command = commandType,
            targetDrone = targetDrone, // Target a specific drone, if provided
            parameters = parameters ?? new Dictionary<string, object>() // Use empty parameters if null
        };

        // Wrap the command data in a DroneInstruction
        DroneInstruction instruction = new DroneInstruction
        {
            type = "command", // Indicates this is a command
            commandData = commandData // Assign the command data
        };

        // Serialize the instruction to JSON
        string json = JsonConvert.SerializeObject(instruction, Formatting.Indented);
        Debug.Log("Sending command: " + json);
        // Enqueue the JSON message to the server queue
        messagesToServerIRLQueue.Enqueue(json);
        messageAvailable.Set(); // Signal that a message is ready to send
    }


    #endregion


    string messageFromIRLServer;
    void Update()
    {

        if (!droneClientCreated)
        {
            if (messagesFromServerIRLQueue.TryDequeue(out messageFromIRLServer))
            {
                Debug.Log("Dequeued message: " + messageFromIRLServer);

                try
                {
                    // Deserialize the message into DronePositionResponse
                    DronePositionResponse droneFirstPosition = Newtonsoft.Json.JsonConvert.DeserializeObject<DronePositionResponse>(messageFromIRLServer);

                    // Check if the type is "Positions"
                    if (droneFirstPosition.type == "Positions")
                    {
                        // Iterate through the Positions dictionary
                        foreach (var entry in droneFirstPosition.Positions)
                        {
                            string droneID = entry.Key; // The actual ID, e.g., "radio://0/80/2M/E7E7E7E701"
                            float[] positionData = entry.Value; // The position array [x, y, z, yaw]

                            if (positionData.Length == 4) // Ensure the position data is valid
                            {
                                // Add a new DroneInformation object for each drone
                                droneInformationClient.Add(new DroneInformation
                                {
                                    droneIP = droneID,
                                    takeoff = false,
                                    dronePosition = new DronePosition
                                    {
                                        //ATTENTION : unity and the server have different axis server is x,y,z and unity is x,z,y
                                        positionInfo = true,
                                        positionDroneX = positionData[0],
                                        positionDroneY = positionData[2],
                                        positionDroneZ = positionData[1],
                                        rotationDroneYaw = positionData[3]
                                    },
                                    droneVelocity = new DroneVelocity
                                    {
                                        vitesseDroneX = 0,
                                        vitesseDroneY = 0,
                                        vitesseDroneZ = 0,
                                        vitesseDroneYaw = 0
                                    }
                                });
                            }
                            else
                            {
                                Debug.LogWarning($"Invalid position data for drone ID {droneID}: {string.Join(", ", positionData)}");
                            }
                        }

                        droneClientCreated = true;
                    }
                    else
                    {
                        Debug.LogWarning($"Unexpected message type: {droneFirstPosition.type}");
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Error processing message: {ex.Message}");
                }
            }
        }
        else if (droneClientCreated && DroneSwarmControle.droneInformation != null && DroneSwarmControle.droneInitialized)
        {

            DroneSpeedDataList droneSpeedDataList = new DroneSpeedDataList();

            if (DroneSwarmControle.droneInformation[0].takeoff == true && referenceToDroneSwarmControle.Controller)
            {


                for (int i = 0; i < DroneSwarmControle.droneInformation.Count; i++)
                {
                    DroneSpeedData data = new DroneSpeedData
                    {
                        //ATTENTION : unity and the server have different axis server is x,y,z and unity is x,z,y
                        droneIP = DroneSwarmControle.droneInformation[i].droneIP,
                        Vx = (float)Math.Round(DroneSwarmControle.droneInformation[i].droneVelocity.vitesseDroneX, 3),
                        Vy = (float)Math.Round(DroneSwarmControle.droneInformation[i].droneVelocity.vitesseDroneZ, 3),
                        Vz = (float)Math.Round(DroneSwarmControle.droneInformation[i].droneVelocity.vitesseDroneY, 3),
                        yaw_rate = (float)Math.Round(DroneSwarmControle.droneInformation[i].droneVelocity.vitesseDroneYaw, 3)
                    };
                    droneSpeedDataList.Velocity.Add(data);
                }

                DroneInstruction speedMessage = new DroneInstruction
                {
                    type = "speed",
                    droneSpeedDataList = droneSpeedDataList,
                    commandData = null
                };

                string json = JsonConvert.SerializeObject(speedMessage, Formatting.Indented);
                messagesToServerIRLQueue.Enqueue(json);
                Debug.Log("Sending speed message: " + json);
                messageAvailable.Set();
            }

                // Handle Takeoff Command
                if (referenceToDroneSwarmControle.takeOff && DroneSwarmControle.droneInformation[0].takeoff == false)
            {
                SendCommand("takeoff", new Dictionary<string, object>
                {
                    { "height", 0.5 }, // Example parameter: takeoff height
                    { "duration", 3.0 } // Example parameter: takeoff duration 
                });
                referenceToDroneSwarmControle.takeOff = false;
            }

            // Handle Land Command
            if (DroneSwarmControle.droneInformation[0].takeoff == true && referenceToDroneSwarmControle.land)
            {
                SendCommand("land", new Dictionary<string, object>
                {
                    { "height", 0.0 }, // Land to ground level
                    { "duration", 5.0 } // Example parameter: landing duration
                });
                referenceToDroneSwarmControle.land = false;
            }

            if (messagesFromServerIRLQueue.TryDequeue(out messageFromIRLServer))
            {
                //Debug.Log("Message from IRL server: " + messageFromIRLServer);

                try
                {
                    // Deserialize the message
                    var dronePositionResponse = JsonConvert.DeserializeObject<DronePositionResponse>(messageFromIRLServer);

                    // Check the message type
                    if (dronePositionResponse.type == "Positions")
                    {
                        //Debug.Log("Processing Positions message...");

                        // Handle the position data
                        foreach (var droneData in dronePositionResponse.Positions)
                        {
                            var droneInfo = DroneSwarmControle.droneInformation.FirstOrDefault(
                                d => d.droneIP == droneData.Key);

                            if (droneInfo != null)
                            {
                                //ATTENTION : unity and the server have different axis server is x,y,z and unity is x,z,y
                                droneInfo.dronePosition.positionDroneX = droneData.Value[0];
                                droneInfo.dronePosition.positionDroneY = droneData.Value[2];
                                droneInfo.dronePosition.positionDroneZ = droneData.Value[1];
                                droneInfo.dronePosition.rotationDroneYaw = droneData.Value[3];
                            }
                        }
                    }
                    else if (dronePositionResponse.type == "status")
                    {
                        if (dronePositionResponse.additionalInformation == "takeoff initiated")
                        {
                            DroneSwarmControle.droneInformation[0].takeoff = true;
                            Debug.Log("Drone has taken off.");
                        }
                        else if (dronePositionResponse.additionalInformation == "land initiated")
                        {
                            DroneSwarmControle.droneInformation[0].takeoff = false;
                            Debug.Log("Drone has landed.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Unhandled message type: {dronePositionResponse.type}");
                    }

                    // Log additional information if available
                    if (!string.IsNullOrEmpty(dronePositionResponse.additionalInformation))
                    {
                        Debug.Log($"Additional Info: {dronePositionResponse.additionalInformation}");
                    }
                }
                catch (JsonSerializationException ex)
                {
                    Debug.LogError("Error parsing message: " + ex.Message);
                }
            }



        }
    }   

    void OnApplicationQuit()
    {
        cancellationTokenSource.Cancel();
        stream.Close();
    }
}
