using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class RandomizeObjectSelection : MonoBehaviour
{
    public bool raycast;
    public float raycastDistance = 100f;
    public GameObject[] objects;
    private Transform parentInstance; // Reference to the actual instance in the scene

    void Start()
    {
        // Find the actual instance of ProcTerrainPrefab in the scene
        FindParentInstance();
        PickItem();
        SetParentToSpawners();
    }

    void FindParentInstance()
    {
        // First try to find the parent through the transform hierarchy
        Transform current = transform;
        while (current.parent != null)
        {
            current = current.parent;
            if (current.name.Contains("ProcTerrainPrefab"))
            {
                parentInstance = current;
                break;
            }
        }

        // If not found in hierarchy, try to find in scene
        if (parentInstance == null)
        {
            GameObject procTerrainObj = GameObject.Find("ProcTerrainPrefab(Clone)");
            if (procTerrainObj != null)
            {
                parentInstance = procTerrainObj.transform;
            }
        }

        // If still not found, log a warning
        if (parentInstance == null)
        {
            Debug.LogWarning("Could not find ProcTerrainPrefab instance in the scene. Objects will be spawned without a parent.");
        }
    }

    void PickItem()
    {
        int randomIndex = Random.Range(0, objects.Length);
        if (raycast)
        {
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, raycastDistance))
            {
                Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                GameObject clone = parentInstance != null
                    ? Instantiate(objects[randomIndex], hit.point, spawnRotation, parentInstance)
                    : Instantiate(objects[randomIndex], hit.point, spawnRotation);
            }
        }
        else
        {
            GameObject clone = parentInstance != null
                ? Instantiate(objects[randomIndex], transform.position, Quaternion.identity, parentInstance)
                : Instantiate(objects[randomIndex], transform.position, Quaternion.identity);
        }
    }

    public void SetParentToSpawners()
    {
        // Find all objects with these specific names
        GameObject[] skyscraperSpawners = GameObject.FindObjectsOfType<GameObject>()
            .Where(obj => obj.name == "SkyscraperSpawner(Clone)")
            .ToArray();

        GameObject[] palmSpawners = GameObject.FindObjectsOfType<GameObject>()
            .Where(obj => obj.name == "PalmSpawner(Clone)")
            .ToArray();

        // Delete all found objects
        foreach (GameObject spawner in skyscraperSpawners)
        {
            spawner.transform.SetParent(parentInstance);
        }

        foreach (GameObject spawner in palmSpawners)
        {
            spawner.transform.SetParent(parentInstance);
        }
    }
}