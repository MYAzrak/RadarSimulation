using UnityEngine;
using Unity.Mathematics;

public struct TriangleData
{
    public Vector3 center;
    public float distanceToSurface;
    public Vector3 normal;
    public float area;
    public Vector3 velocity;
    public float cosTheta;

    public TriangleData(Vector3 p1, Vector3 p2, Vector3 p3, Rigidbody rigidbody)
    {
        center = (p1 + p2 + p3) / 3f;

        distanceToSurface = ShipBouyancyScript.shipBouyancyScriptInstance.GetDistanceToWater(center);

        Vector3 crossProduct = Vector3.Cross(p2 - p1, p3 - p1);

        normal = crossProduct.normalized;
        area = crossProduct.magnitude / 2;

        // Stabilizing Forces
        velocity = rigidbody.velocity + Vector3.Cross(rigidbody.angularVelocity, center - rigidbody.transform.TransformPoint(rigidbody.centerOfMass));
        cosTheta = Vector3.Dot(velocity.normalized, normal);
    }
}