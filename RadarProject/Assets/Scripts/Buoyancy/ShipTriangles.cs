using UnityEngine;
using System.Collections.Generic;
using Unity.Jobs;
using Unity.Collections;
using UnityEngine.Rendering.HighDefinition;
using Unity.Mathematics;

public class ShipTriangles
{
    Transform shipTransform;
    Vector3[] shipVertices;
    int[] shipTriangles;
    float[] allDistancesToWater;

    public List<TriangleData> underWaterTriangleData = new();

    // Buffers for WaterSimulationSearchJob
    public NativeArray<float3> startPositionBuffer, candidateLocationBuffer;
    public NativeArray<float> errorBuffer, heightBuffer;
    public NativeArray<int> stepCountBuffer;

    public ShipTriangles(GameObject ship)
    {
        shipTransform = ship.transform;
        
        shipVertices = ship.GetComponent<MeshFilter>().mesh.vertices;
        shipTriangles = ship.GetComponent<MeshFilter>().mesh.triangles;
        
        allDistancesToWater = new float[shipVertices.Length];

        startPositionBuffer = new NativeArray<float3>(shipVertices.Length, Allocator.Persistent);
        candidateLocationBuffer = new NativeArray<float3>(shipVertices.Length, Allocator.Persistent);
        errorBuffer = new NativeArray<float>(shipVertices.Length, Allocator.Persistent);
        heightBuffer = new NativeArray<float>(shipVertices.Length, Allocator.Persistent);
        stepCountBuffer = new NativeArray<int>(shipVertices.Length, Allocator.Persistent);
    }

    public void GenerateUnderwaterMesh()
    {
        underWaterTriangleData.Clear();

        for (int i = 0; i < shipVertices.Length; i++)
        {
            Vector3 worldSpacePosition = shipTransform.TransformPoint(shipVertices[i]);
            startPositionBuffer[i] = worldSpacePosition;
        }

        WaterSimSearchData simSearchData = new();
        if (!ShipBouyancyScript.shipBouyancyScriptInstance.water.FillWaterSearchData(ref simSearchData))
            return;
            
        WaterSimulationSearchJob waterSimulationSearchJob = new()
        {
            startPositionBuffer = startPositionBuffer,
            targetPositionBuffer = startPositionBuffer,
            candidateLocationBuffer = candidateLocationBuffer,
            heightBuffer = heightBuffer,
            errorBuffer = errorBuffer,
            stepCountBuffer = stepCountBuffer,
            simSearchData = simSearchData,
        };

        JobHandle handler = waterSimulationSearchJob.Schedule(shipVertices.Length, 1);
        handler.Complete();

        for (int i = 0; i < shipVertices.Length; i ++)
        {
            allDistancesToWater[i] = startPositionBuffer[i].y - heightBuffer[i];
        }

        AddTriangles();
    }

    void AddTriangles()
    {
        List<VertexData> vertexData = new()
        {
            new VertexData(),
            new VertexData(),
            new VertexData()
        };

        int i = 0;
        while (i < shipTriangles.Length)
        {
            // Loop through the 3 vertices
            for (int x = 0; x < 3; x++)
            {
                vertexData[x] = new VertexData(startPositionBuffer[shipTriangles[i]], allDistancesToWater[shipTriangles[i]]);
                i++;
            }

            // All vertices are above the water
            if (vertexData[0].distance > 0f && vertexData[1].distance > 0f && vertexData[2].distance > 0f)
                continue;

            // All vertices are underwater
            if (vertexData[0].distance < 0f && vertexData[1].distance < 0f && vertexData[2].distance < 0f)
            {
                Vector3 p1 = vertexData[0].worldSpacePosition;
                Vector3 p2 = vertexData[1].worldSpacePosition;
                Vector3 p3 = vertexData[2].worldSpacePosition;
                
                underWaterTriangleData.Add(new TriangleData(p1, p2, p3));
            }
            // Some vertices are below the water
            else
            {
                // Sort the vertices
                vertexData.Sort((x, y) => x.distance.CompareTo(y.distance));
                vertexData.Reverse();

                // Only one vertice is above the water
                if (vertexData[0].distance >= 0f && vertexData[1].distance <= 0f && vertexData[2].distance <= 0f)
                {
                    OneVertexAboveWater(vertexData);
                }
                // Only two vertices are above the water
                else if (vertexData[0].distance >= 0f && vertexData[1].distance >= 0f && vertexData[2].distance <= 0f)
                {
                    TwoVerticesAboveWater(vertexData);
                }
            }
        }
    }

    // Cut the triangles
    private void OneVertexAboveWater(List<VertexData> vertexData)
    {
        Vector3 H = vertexData[0].worldSpacePosition;
        Vector3 M = vertexData[1].worldSpacePosition;
        Vector3 L = vertexData[2].worldSpacePosition;

        float hH = vertexData[0].distance;
        float hM = vertexData[1].distance;
        float hL = vertexData[2].distance;

        float tM = -hM / (hH - hM);
        float tL = -hL / (hH - hL);

        Vector3 IM = (tM * (H - M)) + M;
        Vector3 IL = (tL * (H - L)) + L;

        underWaterTriangleData.Add(new TriangleData(L, IM, M));
        underWaterTriangleData.Add(new TriangleData(L, IL, M));
    }

    private void TwoVerticesAboveWater(List<VertexData> vertexData)
    {
        Vector3 H = vertexData[0].worldSpacePosition;
        Vector3 M = vertexData[1].worldSpacePosition;
        Vector3 L = vertexData[2].worldSpacePosition;

        float hH = vertexData[0].distance;
        float hM = vertexData[1].distance;
        float hL = vertexData[2].distance;

        float tM = -hL / (hM - hL);
        float tH = -hL / (hH - hL);

        Vector3 JM = (tM * (M - L)) + L;
        Vector3 JH = (tH * (H - L)) + L;

        underWaterTriangleData.Add(new TriangleData(L, JM, JH));
    }

    // Helper struct to store vertex information
    struct VertexData
    {
        public Vector3 worldSpacePosition;
        public float distance;

        public VertexData(Vector3 worldSpacePosition, float distance)
        {
            this.worldSpacePosition = worldSpacePosition;
            this.distance = distance;
        }
    }
}