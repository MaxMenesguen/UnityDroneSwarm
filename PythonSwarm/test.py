from cflib.crtp import init_drivers
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie.swarm import CachedCfFactory,Swarm

from cflib.crazyflie.log import LogConfig
import logging
import os
import time

# Setup logging
logging.basicConfig(level=logging.INFO)
logger = logging.getLogger(__name__)

# Initialize URIs
URIS = {
    'radio://0/80/2M/E7E7E7E701',
    'radio://0/28/2M/E7E7E7E703',
    #'radio://0/80/2M/E7E7E7E7E7'
}

uri = 'radio://0/80/2M/E7E7E7E701'

# Initialize factory and swarm
factory = CachedCfFactory(ro_cache='./cache', rw_cache='./cache')
swarm = Swarm(URIS, factory)

# Ensure a log directory exists
LOG_DIR = "./log"
os.makedirs(LOG_DIR, exist_ok=True)  # Creates the folder if it doesn't exist

# Path to the log file
LOG_FILE = os.path.join(LOG_DIR, "positions.txt")


def fetch_positions():
    """Connect to drones and fetch positions."""
    try:
        # Initialize drivers
        init_drivers()

        # Connect to drones
        swarm.open_links()

        try :
            # Fetch positions
            positions = swarm.get_estimated_positions()
            logger.info("Drone Positions: %s", positions)
            print("Drone Positions:", positions)
        except Exception as e:
            logger.error("Error during getposition: %s", e)

    except Exception as e:
        logger.error("Error during operation: %s", e)

    finally:
        # Ensure links are closed
        swarm.close_links()


def log_positions():
    """Log 100 drone positions with timestamps and intervals."""
    init_drivers()

    try:
        # Open links to the swarm
        swarm.open_links()

        # Initialize previous timestamp
        prev_time = time.time()

        with open(LOG_FILE, "w") as log_file:
            log_file.write("Drone Position Logs\n")
            log_file.write("Format: Timestamp, x, y, z, yaw, Interval\n\n")

            intervals = []  # To store time intervals between requests

            for i in range(100):
                # Fetch positions
                positions = swarm.get_estimated_positions()

                # Get current timestamp
                current_time = time.time()

                # Calculate interval
                interval = current_time - prev_time
                intervals.append(interval)

                # Update prev_time for the next loop
                prev_time = current_time

                # Log each position
                for uri, pos in positions.items():
                    log_entry = (
                        f"Timestamp: {current_time:.2f}, "
                        f"x: {pos.x:.2f}, y: {pos.y:.2f}, z: {pos.z:.2f}, yaw: {pos.yaw:.2f}, "
                        f"Interval: {interval:.2f}s\n"
                    )
                    log_file.write(log_entry)
                    logger.info(log_entry.strip())

                # Sleep for a short time (optional, to avoid overwhelming the system)
                #time.sleep(0.1)

            # Log the average interval
            avg_interval = sum(intervals) / len(intervals)
            log_file.write(f"\nAverage Interval: {avg_interval:.2f}s\n")
            logger.info(f"Average Interval: {avg_interval:.2f}s")

    except Exception as e:
        logger.error(f"Error during logging: {e}")

    finally:
        # Close links to the swarm
        swarm.close_links()

# Global positions storage
positions = {}

def position_callback(timestamp, data, logconf, uri):
    """Callback to handle received position data."""
    positions[uri] = {
        'x': data['stateEstimate.x'],
        'y': data['stateEstimate.y'],
        'z': data['stateEstimate.z'],
        'yaw': data['stateEstimate.yaw']
    }
    logger.info(f"Drone {uri} Position: {positions[uri]}")

def setup_logging(scf):
    """Set up logging for a single drone."""
    uri = scf.cf.link_uri  # Retrieve the URI from the SyncCrazyflie instance
    log_config = LogConfig(name='stateEstimate', period_in_ms=10)  # ~60 Hz
    log_config.add_variable('stateEstimate.x', 'FP16')
    log_config.add_variable('stateEstimate.y', 'FP16')
    log_config.add_variable('stateEstimate.z', 'FP16')
    log_config.add_variable('stateEstimate.yaw', 'FP16')

    scf.cf.log.add_config(log_config)
    log_config.data_received_cb.add_callback(lambda ts, d, lc: position_callback(ts, d, lc, uri))
    log_config.start()
    #debug
    logger.info(f"Logging started for {scf.cf.link_uri}")


def open_stream():
    """Open a persistent position stream with the swarm."""
    init_drivers()

    try:
        # Open swarm connections
        swarm.open_links()
        logger.info("Swarm connections opened.")

        # Set up logging for all drones in parallel
        swarm.parallel_safe(setup_logging)

        logger.info("Logging started for all drones. Streaming positions...")
        while True:
            pass  # Keep the program running for streaming

    except KeyboardInterrupt:
        logger.info("Shutting down...")
    finally:
        swarm.close_links()
        logger.info("Swarm connections closed.")

if __name__ == "__main__":
    open_stream()
