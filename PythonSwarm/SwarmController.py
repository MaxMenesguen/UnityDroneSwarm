from cflib.crazyflie.log import LogConfig
from swarm import Swarm
from cflib.crtp import init_drivers
import logging
from cflib.crazyflie import Crazyflie
from cflib.crazyflie.swarm import CachedCfFactory
import time




# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

class SwarmController:
    def __init__(self, uris, positions_from_cf_queue):
        self.uris = uris
        self.positions_from_cf_queue = positions_from_cf_queue
        self.positions = {}
        self.swarm = None

    def open_swarm(self):
        """Initialize and open links to the swarm with caching."""
        init_drivers()
        
        # Initialize the Crazyflie factory with caching
        cf_factory = CachedCfFactory(rw_cache='./cache', ro_cache='./cache')

        # Create the Swarm instance using the factory
        self.swarm = Swarm(self.uris, factory=cf_factory)

        # Open links to all drones in the swarm
        self.swarm.open_links()
        logger.info("Swarm connections opened with caching.")

    def close_swarm(self):
        """Close links to the swarm."""
        if self.swarm:
            self.swarm.close_links()
            logger.info("Swarm connections closed.")

    def log_positions(self, scf):
        """Setup logging for position variables."""
        uri = scf.cf.link_uri
        log_config = LogConfig(name='stateEstimate', period_in_ms=40)
        log_config.add_variable('stateEstimate.x', 'float')
        log_config.add_variable('stateEstimate.y', 'float')
        log_config.add_variable('stateEstimate.z', 'float')
        log_config.add_variable('stateEstimate.yaw', 'float')

        def position_callback(timestamp, data, logconf):
            # Update the drone's position in the positions dictionary as an array
            self.positions[uri] = [
                data['stateEstimate.x'],
                data['stateEstimate.y'],
                data['stateEstimate.z'],
                data['stateEstimate.yaw']
            ]
            #logger.info(f"Drone {uri} Position: {self.positions[uri]}")

            if len(self.positions) == len(self.uris):
                # Prepare the "Positions" dictionary structure with a type field
                positions_message = {
                    "type": "Positions",
                    "Positions": self.positions.copy()
                }
                # self.log_counter = getattr(self, "log_counter", 0)
                # if self.log_counter % 10 == 0:  # Log every 10th update
                #     logger.info(f"Positions: {positions_message}")
                # self.log_counter += 1
                self.positions_from_cf_queue.put(positions_message)  # Add to the queue

        scf.cf.log.add_config(log_config)
        log_config.data_received_cb.add_callback(position_callback)
        
        log_config.start()

    def start_logging(self):
        """Start logging for all drones in the swarm."""
        self.swarm.parallel_safe(self.log_positions)
    
    def takeoff(self, height, duration):
        """Command all drones in the swarm to take off."""
        def takeoff_drone(scf):
            commander = scf.cf.high_level_commander
            commander.takeoff(height, duration)
        
        self.swarm.parallel_safe(takeoff_drone)
        logger.info(f"Takeoff command issued: height={height}m, duration={duration}s")

    def land(self, height, duration):
        """Command all drones in the swarm to land."""
        def land_drone(scf):
            commander = scf.cf.high_level_commander
            commander.land(height, duration)
        
        self.swarm.parallel_safe(land_drone)
        logger.info(f"Land command issued: height={height}m, duration={duration}s")

    def send_velocity_command(self, drone_ip, vx, vy, vz, yaw_rate):
        """Send a velocity command to a specific Crazyflie drone."""
        def set_velocity(scf):
            if scf.cf.link_uri == drone_ip:
                commander = scf.cf.commander
                commander.send_velocity_world_setpoint(vx, vy, vz, yaw_rate)

        self.swarm.parallel_safe(set_velocity)
        logger.info(f"Sent velocity to {drone_ip}: Vx={vx}, Vy={vy}, Vz={vz}, YawRate={yaw_rate}")

    def run(self):
        """Run the swarm controller in a thread."""
        try:
            
            self.open_swarm()
            #time.sleep(5)  # Allow time for TOC and other services to initialize
            self.start_logging()
            logger.info("started Streaming positions...")
            
            
        except KeyboardInterrupt:
            logger.info("Shutting down SwarmController...")
        # finally:
        #     self.close_swarm()
