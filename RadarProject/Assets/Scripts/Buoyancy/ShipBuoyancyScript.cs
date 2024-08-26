using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Crest;
using System.Collections;

public class ShipBouyancyScript : MonoBehaviour
{
    public static ShipBouyancyScript shipBouyancyScriptInstance;
    
    [Header("Generating Underwater Mesh")]
    [SerializeField] float minWaitTime = 0.01f;
    [SerializeField] float maxWaitTime = 0.5f;

    ShipTriangles shipTriangles;
    Rigidbody ship;
    float waterDensity = 1025f; // Density of the UAE water

    // Stabilizing Forces

    // For ViscousWaterResistanceForce
    float viscosity = 0.00087f; // viscosity of the ocean at 30°C

    // For PressureDragForce
    float CPD1 = 10f;
    float CPD2 = 10f;
    float CSD1 = 10f;
    float CSD2 = 10f;
    float Fp = 0.5f;
    float Fs = 0.5f;

    float shipLength;
    float shipWidth;

    SampleHeightHelper sampleHeightHelper = new();

    void Start()
    {
        ship = gameObject.GetComponent<Rigidbody>();
        shipTriangles = new ShipTriangles(gameObject);
        shipBouyancyScriptInstance = this;

        shipLength = ship.GetComponent<MeshFilter>().mesh.bounds.size.z;
        shipWidth = ship.GetComponent<MeshFilter>().mesh.bounds.size.x;

        StartCoroutine(GenerateUnderwaterMeshCoroutine());
    }

    void Update()
    {
        //shipTriangles.GenerateUnderwaterMesh();
    }

    void FixedUpdate()
    {
        if (shipTriangles.underWaterTriangleData.Count == 0) return;

        AddUnderWaterForces();

        // Artifically align the ship upward because sometimes it sinks for some reason
        AlignShipUpward();
    }

    // Improves performance but lowers the accuracy, otherwise generate water mesh can be in Update
    IEnumerator GenerateUnderwaterMeshCoroutine()
    {
        while (Application.isPlaying)
        {
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(minWaitTime, maxWaitTime)); 
            shipTriangles.GenerateUnderwaterMesh();
        }
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

            // Currently causes issues with waves and can cause the ship to fly
            //Vector3 viscousWaterResistanceForce = ViscousWaterResistanceForce(waterDensity, triangleData);
            //forces += viscousWaterResistanceForce;

            Vector3 pressureDragForce = PressureDragForce(triangleData);
            forces += pressureDragForce;

            ship.AddForceAtPosition(forces, triangleData.center);
        }
    }

    // A small residual torque is applied, so if the number of triangles is low the object will rotate
    Vector3 BuoyancyForce(float density, TriangleData triangleData)
    {
        Vector3 force = density * -Physics.gravity.y * triangleData.distanceToWater * triangleData.area * triangleData.normal;

        force.x = 0f;
        force.z = 0f;

        return force;
    }

    Vector3 PressureDragForce(TriangleData triangleData)
    {   
        float velocity = triangleData.velocity.magnitude;
        velocity /= velocity; // Assume velocity reference is the same as velocity

        Vector3 force;
        if (triangleData.cosTheta > 0)
        {
            force = -(CPD1 * velocity + CPD2 * (velocity * velocity)) * triangleData.area * math.pow(triangleData.cosTheta, Fp) * triangleData.normal;
        }
        else
        {
            force = (CSD1 * velocity + CSD2 * (velocity * velocity)) * triangleData.area * math.pow(triangleData.cosTheta, Fs) * triangleData.normal;
        }

        if (float.IsNaN(force.x) || float.IsNaN(force.y) || float.IsNaN(force.z))
            return Vector3.zero;

        return force;
    }

    void AlignShipUpward()
    {
        Quaternion newRotation = Quaternion.LookRotation(ship.transform.forward, Vector3.up);
        ship.transform.rotation = Quaternion.Slerp(ship.transform.rotation, newRotation, Time.deltaTime);
    }

    public float GetDistanceToWater(Vector3 position)
    {   
        sampleHeightHelper.Init(position, shipWidth, true);
        if (sampleHeightHelper.Sample(out float waterHeight))
        {
            return position.y - waterHeight;
        }

        return position.y;
    }
}