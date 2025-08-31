using System;
using DG.Tweening;
using UnityEngine;

public class CheckPoint : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sr;

    private Color baseColor;

    private void Start()
    {
        baseColor = sr.color;
    }

    public void OnTriggerEnter2D(Collider2D other)
    {
        GameManager.instance.UpdateCheckPoint(transform.position);
        sr.DOColor(Color.white, 0.1f).SetEase(Ease.OutExpo);
        sr.DOColor(baseColor, 0.2f).SetEase(Ease.InExpo).SetDelay(0.1f);
    }
}
