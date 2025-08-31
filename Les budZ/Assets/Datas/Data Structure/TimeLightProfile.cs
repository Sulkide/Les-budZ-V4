using UnityEngine;

[CreateAssetMenu(menuName = "Lighting/TimeLightProfile")]
public class TimeLightProfile : ScriptableObject
{
    public string startTime;    // heure de début au format "HH:mm:ss"
    public string endTime;      // heure de fin au format "HH:mm:ss"
    public Vector3 startRotation; // Rotation Euler de la lumière à startTime
    public Vector3 endRotation;   // Rotation Euler de la lumière à endTime
    public Color startColor;      // Couleur de la lumière à startTime
    public Color endColor;        // Couleur de la lumière à endTime
}