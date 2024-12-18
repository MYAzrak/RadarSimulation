
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Collections.Generic;

public static class ExtensionMethods
{
    public static TaskAwaiter GetAwaiter(this UnityWebRequestAsyncOperation asyncOp)
    {
        var tcs = new TaskCompletionSource<object>();
        asyncOp.completed += obj => { tcs.SetResult(null); };
        return ((Task)tcs.Task).GetAwaiter();
    }
}

public class TerrainGenerator : MonoBehaviour
{
    [Header("Terrain Grid Settings")]
    [SerializeField] private double centerLatitude = 25.0657;
    [SerializeField] private double centerLongitude = 55.1713;
    [SerializeField] private float chunkSizeKm = 5f;
    [SerializeField] private int chunksEast = 2;
    [SerializeField] private int chunksSouth = 2;

    [Header("Terrain Settings")]
    [SerializeField] private int resolution = 256;
    [SerializeField] private float heightScale = 2.0f;
    [SerializeField] private float heightExaggeration = 1.5f;
    [SerializeField] private int zoom = 12;

    [Header("Material Settings")]
    [SerializeField] private Material terrainMaterial;

    private string mapboxApiKey = "pk.eyJ1IjoieW91c2lmYWxob3NhbmkiLCJhIjoiY2wzNGlueHJtMDRjaDNjbXE2MWdpdnZ1bSJ9.anr7scb4pI8DPYaipHOTcw";
    private List<Terrain> terrainChunks = new List<Terrain>();
    private Dictionary<Vector2Int, float[,]> heightmapCache = new Dictionary<Vector2Int, float[,]>();
    private float globalMinHeight = float.MaxValue;
    private float globalMaxHeight = float.MinValue;
    
    private class MapTile
    {
        public int X;
        public int Y;
        public int Zoom;
        public Texture2D Heightmap;
        public double North, South, East, West;
    }

    private List<MapTile> tiles = new List<MapTile>();

    private async void Start()
    {
        await GenerateTerrainGrid();
    }

    private async Task GenerateTerrainGrid()
    {
        Debug.Log($"Generating terrain grid: {chunksEast}x{chunksSouth} chunks");
        
        float chunkSize = chunkSizeKm * 1000;
        GameObject gridParent = new GameObject("TerrainGrid");
        gridParent.transform.position = Vector3.zero;

        // Calculate the total area covered
        double latExtent = (chunksSouth * chunkSizeKm) / 111.32;
        double lonExtent = (chunksEast * chunkSizeKm) / (111.32 * System.Math.Cos(centerLatitude * System.Math.PI / 180.0));

        double minLat = centerLatitude - (latExtent / 2);
        double maxLat = centerLatitude + (latExtent / 2);
        double minLon = centerLongitude - (lonExtent / 2);
        double maxLon = centerLongitude + (lonExtent / 2);

        // Get tile coordinates
        int minTileX = LongitudeToTileX(minLon, zoom);
        int maxTileX = LongitudeToTileX(maxLon, zoom);
        int minTileY = LatitudeToTileY(maxLat, zoom);
        int maxTileY = LatitudeToTileY(minLat, zoom);

        Debug.Log($"Downloading tiles from ({minTileX},{minTileY}) to ({maxTileX},{maxTileY})");

        // First pass: Download all tiles and find global height range
        for (int y = minTileY; y <= maxTileY; y++)
        {
            for (int x = minTileX; x <= maxTileX; x++)
            {
                string url = $"https://api.mapbox.com/v4/mapbox.terrain-rgb/{zoom}/{x}/{y}@2x.pngraw" +
                            $"?access_token={mapboxApiKey}";

                try
                {
                    Texture2D heightmap = await DownloadHeightmapTexture(url);
                    
                    MapTile tile = new MapTile
                    {
                        X = x,
                        Y = y,
                        Zoom = zoom,
                        Heightmap = heightmap,
                        North = TileYToLatitude(y, zoom),
                        South = TileYToLatitude(y + 1, zoom),
                        West = TileXToLongitude(x, zoom),
                        East = TileXToLongitude(x + 1, zoom)
                    };
                    
                    tiles.Add(tile);
                    UpdateGlobalHeightRange(heightmap);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Failed to download tile {x},{y}: {e.Message}");
                }
            }
        }

        Debug.Log($"Global height range: {globalMinHeight}m to {globalMaxHeight}m");

        // Second pass: Generate terrain chunks using global height range
        for (int z = 0; z < chunksSouth; z++)
        {
            for (int x = 0; x < chunksEast; x++)
            {
                double chunkCenterLat = minLat + (z + 0.5) * (latExtent / chunksSouth);
                double chunkCenterLon = minLon + (x + 0.5) * (lonExtent / chunksEast);
                
                float[,] heights = GenerateHeightmapForChunk(
                    chunkCenterLat + (latExtent / chunksSouth / 2),
                    chunkCenterLat - (latExtent / chunksSouth / 2),
                    chunkCenterLon - (lonExtent / chunksEast / 2),
                    chunkCenterLon + (lonExtent / chunksEast / 2)
                );
                
                heightmapCache.Add(new Vector2Int(x, z), heights);
                await GenerateTerrainChunk(x, z, chunkSize, gridParent);
            }
        }

        SetupTerrainNeighbors();
    }

    private void UpdateGlobalHeightRange(Texture2D heightmap)
    {
        for (int y = 0; y < heightmap.height; y++)
        {
            for (int x = 0; x < heightmap.width; x++)
            {
                Color pixel = heightmap.GetPixel(x, y);
                float height = DecodeHeight(pixel);
                globalMinHeight = Mathf.Min(globalMinHeight, height);
                globalMaxHeight = Mathf.Max(globalMaxHeight, height);
            }
        }
    }

