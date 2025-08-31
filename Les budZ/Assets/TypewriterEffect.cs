using UnityEngine;
using UnityEngine.UI;
using System.Collections;

[RequireComponent(typeof(Text))]
public class TypewriterEffect : MonoBehaviour
{
    private Text uiText;
    private Coroutine typingCo;
    private float charsPerSecond = 45f;
    private string targetText = "";
    private bool isTyping = false;

    void Awake()
    {
        uiText = GetComponent<Text>();
    }

    public void SetSpeed(float cps)
    {
        charsPerSecond = Mathf.Max(1f, cps);
    }

    public void StartTyping(string full)
    {
        if (typingCo != null) StopCoroutine(typingCo);
        targetText = full ?? "";
        typingCo = StartCoroutine(TypeRoutine());
    }

    IEnumerator TypeRoutine()
    {
        isTyping = true;
        uiText.text = "";
        float t = 0f;
        int idx = 0;
        while (idx < targetText.Length)
        {
            t += Time.deltaTime * charsPerSecond;
            int next = Mathf.Min(targetText.Length, Mathf.FloorToInt(t));
            if (next != idx)
            {
                idx = next;
                uiText.text = targetText.Substring(0, idx);
            }
            yield return null;
        }
        isTyping = false;
    }

    public bool IsTyping() => isTyping;

    public void StopAndShowAll(string full)
    {
        if (typingCo != null) StopCoroutine(typingCo);
        isTyping = false;
        uiText.text = full ?? "";
    }
}