# -*- coding: utf-8 -*-
#
#     ||          ____  _ __
#  +------+      / __ )(_) /_______________ _____  ___
#  | 0xBC |     / __  / / __/ ___/ ___/ __ `/_  / / _ \
#  +------+    / /_/ / / /_/ /__/ /  / /_/ / / /_/  __/
#   ||  ||    /_____/_/\__/\___/_/   \__,_/ /___/\___/
#
#  Copyright (C) 2016 Bitcraze AB
#
#  This program is free software; you can redistribute it and/or
#  modify it under the terms of the GNU General Public License
#  as published by the Free Software Foundation; either version 2
#  of the License, or (at your option) any later version.
#
#  This program is distributed in the hope that it will be useful,
#  but WITHOUT ANY WARRANTY; without even the implied warranty of
#  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
#  GNU General Public License for more details.
# You should have received a copy of the GNU General Public License
# along with this program. If not, see <https://www.gnu.org/licenses/>.
import time
from collections import namedtuple
from threading import Thread, Lock

from cflib.crazyflie import Crazyflie
from cflib.crazyflie.log import LogConfig
from cflib.crazyflie.syncCrazyflie import SyncCrazyflie
from cflib.crazyflie.syncLogger import SyncLogger

SwarmPosition = namedtuple('SwarmPosition', 'x y z yaw')


class _Factory:
    """
    Default Crazyflie factory class.
    """

    def construct(self, uri):
        return SyncCrazyflie(uri)


class CachedCfFactory:
    """
    Factory class that creates Crazyflie instances with TOC caching
    to reduce connection time.
    """

    def __init__(self, ro_cache=None, rw_cache=None):
        self.ro_cache = ro_cache
        self.rw_cache = rw_cache

    def construct(self, uri):
        cf = Crazyflie(ro_cache=self.ro_cache, rw_cache=self.rw_cache)
        return SyncCrazyflie(uri, cf=cf)


