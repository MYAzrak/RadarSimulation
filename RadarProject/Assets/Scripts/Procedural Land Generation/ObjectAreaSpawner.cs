using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ObjectAreaSpawner : MonoBehaviour
{
    public GameObject objectToSpread;
    public int numObjectsToSpawn = 100;

    public float itemXSpread = 200;
    public float itemYSpread = 0;
    public float itemZSpread = 2500;

    void Start()
    {
        for (int i = 0; i < numObjectsToSpawn; i++)
        {
            SpreadItem();
        }
    }

    void SpreadItem()
    {
        Vector3 randPosition = new Vector3(Random.Range(-itemXSpread, itemXSpread), Random.Range(-itemYSpread, itemYSpread), Random.Range(-itemZSpread, itemZSpread)) + transform.position;
        GameObject clone = Instantiate(objectToSpread, randPosition, Quaternion.identity);
    }
}
