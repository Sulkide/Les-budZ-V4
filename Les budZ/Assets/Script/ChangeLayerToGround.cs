using UnityEngine;

public class ChangeLayerToGround : MonoBehaviour
{
    [SerializeField] bool includeInactive = true;
    public string layerName = "Ground";

    void Start()
    {
        int layer = LayerMask.NameToLayer(layerName);
        Transform[] all = GetComponentsInChildren<Transform>(includeInactive);
        foreach (Transform t in all)
            t.gameObject.layer = layer;
    }
}
