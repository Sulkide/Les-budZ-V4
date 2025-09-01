using UnityEngine;
using Kolibri2d;
using Unity.VisualScripting;

public class MeshClone1erPlan : MonoBehaviour
{
    [Header("Clone Ground sous couche")]
    public Terrain2D.Terrain2DMaterialHolder materialHolderClone;
    public Material overridedMaterial;
    public Vector3 positionOffset;

    [Header("Fill 1er plan (Polygon child)")]
    public Material overridedFillMaterial;
    public Vector3 fillOffset = Vector3.zero;
    
    [Header("3D border")]
    public Material borderMaterial;

    public float borderZ = 0f;
    public float topRepeat = 0.1f;  
    public float sideRepeatU = 0.1f;
    public float sideRepeatV = 0.1f;

    private void Start()
    {
        
        
        // Duplique manuellement l'objet sans exécuter Start() du clone
        GameObject clone = Instantiate(gameObject);
        clone.name = gameObject.name + "_Clone";
        clone.transform.SetParent(transform);
        clone.transform.localPosition = positionOffset;
        clone.transform.localRotation = Quaternion.identity;



        // Supprime immédiatement ce script dans le clone
        DestroyImmediate(clone.GetComponent<MeshClone1erPlan>());
        DestroyImmediate(clone.GetComponent<SplineDecorator>());

        // Override AVANT que les autres scripts ne s'exécutent
        ApplyOverrides(clone);
        
        
        
    }

    private void ApplyOverrides(GameObject target)
    {
        
        if (target.transform.Find("Decorations").gameObject != null)
        {
            Destroy(target.transform.Find("Decorations").gameObject);
        }
        
        Terrain2D terrain2D = target.GetComponent<Terrain2D>();
        if (terrain2D != null && terrain2D.materialHolders != null && terrain2D.materialHolders.Count > 0)
        {
            terrain2D.materialHolders[0] = materialHolderClone;
            terrain2D.materialHolders[0].material = overridedMaterial;
            terrain2D.materialHolders[0].createColliders = false;
            
            terrain2D.Refresh();
        }

        if (gameObject.GetComponent<MeshSurfaceFiller>() != null)
        {
            string name = gameObject.GetComponent<MeshSurfaceFiller>().outputName;
            Transform wall = target.transform.Find(name);
            if (wall != null)
            {
                Destroy(wall.gameObject);
            }
            else
            {
                Debug.Log("Polygon non trouvé dans le clone.");
            }
        }


        
        

        
        // Trouver et modifier l'objet "Polygon"
        Transform polygon = target.transform.Find("Terrain2D Sprites").transform.GetChild(0).GetChild(0).transform;
        if (polygon != null)
        {
            MeshRenderer mr = polygon.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                mr.material = overridedFillMaterial;
            }

            Vector3 pos = polygon.localPosition;
            pos += fillOffset;
            polygon.localPosition = pos;

            BorderExtruder border = polygon.AddComponent<BorderExtruder>();
            
            border.sourceMeshFilter = polygon.GetComponent<MeshFilter>();
            border.overrideMaterial = borderMaterial;
            border.depth = borderZ;
            border.topRepeat = topRepeat;
            border.sideRepeatU = sideRepeatU;
            border.sideRepeatV = sideRepeatV;

            /*
            GameObject polygoneClone = Instantiate(polygon.gameObject, polygon.transform);
            polygoneClone.name = gameObject.name + "polygone shadowCaster";
            polygoneClone.transform.SetParent(polygon.transform);
            */

        }
        else
        {
            Debug.Log("Polygon non trouvé dans le clone.");
        }
        

        if (target.transform.Find("_Facade Background").gameObject != null)
        {
            Destroy(target.transform.Find("_Facade Background").gameObject);
        }
        
        if (target.transform.Find("PLACE HOLDER").gameObject != null)
        {
            Destroy(target.transform.Find("PLACE HOLDER").gameObject);
        }
        


     

        
    }
}
