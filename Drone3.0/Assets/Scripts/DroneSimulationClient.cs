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
    public DroneSwarmControle referenceToDroneSwarmControle;

    private TcpClient client;
    private NetworkStream stream;
    private int numberOfDrones = 0;
    private float sizeOfBoidBoundingBox =0f;
    private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
    private ConcurrentQueue<string> messagesFromServerQueue = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> messagesToServerQueue = new ConcurrentQueue<string>();
    public static bool droneClientCreated = false;
    public static List<DroneInformation> droneInformationClient = new List<DroneInformation>();
    private ManualResetEventSlim messageAvailable = new ManualResetEventSlim(false);
    // Configuration
    private string serverIP = "127.0.0.1";
    private int serverPort = 8080;

    //private List<Double> reactionTime = new List<Double>(); //for ping test

    void Start()
    {
        referenceToDroneSwarmControle = FindObjectOfType<DroneSwarmControle>();
        numberOfDrones = referenceToDroneSwarmControle.NumberOfDrones;
        sizeOfBoidBoundingBox = referenceToDroneSwarmControle.SizeOfBoidBoundingBox; //still need to setup to use it 
        // Start the async operation without awaiting it
        Task.Run(async () =>
        {
            try
            {
                await ConnectToServer(numberOfDrones, sizeOfBoidBoundingBox, cancellationTokenSource.Token);
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
        bool conect = false;
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
            buffer = new byte[2048 * 4];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);

            if (response.StartsWith("{\"Positions\":{\"SimulationDrone1\":"))
            {
                
                conect = true;
                messagesFromServerQueue.Enqueue(response);
                // Handle Server messages in separate tasks
                var receiveTask = ReceiveMessagesAsyncClient(client, token);
                var sendTask = SendMessagesAsyncClient(client, token);

                await Task.WhenAll(receiveTask, sendTask); // Wait for any task to complete
            }
            else
            {
                int retryCount = 0;
                int maxRetries = 5;
                while (retryCount < maxRetries && !conect)
                {
                    try
                    {
                        await ConnectToServer(numberOfDrones, sizeOfBoidBoundingBox, token);

                        // Listen for a response from the server
                        buffer = new byte[1024];
                        bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        if (response.StartsWith("{\"Positions\":{\"SimulationDrone1\":"))
                        {
                            conect = true;
                        }

                    }
                    catch (Exception ex)
                    {
                        Debug.LogError(ex.Message);
                        retryCount++;
                        await Task.Delay(1000); // Wait before retrying
                    }
                }
            }

            Debug.Log("Received: " + response);
            
        }
        catch (SocketException ex)
        {
            Debug.LogError("SocketException: " + ex.Message);
        }
        //maybe cause bug
        /*finally
        {
            stream?.Close();
            client?.Close();
        }*/
        
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
        byte[] buffer = new byte[1024];
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
    DateTime startTime;
    DateTime endTime;
    //with 10000 operations i get 14,3 ms of ping time in local network
    //witch is pretty good
    private void Update()
    {
        if (!droneClientCreated) 
        { 
            if (messagesFromServerQueue.TryDequeue(out messageFromServer))
            {
                Debug.Log("Dequeued message : " + messageFromServer);
                DronePositionResponse droneFirstPosition = Newtonsoft.Json.JsonConvert.DeserializeObject<DronePositionResponse>(messageFromServer);
                for (int i = 0; i < droneFirstPosition.Positions.Count; i++)
                {
                    droneInformationClient.Add(new DroneInformation
                    {

                        droneIP = "SimulationDrone" + (i + 1).ToString(),
                        takeoff = true,
                        dronePosition = new DronePosition
                        {
                            positionInfo = true,
                            positionDroneX = droneFirstPosition.Positions["SimulationDrone" + (i + 1).ToString()][0],
                            positionDroneY = droneFirstPosition.Positions["SimulationDrone" + (i + 1).ToString()][1],
                            positionDroneZ = droneFirstPosition.Positions["SimulationDrone" + (i + 1).ToString()][2],
                            rotationDroneYaw = droneFirstPosition.Positions["SimulationDrone" + (i + 1).ToString()][3]
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
                droneClientCreated = true;
            }
                
        }
        //add a ready to be sent check?
        else if (droneClientCreated && DroneSwarmControle.droneInformation != null && DroneSwarmControle.droneInitialized)
        {
            List<DroneSpeedData> droneSpeedDataList = new List<DroneSpeedData>();

            for (int i = 0; i < DroneSwarmControle.droneInformation.Count; i++)
            {
                DroneSpeedData data = new DroneSpeedData
                {
                    droneIP = DroneSwarmControle.droneInformation[i].droneIP,
                    Vx = DroneSwarmControle.droneInformation[i].droneVelocity.vitesseDroneX,
                    Vy = DroneSwarmControle.droneInformation[i].droneVelocity.vitesseDroneY,
                    Vz = DroneSwarmControle.droneInformation[i].droneVelocity.vitesseDroneZ,
                    yaw_rate = DroneSwarmControle.droneInformation[i].droneVelocity.vitesseDroneYaw
                };
                droneSpeedDataList.Add(data);
            }

            // Serialize the list directly to JSON using Newtonsoft.Json
            string json = JsonConvert.SerializeObject(droneSpeedDataList, Formatting.Indented);
            Debug.Log("Serialized Speed JSON: " + json);
            messagesToServerQueue.Enqueue(json);
            messageAvailable.Set(); // Signal that a message is ready to be sent

            if (messagesFromServerQueue.TryDequeue(out messageFromServer))
            {
                Debug.Log("Dequeued message : " + messageFromServer);
                DronePositionResponse dronePositionReponse = Newtonsoft.Json.JsonConvert.DeserializeObject<DronePositionResponse>(messageFromServer);
                for (int i = 0; i < dronePositionReponse.Positions.Count; i++)
                {
                    DroneSwarmControle.droneInformation[i].dronePosition.positionDroneX = dronePositionReponse.Positions[DroneSwarmControle.droneInformation[i].droneIP][0];
                    DroneSwarmControle.droneInformation[i].dronePosition.positionDroneY = dronePositionReponse.Positions[DroneSwarmControle.droneInformation[i].droneIP][1];
                    DroneSwarmControle.droneInformation[i].dronePosition.positionDroneZ = dronePositionReponse.Positions[DroneSwarmControle.droneInformation[i].droneIP][2];
                    DroneSwarmControle.droneInformation[i].dronePosition.rotationDroneYaw = dronePositionReponse.Positions[DroneSwarmControle.droneInformation[i].droneIP][3];
                    
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
