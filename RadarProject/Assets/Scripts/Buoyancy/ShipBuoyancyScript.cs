using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;

public class ShipBouyancyScript : MonoBehaviour
{
    public static ShipBouyancyScript shipBouyancyScriptInstance;
    public WaterSurface water;
    
    ShipTriangles shipTriangles;
    Rigidbody ship;
    float waterDensity = 1025f; // Density of the UAE water

    void Start()
    {
        ship = gameObject.GetComponent<Rigidbody>();
        water = FindFirstObjectByType<WaterSurface>();
        shipTriangles = new ShipTriangles(gameObject);
        shipBouyancyScriptInstance = this;
    }

    void Update()
    {
        shipTriangles.GenerateUnderwaterMesh();
    }
    
    void FixedUpdate()
    {
        if (shipTriangles.underWaterTriangleData.Count == 0) return;
        
        AddUnderWaterForces();
    }

    // Add all forces that act on the squares below the water
    void AddUnderWaterForces()
    {
        List<TriangleData> underWaterTriangleData = shipTriangles.underWaterTriangleData;

        for (int i = 0; i < underWaterTriangleData.Count; i++)
        {
            TriangleData triangleData = underWaterTriangleData[i];
            Vector3 buoyancyForce = BuoyancyForce(waterDensity, triangleData);
            ship.AddForceAtPosition(buoyancyForce, triangleData.center);
        }
    }

    // A small residual torque is applied, so if the number of triangles is low the object will rotate
    private Vector3 BuoyancyForce(float density, TriangleData triangleData)
    {
        Vector3 buoyancyForce = density * Physics.gravity.y * triangleData.distanceToSurface * triangleData.area * triangleData.normal;

        // The horizontal component of the hydrostatic forces cancel out
        buoyancyForce.x = 0f;
        buoyancyForce.z = 0f;

        return buoyancyForce;
    }

    public float GetDistanceToWater(Vector3 position)
    {
        WaterSearchParameters searchParameters = new()
        {
            startPosition = position
        };
        water.FindWaterSurfaceHeight(searchParameters, out WaterSearchResult searchResult);
        float distanceToWater = position.y - searchResult.height;
        return distanceToWater;
    }

    void OnDestroy()
    {
        shipTriangles.startPositionBuffer.Dispose();
        shipTriangles.candidateLocationBuffer.Dispose();
        shipTriangles.heightBuffer.Dispose();
        shipTriangles.errorBuffer.Dispose();
        shipTriangles.stepCountBuffer.Dispose();
    }
}