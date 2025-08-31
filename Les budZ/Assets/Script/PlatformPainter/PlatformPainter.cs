using UnityEngine;

public class PlatformPainter : MonoBehaviour
{
    [Tooltip("Définir la taille (diamètre) du brush.")]
    public float brushSize = 1.0f;
    
    public enum SpriteShape { Circle, Square, Triangle, Losange, Hexagon }
    [Tooltip("Sélectionne le sprite utilisé pour la forme.")]
    public SpriteShape shapeType = SpriteShape.Circle;

    [Tooltip("Angle de rotation (en degrés) appliqué à la forme choisie.")]
    public float shapeRotation = 0f;
    
    [Tooltip("Active le mode ligne (création d'un rectangle + formes).")]
    public bool lineMode = false;

    [Tooltip("Active l'utilisation d'un angle fixe pour la ligne.")]
    public bool useFixedAngle = false;

    [Tooltip("Angle fixe (en degrés) de la ligne par rapport à l'axe Y. Ex : 90 pour une ligne horizontale.")]
    public float fixedAngle = 0f;

    [Tooltip("Contrôle l'apparition des deux formes aux extrémités.")]
    public bool showCircles = true;
    
    [Tooltip("GameObject parent dans lequel les plateformes seront instanciées.")]
    public Transform parentObject;
}