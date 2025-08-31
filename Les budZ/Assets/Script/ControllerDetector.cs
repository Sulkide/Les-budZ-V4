using UnityEngine;
using UnityEngine.InputSystem;

public class ControllerDetector : MonoBehaviour
{
    void Start()
    {
        foreach (var device in InputSystem.devices)
        {
            if (device is Gamepad gamepad)
            {
                Debug.Log($"Manette détectée : {gamepad.name}, ID : {gamepad.deviceId}");
            }
        }
    }
}