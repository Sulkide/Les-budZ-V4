using UnityEngine;

public class PersistentSingleton : MonoBehaviour
{
    private static PersistentSingleton instance;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
    }
}