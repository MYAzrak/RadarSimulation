using UnityEngine;
using System.Collections.Generic;

public class ShipTriangles
{
    Transform shipTransform;
    Vector3[] shipVertices;
    int[] shipTriangles;

    Rigidbody shipRigidbody;
    float[] heights;
    Vector3[] samplePoints;

    public List<TriangleData> underWaterTriangleData = new();
    public List<TriangleData> aboveWaterTriangleData = new();

    int shipVerticesLength;

    public ShipTriangles(GameObject ship)
    {
        shipTransform = ship.transform;
        shipRigidbody = ship.GetComponent<Rigidbody>();

        shipVertices = ship.GetComponent<MeshFilter>().mesh.vertices;
        shipTriangles = ship.GetComponent<MeshFilter>().mesh.triangles;

        shipVerticesLength = shipVertices.Length;

        heights = new float[shipVerticesLength];
        samplePoints = new Vector3[shipVerticesLength];
    }

    public void GenerateUnderwaterMesh()
    {
        aboveWaterTriangleData.Clear();
        underWaterTriangleData.Clear();

        samplePoints = new Vector3[shipVerticesLength];
        for (int i = 0; i < shipVerticesLength; i++)
        {
            Vector3 worldSpacePosition = shipTransform.TransformPoint(shipVertices[i]);
            samplePoints[i] = worldSpacePosition;
        }

        heights = ShipBouyancyScript.shipBouyancyScriptInstance.GetDistanceToWater(samplePoints);

        AddTriangles();

        samplePoints = new Vector3[underWaterTriangleData.Count];
        int count = underWaterTriangleData.Count;
        for (int i = 0; i < count; i++)
        {
            samplePoints[i] = underWaterTriangleData[i].center;
        }

        heights = ShipBouyancyScript.shipBouyancyScriptInstance.GetDistanceToWater(samplePoints);
        count = underWaterTriangleData.Count;
        for (int i = 0; i < count; i++)
        {
            underWaterTriangleData[i].distanceToWater = heights[i];
        }
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
                vertexData[x] = new VertexData(samplePoints[shipTriangles[i]], heights[shipTriangles[i]]);
                i++;
            }

            // All vertices are above the water
            if (vertexData[0].distanceToWater > 0f && vertexData[1].distanceToWater > 0f && vertexData[2].distanceToWater > 0f)
            {
                Vector3 p1 = vertexData[0].worldSpacePosition;
                Vector3 p2 = vertexData[1].worldSpacePosition;
                Vector3 p3 = vertexData[2].worldSpacePosition;

                aboveWaterTriangleData.Add(new TriangleData(p1, p2, p3, shipRigidbody));
            }

            // All vertices are underwater
            if (vertexData[0].distanceToWater < 0f && vertexData[1].distanceToWater < 0f && vertexData[2].distanceToWater < 0f)
            {
                Vector3 p1 = vertexData[0].worldSpacePosition;
                Vector3 p2 = vertexData[1].worldSpacePosition;
                Vector3 p3 = vertexData[2].worldSpacePosition;

                underWaterTriangleData.Add(new TriangleData(p1, p2, p3, shipRigidbody));
            }
            // Some vertices are below the water
            else
            {
                // Sort the vertices
                vertexData.Sort((x, y) => x.distanceToWater.CompareTo(y.distanceToWater));
                vertexData.Reverse();

                // Only one vertice is above the water
                if (vertexData[0].distanceToWater >= 0f && vertexData[1].distanceToWater <= 0f && vertexData[2].distanceToWater <= 0f)
                {
                    OneVertexAboveWater(vertexData);
                }
                // Only two vertices are above the water
                else if (vertexData[0].distanceToWater >= 0f && vertexData[1].distanceToWater >= 0f && vertexData[2].distanceToWater <= 0f)
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

        float hH = vertexData[0].distanceToWater;
        float hM = vertexData[1].distanceToWater;
        float hL = vertexData[2].distanceToWater;

        float tM = -hM / (hH - hM);
        float tL = -hL / (hH - hL);

        Vector3 IM = (tM * (H - M)) + M;
        Vector3 IL = (tL * (H - L)) + L;

        underWaterTriangleData.Add(new TriangleData(L, IM, M, shipRigidbody));
        underWaterTriangleData.Add(new TriangleData(L, IL, M, shipRigidbody));
    }

    private void TwoVerticesAboveWater(List<VertexData> vertexData)
    {
        Vector3 H = vertexData[0].worldSpacePosition;
        Vector3 M = vertexData[1].worldSpacePosition;
        Vector3 L = vertexData[2].worldSpacePosition;

        float hH = vertexData[0].distanceToWater;
        float hM = vertexData[1].distanceToWater;
        float hL = vertexData[2].distanceToWater;

        float tM = -hL / (hM - hL);
        float tH = -hL / (hH - hL);

        Vector3 JM = (tM * (M - L)) + L;
        Vector3 JH = (tH * (H - L)) + L;

        underWaterTriangleData.Add(new TriangleData(L, JM, JH,shipRigidbody));
    }

    // Helper struct to store vertex information
    struct VertexData
    {
        public Vector3 worldSpacePosition;
        public float distanceToWater;

        public VertexData(Vector3 worldSpacePosition, float distanceToWater)
        {
            this.worldSpacePosition = worldSpacePosition;
            this.distanceToWater = distanceToWater;
        }
    }
}