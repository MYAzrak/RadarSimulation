using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeObjectSelection : MonoBehaviour
{
    public bool raycast;
    public float raycastDistance = 100f;
    public GameObject[] objects;
    void Start()
    {
        PickItem();
    }

    void PickItem()
    {
        int randomIndex = Random.Range(0, objects.Length);
        if (raycast)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance)) // If hits something
            {
                Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                GameObject clone = Instantiate(objects[randomIndex], hit.point, spawnRotation);
            }
        }
        else
        {
            GameObject clone = Instantiate(objects[randomIndex], transform.position, Quaternion.identity); // Clones the prefab and doesn't rotate it
        }
    }
}