class Swarm:
    """
    Runs a swarm of Crazyflies. It implements a functional-ish style of
    sequential or parallel actions on all individuals of the swarm.

    When the swarm is connected, a link is opened to each Crazyflie through
    SyncCrazyflie instances. The instances are maintained by the class and are
    passed in as the first argument in swarm wide actions.
    """

    def __init__(self, uris, factory=_Factory()):
        """
        Constructs a Swarm instance and instances used to connect to the
        Crazyflies

        :param uris: A set of uris to use when connecting to the Crazyflies in
        the swarm
        :param factory: A factory class used to create the instances that are
         used to open links to the Crazyflies. Mainly used for unit testing.
        """
        self._cfs = {}
        self._is_open = False
        self._positions = dict()
        self.log_config = dict()

        for uri in uris:
            self._cfs[uri] = factory.construct(uri)

    def open_links(self):
        """
        Open links to all individuals in the swarm
        """
        if self._is_open:
            raise Exception('Already opened')

        try:
            self.parallel(lambda scf: scf.open_link())
            self._is_open = True
        except Exception as e:
            self.close_links()
            raise e


    def close_links(self):
        """
        Close all open links
        """
        for uri, cf in self._cfs.items():
            cf.close_link()

        self._is_open = False

    def __enter__(self):
        self.open_links()
        return self

    def __exit__(self, exc_type, exc_val, exc_tb):
        self.close_links()

    def __get_estimated_position(self, scf):
        log_config = LogConfig(name='stateEstimate', period_in_ms=16)#changed from 50 to 16
        log_config.add_variable('stateEstimate.x', 'FP16')
        log_config.add_variable('stateEstimate.y', 'FP16')
        log_config.add_variable('stateEstimate.z', 'FP16')
        log_config.add_variable('stateEstimate.yaw', 'FP16')

        with SyncLogger(scf, log_config) as logger:
            for entry in logger:
                x = entry[1]['stateEstimate.x']
                y = entry[1]['stateEstimate.y']
                z = entry[1]['stateEstimate.z']
                yaw = entry[1]['stateEstimate.yaw']
                self._positions[scf.cf.link_uri] = SwarmPosition(x, y, z, yaw)
                break
        # log_config.stop()
        # log_config.delete()
        # log_config=None

    def position_callback1(self, timestamp, data, logconf):
        x = data['stateEstimate.x']
        y = data['stateEstimate.y']
        z = data['stateEstimate.z']
        yaw = data['stateEstimate.yaw']
        self._positions["radio://0/80/2M/E7E7E7E701"] = SwarmPosition(x, y, z, yaw)
    def position_callback2(self, timestamp, data, logconf):
        x = data['stateEstimate.x']
        y = data['stateEstimate.y']
        z = data['stateEstimate.z']
        yaw = data['stateEstimate.yaw']
        self._positions["radio://0/27/2M/E7E7E7E702"] = SwarmPosition(x, y, z, yaw)
    def position_callback3(self, timestamp, data, logconf):
        x = data['stateEstimate.x']
        y = data['stateEstimate.y']
        z = data['stateEstimate.z']
        yaw = data['stateEstimate.yaw']
        self._positions["radio://0/28/2M/E7E7E7E703"] = SwarmPosition(x, y, z, yaw)
    def position_callbackE7(self, timestamp, data, logconf):
        x = data['stateEstimate.x']
        y = data['stateEstimate.y']
        z = data['stateEstimate.z']
        yaw = data['stateEstimate.yaw']
        self._positions["radio://0/80/2M/E7E7E7E7E7"] = SwarmPosition(x, y, z, yaw)
    def _alt_prepare_estimator(self,scf: SyncCrazyflie):

        cf = scf.cf 
        self.log_config[scf.cf.link_uri] = LogConfig(name='stateEstimate', period_in_ms=10)
        self.log_config[scf.cf.link_uri].add_variable('stateEstimate.x', 'FP16')
        self.log_config[scf.cf.link_uri].add_variable('stateEstimate.y', 'FP16')
        self.log_config[scf.cf.link_uri].add_variable('stateEstimate.z', 'FP16')
        self.log_config[scf.cf.link_uri].add_variable('stateEstimate.yaw', 'FP16')
        self.log_config[scf.cf.link_uri].add_variable('stateEstimate.yaw', 'FP16')
        # self.log_config[scf.cf.link_uri].add_variable('', 'FP16')

        # self.log_conf2 = LogConfig(name='state', period_in_ms=100)
        # self.log_conf2.add_variable('ctrltarget.x', 'float')
        # self.log_conf2.add_variable('ctrltarget.y', 'float')
        # self.log_conf2.add_variable('ctrltarget.z', 'float')

        # self.log_conf2 = LogConfig(name='state', period_in_ms=10)
        # self.log_conf2.add_variable('zranging.offset', 'float')
        # self.log_conf2.add_variable('zranging.history', 'float')
        # self.log_conf2.add_variable('zranging.collect', 'float')
        cf.log.add_config(self.log_config[scf.cf.link_uri])
        self.callback = self.Callback(scf.cf.link_uri,self)
        self.log_config[scf.cf.link_uri].data_received_cb.add_callback(self.callback.position_callback)
        self.log_config[scf.cf.link_uri].start()

    class Callback():
        def __init__(self,uri, papa:super):
            self.uri = uri
            self.papa = papa

        def position_callback(self, timestamp, data, logconf):
            x = data['stateEstimate.x']
            y = data['stateEstimate.y']
            z = data['stateEstimate.z']
            yaw = data['stateEstimate.yaw']
            self.papa._positions[self.uri] = SwarmPosition(x, y, z, yaw)

    def alt_prepare_estimator(self):
        self.parallel_safe(self._alt_prepare_estimator)
    def _alt_get_estimated_position(self,scf:SyncCrazyflie):
        self._positions[scf.cf.link_uri]
    def alt_get_estimated_positions(self):
        """
        Return a `dict`, keyed by URI and with the SwarmPosition namedtuples as
        value, with the estimated (x, y, z) of each Crazyflie in the swarm.
        """
        self.parallel_safe(self._alt_get_estimated_position)
        return self._positions
    def get_estimated_positions(self):
        """
        Return a `dict`, keyed by URI and with the SwarmPosition namedtuples as
        value, with the estimated (x, y, z) of each Crazyflie in the swarm.
        """
        self.parallel_safe(self.__get_estimated_position)
        return self._positions
    
    def __wait_for_position_estimator(self, scf):
        log_config = LogConfig(name='Kalman Variance', period_in_ms=5000)
        log_config.add_variable('kalman.varPX', 'float')
        log_config.add_variable('kalman.varPY', 'float')
        log_config.add_variable('kalman.varPZ', 'float')

        var_y_history = [1000] * 10
        var_x_history = [1000] * 10
        var_z_history = [1000] * 10

        threshold = 0.001

        with SyncLogger(scf, log_config) as logger:
            for log_entry in logger:
                data = log_entry[1]

                var_x_history.append(data['kalman.varPX'])
                var_x_history.pop(0)
                var_y_history.append(data['kalman.varPY'])
                var_y_history.pop(0)
                var_z_history.append(data['kalman.varPZ'])
                var_z_history.pop(0)

                min_x = min(var_x_history)
                max_x = max(var_x_history)
                min_y = min(var_y_history)
                max_y = max(var_y_history)
                min_z = min(var_z_history)
                max_z = max(var_z_history)

                if (max_x - min_x) < threshold and (
                        max_y - min_y) < threshold and (
                        max_z - min_z) < threshold:
                    break

    def __reset_estimator(self, scf):
        cf = scf.cf
        cf.param.set_value('kalman.resetEstimation', '1')
        time.sleep(0.1)
        cf.param.set_value('kalman.resetEstimation', '0')
        self.__wait_for_position_estimator(scf)

    def reset_estimators(self):
        """
        Reset estimator on all members of the swarm and wait for a stable
        positions. Blocks until position estimators finds a position.
        """
        self.parallel_safe(self.__reset_estimator)

    def sequential(self, func, args_dict=None):
        """
        Execute a function for all Crazyflies in the swarm, in sequence.

        The first argument of the function that is passed in will be a
        SyncCrazyflie instance connected to the Crazyflie to operate on.
        A list of optional parameters (per Crazyflie) may follow defined by
        the `args_dict`. The dictionary is keyed on URI and has a list of
        parameters as value.

        Example:
        ```python
        def my_function(scf, optional_param0, optional_param1)
            ...

        args_dict = {
            URI0: [optional_param0_cf0, optional_param1_cf0],
            URI1: [optional_param0_cf1, optional_param1_cf1],
            ...
        }


        swarm.sequential(my_function, args_dict)
        ```

        :param func: The function to execute
        :param args_dict: Parameters to pass to the function
        """
        for uri, cf in self._cfs.items():
            args = self._process_args_dict(cf, uri, args_dict)
            func(*args)

    def parallel(self, func, args_dict=None):
        """
        Execute a function for all Crazyflies in the swarm, in parallel.
        One thread per Crazyflie is started to execute the function. The
        threads are joined at the end. Exceptions raised by the threads are
        ignored.

        For a more detailed description of the arguments, see `sequential()`

        :param func: The function to execute
        :param args_dict: Parameters to pass to the function
        """
        try:
            self.parallel_safe(func, args_dict)
        except Exception:
            pass

    def parallel_safe(self, func, args_dict=None):
        """
        Execute a function for all Crazyflies in the swarm, in parallel.
        One thread per Crazyflie is started to execute the function. The
        threads are joined at the end and if one or more of the threads raised
        an exception this function will also raise an exception.

        For a more detailed description of the arguments, see `sequential()`

        :param func: The function to execute
        :param args_dict: Parameters to pass to the function
        """
        threads = []
        reporter = self.Reporter()

        for uri, scf in self._cfs.items():
            args = [func, reporter] + \
                self._process_args_dict(scf, uri, args_dict)

            thread = Thread(target=self._thread_function_wrapper, args=args)
            threads.append(thread)
            thread.start()

        for thread in threads:
            thread.join()

        if reporter.is_error_reported():
            first_error = reporter.errors[0]
            raise Exception('One or more threads raised an exception when '
                            'executing parallel task') from first_error

    def _thread_function_wrapper(self, *args):
        reporter = None
        try:
            func = args[0]
            reporter = args[1]
            func(*args[2:])
        except Exception as e:
            if reporter:
                reporter.report_error(e)

    def _process_args_dict(self, scf, uri, args_dict):
        args = [scf]

        if args_dict:
            args += args_dict[uri]

        return args

    class Reporter:
        def __init__(self):
            self.error_reported = False
            self._errors = []

        @property
        def errors(self):
            return self._errors

        def report_error(self, e):
            self.error_reported = True
            self._errors.append(e)

        def is_error_reported(self):
            return self.error_reported
