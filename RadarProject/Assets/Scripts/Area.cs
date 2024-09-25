using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class Area : MonoBehaviour
{
    private static Dictionary<GameObject, Vector3[]> verticesDict = new();
    public Transform cameraTransform;
    
    void Start()
    {
        StartCoroutine(CalculateAreaEverySecond());
    }

    private IEnumerator CalculateAreaEverySecond()
    {
        while (true)
        {
            // Check if the camera is looking at an object
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit))
            {
                GameObject targetObject = hit.collider.gameObject; // Get the object hit by the ray
                float area = CalculateCrossSectionArea(targetObject);
                Debug.Log($"Cross Section Area of {targetObject.name}: {area}");
            }

            yield return new WaitForSeconds(1f); // Wait for 1 second before next check
        }
    }

    public float CalculateCrossSectionArea(GameObject g)
    {
        Vector3 normal = cameraTransform.forward; // Use camera's forward direction as normal

        // If gameobject not in the dict then get the verticies 
        if (!verticesDict.TryGetValue(g, out Vector3[] vertices))
        {
            MeshFilter[] meshes = g.GetComponentsInChildren<MeshFilter>();
            HashSet<Vector3> uniqueVertices = new();

            foreach (MeshFilter meshFilter in meshes)
            {
                foreach (Vector3 vertex in meshFilter.mesh.vertices)
                {
                    uniqueVertices.Add(meshFilter.transform.TransformPoint(vertex));
                }
            }

            vertices = new Vector3[uniqueVertices.Count];
            uniqueVertices.CopyTo(vertices);
            verticesDict.Add(g, vertices);
        }

        normal = normal.normalized;
        Vector3 perp1 = Perpendicular(normal).normalized;
        Vector3 perp2 = Vector3.Cross(normal, perp1);

        Vector2[] inThePlane = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            inThePlane[i] = new Vector2(Vector3.Dot(perp1, vertices[i]), Vector3.Dot(perp2, vertices[i]));
        }

        return CalculateHullArea(ConvexHull(inThePlane));
    }

    public Vector3 Perpendicular(Vector3 v3)
    {
        return v3.z < v3.x ? new Vector3(v3.y, -v3.x, 0) : new Vector3(0, -v3.z, v3.y);
    }

    public float CalculateHullArea(Vector2[] hull)
    {
        float sum = 0f;
        for (int i = 1; i < hull.Length - 1; i++)
        {
            sum += TriangleArea(hull[0], hull[i], hull[i + 1]);
        }
        return sum;
    }

    public float TriangleArea(Vector2 v0, Vector2 v1, Vector2 v2)
    {
        Vector3 crossProduct = Vector3.Cross(v1 - v0, v2 - v0);
        return crossProduct.magnitude / 2;
    }

    // To find orientation of ordered triplet (p, q, r).
    // The function returns following values
    // 0 --> p, q and r are collinear
    // 1 --> Clockwise
    // 2 --> Counterclockwise
    int Orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) -
                    (q.x - p.x) * (r.y - q.y);
    
        if (val == 0) return 0;  // collinear
        return (val > 0)? 1: 2; // clock or counterclock wise
    }

    // Using Jarvis’ Algorithm
    public Vector2[] ConvexHull(Vector2[] points)
    {
        int n = points.Length;
        if (n < 3) return points;

        List<Vector2> hull = new();

        int leftMost = 0;
        for (int i = 1; i < n; i++)
            if (points[i].x < points[leftMost].x)
                leftMost = i;

        // Start from leftmost point, keep moving counterclockwise
        // until reach the start point again.  This loop runs O(h)
        // times where h is number of points in result or output
        int p = leftMost, q;
        do
        {
            // Add current point to result
            hull.Add(points[p]);

            // Search for a point 'q' such that orientation(p, q,
            // x) is counterclockwise for all points 'x'. The idea
            // is to keep track of last visited most counterclock-
            // wise point in q. If any point 'i' is more counterclock-
            // wise than q, then update q.
            q = (p + 1) % n;
            for (int i = 0; i < n; i++)
            {
                // If i is more counterclockwise than current q, then
                // update q
                if (Orientation(points[p], points[i], points[q]) == 2)
                    q = i;
            }

            // Now q is the most counterclockwise with respect to p
            // Set p as q for next iteration, so that q is added to
            // result 'hull'
            p = q;

        } while (p != leftMost); // While we don't come to first point

        return hull.ToArray();
    }
}
