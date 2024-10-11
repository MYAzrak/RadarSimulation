using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;

public class RandomizeObjectSelection : MonoBehaviour
{
    public bool raycast;
    public float raycastDistance = 100f;
    public GameObject[] objects;
    ObjectPool<GameObject> pool;

    void Start()
    {
        PickItem();
    }

    void PickItem()
    {
        int randomIndex = Random.Range(0, objects.Length);
        if (raycast)
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, raycastDistance))
            {
                Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                Instantiate(objects[randomIndex], hit.point, spawnRotation);
            }
        }
        else
        {
            Instantiate(objects[randomIndex], transform.position, Quaternion.identity);
        }
    }

    public void SetPool(ObjectPool<GameObject> pool)
    {
        this.pool = pool;
    }
}