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


public class DroneSimulationClient : MonoBehaviour
{
    #region variables
    public DroneSwarmControle referenceToDroneSwarmControle;

    private TcpClient client;
    private NetworkStream stream;
    public int numberOfSimuDrones = 0;
    private float sizeOfBoidBoundingBox =0f;
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private ConcurrentQueue<string> messagesFromServerQueue = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> messagesToServerQueue = new ConcurrentQueue<string>();
    public static bool droneClientCreated = false;
    public static List<DroneInformation> droneInformationClient = new List<DroneInformation>();
    private ManualResetEventSlim messageAvailable = new ManualResetEventSlim(false);
    #endregion
    // Configuration
    private string serverIP = "127.0.0.1";
    private int serverPort = 8080;

    //private List<Double> reactionTime = new List<Double>(); //for ping test

    void Start()
    {
        referenceToDroneSwarmControle = FindObjectOfType<DroneSwarmControle>();
        numberOfSimuDrones = referenceToDroneSwarmControle.NumberOfSimuDrones;
        sizeOfBoidBoundingBox = referenceToDroneSwarmControle.SizeOfBoidBoundingBox; //still need to setup to use it 
        // Start the async operation without awaiting it
        Task.Run(async () =>
        {
            try
            {
                await ConnectToServer(numberOfSimuDrones, sizeOfBoidBoundingBox, cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                // Log the exception to Unity's console. Note that Debug.Log should be called from the main thread.
                // In a real application, consider using a thread-safe logging mechanism or scheduling the log to be posted back on the main thread.
                Debug.LogError(ex.Message);
            }
        });
    }


    async Task ConnectToServer(int numberOfDrones,float sizeOfBoidBoundingBox, CancellationToken token)
    {
        
        try
        {
            
            client = new TcpClient(serverIP, serverPort);
            
            Debug.Log("Connected to the server.");

            stream = client.GetStream();

            // Send a message to the server
            string messageToSend = $"The number of drone you have to create is : {numberOfDrones}";
            byte[] buffer = Encoding.ASCII.GetBytes(messageToSend);
            await stream.WriteAsync(buffer, 0, buffer.Length);
            Debug.Log("Sent: " + messageToSend);

            // Listen for a response from the server
            buffer = new byte[2048 * 16];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            messagesFromServerQueue.Enqueue(response);
            // Handle Server messages in separate tasks
            var receiveTask = ReceiveMessagesAsyncClient(client, token);
            var sendTask = SendMessagesAsyncClient(client, token);

            await Task.WhenAll(receiveTask, sendTask); // Wait for any task to complete
            
            

            Debug.Log("Received: " + response);
            
        }
        catch (SocketException ex)
        {
            Debug.LogError("SocketException: " + ex.Message);
        }
        
        
    }
    async Task SendMessagesAsyncClient(TcpClient client, CancellationToken token)
    {
        var stream = client.GetStream();

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (messagesToServerQueue.TryDequeue(out string message))
                {
                    byte[] buffer = Encoding.ASCII.GetBytes(message);
                    await stream.WriteAsync(buffer, 0, buffer.Length, token);
                    messageAvailable.Reset();
                }
                else
                {
                    messageAvailable.Wait(token); // Wait until a message is available
                }
            }
            Debug.Log("Close SendMessagesAsyncClient");
        }
        catch (OperationCanceledException) { /* Graceful shutdown */ }
        catch (Exception ex) { Debug.LogError($"Send error: {ex.Message}"); }
    }
    async Task ReceiveMessagesAsyncClient(TcpClient client, CancellationToken token)
    {
        var stream = client.GetStream();
        byte[] buffer = new byte[2048*16];
        try
        {
            
            while (!token.IsCancellationRequested)
            {
                //try to put everything in try catch to get the error
                //try to not wave asyncrones method (ReadAsync)
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead == 0) break; // Connection closed
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                messagesFromServerQueue.Enqueue(message);
            }
        }
        catch (OperationCanceledException)
        {
            Debug.Log("Read operation was canceled.");
        }
        catch (IOException ex)
        {
            Debug.LogError($"Receive error: {ex.Message}");
        }
        finally
        {
            // Consider cleanup here if necessary
        }
    }

    string messageFromServer;
    //bool firstmessagesent = false; For ping test
    //DateTime startTime;
    //DateTime endTime;
    //with 10000 operations i get 14,3 ms of ping time in local network
    //witch is pretty good
    private void Update()
    {
        // Check if drone information is initialized
        if (!droneClientCreated)
        {
            if (messagesFromServerQueue.TryDequeue(out messageFromServer))
            {
                Debug.Log("Dequeued message: " + messageFromServer);

                // Deserialize the message into DronePositionResponse
                DronePositionResponse droneFirstPosition = Newtonsoft.Json.JsonConvert.DeserializeObject<DronePositionResponse>(messageFromServer);

                // Check the type of the message
                if (droneFirstPosition.type == "Positions")
                {
                    Debug.Log("Processing Positions message for drone initialization...");

                    for (int i = 0; i < droneFirstPosition.Positions.Count; i++)
                    {
                        string droneKey = "SimulationDrone" + (i + 1).ToString(); // Adjust based on your simulation keys
                        if (droneFirstPosition.Positions.ContainsKey(droneKey))
                        {
                            droneInformationClient.Add(new DroneInformation
                            {
                                droneIP = droneKey,
                                takeoff = true,
                                dronePosition = new DronePosition
                                {
                                    positionInfo = true,
                                    positionDroneX = droneFirstPosition.Positions[droneKey][0],
                                    positionDroneY = droneFirstPosition.Positions[droneKey][1],
                                    positionDroneZ = droneFirstPosition.Positions[droneKey][2],
                                    rotationDroneYaw = droneFirstPosition.Positions[droneKey][3]
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
                            Debug.LogWarning($"Key {droneKey} not found in Positions dictionary.");
                        }
                    }

                    droneClientCreated = true;
                }
                else
                {
                    Debug.LogWarning($"Unhandled message type: {droneFirstPosition.type}");
                }
            }
        }
        //add a ready to be sent check?
        //send the speed of the drones to the server
        //and receive the position of the drones from the server
        else if (droneClientCreated && DroneSwarmControle.droneInformation != null && DroneSwarmControle.droneInitialized)
        {
            // Prepare the speed data list
            DroneSpeedDataList droneSpeedDataList = new DroneSpeedDataList();

            for (int i = 0; i < DroneSwarmControle.droneInformation.Count; i++)
            {
                DroneSpeedData data = new DroneSpeedData
                {
                    droneIP = DroneSwarmControle.droneInformation[i].droneIP,
                    Vx = (float)Math.Round(DroneSwarmControle.droneInformation[i].droneVelocity.vitesseDroneX, 3),
                    Vy = (float)Math.Round(DroneSwarmControle.droneInformation[i].droneVelocity.vitesseDroneY, 3),
                    Vz = (float)Math.Round(DroneSwarmControle.droneInformation[i].droneVelocity.vitesseDroneZ, 3),
                    yaw_rate = (float)Math.Round(DroneSwarmControle.droneInformation[i].droneVelocity.vitesseDroneYaw, 3)
                };

                // Add to the speed data list
                droneSpeedDataList.Velocity.Add(data);
            }

            // Create the DroneInstruction object for speed
            DroneInstruction speedMessage = new DroneInstruction
            {
                type = "speed", // Specify that this is a speed message
                droneSpeedDataList = droneSpeedDataList,
                commandData = null // No command data for speed messages
            };

            // Serialize the DroneInstruction object to JSON
            string json = JsonConvert.SerializeObject(speedMessage, Formatting.Indented);

            
            //Debug.Log("Serialized Speed JSON: " + json);

            // Enqueue the JSON message to the server queue
            messagesToServerQueue.Enqueue(json);
            messageAvailable.Set(); // Signal that a message is ready to be sent

            if (messagesFromServerQueue.TryDequeue(out messageFromServer))
            {
                //Debug.Log("Dequeued message : " + messageFromServer);

                // Deserialize the message into DronePositionResponse
                DronePositionResponse dronePositionResponse = Newtonsoft.Json.JsonConvert.DeserializeObject<DronePositionResponse>(messageFromServer);

                // Check the type of the message
                if (dronePositionResponse.type == "Positions")
                {
                    //Debug.Log("Processing Positions message...");

                    for (int i = 0; i < dronePositionResponse.Positions.Count; i++)
                    {
                        DroneSwarmControle.droneInformation[i].dronePosition.positionDroneX = dronePositionResponse.Positions[DroneSwarmControle.droneInformation[i].droneIP][0];
                        DroneSwarmControle.droneInformation[i].dronePosition.positionDroneY = dronePositionResponse.Positions[DroneSwarmControle.droneInformation[i].droneIP][1];
                        DroneSwarmControle.droneInformation[i].dronePosition.positionDroneZ = dronePositionResponse.Positions[DroneSwarmControle.droneInformation[i].droneIP][2];
                        DroneSwarmControle.droneInformation[i].dronePosition.rotationDroneYaw = dronePositionResponse.Positions[DroneSwarmControle.droneInformation[i].droneIP][3];
                    }
                }
                else
                {
                    Debug.LogWarning($"Unhandled message type: {dronePositionResponse.type}");
                }
            }

        }
        //ping test
        /*else if (droneClientCreated && !firstmessagesent)
        {
            startTime = DateTime.Now;
            messagesToServerQueue.Enqueue("1");
            messageAvailable.Set(); // Signal that a message is ready to be sent
            firstmessagesent = true;
            Debug.Log("Client sending number: 1");
            
        }
        else if (droneClientCreated && firstmessagesent)
        {
            if (messagesFromServerQueue.TryDequeue(out messageFromServer))
            {
                int number = ExtractNumber(messageFromServer);
                if (number < 1000)
                {
                    DateTime endTime = DateTime.Now;
                    reactionTime.Add((endTime - startTime).TotalMilliseconds);
                    int nextNumber = number + 1;
                    string messageToSend = nextNumber.ToString();
                    messagesToServerQueue.Enqueue(messageToSend);
                    messageAvailable.Set(); // Signal that a message is ready to be sent
                    startTime = DateTime.Now;
                    Debug.Log($"Client sending number: {nextNumber}");
                }
                //Debug
                else
                {
                    DateTime endTime = DateTime.Now;
                    reactionTime.Add((endTime - startTime).TotalMilliseconds);
                    double averageTime = reactionTime.Average();
                    Debug.Log("Average time of Network: " + averageTime);
                    
                    //OnDestroy(); // Reuse the cleanup logic
                }
            }
        }*/
    }
    // Utility method to extract numbers from server messages
    int ExtractNumber(string message)
    {
        // Assuming message is just a number for simplicity
        return int.TryParse(message, out int number) ? number : 0;
    }
    void OnDestroy()
    {
        

        // Check if the stream and client exist and close them if they do
        if (stream != null)
        {
            try
            {
                stream.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error closing stream: {ex.Message}");
            }
        }
        if (client != null)
        {
            try
            {
                client.Close();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Error closing client: {ex.Message}");
            }
        }
        cancellationTokenSource.Cancel(); // Signal all ongoing operations to cancel
        
    }

    //debug method
    private void OnApplicationQuit()
    {
        OnDestroy(); // Reuse the cleanup logic
    }
}
