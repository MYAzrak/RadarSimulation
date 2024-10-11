using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class ObjectAreaSpawner : MonoBehaviour
{
    public ObjectPool<GameObject> pool;
    public List<GameObject> objectToSpread;
    public Transform parent;
    public int numObjectsToSpawn = 100;

    public float itemXSpread = 2000;
    public float itemYSpread = 0;
    public float itemZSpread = 2500;

    public bool raycast = true;
    public float raycastDistance = 500f;

    void Start()
    {
        pool = new ObjectPool<GameObject>(CreateObject, OnTakeObjectFromPool, OnReturnObjectFromPool, OnDestroyObject, true, 50, 50);
    }
    
    public void GenerateObjects(Transform parent)
    {
        this.parent = parent;
        transform.position = parent.position;

        for (int i = 0; i < numObjectsToSpawn; i++)
        {
            pool.Get();
        }
    }

    GameObject CreateObject()
    {
        GameObject gameObject = Instantiate(objectToSpread[Random.Range(0, objectToSpread.Count)], Vector3.zero, Quaternion.identity, parent);
        gameObject.tag = "LandPoolObject";
        gameObject.transform.parent = transform;
        return gameObject;
    }

    void OnTakeObjectFromPool(GameObject gameObject)
    {
        Vector3 randPosition = new Vector3(Random.Range(-itemXSpread, itemXSpread), 500, Random.Range(-itemZSpread, itemZSpread)) + parent.transform.position;
        gameObject.transform.position = randPosition;

        if (raycast)
        {
            if (Physics.Raycast(gameObject.transform.position, Vector3.down, out RaycastHit hit, raycastDistance))
            {
                Quaternion spawnRotation = Quaternion.FromToRotation(Vector3.up, hit.normal);
                gameObject.transform.position = hit.point;
                gameObject.transform.rotation = spawnRotation;
            }
        }
        else
        {
            gameObject.transform.position = gameObject.transform.position;
            gameObject.transform.rotation = Quaternion.identity;
        }

        gameObject.gameObject.SetActive(true);
    }

    void OnReturnObjectFromPool(GameObject gameObject)
    {
        gameObject.gameObject.SetActive(false);
    }

    void OnDestroyObject(GameObject gameObject)
    {
        Destroy(gameObject.gameObject);
    }

    public void UnloadAllObjects()
    {
        if (parent == null) return;
        
        foreach (Transform child in transform)
        {
            if (child.CompareTag("LandPoolObject") && child.gameObject.activeInHierarchy)
            {
                pool.Release(child.gameObject);
            }
        }
    }
}
