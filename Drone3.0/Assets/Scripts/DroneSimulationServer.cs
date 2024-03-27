using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine.Windows;
using System.Linq;
public class DroneSimulationServer : MonoBehaviour
{

    private List<DroneInformation> droneServerInformation = new List<DroneInformation>();
    private TcpListener tcpListener;
    private TcpClient connectedClient;
    private int serverPort = 8080; // Ensure this matches the server's listening port
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private ConcurrentQueue<string> messagesFromClientQueue = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> messagesToClientQueue = new ConcurrentQueue<string>();
    private ManualResetEventSlim messageAvailable = new ManualResetEventSlim(false);


    public int numberOfDrones = 5;
    private bool droneCreatedSent = false;
    //add link to the game object for the drone space 
    public Vector3 droneSpaceOrigin = new Vector3(0, 0, 0);
    public Vector3 droneSpaceEnd = new Vector3(1, 1, 1);
    void Awake()
    {
        

    }
    void Start()
    {
        Task.Run(async () =>
        {
            try
            {
                await StartServerAsync(cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                // Log the exception to Unity's console. Note that Debug.Log should be called from the main thread.
                // In a real application, consider using a thread-safe logging mechanism or scheduling the log to be posted back on the main thread.
                Debug.LogError(ex.Message);
            }
        });
    }  
    async Task StartServerAsync(CancellationToken token)
    {
        // Initialize and start the TCP listener
        tcpListener = new TcpListener(IPAddress.Any, serverPort);
        tcpListener.Start();
        Debug.Log($"Server started on port {serverPort}.");

        try
        {
            // Accept only one client
            connectedClient = await tcpListener.AcceptTcpClientAsync();
            Debug.Log("Client connected.");

            // Handle client in separate tasks
            var receiveTask = ReceiveMessagesAsyncServer(connectedClient, token);
            var sendTask = SendMessagesAsyncServer(connectedClient, token);

            await Task.WhenAny(receiveTask, sendTask); // Wait for any task to complete
        }
        catch (Exception ex)
        {
            Debug.LogError($"Server error: {ex.Message}");
        }
        finally
        {
            tcpListener.Stop();
        }
    }
    async Task ReceiveMessagesAsyncServer(TcpClient client, CancellationToken token)
    {
        var stream = client.GetStream();
        byte[] buffer = new byte[2048*4]; //originally 1024

        try
        {
            while (!token.IsCancellationRequested)
            {
                int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, token);
                if (bytesRead == 0) break; // Client closed connection
                string message = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                messagesFromClientQueue.Enqueue(message);
            }
        }
        catch (OperationCanceledException) { /* Graceful shutdown */ }
        catch (Exception ex) { Debug.LogError($"Receive error: {ex.Message}"); }
    }

