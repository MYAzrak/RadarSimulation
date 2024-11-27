import requests
from typing import Optional


def create_radar_with_id(
    radar_id: int,
    latitude: float = 0.0,
    longitude: float = 0.0,
    range_km: float = 100.0,
    azimuth_resolution: float = 1.0,
    base_url: str = "http://localhost:7777"
) -> Optional[dict]:
    """
    Wrapper function to create a radar with a specified ID.
    
    Args:
        radar_id (int): The ID to assign to the radar
        name (str, optional): Name of the radar. Defaults to "Default Radar"
        latitude (float, optional): Latitude position. Defaults to 0.0
        longitude (float, optional): Longitude position. Defaults to 0.0
        range_km (float, optional): Range in kilometers. Defaults to 100.0
        azimuth_resolution (float, optional): Azimuth resolution in degrees. Defaults to 1.0
        base_url (str, optional): Base URL of the API. Defaults to "http://localhost:7777"
    
    Returns:
        Optional[dict]: Response from the API if successful, None if failed
        
    Raises:
        requests.exceptions.RequestException: If the API call fails
    """
    try:
        payload = {
            "radar_id": radar_id,
            "latitude": latitude,
            "longitude": longitude,
            "range_km": range_km,
            "azimuth_resolution": azimuth_resolution
        }
        
        response = requests.post(f"{base_url}/radars/", json=payload)
        response.raise_for_status()  # Raises an HTTPError if the status is 4xx, 5xx
        
        return response.json()
        
    except requests.exceptions.RequestException as e:
        print(f"Error creating radar: {str(e)}")
        return None


def update_radar_location(
    radar_id: int,
    latitude: float,
    longitude: float,
    range_km: float,
    azimuth_resolution: float,
    base_url: str = "http://localhost:7777"
) -> Optional[dict]:
    """
    Wrapper function to update a radar's location.
    
    Args:
        radar_id (int): The ID of the radar to update
        latitude (float): New latitude position (-90 to 90)
        longitude (float): New longitude position (-180 to 180)
        base_url (str, optional): Base URL of the API. Defaults to "http://localhost:7777"
    
    Returns:
        Optional[dict]: Response from the API if successful, None if failed
        
    Raises:
        requests.exceptions.RequestException: If the API call fails
        ValueError: If latitude or longitude values are out of valid range
    """
    # Validate latitude and longitude ranges
    if not -90 <= latitude <= 90:
        raise ValueError("Latitude must be between -90 and 90 degrees")
    if not -180 <= longitude <= 180:
        raise ValueError("Longitude must be between -180 and 180 degrees")
    
    try:
        payload = {
            "latitude": latitude,
            "longitude": longitude,
            "range_km": range_km,
            "azimuth_resolution": azimuth_resolution
        }
        
        response = requests.patch(
            f"{base_url}/radars/{radar_id}/location",
            json=payload
        )
        response.raise_for_status()
        
        return response.json()
        
    except requests.exceptions.RequestException as e:
        print(f"Error updating radar location: {str(e)}")
        return None
import math
from typing import List, Tuple, Optional
import sys
from OnboardSoftware.utils.locations import getLatLong

def clearall(base_url: str = "http://localhost:7777") -> bool:
    """
    Helper function to clear all radars and their associated detections from the database.
    
    Args:
        base_url (str, optional): Base URL of the API. Defaults to "http://localhost:7777"
    
    Returns:
        bool: True if successful, False if failed
    """
    try:
        # Get all radars
        del_detections = requests.delete(f"{base_url}/radars")
        
        del_detections.raise_for_status()
            
        return True
        
    except requests.exceptions.RequestException as e:
        print(f"Error clearing database: {str(e)}")
        return False

def process_radar_detections(
    radar_id: int,
    radar_lat: float,
    radar_long: float,
    predictions: List[Tuple[float, float]],  # List of (scaled_distance, azimuth) tuples
    radar_range: float,
    ppi_max_distance: float,
    azimuth_resolution: float,
    base_url: str = "http://localhost:7777",
    confidence: float = 0.9,
    vessel_type: str = "UNKNOWN"
) -> List[Optional[dict]]:
    """
    Process radar detections by converting scaled distances and azimuths to geographic
    coordinates and adding them to the database.
    
    Args:
        radar_id (int): ID of the radar making the detections
        radar_lat (float): Latitude of the radar
        radar_long (float): Longitude of the radar
        predictions (List[Tuple[float, float]]): List of (scaled_distance, azimuth) tuples
        radar_range (float): Actual radar range in kilometers
        ppi_max_distance (float): Maximum distance in the PPI display units
        azimuth_resolution (float): Angular resolution of the radar in degrees
        base_url (str, optional): Base URL of the API. Defaults to "http://localhost:7777"
        confidence (float, optional): Confidence score for detections. Defaults to 0.9
        vessel_type (str, optional): Type of vessel detected. Defaults to "UNKNOWN"
    
    Returns:
        List[Optional[dict]]: List of API responses for each detection, None for failed detections
    """
    results = []
    del_req = requests.delete(f"{base_url}/detections/by_radar/{radar_id}")
    del_req.raise_for_status()

    for scaled_distance, azimuth_idx in predictions:
        try:
            # Scale azimuth by resolution to get actual angle in degrees
            azimuth = azimuth_idx * azimuth_resolution
            
            # Convert scaled distance to actual distance in meters
            actual_distance = (scaled_distance / ppi_max_distance) * (radar_range )  # Convert km to meters
            
            # Convert polar coordinates (distance, azimuth) to cartesian coordinates (x, y)
            # Azimuth is in degrees, convert to radians for math functions
            # Azimuth 0° is North, increases clockwise
            azimuth_rad = math.radians(90 - azimuth)  # Convert to mathematical angle (0° = East, CCW)
            
            x = actual_distance * math.cos(azimuth_rad)  # East-West distance
            y = actual_distance * math.sin(azimuth_rad)  # North-South distance
            
            # Convert to latitude/longitude using the radar position as center
            lat, long = getLatLong(x, y, radar_lat, radar_long)
            
            # Create detection in database
            payload = {
                "radar_id": radar_id,
                "latitude": lat,
                "longitude": long,
                "confidence": confidence,
                "vessel_type": vessel_type
            }
            
            response = requests.post(f"{base_url}/detections/", json=payload)
            response.raise_for_status()
            results.append(response.json())
            
        except requests.exceptions.RequestException as e:
            print(f"Error creating detection: {str(e)}")
            results.append(None)
        except Exception as e:
            print(f"Error processing detection: {str(e)}")
            results.append(None)
    
    return results
