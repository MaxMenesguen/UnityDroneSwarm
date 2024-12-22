import socket
import json
import threading
import time
import queue
from SwarmController import SwarmController
import logging
#from test import open_stream
# Create a FIFO queue
message_recived_queue = queue.Queue()
message_to_send_queue = queue.Queue()
positions_from_cf_queue = queue.Queue()

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)


class DroneServer:
    def __init__(self, host='0.0.0.0', port=8080):
        self.server_address = (host, port)
        self.drone_server_information = []
        self.number_of_drones = None
        self.sock = None
        self.client_socket = None
        self.running = True  # Shared flag to control thread execution

    def start_server(self):
        # Create a socket and bind it to the server address
        self.sock = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.sock.bind(self.server_address)
        self.sock.listen(1)
        print(f"Server started on port {self.server_address[1]}.")
        # Accepting a client connection
        client_socket, client_address = self.sock.accept()
        print(f"Client connected from {client_address}")

        # Create threads to handle sending and receiving
        threading.Thread(target=self.handle_receive, args=(client_socket,), daemon=True).start()
        threading.Thread(target=self.handle_send, args=(client_socket,), daemon=True).start()
        
    def close(self):
        """Close the server and client sockets."""
        self.running = False  # Stop the threads
        if self.client_socket:
            print("Closing client connection...")
            self.client_socket.close()
            self.client_socket = None
        
        if self.sock:
            print("Shutting down server...")
            self.sock.close()
            self.sock = None
            


    def handle_receive(self, client_socket):
        try:
            while self.running:
                data = client_socket.recv(4096*4)  # Adjust buffer size if needed
                if not data:
                    break
                message = data.decode('ascii')
                print(f"Received from client: {message}")

                message_recived_queue.put(message)# Add the message to the queue
        except socket.error as e:
            print(f"Socket error: {e}")
        finally:
            client_socket.close()

    def handle_send(self, client_socket):
        try:
            while self.running:
                try:
                    message_dict = message_to_send_queue.get(timeout=0.001)  # Non-blocking with timeout
                    message = json.dumps(message_dict)
                    client_socket.sendall(message.encode('ascii'))
                except queue.Empty:
                    pass  # No messages to send, keep checking
        except socket.error as e:
            print(f"Socket error: {e}")
        finally:
            client_socket.close()
    
    def send_status(message_queue, status_info):
        status_message = {
            "type": "status",
            "additionalInformation": status_info
        }
        message_queue.put(status_message)


if __name__ == "__main__":

    URIS = {
    'radio://0/80/2M/E7E7E7E701',
    #'radio://0/28/2M/E7E7E7E703',
    #*'radio://0/80/2M/E7E7E7E7E7'
    }
    server = DroneServer(port=8080)
    swarm_controller = SwarmController(URIS, positions_from_cf_queue)
    try:
        target = swarm_controller.open_swarm()
        #wait 2s for the drones to connect
        #time.sleep(2)
        server.start_server()
        target = swarm_controller.start_logging()

        logger.info("drones position runed")

        #print recived messages
        try:
            while True:
                #Process received messages
                if not message_recived_queue.empty():
                    command_message = message_recived_queue.get()
                    try:
                        # Parse the JSON message
                        command_data = json.loads(command_message)
                        print("command_data:", command_data)

                        # Extract the high-level type of message
                        message_type = command_data.get("type", "")
                        print("message_type:", message_type)

                        if message_type == "command":
                            # Extract command details
                            command_details = command_data.get("commandData", {})
                            command = command_details.get("command", "")
                            parameters = command_details.get("parameters", {})
                            print("command:", command)
                            print("parameters:", parameters)

                            # Handle specific commands
                            if command == "takeoff":
                                height = parameters.get("height", 0.5)  # Default height 0.5m
                                duration = parameters.get("duration", 3.0)  # Default duration 3.0s
                                swarm_controller.takeoff(height, duration)
                                DroneServer.send_status(message_to_send_queue, "takeoff initiated")

                            elif command == "land":
                                height = parameters.get("height", 0.0)  # Default height 0.0m (ground)
                                duration = parameters.get("duration", 3.0)  # Default duration 3.0s
                                swarm_controller.land(height, duration)
                                DroneServer.send_status(message_to_send_queue, "land initiated")

                            else:
                                message_to_send_queue.put({"error": f"unknown command: {command}"})

                        elif message_type == "speed":
                            drone_speed_data_list = command_data.get("droneSpeedDataList", {}).get("Velocity", [])
                            print("drone_speed_data_list:", drone_speed_data_list)
                            for drone_speed in drone_speed_data_list:
                                drone_ip = drone_speed.get("droneIP")
                                vx = drone_speed.get("Vx", 0.0)
                                vy = drone_speed.get("Vy", 0.0)
                                vz = drone_speed.get("Vz", 0.0)
                                yaw_rate = drone_speed.get("yaw_rate", 0.0)

                                # Send the velocity command to the drone
                                logger.info(f"Sending velocity command to {drone_ip}: Vx={vx}, Vy={vy}, Vz={vz}, YawRate={yaw_rate}")
                                swarm_controller.send_velocity_command(drone_ip, vx, vy, vz, yaw_rate)
                                

                            #message_to_send_queue.put({"status": "speed command executed"})

                        else:
                            message_to_send_queue.put({"error": f"unknown message type: {message_type}"})

                    except json.JSONDecodeError as e:
                        print(f"JSON decode error: {str(e)}")
                        message_to_send_queue.put({"error": "invalid JSON"})


                # Process drone positions
                if not positions_from_cf_queue.empty():
                    # Get the latest positions dictionary from the queue
                    position_dict = positions_from_cf_queue.get()
                    #print("position_dict:", position_dict)

                    message_to_send_queue.put(position_dict)

                time.sleep(0.002)
        except KeyboardInterrupt:
            pass
    except (IndexError, KeyboardInterrupt) as e:
        logger.info("Error: ", e)
    finally:
        swarm_controller.close_swarm()
        server.close()
        
    
