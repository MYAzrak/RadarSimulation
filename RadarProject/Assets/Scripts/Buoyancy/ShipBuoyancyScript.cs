using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Crest;
using System.Collections;
using System;
using UnityEngine.Profiling;

public class ShipBouyancyScript : MonoBehaviour
{
    public static ShipBouyancyScript shipBouyancyScriptInstance;

    [SerializeField] float amplifyForce = 0.5f;
    public float timeToWait = 0;
    public float rotationSpeed = 2.0f;

    ShipTriangles shipTriangles;
    Rigidbody ship;
    float waterDensity = 1025f; // Density of the UAE water

    Vector3[] forces;               // Forces to apply
    bool recalculateForces = false; // Recaulate forces when needed for performance

    // Stabilizing Forces
    // For PressureDragForce
    float CPD1 = 10f;
    float CPD2 = 10f;
    float CSD1 = 10f;
    float CSD2 = 10f;
    float Fp = 0.5f;
    float Fs = 0.5f;

    //float shipLength;
    float shipWidth;

    ICollProvider collProvider;

    /*
    public void OnDrawGizmosSelected()
    {
        var r = GetComponent<Renderer>();
        if (r == null)
            return;
        var bounds = r.bounds;
        Gizmos.matrix = Matrix4x4.identity;
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(bounds.center, bounds.extents * 2);
        
        float width = bounds.size.x; 
        float height = bounds.size.y;  
        float depth = bounds.size.z;  

        Debug.Log($"Width: {width}, Height: {height}, Depth: {depth}");
    }
    */

    void Start()
    {
        ship = gameObject.GetComponent<Rigidbody>();
        shipTriangles = new ShipTriangles(gameObject);
        shipBouyancyScriptInstance = this;

        //shipLength = ship.GetComponent<MeshFilter>().mesh.bounds.size.z * ship.transform.localScale.x; // Multiply by scale to get the correct bounds
        shipWidth = ship.GetComponent<MeshFilter>().mesh.bounds.size.x * ship.transform.localScale.x; // Multiply by scale to get the correct bounds

        collProvider = OceanRenderer.Instance.CollisionProvider;

        StartCoroutine(GenerateUnderwaterMeshCoroutine());
    }

    void FixedUpdate()
    {
        if (recalculateForces)
            RecalculateForces();

        if (shipTriangles.underWaterTriangleData.Count > 0)
            AddUnderWaterForces();

        if (shipTriangles.aboveWaterTriangleData.Count > 0)
            AddAboveWaterForces();

        // Align the ship upward to avoid sinking
        AlignShipUpward();
    }

    // Improves performance but lowers the accuracy, otherwise generate water mesh can be in Update
    IEnumerator GenerateUnderwaterMeshCoroutine()
    {
        while (Application.isPlaying)
        {
            if (timeToWait == 0)
                yield return new WaitForSeconds(UnityEngine.Random.Range(0.2f, 1f));
            else
                yield return new WaitForSeconds(timeToWait);
            shipTriangles.GenerateUnderwaterMesh();
            recalculateForces = true;
        }
    }

    // Only recalculate forces when needed to improve performance
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

        recalculateForces = false;
    }

    // Add all forces that act on the squares below the water
    void AddUnderWaterForces()
    {
        List<TriangleData> underWaterTriangleData = shipTriangles.underWaterTriangleData;

        for (int i = 0; i < underWaterTriangleData.Count; i++)
        {
            TriangleData triangleData = underWaterTriangleData[i];
            ship.AddForceAtPosition(forces[i], triangleData.center);
        }
    }

    // A small residual torque is applied, so if the number of triangles is low the object will rotate
    Vector3 BuoyancyForce(float density, TriangleData triangleData)
    {
        Vector3 force = density * -Physics.gravity.y * triangleData.distanceToWater * triangleData.area * triangleData.normal;

        force.x = 0f;
        force.z = 0f;

        if (!IsValidForce(force))
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

        if (!IsValidForce(force))
            return Vector3.zero;

        return force;
    }

    void AddAboveWaterForces()
    {
        //Get all triangles
        List<TriangleData> aboveWaterTriangleData = shipTriangles.aboveWaterTriangleData;

        //Loop through all triangles
        for (int i = 0; i < aboveWaterTriangleData.Count; i++)
        {
            TriangleData triangleData = aboveWaterTriangleData[i];

            Vector3 force = Vector3.zero;

            int shipDragCoefficient = 1;
            force += AirResistanceForce(waterDensity, triangleData, shipDragCoefficient);

            ship.AddForceAtPosition(force, triangleData.center);
        }
    }

    public Vector3 AirResistanceForce(float rho, TriangleData triangleData, float C_air)
    {
        //Only add air resistance if normal is pointing in the same direction as the velocity
        if (triangleData.cosTheta < 0f)
        {
            return Vector3.zero;
        }

        //Find air resistance force
        Vector3 airResistanceForce = 0.5f * rho * triangleData.velocity.magnitude * triangleData.velocity * triangleData.area * C_air;

        //Acting in the opposite side of the velocity
        airResistanceForce *= -1f;

        if (!IsValidForce(airResistanceForce))
            return Vector3.zero;

        return airResistanceForce;
	}

    bool IsValidForce(Vector3 force)
    {
        if (float.IsNaN(force.x) || float.IsNaN(force.y) || float.IsNaN(force.z))
            return false;
        
        if (float.IsInfinity(force.x) || float.IsInfinity(force.y) || float.IsInfinity(force.z))
            return false;

        if (float.IsNegativeInfinity(force.x) || float.IsNegativeInfinity(force.y) || float.IsNegativeInfinity(force.z))
            return false;
        
        return true;
    }

    void AlignShipUpward()
    {
        Quaternion newRotation = Quaternion.LookRotation(ship.transform.forward, Vector3.up);
        ship.transform.rotation = Quaternion.Slerp(ship.transform.rotation, newRotation, Time.deltaTime * rotationSpeed);
    }

    public float[] GetDistanceToWater(Vector3[] _queryPoints)
    {
        int _queryPointsLength = _queryPoints.Length;
        Vector3[] _queryResultDisps = new Vector3[_queryPointsLength];

        collProvider.Query(GetHashCode(), shipWidth, _queryPoints, _queryResultDisps, null, null);

        float[] heightDiff = new float[_queryPointsLength];

        for (int i = 0; i < _queryPointsLength; i++)
        {
            //var waterHeight = OceanRenderer.Instance.SeaLevel + _queryResultDisps[i].y;
            heightDiff[i] = _queryPoints[i].y - _queryResultDisps[i].y;
        }

        return heightDiff;
    }
}
