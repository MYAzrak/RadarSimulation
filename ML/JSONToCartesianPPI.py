import json
import argparse
import numpy as np
import matplotlib.pyplot as plt


def load_json_file(file_path):
    with open(file_path, 'r') as f:
        return json.load(f)


def plot_ppi_and_ships(data):
    # Set global font size for all text elements
    plt.rcParams.update({'font.size': 14})  # Default font size for all elements
    
    # Extract PPI image data
    ppi_image = np.array(data['PPI']).T
    height, width = ppi_image.shape
    ppi_image = np.where(ppi_image != 0, 1, ppi_image)
    print(height, width)

    # Create a figure and axis
    fig, ax = plt.subplots(figsize=(12, 10))

    # Display the PPI image
    im = ax.imshow(ppi_image, cmap='viridis', origin='lower',
                   extent=[0, width, 0, height])

    # Plot transformed ships
    for ship in data['ships']:
        ship['Azimuth'] = ship["Azimuth"]/360 * width
        ship['Distance'] = ship['Distance']/int(data['range']) * height
        ax.plot(ship['Azimuth'], ship['Distance'], 'ro', markersize=5)
        ax.text(ship['Azimuth'], ship['Distance'], str(ship['Id']),
                fontsize=12,  # Increased font size for ship IDs
                ha='right', va='bottom', color='white')

    # Set labels and title with larger font sizes
    ax.set_xlabel('Azimuth (pixels)', fontsize=16)
    ax.set_ylabel('Distance (pixels)', fontsize=16)
    ax.set_title('PPI Visualization', fontsize=18)

    # Add Azimuth markings
    Azimuth_ticks = np.linspace(0, width, 9)
    Azimuth_labels = ['0°', '45°', '90°', '135°',
                      '180°', '225°', '270°', '315°', '360°']
    ax.set_xticks(Azimuth_ticks)
    ax.set_xticklabels(Azimuth_labels, fontsize=14)  # Increased tick label font size
    
    # Add Distance markings
    ax.set_yticks(np.linspace(0, height, 6))
    ax.set_yticklabels(ax.get_yticks(), fontsize=14)  # Increased tick label font size

    # Add colorbar with larger font
    cbar = fig.colorbar(im, ax=ax)
    cbar.set_label('Intensity', fontsize=16)  # Larger colorbar label
    cbar.ax.tick_labels = plt.getp(cbar.ax.axes, 'yticklabels')
    plt.setp(cbar.ax.get_yticklabels(), fontsize=14)  # Larger colorbar tick labels

    # Show the plot
    plt.tight_layout()
    plt.show()


def main(file_path):
    data = load_json_file(file_path)
    plot_ppi_and_ships(data)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Visualize PPI data and ship positions.")
    parser.add_argument("file_path", help="Path to the processed JSON file")
    args = parser.parse_args()

    main(args.file_path)