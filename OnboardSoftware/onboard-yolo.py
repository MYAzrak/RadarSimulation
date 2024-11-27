import websocket
import json
import numpy as np
import threading
import argparse
import time
from ultralytics import YOLO
from Inference.yolo_infer import run_model
from utils.api.radar import create_radar_with_id, update_radar_location, process_radar_detections 
from utils.locations import getLatLong

# Only import matplotlib-related code if plotting is enabled
def setup_plotting():
    import matplotlib.pyplot as plt
    from matplotlib.animation import FuncAnimation
    fig, ax = plt.subplots(figsize=(10, 10), subplot_kw=dict(projection='polar'))
    return fig, ax, plt, FuncAnimation

class RadarProcessor:
    def __init__(self, radar_id, model_path, enable_color=False, clip_value=None, enable_plot=False):
        self.radar_id = radar_id
        self.model = YOLO(model_path)
        self.color = enable_color
        self.clip = clip_value
        self.enable_plot = enable_plot
        self.reconnect_delay = 5
        
        # Data storage
        self.latest_data = None
        self.latest_ships = None
        self.latest_gt = None
        self.radar_range = None
        self.data_lock = threading.Lock()
        
        # Initialize plotting if enabled
        if self.enable_plot:
            self.fig, self.ax, self.plt, self.FuncAnimation = setup_plotting()
            self.im = None
            self.cbar = None
            self.scatter = None
            self.scatter_gt = None
            self.legend = None
            
    def create_ppi_plot(self, data, azimuth, range_bins, ships, radar_range, gt):
        if not self.enable_plot:
            return None
            
        if self.clip:
            mean = np.mean(data)
            std = np.std(data)
            data = np.clip(data, 0, min(2000, mean + self.clip * std))

        if self.color:
            data = np.where(data != 0, 1, data)
        vmin = data.min()
        vmax = data.max()

        theta = np.radians(azimuth)
        r, theta = np.meshgrid(range_bins, theta)

        if self.im is None:
            self.im = self.ax.pcolormesh(theta, r, data, cmap='magma', vmin=vmin, vmax=vmax)
            self.ax.set_theta_zero_location("N")
            self.ax.set_theta_direction(-1)
            self.ax.set_rlabel_position(0)
            self.ax.set_title("PPI Plot")
            self.cbar = self.plt.colorbar(self.im, ax=self.ax)
            self.cbar.set_label('Intensity')
        else:
            self.im.set_array(data.ravel())
            self.im.set_clim(vmin=vmin, vmax=vmax)

        if len(ships) > 0:
            ship_thetas = np.radians([ship[1] / 720 * 360 for ship in ships])
            ship_distances = [ship[0] for ship in ships]
            
            if self.scatter is None:
                self.scatter = self.ax.scatter(ship_thetas, ship_distances,
                                       c='cyan', s=10, zorder=5, label="Predicted")
            else:
                self.scatter.set_offsets(np.column_stack((ship_thetas, ship_distances)))

        if len(gt) > 0:
            ship_thetas = np.radians([ship['Azimuth'] for ship in gt])
            ship_distances = [ship['Distance'] / int(radar_range) * data.shape[1] for ship in gt]
            
            if self.scatter_gt is None:
                self.scatter_gt = self.ax.scatter(ship_thetas, ship_distances,
                                          c='green', s=5, zorder=5, label="Ground Truth")
            else:
                self.scatter_gt.set_offsets(np.column_stack((ship_thetas, ship_distances)))
                
        if self.legend is None:
            self.legend = self.ax.legend(loc='upper left')

        return self.im, self.scatter, self.scatter_gt

    def on_message(self, ws, message):
        try:
            data = json.loads(message)
            ppi = data.get('PPI', 'NA')
            radar_loc_unity = data.get('radarLocation', 'NA')
            ground_truth = data.get('ships', [])
            r_range = data.get('range', 5000)
            
            if ppi == "NA":
                return
                
            ppi = np.array(ppi, dtype=np.float32)
            print(f"Max value location: {np.unravel_index(ppi.argmax(), ppi.shape)}")
            
            ships = run_model(ppi, self.model)
            
            lat, long = getLatLong(radar_loc_unity['x'], radar_loc_unity['z'])
            print(f"PPI shape: {ppi.shape}")
            
            try:
                update_radar_location(self.radar_id, lat, long, r_range//1000, ppi.shape[0])
                process_radar_detections(self.radar_id, lat, long, ships, r_range, ppi.shape[1], 360.0/ppi.shape[0])
            except Exception as e:
                print(f"Error reaching server: {e}")

            with self.data_lock:
                self.latest_data = ppi
                self.latest_ships = ships
                self.latest_gt = ground_truth
                self.radar_range = r_range
                
        except Exception as e:
            print(f"Error processing message: {e}")

    def update_plot(self, frame):
        if not self.enable_plot:
            return None
            
        with self.data_lock:
            if self.latest_data is not None:
                num_azimuth, num_range = self.latest_data.shape
                azimuth = np.linspace(0, 360, num_azimuth)
                range_bins = np.linspace(0, num_range, num_range)
                return self.create_ppi_plot(self.latest_data, azimuth, range_bins, 
                                          self.latest_ships, self.radar_range, self.latest_gt)

    def run(self):
        def on_error(ws, error):
            print(f"WebSocket error: {error}")

        def on_close(ws, close_status_code, close_msg):
            print(f"WebSocket connection closed: {close_status_code} - {close_msg}")

        def on_open(ws):
            print("WebSocket connection opened")

        while True:
            try:
                ws = websocket.WebSocketApp(
                    f"ws://localhost:8080/radar{self.radar_id}",
                    on_message=lambda ws, msg: self.on_message(ws, msg),
                    on_error=on_error,
                    on_close=on_close,
                    on_open=on_open
                )
                ws.run_forever()
            except Exception as e:
                print(f"WebSocket connection error: {e}")

            print(f"Connection lost. Reconnecting in {self.reconnect_delay} seconds...")
            time.sleep(self.reconnect_delay)

def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('-r', type=int, default=0, help='Radar ID')
    parser.add_argument('-c', '--color', action='store_true', help='Enable color output')
    parser.add_argument('-v', '--plot_ppi', action='store_true', help='Plot PPI Image')
    parser.add_argument('--model', type=str, default='best_model.pth', help='Path to model weights')
    parser.add_argument('--clip', type=int, default=0, help='Clip standard deviations')
    args = parser.parse_args()

    if not isinstance(args.r, int):
        print("Invalid Radar ID")
        return

    # Create radar with ID
    create_radar_with_id(radar_id=args.r)

    # Initialize radar processor
    processor = RadarProcessor(
        radar_id=args.r,
        model_path=args.model,
        enable_color=args.color,
        clip_value=args.clip if args.clip != 0 else None,
        enable_plot=args.plot_ppi
    )

    # Start WebSocket connection in a separate thread
    websocket_thread = threading.Thread(target=processor.run)
    websocket_thread.daemon = True
    websocket_thread.start()

    # Set up the animation if plotting is enabled
    if args.plot_ppi:
        ani = processor.FuncAnimation(processor.fig, processor.update_plot, interval=100, blit=False)
        processor.plt.show()
    else:
        # Keep the main thread alive
        try:
            while True:
                time.sleep(1)
        except KeyboardInterrupt:
            print("Shutting down...")

if __name__ == "__main__":
    main()
