using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.InputSystem;
using DG.Tweening;

public class EndLevelTrigger : MonoBehaviour
{
    [Header("UI et Préfab")]
    public Image transitionImage;
    public Image endImageUI;
    public RectTransform imageFinalPos;
    public GameObject playerInfoPrefab;
    public Transform playersContainer;

    [Header("Slider et Texte")]
    public Slider progressSlider;
    public RectTransform sliderFinalPos;
    public RectTransform sliderTextFinalPos;
    public RectTransform textBluePrintFinalPos;
    public TextMeshProUGUI progressValueText;
    public TextMeshProUGUI progressValueBluePrintText;
    public TextMeshProUGUI maxValueSliderText;
    public TextMeshProUGUI maxValueBluePrintText;

    [Header("Textes informatifs")]
    public TextMeshProUGUI firstPlayerText;
    public TextMeshProUGUI bonusAwardedText;
    public TextMeshProUGUI levelScoreText;

    [Header("Confirmation")]
    public TextMeshProUGUI confirmText;

    [Header("Icônes et Noms")]
    public Sprite[] playerIcons;
    public string[] playerNames;

    [Header("Layout")]
    [Tooltip("Espacement vertical (pixels) entre chaque icône de joueur")]
    public float playerInfoSpacingY = 100f;

    [Header("Animations")]
    public float idlePulseScale = 1.05f;
    public float idlePulseDuration = 2f;
    public float selectionGrowScale = 1.2f;
    public float selectionAnimDuration = 0.2f;
    public float doubleClickMaxInterval = 0.4f;
    public float doubleClickShakeDuration = 0.2f;

    [Header("Navigation Niveau")]
    public string nextSceneName;
    public int nextRespawnIndex;

    private bool eventTriggered = false;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (eventTriggered) return;
        if (other.gameObject.layer != LayerMask.NameToLayer("Player")) return;
        eventTriggered = true;

        // +15 points immédiat pour le joueur ayant terminé
        var pmFinish = other.GetComponent<PlayerMovement>();
        if (pmFinish != null)
            GameManager.instance.addScore(15, pmFinish.parentName);

