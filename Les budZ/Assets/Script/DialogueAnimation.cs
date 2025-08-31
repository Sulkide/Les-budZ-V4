using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class DialogueAnimation : MonoBehaviour
{
    private RectTransform rectTransform;

    private Vector3 scale;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        rectTransform = GetComponent<RectTransform>();
        
        scale = rectTransform.localScale;
        
        rectTransform.DOShakeRotation(0.5f, 60f).OnComplete(() =>
        {
            rectTransform.DORotate(Vector3.zero, 0.1f).SetEase(Ease.InOutQuad);
        });
        rectTransform.DOShakeScale(0.5f, 0.2f).OnComplete(() =>
        {
            rectTransform.DOScale(scale, 0.1f).SetEase(Ease.InOutQuad);
        });
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public IEnumerator FadeIn()
    {
        rectTransform.DOShakeRotation(2f, 1f).OnComplete(() =>
        {
            rectTransform.DORotate(Vector3.zero, 2f).SetEase(Ease.InOutQuad);
        });
        rectTransform.DOShakeScale(2f, 0.02f).OnComplete(() =>
        {
            rectTransform.DOScale(scale, 2f).SetEase(Ease.InOutQuad);
        });
        yield return new WaitForSeconds(8);
    }
}
