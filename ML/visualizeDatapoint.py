import json
import argparse
import numpy as np
import matplotlib.pyplot as plt


def load_json_file(file_path):
    with open(file_path, 'r') as f:
        return json.load(f)


def plot_ppi_and_ships(data):
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
    for ship in data['transformedShips']:
        ship['azimuth'] = ship["azimuth"]/360 * width
        ax.plot(ship['azimuth'], ship['distance'], 'ro', markersize=5)
        ax.text(ship['azimuth'], ship['distance'], str(ship['Id']),
                fontsize=8, ha='right', va='bottom', color='white')

    # Set labels and title
    ax.set_xlabel('Azimuth (pixels)')
    ax.set_ylabel('Distance (pixels)')
    ax.set_title('PPI Visualization with Ship Positions')

    # Add azimuth markings
    azimuth_ticks = np.linspace(0, width, 9)
    azimuth_labels = ['0°', '45°', '90°', '135°',
                      '180°', '225°', '270°', '315°', '360°']
    ax.set_xticks(azimuth_ticks)
    ax.set_xticklabels(azimuth_labels)

    # Add distance markings
    ax.set_yticks(np.linspace(0, height, 6))

    # Add colorbar
    cbar = fig.colorbar(im, ax=ax, label='Intensity')

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
