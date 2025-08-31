using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class Entity : MonoBehaviour
{
    
    #region DATA INITIALIZATION

    // Stored data
    [SerializeField] private EntityData entityData;
        
    // modifiable data
    protected EntityDataInstance currentEntityData { get; private set; }
        
    private void Awake()
    {
        currentEntityData = entityData.Instance();
    }
    
    #endregion

    protected void EditLife(int newLife)
    {
        currentEntityData.hp = Mathf.Clamp(newLife, 0, entityData.hp);
        if (currentEntityData.hp == 0)
        {
            Die();
        }
    }
    

    protected abstract void Die();

}
