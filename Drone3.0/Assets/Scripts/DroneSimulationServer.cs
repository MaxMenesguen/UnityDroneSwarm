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
    public Vector3 droneSpaceEnd = new Vector3(5, 5, 5);
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
            var receiveTask = ReceiveMessagesAsync(connectedClient, token);
            var sendTask = SendMessagesAsync(connectedClient, token);

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
    async Task ReceiveMessagesAsync(TcpClient client, CancellationToken token)
    {
        var stream = client.GetStream();
        byte[] buffer = new byte[1024];

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

    async Task SendMessagesAsync(TcpClient client, CancellationToken token)
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
                }
                else
                {
                    Debug.Log("No matching number found.");
                }
            }
            
            
        }
        
        


        // Main game loop or equivalent
        // Process messages from messagesFromClientQueue
        // For each message, determine the target client and enqueue the response in the respective client's queue in clientMessageQueues
    }

    private List<DroneInformation> CreatFakeDrone(int numberOfDrones)
    {
        for (int i = 0; i < numberOfDrones; i++)
        {
            droneServerInformation.Add(new DroneInformation
            {
                droneIP = "SimulationDrone" + (i + 1).ToString(),
                takeoff = true,
                dronePosition = new DronePosition
                {
                    positionInfo = true,
                    positionDroneX = (float)Math.Round(UnityEngine.Random.Range(droneSpaceOrigin[0], droneSpaceEnd[0]), 2),
                    positionDroneY = (float)Math.Round(UnityEngine.Random.Range(droneSpaceOrigin[1], droneSpaceEnd[1]), 2),
                    positionDroneZ = (float)Math.Round(UnityEngine.Random.Range(droneSpaceOrigin[2], droneSpaceEnd[2]), 2),
                    rotationDroneYaw = (int)UnityEngine.Random.Range(0, 360)
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

    void OnDestroy()
    {
        // Ensure the listener is stopped when the GameObject is destroyed
        tcpListener?.Stop();
        cancellationTokenSource.Cancel(); // Signal all tasks to cancel
    }
}
//pseudocode
/*var messagesFromClientQueue = new ConcurrentQueue<string>();
var messagesToClientQueue = new ConcurrentQueue<string>();
void Start()
{
    //create TCP server
    //start task of ServerListener
    //start task of ServerSender
}
void Update()
{
    //try to deque messages from messagesFromClientQueue
    //do somthing with the message (get the direction of what the message is telling it to do)
    //moddify some internal values with the direction of the message
    //make a new json message 
    //que it on the messagesToClientQueue
}
public void TCPServerListener()
{
    while (true)
    {
        //listen for the client 
        //decode the message
        //enque the decrepted message
    }
}
public void TCPServerSender()
{
    while (true)
    {
        //try to deque messagesToClientQueue
        //encode message 
        //send it via the TCPserver to the client
    }
}*/
// Using concurrent collections for thread-safe operations