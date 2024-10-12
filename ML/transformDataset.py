import os
import json
import argparse
import numpy as np
from math import atan2, degrees, sqrt


def load_json_file(file_path):
    with open(file_path, 'r') as f:
        return json.load(f)


def save_json_file(file_path, data):
    with open(file_path, 'w') as f:
        json.dump(data, f, indent=2)


def transform_to_relative_coords(ship_pos, radar_pos):
    return {
        'x': ship_pos['x'] - radar_pos['x'],
        'z': ship_pos['z'] - radar_pos['z']
    }


def scale_coordinates(coord, image_width, radar_range):
    return {
        'x': coord['x'] / radar_range * image_width,
        'z': coord['z'] / radar_range * image_width
    }


def cartesian_to_polar(x, z):
    distance = sqrt(x**2 + z**2)
    azimuth = degrees(atan2(z, x)) % 360
    return azimuth, distance


def is_in_range(coord, radar_range):
    return sqrt(coord['x']**2 + coord['z']**2) <= radar_range


def process_file(file_path):
    data = load_json_file(file_path)

    radar_pos = data['radarLocation']
    radar_range = data['range']

    # Extract image width from PPI shape
    image_width = np.array(data['PPI']).shape[1]
    print(image_width)

    transformed_ships = []

    for ship in data['ships']:
        # Transform to relative coordinates
        rel_pos = transform_to_relative_coords(ship['Position'], radar_pos)

        # Check if the ship is within range
        if is_in_range(rel_pos, radar_range):
            # Scale the coordinates
            scaled_pos = scale_coordinates(rel_pos, image_width, radar_range)

            # Transform to polar coordinates (azimuth, distance)
            azimuth, distance = cartesian_to_polar(
                scaled_pos['x'], scaled_pos['z'])

            transformed_ships.append({
                'Id': ship['Id'],
                'azimuth': azimuth,
                'distance': distance
            })

    # Update the original data with transformed ships
    data['transformedShips'] = transformed_ships

    return data


def main(input_directory, output_directory):
    # Create the output directory if it doesn't exist
    os.makedirs(output_directory, exist_ok=True)

    for filename in os.listdir(input_directory):
        if filename.endswith('.json'):
            input_file_path = os.path.join(input_directory, filename)
            output_file_path = os.path.join(output_directory, filename)

            updated_data = process_file(input_file_path)

            # Save the updated data to the new JSON file in the output directory
            save_json_file(output_file_path, updated_data)

            print(f"Processed {filename} and saved to {output_file_path}")


if __name__ == "__main__":
    parser = argparse.ArgumentParser(
        description="Process PPI data and transform ship coordinates.")
    parser.add_argument("input_directory",
                        help="Directory containing input JSON files")
    parser.add_argument("output_directory",
                        help="Directory to save processed JSON files")
    args = parser.parse_args()

    main(args.input_directory, args.output_directory)
