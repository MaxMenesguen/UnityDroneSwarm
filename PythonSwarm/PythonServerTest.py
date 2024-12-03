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


class DroneSimulationServer:
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
            




    def compute_drone_positions(self):
        while self.running: 
            # Check if there are any receved messages in the queue
            if not message_recived_queue.empty():
                message_dict = message_recived_queue.get()
                if "Number of drones" in message_dict:
                    self.number_of_drones = message_dict["Number of drones"]
                    self.create_fake_drones(self.number_of_drones)
                elif "Speed data" in message_dict:
                    speed_data_list = message_dict["Speed data"]
                    self.update_drone_positions(speed_data_list)
            # manage the message to send to the client
            time.sleep(0.002)

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
                if not message_to_send_queue.empty():
                    message_dict = message_to_send_queue.get()
                    message = json.dumps(message_dict)
                    client_socket.sendall(message.encode('ascii'))
                    print(f"Sent to client: {message}")
        except socket.error as e:
            print(f"Socket error: {e}")
        finally:
            client_socket.close()


if __name__ == "__main__":

    URIS = {
    'radio://0/80/2M/E7E7E7E701',
    'radio://0/28/2M/E7E7E7E703',
    #'radio://0/80/2M/E7E7E7E7E7'
    }
    server = DroneSimulationServer(port=8080)
    swarm_controller = SwarmController(URIS, positions_from_cf_queue)
    try:
        target = swarm_controller.open_swarm()
        server.start_server()
        target = swarm_controller.start_logging()

        logger.info("drones position runed")

        #print recived messages
        try:
            while True:
                # Process received messages
                if not message_recived_queue.empty():
                    message_dict = message_recived_queue.get()
                    logger.info(message_dict)

                # Process drone positions
                if not positions_from_cf_queue.empty():
                    # Get the latest positions dictionary from the queue
                    position_dict = positions_from_cf_queue.get()

                    # Convert the dictionary to JSON format
                    json_message = json.dumps(position_dict)

                    # Queue the JSON message for sending to Unity
                    message_to_send_queue.put(json_message)

                    # Log the JSON message for debugging
                    logger.info(f"Queued JSON message: {json_message}")

                time.sleep(0.002)
        except KeyboardInterrupt:
            # swarm_controller.close_swarm()
            # server.close_server()
            pass
    except IndexError  as e:
        # swarm_controller.close_swarm()
        # server.close_server()
        logger.info("Error: ", e)
    finally:
        swarm_controller.close_swarm()
        server.close()
        
    
    

############################################################################################################


    # def create_drone_response(self):
    #     # Prepare drone position data as a dictionary
    #     return {
    #         "Positions": {
    #             f"SimulationDrone{i+1}": [
    #                 round(drone["position_x"], 3),
    #                 round(drone["position_y"], 3),
    #                 round(drone["position_z"], 3),
    #                 round(drone["yaw"], 3)
    #             ]
    #             for i, drone in enumerate(self.drone_server_information)
    #         }
    #     }

    # def update_drone_positions(self, speed_data_list):
    #     # Update drone positions based on speed data received
    #     for drone_speed_data in speed_data_list.values():
    #         # Example: Update drone position based on speed and deltaTime
    #         # (You might need to adapt this part depending on how you send data)
    #         pass

    # def create_fake_drones(self, number_of_drones):
    #     for i in range(number_of_drones):
    #         self.drone_server_information.append({
    #             "drone_ip": f"SimulationDrone{i+1}",
    #             "position_x": 0.0,
    #             "position_y": 0.0,
    #             "position_z": 0.0,
    #             "yaw": 0.0,
    #             "velocity_x": 0.0,
    #             "velocity_y": 0.0,
    #             "velocity_z": 0.0,
    #             "yaw_rate": 0.0
    #         })