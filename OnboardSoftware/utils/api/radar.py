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
