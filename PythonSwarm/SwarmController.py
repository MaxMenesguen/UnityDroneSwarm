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
        log_config = LogConfig(name='stateEstimate', period_in_ms=16)
        log_config.add_variable('stateEstimate.x', 'FP16')
        log_config.add_variable('stateEstimate.y', 'FP16')
        log_config.add_variable('stateEstimate.z', 'FP16')
        log_config.add_variable('stateEstimate.yaw', 'FP16')

        def position_callback(timestamp, data, logconf):    
            self.positions[uri] = {
                'x': data['stateEstimate.x'],
                'y': data['stateEstimate.y'],
                'z': data['stateEstimate.z'],
                'yaw': data['stateEstimate.yaw']
            }
            #self.positions_from_cf_queue.put({uri: self.positions[uri]})  # Add to shared queue
            self.positions_from_cf_queue.put(self.positions.copy())
            #logger.info(f"Drone {uri} Position: {self.positions[uri]}")

        scf.cf.log.add_config(log_config)
        log_config.data_received_cb.add_callback(position_callback)
        log_config.start()

    def start_logging(self):
        """Start logging for all drones in the swarm."""
        self.swarm.parallel_safe(self.log_positions)

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
