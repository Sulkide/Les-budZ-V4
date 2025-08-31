using Unity.Cinemachine;
using UnityEngine;

public class CameraControler : MonoBehaviour
{
    [SerializeField] private CinemachineCamera camera;
    public void SwitchTarget(Transform objectToTarget)
    {
        camera.Target.TrackingTarget = objectToTarget;
    }
}

