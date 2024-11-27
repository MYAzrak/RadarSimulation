import React, { useState, useEffect } from "react";
import Map, { Marker } from "react-map-gl";

const REFRESH_INTERVAL = 10000; // 10 seconds

export default function App() {
  const [detections, setDetections] = useState([]);
  const [radars, setRadars] = useState([]);

  // Fetch recent detections
  const fetchDetections = async () => {
    try {
      const response = await fetch(
        "http://localhost:7777/detections/recent/?minutes=10000",
      );
      const data = await response.json();
      setDetections(data);
    } catch (error) {
      console.error("Error fetching detections:", error);
    }
    try {
      const response = await fetch("http://localhost:7777/radars");
      const data = await response.json();
      setRadars(data);
    } catch (error) {
      console.log("Error fetching radars:", error);
    }
  };

  // Initial fetch and setup interval
  useEffect(() => {
    fetchDetections();
    const interval = setInterval(fetchDetections, REFRESH_INTERVAL);
    return () => clearInterval(interval);
  }, []);

  return (
    <div className="w-screen h-screen">
      <Map
        mapboxAccessToken="pk.eyJ1IjoieW91c2lmYWxob3NhbmkiLCJhIjoiY2wzNGlueHJtMDRjaDNjbXE2MWdpdnZ1bSJ9.anr7scb4pI8DPYaipHOTcw"
        initialViewState={{
          latitude: 25.3491,
          longitude: 56.3487,
          zoom: 11,
        }}
        mapStyle='mapbox://styles/mapbox/standard-satellite'
        projection= 'globe'
      >
        {detections.map((detection) => (
          <Marker
            key={detection.detection_id}
            latitude={detection.latitude}
            longitude={detection.longitude}
          >
            <div className="w-3 h-3 bg-red-500 rounded-full" />
          </Marker>
        ))}
        {radars.map((radar) => (
          <Marker
            key={radar.radar_id}
            latitude={radar.latitude}
            longitude={radar.longitude}
          >
            <div className="w-3 h-3 bg-green-500 rounded-full" />
          </Marker>
        ))}
      </Map>
    </div>
  );
}
