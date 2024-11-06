import math

CENTER_LAT = 25.3491
CENTER_LONG = 56.3487


def getLatLong(x: float, y: float, center_lat=CENTER_LAT, center_long=CENTER_LONG) -> tuple[float, float]:
    """
    Convert Unity x,y coordinates to latitude/longitude based on a center point.
    
    Args:
        x (float): Unity x coordinate (east-west position in meters)
        y (float): Unity y coordinate (north-south position in meters)
        center_lat (float): Latitude of the center point (degrees)
        center_long (float): Longitude of the center point (degrees)
    
    Returns:
        tuple[float, float]: (latitude, longitude) in degrees
    """
    # Earth's radius in meters
    EARTH_RADIUS = 6371000  # 6,371 km

    # Convert to radians for calculations
    center_lat_rad = math.radians(center_lat)
    center_long_rad = math.radians(center_long)

    # Calculate change in latitude
    # Moving north/south along a meridian: 1 meter = 1/Earth_radius radians
    delta_lat_rad = y / EARTH_RADIUS  # y is north-south distance
    
    # Calculate change in longitude
    # At given latitude, the radius of the parallel circle is R * cos(lat)
    # This affects how much a given east-west distance changes longitude
    delta_long_rad = x / (EARTH_RADIUS * math.cos(center_lat_rad))  # x is east-west distance

    # Calculate new coordinates in radians
    new_lat_rad = center_lat_rad + delta_lat_rad
    new_long_rad = center_long_rad + delta_long_rad

    # Convert back to degrees
    new_lat = math.degrees(new_lat_rad)
    new_long = math.degrees(new_long_rad)

    return (new_lat, new_long)

# Example usage
if __name__ == "__main__":
    # Example: Center point at Portland, OR
        
    # Test with some distances
    tests = [
        (0, 0),        # Should return center point
        (1000, 0),     # 1km east
        (0, 1000),     # 1km north
        (-1000, -1000) # 1km southwest
    ]
    
    for x, y in tests:
        lat, long = getLatLong(x, y)
        print(f"Unity coordinates ({x}m, {y}m) -> (lat: {lat:.6f}°, long: {long:.6f}°)")