    private float DecodeHeight(Color pixel)
    {
        float height = -10000f + ((pixel.r * 255f * 256f * 256f + pixel.g * 255f * 256f + pixel.b * 255f) * 0.1f);
        return height * heightExaggeration;
    }

    private float[,] GenerateHeightmapForChunk(double north, double south, double west, double east)
    {
        float[,] heights = new float[resolution, resolution];
        bool[,] heightSet = new bool[resolution, resolution];

        foreach (var tile in tiles)
        {
            if (tile.East < west || tile.West > east || tile.South > north || tile.North < south)
                continue;

            double overlapNorth = System.Math.Min(north, tile.North);
            double overlapSouth = System.Math.Max(south, tile.South);
            double overlapWest = System.Math.Max(west, tile.West);
            double overlapEast = System.Math.Min(east, tile.East);

            double tileWidth = tile.East - tile.West;
            double tileHeight = tile.North - tile.South;

            for (int y = 0; y < resolution; y++)
            {
                double lat = north - (y * (north - south) / (resolution - 1));
                if (lat > overlapNorth || lat < overlapSouth) continue;

                for (int x = 0; x < resolution; x++)
                {
                    double lon = west + (x * (east - west) / (resolution - 1));
                    if (lon < overlapWest || lon > overlapEast) continue;

                    int pixelX = (int)((lon - tile.West) / tileWidth * tile.Heightmap.width);
                    int pixelY = (int)((tile.North - lat) / tileHeight * tile.Heightmap.height);

                    if (pixelX >= 0 && pixelX < tile.Heightmap.width && pixelY >= 0 && pixelY < tile.Heightmap.height)
                    {
                        Color pixel = tile.Heightmap.GetPixel(pixelX, pixelY);
                        float height = DecodeHeight(pixel);
                        
                        if (!heightSet[y, x])
                        {
                            heights[y, x] = height;
                            heightSet[y, x] = true;
                        }
                    }
                }
            }
        }

        return heights;
    }

    private async Task GenerateTerrainChunk(int x, int z, float chunkSize, GameObject parent)
    {
        GameObject chunkObj = new GameObject($"TerrainChunk_{x}_{z}");
        chunkObj.transform.parent = parent.transform;
        chunkObj.transform.position = new Vector3(x * chunkSize, 0, -z * chunkSize);

        TerrainData terrainData = new TerrainData();
        terrainData.heightmapResolution = resolution;
        
        // Use actual height range for terrain size
        float terrainHeight = (globalMaxHeight - globalMinHeight) * heightScale;
        terrainData.size = new Vector3(chunkSize, terrainHeight, chunkSize);

        Terrain terrain = chunkObj.AddComponent<Terrain>();
        TerrainCollider collider = chunkObj.AddComponent<TerrainCollider>();
        
        terrain.terrainData = terrainData;
        collider.terrainData = terrainData;

        if (terrainMaterial != null)
        {
            terrain.materialTemplate = terrainMaterial;
        }

        terrainChunks.Add(terrain);

        // Apply heights using global normalization
        float[,] heights = heightmapCache[new Vector2Int(x, z)];
        float[,] normalizedHeights = new float[resolution, resolution];
        
        for (int y = 0; y < resolution; y++)
        {
            for (int x2 = 0; x2 < resolution; x2++)
            {
                normalizedHeights[y, x2] = Mathf.InverseLerp(globalMinHeight, globalMaxHeight, heights[y, x2]);
            }
        }

        terrainData.SetHeights(0, 0, normalizedHeights);
    }

    private void SetupTerrainNeighbors()
    {
        for (int z = 0; z < chunksSouth; z++)
        {
            for (int x = 0; x < chunksEast; x++)
            {
                int index = z * chunksEast + x;
                Terrain currentTerrain = terrainChunks[index];
                
                Terrain leftN = x > 0 ? terrainChunks[index - 1] : null;
                Terrain rightN = x < chunksEast - 1 ? terrainChunks[index + 1] : null;
                Terrain topN = z > 0 ? terrainChunks[index - chunksEast] : null;
                Terrain bottomN = z < chunksSouth - 1 ? terrainChunks[index + chunksEast] : null;

                currentTerrain.SetNeighbors(leftN, topN, rightN, bottomN);
            }
        }
    }

    private double TileYToLatitude(int y, int z)
    {
        double n = System.Math.PI - 2.0 * System.Math.PI * y / (double)(1 << z);
        return 180.0 / System.Math.PI * System.Math.Atan(0.5 * (System.Math.Exp(n) - System.Math.Exp(-n)));
    }

    private int LatitudeToTileY(double lat, int zoom)
    {
        double latRad = lat * System.Math.PI / 180.0;
        return (int)((1.0 - System.Math.Log(System.Math.Tan(latRad) + 1.0 / System.Math.Cos(latRad)) / System.Math.PI) / 2.0 * (1 << zoom));
    }

    private double TileXToLongitude(int x, int z)
    {
        return x / (double)(1 << z) * 360.0 - 180.0;
    }

    private int LongitudeToTileX(double lon, int zoom)
    {
        return (int)(((lon + 180.0) / 360.0 * (1 << zoom)));
    }

    private async Task<Texture2D> DownloadHeightmapTexture(string url)
    {
        using (UnityWebRequest request = UnityWebRequestTexture.GetTexture(url))
        {
            request.downloadHandler = new DownloadHandlerTexture(true);
            await request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                throw new System.Exception(request.error);
            }

            return ((DownloadHandlerTexture)request.downloadHandler).texture;
        }
    }
}
