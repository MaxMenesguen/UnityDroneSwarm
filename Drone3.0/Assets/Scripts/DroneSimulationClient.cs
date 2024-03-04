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


public class DroneSimulationClient : MonoBehaviour
{
    private TcpClient client;
    private NetworkStream stream;
    private int numberOfDrones = 0;
    private ConcurrentQueue<string> messagesFromServerQueue = new ConcurrentQueue<string>();
    private ConcurrentQueue<string> messagesToServerQueue = new ConcurrentQueue<string>();
    public static bool droneClientCreated = false;
    public static List<DroneInformation> droneInformationClient = new List<DroneInformation>();
    // Configuration
    private string serverIP = "127.0.0.1";
    private int serverPort = 8080;

    void Start()
    {
        numberOfDrones = DroneSwarmControle.numberOfDronesPublic;
        // Start the async operation without awaiting it
        Task.Run(async () =>
        {
            try
            {
                await ConnectToServer(numberOfDrones);
            }
            catch (Exception ex)
            {
                // Log the exception to Unity's console. Note that Debug.Log should be called from the main thread.
                // In a real application, consider using a thread-safe logging mechanism or scheduling the log to be posted back on the main thread.
                Debug.LogError(ex.Message);
            }
        });
    }


    async Task ConnectToServer(int numberOfDrones)
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
            buffer = new byte[1024];
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            string response = Encoding.ASCII.GetString(buffer, 0, bytesRead);
            messagesFromServerQueue.Enqueue(response);
            Debug.Log("Received: " + response);
        }
        catch (SocketException ex)
        {
            Debug.LogError("SocketException: " + ex.Message);
        }
    }

    string messageFromServer;
    private void Update()
    {
        if (!droneClientCreated) 
        { 
            if (messagesFromServerQueue.TryDequeue(out messageFromServer))
            {
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
        else
        {
            if (messagesFromServerQueue.TryDequeue(out messageFromServer))
            {

            }
        }
    }
    void OnDestroy()
    {
        // Close the stream and client when the object is destroyed
        stream?.Close();
        client?.Close();
    }
}
