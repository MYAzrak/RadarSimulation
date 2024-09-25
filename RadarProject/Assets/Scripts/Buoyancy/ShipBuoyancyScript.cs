using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Crest;
using System.Collections;
using UnityEngine.Profiling;

public class ShipBouyancyScript : MonoBehaviour
{
    public static ShipBouyancyScript shipBouyancyScriptInstance;

    [SerializeField] float amplifyForce = 0.5f;
    public float timeToWait = 0.1f;
    public float rotationSpeed = 2.0f;

    ShipTriangles shipTriangles;
    Rigidbody ship;
    float waterDensity = 1025f; // Density of the UAE water

    Vector3[] forces;               // Forces to apply
    bool recalculateForces = false; // Recaulate forces when needed for performances

    // Stabilizing Forces
    // For PressureDragForce
    float CPD1 = 10f;
    float CPD2 = 10f;
    float CSD1 = 10f;
    float CSD2 = 10f;
    float Fp = 0.5f;
    float Fs = 0.5f;

    float shipWidth;

    void Start()
    {
        ship = gameObject.GetComponent<Rigidbody>();
        shipTriangles = new ShipTriangles(gameObject);
        shipBouyancyScriptInstance = this;

        shipWidth = ship.GetComponent<MeshFilter>().mesh.bounds.size.x;

        StartCoroutine(GenerateUnderwaterMeshCoroutine());
    }

    void FixedUpdate()
    {
        if (shipTriangles.underWaterTriangleData.Count == 0) return;

        AddUnderWaterForces();

        // Align the ship upward to avoid sinking
        AlignShipUpward();
    }

    // Improves performance but lowers the accuracy, otherwise generate water mesh can be in Update
    IEnumerator GenerateUnderwaterMeshCoroutine()
    {
        while (Application.isPlaying)
        {
            yield return new WaitForSeconds(timeToWait); 
            shipTriangles.GenerateUnderwaterMesh();
            recalculateForces = true;
        }
    }

    void RecalculateForces()
    {
        List<TriangleData> underWaterTriangleData = shipTriangles.underWaterTriangleData;
        forces = new Vector3[shipTriangles.underWaterTriangleData.Count];

        for (int i = 0; i < underWaterTriangleData.Count; i++)
        {
            TriangleData triangleData = underWaterTriangleData[i];

            Vector3 force = Vector3.zero;
            Vector3 buoyancyForce = BuoyancyForce(waterDensity, triangleData);
            force += buoyancyForce;

            Vector3 pressureDragForce = PressureDragForce(triangleData);
            force += pressureDragForce;

            force *= amplifyForce;

            forces[i] = force;
        }
    }

    // Add all forces that act on the squares below the water
    void AddUnderWaterForces()
    {
        List<TriangleData> underWaterTriangleData = shipTriangles.underWaterTriangleData;

        for (int i = 0; i < underWaterTriangleData.Count; i++)
        {
            TriangleData triangleData = underWaterTriangleData[i];

            Vector3 force = Vector3.zero;
            Vector3 buoyancyForce = BuoyancyForce(waterDensity, triangleData);
            force += buoyancyForce;

            Vector3 pressureDragForce = PressureDragForce(triangleData);
            force += pressureDragForce;

            force *= amplifyForce;

            ship.AddForceAtPosition(force, triangleData.center);
        }
    }

    // A small residual torque is applied, so if the number of triangles is low the object will rotate
    Vector3 BuoyancyForce(float density, TriangleData triangleData)
    {
        Vector3 force = density * -Physics.gravity.y * triangleData.distanceToWater * triangleData.area * triangleData.normal;

        force.x = 0f;
        force.z = 0f;

        if (float.IsNaN(force.x) || float.IsNaN(force.y) || float.IsNaN(force.z))
            return Vector3.zero;

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
        ship.transform.rotation = Quaternion.Slerp(ship.transform.rotation, newRotation, Time.deltaTime * rotationSpeed);
    }

    public float[] GetDistanceToWater(Vector3[] _queryPoints)
    {   
        Vector3[] _queryResultDisps = new Vector3[_queryPoints.Length];

        var collProvider = OceanRenderer.Instance.CollisionProvider;

        collProvider.Query(GetHashCode(), shipWidth, _queryPoints, _queryResultDisps, null, null);

        float[] heightDiff = new float[_queryPoints.Length];

        for(int i = 0; i < _queryPoints.Length; i++)
        {
            var waterHeight = OceanRenderer.Instance.SeaLevel + _queryResultDisps[i].y;
            heightDiff[i] = _queryPoints[i].y - waterHeight;
        }

        return heightDiff;
    }
}