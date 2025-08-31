using UnityEngine;
using System.Collections.Generic;

public class CheckPrefabInstances : MonoBehaviour
{
    [SerializeField] private List<GameObject> prefabList = new List<GameObject>(4);

    private void Update()
    {
        for (int i = 0; i < prefabList.Count; i++)
        {
            if (!IsInstancePresent(prefabList[i]))
            {
                HandleMissingObject(i);
            }
        }
    }

    private bool IsInstancePresent(GameObject prefab)
    {
        GameObject[] allObjects = FindObjectsOfType<GameObject>();

        foreach (GameObject obj in allObjects)
        {
            if (obj.name.StartsWith(prefab.name))
            {
                return true;
            }
        }
        return false;
    }

    private void HandleMissingObject(int index)
    {
        switch (index)
        {
            case 0:
                MethodForObject0();
                break;
            case 1:
                MethodForObject1();
                break;
            case 2:
                MethodForObject2();
                break;
            case 3:
                MethodForObject3();
                break;
        }
    }
    
    private void MethodForObject0()
    {
        GameManager.instance.isDarckoxPresent = false;
    }

    private void MethodForObject1()
    {
        GameManager.instance.isSlowPresent = false;
    }

    private void MethodForObject2()
    {
        GameManager.instance.isSulkidePresent = false;
    }

    private void MethodForObject3()
    {
        GameManager.instance.isSulanaPresent = false;
    }
}
