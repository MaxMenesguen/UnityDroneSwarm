# Drone Swarm

## Description

The Drone Swarm project is an application designed to control Crazyflie 2.x drones from Bitcraze using Unity with a Python backend. It provides a dual-component system for simulating and controlling drone swarms, featuring two distinct modes:

1. **Simulation Mode**: Safely test swarm behavior without risk. The simulation closely replicates real-world drone dynamics and behavior.
2. **Real-World Control Mode**: Use Unity to send commands to Crazyflie drones, leveraging the behavior validated in simulation mode.

With a range of advanced features, this project makes it easy to control and test Crazyflie drones efficiently and effectively.

### Key Features

- **Simulation Environment**:
  - High-fidelity drone behavior simulation with boid-like dynamics.
  - Simple file-based programming to modify and add drone behaviors.
  - Automatic obstacle and boundary creation with adjustable parameters.
  - Real-time visual feedback in Unity.

- **Real-World Application**:
  - Integration with Crazyflie drones for live testing.
  - Automatic TCP server setup in Python and connection from Unity.
  - Command-based control system including **Take Off**, **Land**, and more.
  - Automatic control of drones from Unity by sending velocity vectors to the Python backend.
  - Demonstration of multiple pre-implemented behaviors on real drones.

- **Custom Flight Area Tool**:
  - Quickly create and save custom flight areas by defining corner points and height.
  - Saved configurations are stored in JSON for easy reuse.

---

## Installation

### 1. Clone the Repository

```bash
git clone https://github.com/MaxMenesguen/UnityDroneSwarm
```

### 2. Install Unity

- Download and install Unity Hub from the [official Unity website](https://unity.com/).
- Use Unity version 2022.3.11f1.

### 3. Install Python Environment

- Set up a Python environment using `venv` or `conda`.
- Install the required Python dependencies:

```bash
pip install -r requirements.txt
```

---

## Running the Application

### 1. Launching the Unity Simulation

1. Open Unity Hub and add the project folder **Drone3.0**.
2. Set the Unity version to 2022.3.11f1.
3. In the Unity Editor:
   - Locate the Scene in **Assets/Scenes/Drone3.0_scene.unity**.
   - Locate the **SwarmGameObject** in the hierarchy and click on it.
   - Enable the **Drone Simulation** checkbox in the Inspector.
4. Adjust simulation parameters in the Inspector:
   - **Boid Bounding Box Size**: Use the Boundary Box Manager to define and customize your flight area by selecting Simple Cube, Custom Area, or Area Creation modes.
   - **Number of Boids**: Set the number of simulated drones.
   - **Number of Obstacles**: Add random obstacles within the area.
   - **Attraction Object**: Introduce an object to attract boids during the simulation.
5. Customize boid behavior:
   - Go to **Assets/Prefab/DroneBehaviorController** and modify parameters such as:
     - No Clumping Radius
     - Local Area Radius
     - Speed
     - Steering Speed
     - Obstacle Avoidance Distance
     - Other drone behavior parameters.
6. Start the simulation by pressing the **Play** button.

---

### 2. Real-World Application with Crazyflie Drones

1. **Python Server Setup**:
   - Launch the Python backend to enable communication between Unity and the Crazyflie drones.
   - Navigate to the directory **Python/PythonSwarm** and run:

     ```bash
     python PythonServerTest.py
     ```
   - The server handles real-time position tracking and command relay from Crazyflie drones to Unity.

2. **Unity Controls**:
   - Enable the **Drone IRL** checkbox in the Inspector.
   - Use the following controls in Unity:
     - **Take Off**: Initiates drone takeoff.
     - **Land**: Safely lands all drones.
     - Select a behavior from the behavior dropdown menu.
     - Check the **Controller** checkbox to apply the selected behavior to drones after takeoff.

3. **Demo Ready**:
   - Observe real drones executing boid-like behaviors or custom commands.

---

### 3. Custom Flight Area Creation

1. Switch to **Area Creation Mode**:
   - In Unity, set the boundary mode to **Area Creation** in the Inspector.

2. Define Custom Area:
   - Manually set the four corner points of the flight area and the desired height.
   - Save the area configuration using the **Save Custom Area** button.

3. JSON Integration:
   - The custom area is saved in JSON format for easy reuse. Files are stored in **Assets/Scripts/SavedAreas**.

4. Start Simulation:
   - Switch to **Custom Area Mode** in Unity to load and use the saved area configuration.

---

## How It Works

### Unity-Python Integration

- **Unity as the Controller**: Sends takeoff, land, and velocity commands.
- **Python Backend**: Receives commands, processes them, and communicates with Crazyflie drones.
- **Real-Time Feedback**: Unity displays drone positions and updates based on Crazyflie’s state.

### Boid Simulation on Crazyflie Drones

- Drones mimic boid dynamics as demonstrated in the Unity simulation.
- Commands such as attraction to a target, obstacle avoidance, and alignment are directly translated to real drones.

---

## Example Screenshots

### 1. Unity Simulation



### 2. Custom Flight Area



### 3. Real Drone Execution



---

## Future Enhancements

- **Dynamic Command Input**: Allow real-time changes to drone behavior during operations.
- **Advanced Visuals**: Add visual indicators for flight boundaries and drone paths in Unity.
- **Computer Vision**: Implement obstacle detection within the flight area.
