# Drone Swarm Simulator

## Description

This project offers a dual-component system designed for simulating and controlling a drone swarm with boid-like movement dynamics. The initiative is split into two main parts: a simulation environment and a real-world application interface.

### Simulation

The simulation component establishes a server to create virtual drones, managing these entities through UDP communication in JSON format. It inputs speed values for each drone, updates their positions, and communicates these updates back to the client. This process closely mirrors the communication methods used in real drone applications, allowing for a high-fidelity simulation of drone behavior.

### Real-World Application

Mirroring the simulation's interface, the real-world application connects to actual drones after their behavior has been validated within the simulation environment. This direct application lacks the virtual drone server, focusing on real-time interaction with physical drones.

## Installation

1. **Clone the Repository**
   - Use `git clone` followed by the repository URL to clone the project to your local machine.

2. **Install Unity**
   - Download and install Unity Hub from the [official Unity website](https://unity.com/).
   - Within Unity Hub, install Unity version 2022.3.11f1.

3. **Open the Project**
   - Open Unity Hub and navigate to the 'Projects' tab.
   - Click on 'Add' and select the cloned project directory.
   - Ensure the project is set to open with Unity version 2022.3.11f1.
   - If you encounter errors upon opening, it may be due to corrupted assets. Consider reinstalling any necessary assets to resolve these issues.

## Running the Simulation

1. **Launch the Simulation**
   - In the Unity Editor, locate the `SwarmGameObject` in the hierarchy.
   - In the Inspector, enable the `Drone Simulation` checkbox.

2. **Configure Simulation Parameters**
   - Adjust the following settings in the Inspector to customize the simulation:
     - **Size of Boid Bounding Box**: Defines the simulation area.
     - **Number of Boids**: Sets the number of drones in the simulation.
     - **Number of Obstacles**: Generates the specified number of random-sized obstacles within the bounding box.
     - **Create Attraction Object**: Introduces a red ball in the scene that attracts boids. This object can be moved during the simulation.

3. **Boid Behavior Customization**
   - Navigate to `Assets/Prefab/BoidPrefab` in the Project section.
   - Select the BoidPrefab to view and adjust its behavior parameters in the Inspector, including:
     - **No Clumping Radius**
     - **Local Area Radius**
     - **Speed**
     - **Steering Speed**
     - **Distance to Obstacle**
     - **Get Back to Center**

4. **Start the Simulation**
   - Press the Play button in Unity to begin the simulation and observe boid behavior.

## Real-World Application

1. **Configuration**
   - For real-drone operation, ensure only the `API Request` box is checked.

2. **Simulation Controls**
   - **Drone API Tracking**: Activates real-time drone tracking.
   - **Take Off**: Initiates drone takeoff.
   - Use **Go To Coordinate** for individual drone commands or activate the boid simulation for the swarm.