        StartCoroutine(EndLevelSequence());
    }

    private IEnumerator EndLevelSequence()
    {
        GameManager.instance.DisableOffScreenDeath();
        GameManager.instance.MakePlayerInvinsible();
        
        SoundManager.Instance.FadeMusic(3);
        
        BlueprintUIManager.Instance.ClearSlots();
        
        // 1) Bloquer tous les joueurs
        foreach (var pm in FindObjectsOfType<PlayerMovement>())
            pm.areControllsRemoved = true;
        
        // 2) Flash blanc (0.5s)
        if (transitionImage)
        {
            transitionImage.color = Color.white;
            transitionImage.DOFade(1f, 0f);
            yield return new WaitForSeconds(0.5f);
        }

        // 3) Fondu au noir (1s)
        if (transitionImage)
            yield return transitionImage.DOColor(Color.black, 1f).WaitForCompletion();

        // 4) Slide-in de l'image de fin (0.2s)
        if (endImageUI && imageFinalPos)
        {
            var rect = endImageUI.rectTransform;
            Vector2 dest = imageFinalPos.anchoredPosition;
            var canvasRT = rect.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            float offX = canvasRT.rect.width / 2 + rect.rect.width;
            rect.anchoredPosition = new Vector2(offX, dest.y);
            rect.DOAnchorPos(dest, 0.2f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.2f);
        }

        // 5) Instancier et animer l’UI des joueurs + idle pulse
        var inputs = FindObjectsOfType<PlayerInput>()
                     .OrderBy(pi => pi.playerIndex).ToArray();
        var infoRTs = new List<RectTransform>();
        var idleTweens = new List<Tweener>();
        float startY = -Screen.height;

        for (int i = 0; i < inputs.Length; i++)
        {
            int slot = inputs[i].playerIndex + 1;
            var go = Instantiate(playerInfoPrefab, playersContainer);
            var rt = go.GetComponent<RectTransform>();
            infoRTs.Add(rt);

            // Icône
            var iconImg = rt.Find("Icon").GetComponent<Image>();
            var c = GameManager.playerCharacterIs.none;
            switch (slot)
            {
                case 1: c = GameManager.instance.player1Is; break;
                case 2: c = GameManager.instance.player2Is; break;
                case 3: c = GameManager.instance.player3Is; break;
                case 4: c = GameManager.instance.player4Is; break;
            }
            int idx = (int)c;
            if (idx >= 0 && idx < playerIcons.Length)
                iconImg.sprite = playerIcons[idx];

            // Nom & numéro & score initial
            rt.Find("NameText").GetComponent<TextMeshProUGUI>().text =
                (idx >= 0 && idx < playerNames.Length && !string.IsNullOrEmpty(playerNames[idx]))
                ? playerNames[idx]
                : "Joueur " + slot;
            rt.Find("NumberText").GetComponent<TextMeshProUGUI>().text = slot.ToString();
            rt.Find("ScoreText").GetComponent<TextMeshProUGUI>().text = "0";

            // Slide-in
            Vector2 finalP = new Vector2(0, -50f - playerInfoSpacingY * i);
            rt.anchoredPosition = new Vector2(0, startY);
            rt.DOAnchorPos(finalP, 0.2f)
              .SetEase(Ease.OutBack)
              .SetDelay(0.1f * i);

            // Idle pulse
            var tween = rt.DOScale(idlePulseScale, idlePulseDuration)
                          .SetLoops(-1, LoopType.Yoyo)
                          .SetEase(Ease.InOutSine);
            idleTweens.Add(tween);
        }

        yield return new WaitForSeconds(0.2f + 0.1f * inputs.Length);

        // 6) Slider + progress text + individual scores (3s)
        if (progressSlider && sliderFinalPos && sliderTextFinalPos && textBluePrintFinalPos)
        {
            // Slide-in slider & text
            var sRT = progressSlider.GetComponent<RectTransform>();
            var cRT = sRT.GetComponentInParent<Canvas>().GetComponent<RectTransform>();
            float offY = cRT.rect.height / 2 + sRT.rect.height;
            sRT.anchoredPosition = new Vector2(sliderFinalPos.anchoredPosition.x, offY);
            var tRT = progressValueText.rectTransform;
            var bRT = progressValueBluePrintText.rectTransform;
            tRT.anchoredPosition = new Vector2(sliderTextFinalPos.anchoredPosition.x, offY);
            bRT.anchoredPosition = new Vector2(textBluePrintFinalPos.anchoredPosition.x, offY);

            sRT.DOAnchorPos(sliderFinalPos.anchoredPosition, 0.5f).SetEase(Ease.OutQuad);
            tRT.DOAnchorPos(sliderTextFinalPos.anchoredPosition, 0.5f).SetEase(Ease.OutQuad);
            bRT.DOAnchorPos(textBluePrintFinalPos.anchoredPosition, 0.5f).SetEase(Ease.OutQuad);
            yield return new WaitForSeconds(0.5f);

            int levelMax   = GameManager.instance.maxScoreInLevel;
            int levelMaxBluePrint = GameManager.instance.maxBluePrintInLevel;
            int displayMax = levelMax + 15;
            int finalScore = GameManager.instance.Score;
            int finalBluePrint = GameManager.instance.BluePrint;

            if (maxValueSliderText)
                maxValueSliderText.text = displayMax.ToString();
            if(maxValueBluePrintText)
                maxValueBluePrintText.text = levelMaxBluePrint.ToString();

            progressSlider.maxValue = displayMax;
            progressSlider.value    = 0;
            progressSlider.DOValue(finalScore, 3f).SetEase(Ease.Linear);

            DOTween.To(() => 0, x => progressValueText.text = x.ToString(), finalScore, 3f)
                   .SetEase(Ease.Linear);
            DOTween.To(() => 0, x => progressValueBluePrintText.text = x.ToString(), finalBluePrint, 3f)
                .SetEase(Ease.Linear);

            // Animate each player's score
            for (int i = 0; i < inputs.Length; i++)
            {
                int slot = inputs[i].playerIndex + 1;
                int target = GetPlayerScore(slot);
                var scrTxt = infoRTs[i].Find("ScoreText").GetComponent<TextMeshProUGUI>();
                DOTween.To(() => 0, v => scrTxt.text = v.ToString(), target, 3f)
                       .SetEase(Ease.Linear);
            }
        }
        
        yield return new WaitForSeconds(3f);

        // 7) Textes informatifs
        string firstName = infoRTs[0].Find("NameText").GetComponent<TextMeshProUGUI>().text;
        firstPlayerText.alpha = 0f;
        bonusAwardedText.alpha = 0f;
        levelScoreText.alpha = 0f;
        firstPlayerText.text = "Premier joueur : " + firstName;
        bonusAwardedText.text = "Bonus attribué à : " + firstName;
        levelScoreText.text = "Score total du niveau : " + GameManager.instance.Score;

        firstPlayerText.DOFade(1f, 1f).SetEase(Ease.InOutQuad);
        bonusAwardedText.DOFade(1f, 1f).SetEase(Ease.InOutQuad).SetDelay(0.5f);
        levelScoreText.DOFade(1f, 1f).SetEase(Ease.InOutQuad).SetDelay(1f);

        firstPlayerText.rectTransform.DOScale(1.1f, idlePulseDuration)
                         .SetLoops(-1, LoopType.Yoyo)
                         .SetEase(Ease.InOutSine);

        yield return new WaitForSeconds(1.5f);

        // 8) Sélection du bonus (top scorer only)
        var order = Enumerable.Range(0, inputs.Length).ToList();
        order.Sort((a, b) =>
            GetPlayerScore(inputs[b].playerIndex + 1)
          .CompareTo(GetPlayerScore(inputs[a].playerIndex + 1)));
        int topSlot = inputs[order[0]].playerIndex + 1;

        PlayerMovement topPM = topSlot switch
        {
            1 => GameManager.instance.player1Location.GetComponent<PlayerMovement>(),
            2 => GameManager.instance.player2Location.GetComponent<PlayerMovement>(),
            3 => GameManager.instance.player3Location.GetComponent<PlayerMovement>(),
            4 => GameManager.instance.player4Location.GetComponent<PlayerMovement>(),
            _ => null
        };

        var sortedRTs = order.Select(i => infoRTs[i]).ToList();

        // Prepare scales and tweens
        for (int i = 0; i < sortedRTs.Count; i++)
            sortedRTs[i].localScale = Vector3.one * idlePulseScale;

        int sel = 0;
        // Kill idle tween on first selection to keep it grown
        idleTweens[order[sel]].Kill(false);
        sortedRTs[sel].DOScale(selectionGrowScale, selectionAnimDuration).SetEase(Ease.OutBack);

        // Navigation loop
        while (true)
        {
            float v = topPM.moveInput.y;
            if (v > 0.5f)
            {
                // Deselect old
                sortedRTs[sel].DOScale(idlePulseScale, 0.1f);
                // Restart its idle
                idleTweens[order[sel]] = sortedRTs[sel]
                    .DOScale(idlePulseScale, idlePulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);

                sel = (sel + 1) % sortedRTs.Count;

                // Select new
                idleTweens[order[sel]].Kill(false);
                sortedRTs[sel].DOScale(selectionGrowScale, selectionAnimDuration).SetEase(Ease.OutBack);

                yield return new WaitForSeconds(0.2f);
            }
            else if (v < -0.5f)
            {
                // Deselect old
                sortedRTs[sel].DOScale(idlePulseScale, 0.1f);
                idleTweens[order[sel]] = sortedRTs[sel]
                    .DOScale(idlePulseScale, idlePulseDuration)
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine);

                sel = (sel - 1 + sortedRTs.Count) % sortedRTs.Count;

                // Select new
                idleTweens[order[sel]].Kill(false);
                sortedRTs[sel].DOScale(selectionGrowScale, selectionAnimDuration).SetEase(Ease.OutBack);

                yield return new WaitForSeconds(0.2f);
            }

            if (topPM.useInputRegistered)
                break;

            yield return null;
        }

        // 9) Appliquer bonus vie ou XP
        int chosenSlot = inputs[order[sel]].playerIndex + 1;
        bool already = chosenSlot switch
        {
            1 => GameManager.instance.player1Bonus,
            2 => GameManager.instance.player2Bonus,
            3 => GameManager.instance.player3Bonus,
            4 => GameManager.instance.player4Bonus,
            _ => false
        };
        if (already)
            GameManager.instance.addXP(5000);
        else
            GameManager.instance.addOrRemovePlayerBonus("Player " + chosenSlot, true);

        // Feedback sélection
        sortedRTs[sel].DOShakeScale(selectionAnimDuration,
            new Vector3(0.3f, 0.3f, 0f), 7, 90, true);

        yield return new WaitForSeconds(0.5f);

        // 10) Confirmation finale (double-clic rapide)
        if (confirmText)
        {
            confirmText.alpha = 0f;
            confirmText.text = "Appuiez deux fois sur bouton action";
            confirmText.DOFade(1f, 0.5f).SetEase(Ease.InOutQuad);
        }

        float lastTime = -Mathf.Infinity;
        int clickCount = 0;
        bool awaitingRelease = false;

        while (clickCount < 2)
        {
            bool pressed = topPM.useInputRegistered;
            if (pressed && !awaitingRelease)
            {
                float now = Time.time;
                if (now - lastTime <= doubleClickMaxInterval)
                    clickCount++;
                else
                    clickCount = 1;
                lastTime = now;
                awaitingRelease = true;
            }
            else if (!pressed)
            {
                awaitingRelease = false;
            }
            yield return null;
        }

        // Double-clic feedback
        if (confirmText)
            confirmText.rectTransform.DOShakeScale(doubleClickShakeDuration,
                new Vector3(0.2f, 0.2f, 0f), 5, 90, true);

        yield return new WaitForSeconds(doubleClickShakeDuration);

        // 11) Changer de scène
        GameManager.instance.ChangeScene(nextSceneName, nextRespawnIndex);
    }

    private int GetPlayerScore(int slot)
    {
        return slot switch
        {
            1 => GameManager.instance.player1Score,
            2 => GameManager.instance.player2Score,
            3 => GameManager.instance.player3Score,
            4 => GameManager.instance.player4Score,
            _ => 0
        };
    }
}
