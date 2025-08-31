using UnityEngine;

public class InverseRotationParent : MonoBehaviour
{
    void LateUpdate()
    {
        if (transform.parent != null)
        {
            // On inverse la rotation locale en Z du parent pour annuler son effet sur l'enfant
            transform.localRotation = Quaternion.Euler(0f, 0f, -transform.parent.localEulerAngles.z);
        }
    }
}