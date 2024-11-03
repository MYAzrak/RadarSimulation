import * as React from "react";
import Map from "react-map-gl";

export default function App() {
  return (
    <div className="w-screen h-screen">
      <Map
        mapboxAccessToken="pk.eyJ1IjoieW91c2lmYWxob3NhbmkiLCJhIjoiY2wzNGlueHJtMDRjaDNjbXE2MWdpdnZ1bSJ9.anr7scb4pI8DPYaipHOTcw"
        initialViewState={{
          latitude: 25.3491,
          longitude: 56.3487,
          zoom: 11,
        }}
        mapStyle="mapbox://styles/mapbox/dark-v9"
      />
    </div>
  );
}
