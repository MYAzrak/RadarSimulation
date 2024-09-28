using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RandomizeObjectSelection : MonoBehaviour
{
    public GameObject[] objects;
    void Start()
    {
        PickItem();
    }

    void PickItem()
    {
        int randomIndex = Random.Range(0, objects.Length);
        GameObject clone = Instantiate(objects[randomIndex], transform.position, Quaternion.identity); // Clones the prefab and doesn't rotate 
    }
}
