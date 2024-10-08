using UnityEngine;
using System.Collections.Generic;
using Unity.Mathematics;
using Crest;
using System.Collections;

public class ShipBouyancyScript : MonoBehaviour
{
    public static ShipBouyancyScript shipBouyancyScriptInstance;

    [SerializeField] float amplifyForce = 0.5f;
    public float timeToWait = 0.5f;
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

    float shipLength;
    float shipWidth;

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

        shipLength = ship.GetComponent<MeshFilter>().mesh.bounds.size.z * ship.transform.localScale.x; // Multiply by scale to get the correct bounds
        shipWidth = ship.GetComponent<MeshFilter>().mesh.bounds.size.x * ship.transform.localScale.x; // Multiply by scale to get the correct bounds

        StartCoroutine(GenerateUnderwaterMeshCoroutine());
    }

    void FixedUpdate()
    {
        if (shipTriangles.underWaterTriangleData.Count == 0) return;

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

            //Vector3 viscousWaterResistanceForce = ViscousWaterResistanceForce(waterDensity, triangleData, ResistanceCoefficient(waterDensity, ship.velocity.magnitude, shipWidth));
            //force += viscousWaterResistanceForce;

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

    void AddAboveWaterForces()
    {
        //Get all triangles
        List<TriangleData> aboveWaterTriangleData = shipTriangles.aboveWaterTriangleData;

        //Loop through all triangles
        for (int i = 0; i < aboveWaterTriangleData.Count; i++)
        {
            TriangleData triangleData = aboveWaterTriangleData[i];


            //Calculate the forces
            Vector3 forceToAdd = Vector3.zero;

            //Force 1 - Air resistance 
            int shipDragCoefficient = 1;
            forceToAdd += AirResistanceForce(1.225f, triangleData, shipDragCoefficient);

            //Add the forces to the boat
            ship.AddForceAtPosition(forceToAdd, triangleData.center);
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

        if (float.IsNaN(airResistanceForce.x) || float.IsNaN(airResistanceForce.y) || float.IsNaN(airResistanceForce.z))
            return Vector3.zero;

        return airResistanceForce;
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

    public float ResistanceCoefficient(float rho, float velocity, float length)
    {
        float nu = 0.0000008f; // At 30 degrees celcius

        //Reynolds number
        float Rn = velocity * length / nu;

        //The resistance coefficient
        float Cf = 0.075f / Mathf.Pow(Mathf.Log10(Rn) - 2f, 2f);

        return Cf;
    }


    public static Vector3 ViscousWaterResistanceForce(float rho, TriangleData triangleData, float Cf)
    {
        Vector3 B = triangleData.normal;
        Vector3 A = triangleData.velocity;

        Vector3 velocityTangent = Vector3.Cross(B, Vector3.Cross(A, B) / B.magnitude) / B.magnitude;

        Vector3 tangentialDirection = velocityTangent.normalized * -1f;
        
        Vector3 v_f_vec = triangleData.velocity.magnitude * tangentialDirection;

        //The final resistance force
        Vector3 viscousWaterResistanceForce = 0.5f * rho * v_f_vec.magnitude * v_f_vec * triangleData.area * Cf;

        if (float.IsNaN(viscousWaterResistanceForce.x) || float.IsNaN(viscousWaterResistanceForce.y) || float.IsNaN(viscousWaterResistanceForce.z))
            return Vector3.zero;

        return viscousWaterResistanceForce;
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

        for (int i = 0; i < _queryPoints.Length; i++)
        {
            var waterHeight = OceanRenderer.Instance.SeaLevel + _queryResultDisps[i].y;
            heightDiff[i] = _queryPoints[i].y - waterHeight;
        }

        return heightDiff;
    }
}
