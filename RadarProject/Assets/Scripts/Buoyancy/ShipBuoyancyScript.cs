using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Crest;
using System.Collections;

public class ShipBouyancyScript : MonoBehaviour
{
    public static ShipBouyancyScript shipBouyancyScriptInstance;

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

        StartCoroutine(Coroutine());
    }

    void Update()
    {
        //shipTriangles.GenerateUnderwaterMesh();
    }

    void FixedUpdate()
    {
        if (shipTriangles.underWaterTriangleData.Count == 0) return;

        AddUnderWaterForces();

        // Artifically align the ship upward to avoid it from sinking
        AlignShipUpward();
    }

    IEnumerator Coroutine()
    {
        while (Application.isPlaying)
        {
            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.01f, 0.2f));
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
        Vector3 force = density * -Physics.gravity.y * triangleData.distanceToSurface * triangleData.area * triangleData.normal;

        force.x = 0f;
        force.z = 0f;

        return force;
    }

    Vector3 ViscousWaterResistanceForce(float density, TriangleData triangleData)
    {
        float reynoldsNumber = ship.velocity.magnitude * shipLength / viscosity;
        float temp = math.log10(reynoldsNumber) - 2f;
        float CF = 0.075f / (temp * temp);

        Vector3 B = triangleData.normal;
        Vector3 A = triangleData.velocity;

        Vector3 velocityTangent = Vector3.Cross(B, Vector3.Cross(A, B) / B.magnitude) / B.magnitude;
        Vector3 tangentialDirection = velocityTangent.normalized * -1f;

        Vector3 relativeVelocity = tangentialDirection * triangleData.velocity.magnitude;

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