using UnityEngine;

public class Respawn : MonoBehaviour
{
    private GameObject targetObject;

    void Start()
    {
        targetObject = GameObject.FindGameObjectWithTag("AllPlayer");

        if (targetObject != null)
        {
            foreach (Transform child in targetObject.transform)
            {
                child.position = transform.position;
            }
        }
        else
        {
            Debug.LogWarning("Aucun objet avec le tag 'AllPlayer' n'a été trouvé !");
        }
    }
}