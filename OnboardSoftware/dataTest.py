import websocket
import json

import numpy as np
import matplotlib.pyplot as plt


def create_ppi_plot(data, azimuth, range_bins, vmin=None, vmax=None):
    # Convert polar coordinates to cartesian
    theta = np.radians(azimuth)
    r, theta = np.meshgrid(range_bins, theta)
    x = r * np.cos(theta)
    y = r * np.sin(theta)

    # Create the plot
    fig, ax = plt.subplots(figsize=(10, 10), subplot_kw=dict(projection='polar'))
    
    # Plot the data
    im = ax.pcolormesh(theta, r, data, cmap='viridis', vmin=vmin, vmax=vmax)
    
    # Customize the plot
    ax.set_theta_zero_location("N")
    ax.set_theta_direction(-1)
    ax.set_rlabel_position(0)
    ax.set_title("PPI Plot")
    
    # Add a colorbar
    cbar = plt.colorbar(im, ax=ax)
    cbar.set_label('Intensity')

    return fig, ax

# Generate some example data




def on_message(ws, message):
    data = json.loads(message)
    ppi = data.get('PPI', 'NA')
    if ppi == "NA":
        return

    ppi = np.array(ppi)
    num_azimuth = ppi.shape[0]
    num_range = ppi.shape[1]
    
    azimuth = np.linspace(0, 360, num_azimuth)
    range_bins = np.linspace(0, num_range, num_range)
    
    print("shape:", ppi.shape)
    print("max", ppi.max())
    fig, ax = create_ppi_plot(ppi, azimuth, range_bins)
    plt.show()
    # print(f"Received data: {data.get('PPI', 'NA')}")

def on_error(ws, error):
    print(f"Error: {error}")

def on_close(ws, close_status_code, close_msg):
    print("Connection closed")

def on_open(ws):
    print("Connection opened")

if __name__ == "__main__":
    # websocket.enableTrace(True)
    ws = websocket.WebSocketApp("ws://localhost:8080/data",
                                on_message=on_message,
                                on_error=on_error,
                                on_close=on_close,
                                on_open=on_open)
    ws.run_forever()
