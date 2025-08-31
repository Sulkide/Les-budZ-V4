using System;
using System.Collections.Generic;
using DG.Tweening;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;

public class Trampoline : MonoBehaviour
{
    [SerializeField] private int startPower;
    [SerializeField] private int maxPower;
    [SerializeField] private float multiplicatorVelocity;
    List<string> clipsRandomBump = new List<string> { "bumper" };
    private enum BumperMode
    {
        Scripted,
        Automatic
    }
    [SerializeField] BumperMode mode = BumperMode.Automatic;
    [ShowIf("mode", BumperMode.Scripted)]
    public Vector2 scriptedDirection;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            
            PlayerMovement controller = other.gameObject.GetComponent<PlayerMovement>();
            if (controller.canBump)
            {
                SoundManager.Instance.PlayRandomSFX(clipsRandomBump, 1, 1.2f);
                
                controller.canBump = false;
                StartCoroutine(controller.RefillDash(1));
                Rigidbody2D playerRb = other.gameObject.GetComponent<Rigidbody2D>();
                float power = startPower- playerRb.linearVelocityY*multiplicatorVelocity ;
                power = math.clamp(power,startPower, maxPower);
                Debug.Log(power);
                controller.CancelGrapple();
                Vector2 direction = mode switch
                {
                    BumperMode.Automatic => other.gameObject.transform.position - gameObject.transform.position,
                    BumperMode.Scripted => scriptedDirection,
                    _ => throw new ArgumentOutOfRangeException()
                };
                playerRb.AddForce(direction.normalized * power, ForceMode2D.Impulse);
                Sequence dotweenSeq = BumperVisualEffect();
            
                dotweenSeq.OnComplete(() =>
                {
                    controller.canBump = true;
                });
            }
           
        }
    }

    private Sequence BumperVisualEffect()
    {
        Sequence seq = DOTween.Sequence();
        
        seq.Join(transform
            .DOShakeScale(
                duration: 1 * 0.5f,
                strength: new Vector3(1.2f, 0.15f, 0f),
                vibrato: 12,
                randomness: 5f,
                fadeOut: true
            )
            .SetEase(Ease.InOutQuad));

        return seq;
    }
}
