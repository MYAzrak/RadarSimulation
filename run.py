import subprocess
import json
import yaml
import os
import signal
import time
import websocket
import threading
import argparse
import numpy as np

class SimulationManager:
    def __init__(self, config_path, unity_exe_path, output_dir):
        self.config = self.load_config(config_path)
        self.unity_exe_path = unity_exe_path
        self.output_dir = output_dir
        self.simulation_process = None
        self.websocket_threads = []
        self.stop_event = threading.Event()

    def load_config(self, config_path):
        with open(config_path, 'r') as f:
            return yaml.safe_load(f)

    def start_simulation(self):
        cmd = [self.unity_exe_path]
        for key, value in self.config.items():
            cmd.extend([f"-{key}", str(value)])
            # print([f"-{key}", str(value)])
        
        self.simulation_process = subprocess.Popen(cmd, stdout=subprocess.PIPE, stderr=subprocess.PIPE)


    def collect_radar_data(self, radar_id):
        ws = websocket.WebSocketApp(f"ws://localhost:8080/radar{radar_id}",
                                    on_message=lambda ws, msg: self.on_message(radar_id, msg),
                                    on_error=lambda ws, err: print(f"Radar {radar_id} error: {err}"),
                                    on_close=lambda ws: print(f"Radar {radar_id} connection closed"))
        
        while not self.stop_event.is_set():
            ws.run_forever()
            if not self.stop_event.is_set():
                print(f"Reconnecting to radar {radar_id}...")
                time.sleep(5)  # Wait before reconnecting

    def on_message(self, radar_id, message):
        data = json.loads(message)
        timestamp = int(time.time())
        filename = f"{self.output_dir}/radar_{radar_id}_{timestamp}.json"

        # Extract the PPI array
        ppi = np.array(data['PPI'], dtype=np.float32)

        mean = np.mean(ppi)
        std = np.std(ppi)
        ppi = np.clip(ppi, 0, min(5000, mean + (2/3) * std))

        data['PPI'] = ppi.tolist()
        
        with open(filename, 'w') as f:
            json.dump(data, f)

    def run(self):
        self.start_simulation()
        time.sleep(10)  # Wait for the simulation to start up

        for i in range(self.config['nRadars']):
            thread = threading.Thread(target=self.collect_radar_data, args=(i,))
            thread.start()
            self.websocket_threads.append(thread)

        try:
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            print("Stopping simulation...")
            self.stop()

    def stop(self):
        self.stop_event.set()
        if self.simulation_process:
            self.simulation_process.terminate()
            self.simulation_process.wait()
        
        for thread in self.websocket_threads:
            thread.join()

if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Simulation Manager")
    parser.add_argument("config_path", help="Path to the configuration file")
    
    args = parser.parse_args()

    manager = SimulationManager(args.config_path, "", "")

    run_manager = False
    for key, value in manager.config.items():
        if key == "unityBuildDirectory":
            manager.unity_exe_path = os.path.expanduser(value)
        elif key == "outputDirectory":
            manager.output_dir = os.path.expanduser(value)
        elif key == "generateDataset":
            run_manager = value

    if run_manager:
        manager.run()
    else:
        manager.start_simulation()