    async Task SendMessagesAsyncServer(TcpClient client, CancellationToken token)
    {
        var stream = client.GetStream();

        try
        {
            while (!token.IsCancellationRequested)
            {
                if (messagesToClientQueue.TryDequeue(out string message))
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
        }
        catch (OperationCanceledException) { /* Graceful shutdown */ }
        catch (Exception ex) { Debug.LogError($"Send error: {ex.Message}"); }
    }

    String messageFromClient;
    void Update()
    {
        if (!droneCreatedSent)
        {
            if (messagesFromClientQueue.TryDequeue(out messageFromClient))
            {
                Debug.Log("message from client :" + messageFromClient + $" sent by {connectedClient}");
                // Regular expression to match the digits after the specific phrase
                string pattern = @"The number of drone you have to create is : (\d+)";

                Match match = Regex.Match(messageFromClient, pattern);
                if (match.Success)
                {
                    // Convert the matched value to an integer
                    numberOfDrones = int.Parse(match.Groups[1].Value);
                    Debug.Log($"Extracted number of drones: {numberOfDrones}");
                    messagesToClientQueue.Enqueue(ToJson(CreatFakeDrone(numberOfDrones)));
                    messageAvailable.Set();
                    droneCreatedSent = true;
                }
                else
                {
                    Debug.Log("No matching number found.");
                }
            }
            
        }
        else
        {
            if (messagesFromClientQueue.TryDequeue(out messageFromClient))
            {
                Debug.Log("message from client :" + messageFromClient + $" sent by {connectedClient}");
                List<DroneSpeedData> droneSpeedDataList = JsonConvert.DeserializeObject<List<DroneSpeedData>>(messageFromClient);
                //Debug
                /*foreach (var droneSpeedData in droneSpeedDataList)
                {
                    Debug.Log($"Drone {droneSpeedData.droneIP} has speed: Vx={droneSpeedData.Vx}, Vy={droneSpeedData.Vy}, Vz={droneSpeedData.Vz}, yaw_rate={droneSpeedData.yaw_rate}");
                }*/
                messagesToClientQueue.Enqueue(ToJson(UpdateDronePositions(droneServerInformation, droneSpeedDataList,Time.deltaTime)));
                messageAvailable.Set();
                droneCreatedSent = true;

            }
            

            //ping test
            /*if (messagesFromClientQueue.TryDequeue(out messageFromClient))
            {
                Debug.Log($"Server received: {messageFromClient}");

                int number = ExtractNumber(messageFromClient);
                if (number < 1000)
                {
                    int nextNumber = number + 1;
                    string messageToSend = nextNumber.ToString();
                    messagesToClientQueue.Enqueue(messageToSend);
                    messageAvailable.Set(); // Signal that a message is ready to be sent
                    Debug.Log($"Server sending number: {nextNumber}");
                }
            }*/
        }
    }
    // Utility method to extract numbers from server messages
    int ExtractNumber(string message)
    {
        // Assuming message is just a number for simplicity
        return int.TryParse(message, out int number) ? number : 0;
    }

    private List<DroneInformation> CreatFakeDrone(int numberOfDrones)
    {
        for (int i = 0; i < numberOfDrones; i++)
        {
            droneServerInformation.Add(new DroneInformation
            {
                droneIP = $"SimulationDrone{i+1}", //+ (i + 1).ToString(),
                takeoff = true,
                dronePosition = new DronePosition
                {
                    positionInfo = true,
                    positionDroneX = 0,//(float)Math.Round(UnityEngine.Random.Range(droneSpaceOrigin[0], droneSpaceEnd[0]), 2),
                    positionDroneY = 0,//(float)Math.Round(UnityEngine.Random.Range(droneSpaceOrigin[1], droneSpaceEnd[1]), 2),
                    positionDroneZ = 0,//(float)Math.Round(UnityEngine.Random.Range(droneSpaceOrigin[2], droneSpaceEnd[2]), 2),
                    rotationDroneYaw = (int)UnityEngine.Random.Range(0, 360)
                },
                droneVelocity = new DroneVelocity
                {
                    vitesseDroneX = 0,
                    vitesseDroneY = 0,
                    vitesseDroneZ = 0,
                    vitesseDroneYaw = 0
                }
            }) ;
        }
        return droneServerInformation;
    }
    
    private string ToJson(List<DroneInformation>  droneServerInformation)
    {
        DronePositionResponse dronePositionResponse = new DronePositionResponse
        {
            Positions = new Dictionary<string, float[]>()
        };

        foreach (var droneInfo in droneServerInformation)
        {
            dronePositionResponse.Positions.Add(droneInfo.droneIP, new float[] {
            droneInfo.dronePosition.positionDroneX,
            droneInfo.dronePosition.positionDroneY,
            droneInfo.dronePosition.positionDroneZ,
            droneInfo.dronePosition.rotationDroneYaw});
        }
        return  Newtonsoft.Json.JsonConvert.SerializeObject(dronePositionResponse);
    }

    //i dont think this is very optimized
    private List<DroneInformation> UpdateDronePositions(List<DroneInformation> droneServerInformations, List<DroneSpeedData> droneSpeedDataList, float deltaTime)
    {
        // Iterate through each drone information
        foreach (var droneInfo in droneServerInformations)
        {
            // Find the matching speed data based on droneIP
            var matchingSpeedData = droneSpeedDataList.FirstOrDefault(speedData => speedData.droneIP == droneInfo.droneIP);

            // If matching speed data is found, update the drone's position
            if (matchingSpeedData != null)
            {
                droneInfo.dronePosition.positionDroneX += matchingSpeedData.Vx * deltaTime;
                droneInfo.dronePosition.positionDroneY += matchingSpeedData.Vy * deltaTime;
                droneInfo.dronePosition.positionDroneZ += matchingSpeedData.Vz * deltaTime;
                droneInfo.dronePosition.rotationDroneYaw += matchingSpeedData.yaw_rate * deltaTime;

                // Ensure the yaw rotation stays within 0-360 degrees
                while (droneInfo.dronePosition.rotationDroneYaw >= 360f) droneInfo.dronePosition.rotationDroneYaw -= 360f;
                while (droneInfo.dronePosition.rotationDroneYaw < 0f) droneInfo.dronePosition.rotationDroneYaw += 360f;
            }
        }
        // Optional: Check for bounds or limits to drone movement here
        // This could involve checking if drones are within a predefined area
        // and adjusting their positions or velocities accordingly

        return droneServerInformations;
    }

    

    void OnDestroy()
    {
        // Ensure the listener is stopped when the GameObject is destroyed
        tcpListener?.Stop();
        cancellationTokenSource.Cancel(); // Signal all tasks to cancel
    }
}
