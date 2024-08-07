using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using System.Collections.Generic;
using Unity.Mathematics;

public class ShipBouyancyScript : MonoBehaviour
{
    public static ShipBouyancyScript shipBouyancyScriptInstance;
    public WaterSurface water;
    
    ShipTriangles shipTriangles;
    Rigidbody ship;
    float waterDensity = 1025f; // Density of the UAE water

    // Stabilizing Forces

    // For ViscousWaterResistanceForce
    float viscosity = 0.00087f; // viscosity of the ocean at 30°C

    // For PressureDragForce
    float CPD1 = 100f;
    float CPD2 = 100f;
    float CSD1 = 100f;
    float CSD2 = 100f;
    float Fp = 0.5f;
    float Fs = 0.5f;

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
            
            Vector3 forces = Vector3.zero;
            Vector3 buoyancyForce = BuoyancyForce(waterDensity, triangleData);
            forces += buoyancyForce;

            Vector3 viscousWaterResistanceForce = ViscousWaterResistanceForce(waterDensity, triangleData);
            forces += viscousWaterResistanceForce;

            Vector3 pressureDragForce = PressureDragForce(triangleData);
            forces += pressureDragForce;
            
            ship.AddForceAtPosition(forces, triangleData.center);
        }
    }

    // A small residual torque is applied, so if the number of triangles is low the object will rotate
    Vector3 BuoyancyForce(float density, TriangleData triangleData)
    {
        Vector3 buoyancyForce = density * Physics.gravity.y * triangleData.distanceToSurface * triangleData.area * triangleData.normal;

        // The horizontal component of the hydrostatic forces cancel out
        buoyancyForce.x = 0f;
        buoyancyForce.z = 0f;

        return buoyancyForce;
    }

    Vector3 ViscousWaterResistanceForce(float density, TriangleData triangleData)
    {
        float reynoldsNumber = density * ship.velocity.magnitude * ship.GetComponent<MeshFilter>().mesh.bounds.size.z / viscosity;
        float CF = 0.075f / math.pow(math.log10(reynoldsNumber) - 2f, 2f);
        
        Vector3 direction = - (triangleData.velocity - (Vector3.one * Vector3.Dot(triangleData.velocity, triangleData.normal))).normalized;
        Vector3 relativeVelocity = direction * triangleData.velocity.magnitude;

        Vector3 force = 0.5f * density * CF * triangleData.area * triangleData.velocity.magnitude * relativeVelocity;
        
        return force;
    }

    Vector3 PressureDragForce(TriangleData triangleData)
    {   
        float velocity = triangleData.velocity.magnitude;
        velocity /= velocity; // Assume velocity reference is the same as velocity

        Vector3 force;
        if (triangleData.cosTheta > 0)
        {
            force = -(CPD1 * velocity + CPD2 * math.pow(velocity, 2f)) * triangleData.area * math.pow(triangleData.cosTheta, Fp) * triangleData.normal;
        }
        else
        {
            force = (CSD1 * velocity + CSD2 * math.pow(velocity, 2f)) * triangleData.area * math.pow(triangleData.cosTheta, Fs) * triangleData.normal;
        }

        if (float.IsNaN(force.x) || float.IsNaN(force.y) || float.IsNaN(force.z))
            return Vector3.zero;

        return force;
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