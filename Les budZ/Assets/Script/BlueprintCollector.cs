using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class BlueprintUIManager : MonoBehaviour
{
    public static BlueprintUIManager Instance { get; private set; }

    [Header("Références UI")]
    public Canvas canvas;                     // Ton Canvas Screen Space – Camera
    public Image slotBackgroundPrefab;        // Nouveau : prefab d’Image pour le fond
    public Image slotUIPrefab;                // Prefab d’Image pour l’icône de blueprint

    [Header("Disposition des slots")]
    public float slotSpacing = 10f;           
    public Vector2 margin = Vector2.zero;     

    [Header("Sprites des blueprints")]
    public Sprite[] blueprintSprites;         // Tes sprites, dans le même ordre que BluePrintsList

    // Stockage des transforms et images
    private RectTransform[] backgroundSlots;
    private Image[] slotImages;

    private void Awake()
    {
        if (Instance != null && Instance != this)
            Destroy(gameObject);
        else
            Instance = this;
    }

    private void Start()
    {
        
        
        // 1) Validation
        if (canvas == null || slotBackgroundPrefab == null || slotUIPrefab == null)
        {
            Debug.LogError("[UIManager] Assigne canvas, slotBackgroundPrefab et slotUIPrefab !");
            enabled = false; return;
        }

        int count = GameManager.instance.maxBluePrintInLevel;
        backgroundSlots = new RectTransform[count];
        slotImages      = new Image[count];

        RectTransform canvasRT = canvas.GetComponent<RectTransform>();
        RectTransform bgRT     = slotBackgroundPrefab.rectTransform;
        float bgW = bgRT.sizeDelta.x, bgH = bgRT.sizeDelta.y;

        // 2) Création dynamiques : fond + slot enfant
        for (int i = 0; i < count; i++)
        {
            // 2a) Fond
            Image bg = Instantiate(slotBackgroundPrefab, canvas.transform);
            bg.name = $"SlotBG_{i}";
            RectTransform bgRect = bg.rectTransform;
            bgRect.anchorMin = bgRect.anchorMax = new Vector2(1,1);
            bgRect.pivot     = new Vector2(0.5f,0.5f);

            float posX = -margin.x - bgW * .5f - (bgW + slotSpacing) * (count - 1 - i);
            float posY = -margin.y - bgH * .5f;
            bgRect.anchoredPosition = new Vector2(posX, posY);

            // 2b) Icône de blueprint (slot)
            Image slotImg = Instantiate(slotUIPrefab, bgRect);
            slotImg.name    = $"SlotImg_{i}";
            slotImg.enabled = false;
            RectTransform slotRT = slotImg.rectTransform;
            // on centre dans le bg
            slotRT.anchorMin = slotRT.anchorMax = new Vector2(0.5f,0.5f);
            slotRT.pivot     = new Vector2(0.5f,0.5f);
            slotRT.anchoredPosition = Vector2.zero;
            slotRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, bgW * 0.8f);
            slotRT.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   bgH * 0.8f);

            // 2c) Stockage
            backgroundSlots[i] = bgRect;
            slotImages[i]      = slotImg;
        }

        // 3) Activate already collected slots
        foreach (var uniqueId in GameManager.instance.collectedBluePrint)
        {
            int idx = ParseBlueprintIndex(uniqueId);
            if (idx >= 0 && idx < slotImages.Length)
            {
                slotImages[idx].sprite  = blueprintSprites.Length == count 
                                          ? blueprintSprites[idx]
                                          : slotImages[idx].sprite;
                slotImages[idx].enabled = true;
            }
        }
    }

    // Extrait le numéro entre parenthèses : "...(2)_..."
    private int ParseBlueprintIndex(string id)
    {
        var m = Regex.Match(id, @"\((\d+)\)");
        return (m.Success && int.TryParse(m.Groups[1].Value, out int v)) ? v : -1;
    }

    /// <summary>
    /// Anime une copie du sprite vers le fond ciblé, puis active le slot.
    /// </summary>
    public void AnimateCollectAt(int index, Sprite blueprintSprite, Vector3 worldPosition)
    {
        if (index < 0 || index >= backgroundSlots.Length)
        {
            Debug.LogError($"AnimateCollectAt : index {index} hors bornes");
            return;
        }

        // Instancie la copie au niveau du Canvas
        Image copy = Instantiate(slotUIPrefab);
        copy.enabled = true;
        copy.sprite  = blueprintSprite;
        copy.SetNativeSize();

        RectTransform copyRT = copy.rectTransform;
        copyRT.SetParent(canvas.transform, false);
        // to match le fond cible
        var targetBG = backgroundSlots[index];
        copyRT.anchorMin = targetBG.anchorMin;
        copyRT.anchorMax = targetBG.anchorMax;
        copyRT.pivot     = targetBG.pivot;

        // position de départ
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(), 
            Camera.main.WorldToScreenPoint(worldPosition),
            Camera.main, out Vector2 startPos);
        copyRT.anchoredPosition = startPos;

        // tween vers le fond
        Vector2 endPos = targetBG.anchoredPosition;
        copyRT
          .DOAnchorPos(endPos, 0.6f)
          .SetEase(Ease.OutQuad)
          .OnComplete(() =>
          {
              // active l’image finale
              slotImages[index].sprite  = blueprintSprite;
              slotImages[index].enabled = true;
              Destroy(copy.gameObject);
          });
    }
    
    public void ClearSlots()
    {
        for (int i = 0; i < slotImages.Length; i++)
        {
            slotImages[i].enabled = false;
            slotImages[i].sprite  = null;
        }
    }

}
