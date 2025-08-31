using UnityEngine;

[CreateAssetMenu]
public class EntityData : ScriptableObject
{
    [field: Header("entity name"),SerializeField]
    public string entityName { get; private set; }
    
    [field: Header("entity life"),SerializeField]
    public int hp { get; private set; }
    
    [field: Header("entity power"),SerializeField]
    public int power { get; private set; }
    
    [field: Header("entity jump power"),SerializeField]
    public int jumpPower { get; private set; }
    
    [field: Header("entity speed"),SerializeField]
    public int speed { get; private set; }
    
    [field: Header("entity projectile speed"),SerializeField]
    public int projectileSpeed { get; private set; }
    
    public EntityDataInstance Instance()
    {
        return new EntityDataInstance(this);
    }
}

public class EntityDataInstance //WRAPPER
{
    public string entityName;
    public int hp;
    public int power;
    public int speed;
    public int projectileSpeed;
    public int jumpPower;
    
    public EntityDataInstance(EntityData data)
    {
        entityName = data.entityName;
        hp = data.hp;
        power = data.power;
        speed = data.speed;
        projectileSpeed = data.projectileSpeed;
        jumpPower = data.jumpPower;
        
    }
}