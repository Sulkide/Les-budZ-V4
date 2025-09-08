// KeyObjData.cs
using UnityEngine;

[CreateAssetMenu(menuName = "Items/Key Object Data")]
public class KeyObjData : ScriptableObject
{
    [Tooltip("ID unique et stable (sert à éviter les doublons et à sauvegarder)")]
    public string id;

    public string displayName;

    [TextArea] public string description;

    public Sprite icon;
}