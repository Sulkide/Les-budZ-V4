using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class KeyPickupBanner : MonoBehaviour
{
    // Singleton léger : un seul bandeau à la fois.
    private static KeyPickupBanner _current;

    [Header("Durées")]
    [SerializeField] private float slideDuration = 0.35f;
    [SerializeField] private float holdDuration  = 7.0f;

    [Header("Apparence")]
    [SerializeField] private Color barColor = Color.black;
    [SerializeField] private float barAlpha = 1f;

    [Tooltip("Si OneSixthEachBar=false, et que cette valeur > 0, on utilisera une hauteur fixe en pixels par barre.")]
    [SerializeField] private int barPixelHeight = 0;

    // --- OPTION hauteur 1/6 par barre (par défaut ON) ---
    private bool _oneSixthEachBar = true;

    [Header("Texte (tailles par défaut)")]
    [SerializeField] private int titleFontSize = 28;
    [SerializeField] private int descFontSize  = 22;

    // valeurs effectives utilisées pour ce bandeau (peuvent être surchargées via Show)
    private int _titleFontSize;
    private int _descFontSize;

    private Canvas _canvas;
    private RectTransform _topBar, _botBar;
    private Image _itemIcon;
    private Text _titleText, _descText;

    private Coroutine _run;

    // ------- API statique pratique -------
    /// <param name="seconds">Durée d'affichage</param>
    /// <param name="oneSixthEachBar">true = chaque barre = 1/6 d'écran</param>
    /// <param name="titleSize">Taille de police du titre (null = valeur par défaut)</param>
    /// <param name="descSize">Taille de police de la description (null = valeur par défaut)</param>
    public static void Show(
        Sprite icon,
        string title,
        string description,
        float seconds = 7f,
        bool oneSixthEachBar = true,
        int? titleSize = null,
        int? descSize  = null)
    {
        if (_current != null) _current.ForceClose();

        var go = new GameObject("KeyPickupBanner");
        _current = go.AddComponent<KeyPickupBanner>();

        // Durée + options
        _current.holdDuration     = seconds > 0 ? seconds : 7f;
        _current._oneSixthEachBar = oneSixthEachBar;

        // Applique tailles effectives (fallback sur les valeurs sérialisées)
        _current._titleFontSize = Mathf.Max(1, titleSize ?? _current.titleFontSize);
        _current._descFontSize  = Mathf.Max(1, descSize  ?? _current.descFontSize);

        // Construit UI puis peuple
        _current.BuildUI();
        _current.Populate(icon, title, description);

        // Lance la routine
        _current._run = _current.StartCoroutine(_current.Run());
    }

    public void ForceClose()
    {
        if (_run != null) StopCoroutine(_run);
        StartCoroutine(CloseAndDestroyImmediate());
    }

    // ------- construction UI -------
    private void BuildUI()
    {
        // Canvas overlay
        _canvas = gameObject.AddComponent<Canvas>();
        _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        gameObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        gameObject.AddComponent<GraphicRaycaster>();

        // Top bar
        _topBar = MakeBar("TopBar");
        // Bottom bar
        _botBar = MakeBar("BottomBar");

        // Titre (en haut)
        _titleText = MakeText(_topBar, new Vector2(20, -8), anchorTop:true);
        _titleText.alignment = TextAnchor.MiddleLeft;
        _titleText.fontSize  = _titleFontSize;   // <-- taille appliquée ici
        _titleText.color     = Color.white;

        // Icône + description (en bas)
        var iconGO = new GameObject("ItemIcon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(_botBar, false);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot = new Vector2(0, 0.5f);
        iconRT.anchoredPosition = new Vector2(20, 0);
        iconRT.sizeDelta = new Vector2(64, 64);
        _itemIcon = iconGO.GetComponent<Image>();
        _itemIcon.raycastTarget = false;

        _descText = MakeText(_botBar, new Vector2(96, 0), anchorTop:false);
        _descText.alignment = TextAnchor.MiddleLeft;
        _descText.fontSize  = _descFontSize;     // <-- taille appliquée ici
        _descText.color     = Color.white;

        // Place bars “hors-écran” (simulé)
        SetBarsOutside();
    }

    private RectTransform MakeBar(string name)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(_canvas.transform, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 1);
        rt.anchorMax = new Vector2(1, 1);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        var img = go.GetComponent<Image>();
        img.color = new Color(barColor.r, barColor.g, barColor.b, Mathf.Clamp01(barAlpha));
        return rt;
    }

    private Text MakeText(RectTransform parent, Vector2 offset, bool anchorTop)
    {
        var go = new GameObject("Text", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        if (anchorTop)
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0, 1);
            rt.offsetMin = new Vector2(offset.x, offset.y - 36);
            rt.offsetMax = new Vector2(-20, -8);
        }
        else
        {
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0, 0.5f);
            rt.offsetMin = new Vector2(offset.x, 8);
            rt.offsetMax = new Vector2(-20, -8);
        }
        var t = go.GetComponent<Text>();
        t.supportRichText = true;

        // Police par défaut
        try { t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch { t.font = Font.CreateDynamicFontFromOSFont("Arial", 16); }

        return t;
    }

    private void Populate(Sprite icon, string title, string description)
    {
        _itemIcon.sprite  = icon;
        _itemIcon.enabled = icon != null;

        _titleText.text = string.IsNullOrEmpty(title) ? "Nouvel objet" : title;
        _descText.text  = description ?? "";
    }

    private IEnumerator Run()
    {
        yield return SlideIn();
        yield return new WaitForSecondsRealtime(holdDuration);
        yield return CloseAndDestroyImmediate();
    }

    // ------- Anim helpers -------
    private void SetBarsOutside()
    {
        float h = ComputeBarPixelHeight();
        // Top au-dessus de l’écran
        _topBar.anchorMin = new Vector2(0, 1);
        _topBar.anchorMax = new Vector2(1, 1);
        _topBar.offsetMin = new Vector2(0, 0);
        _topBar.offsetMax = new Vector2(0, h);

        // Bottom en dessous de l’écran
        _botBar.anchorMin = new Vector2(0, 0);
        _botBar.anchorMax = new Vector2(1, 0);
        _botBar.offsetMin = new Vector2(0, -h);
        _botBar.offsetMax = new Vector2(0, 0);
    }

    private IEnumerator SlideIn()
    {
        float h = ComputeBarPixelHeight();
        float t = 0f;
        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0, 1, t / slideDuration);

            _topBar.offsetMin = new Vector2(0, -h * k);
            _topBar.offsetMax = new Vector2(0, h * (1 - k));

            _botBar.offsetMin = new Vector2(0, -h * (1 - k));
            _botBar.offsetMax = new Vector2(0, h * k);

            yield return null;
        }
        _topBar.offsetMin = new Vector2(0, -h);
        _topBar.offsetMax = new Vector2(0, 0);
        _botBar.offsetMin = new Vector2(0, 0);
        _botBar.offsetMax = new Vector2(0, h);
    }

    private IEnumerator CloseAndDestroyImmediate()
    {
        float h = ComputeBarPixelHeight();
        float t = 0f;
        var texts  = GetComponentsInChildren<Text>(true);
        var images = GetComponentsInChildren<Image>(true);

        while (t < slideDuration)
        {
            t += Time.unscaledDeltaTime;
            float k = Mathf.SmoothStep(0, 1, t / slideDuration);
            float inv = 1f - k;

            _topBar.offsetMin = new Vector2(0, -h * inv);
            _topBar.offsetMax = new Vector2(0, h * k);
            _botBar.offsetMin = new Vector2(0, -h * k);
            _botBar.offsetMax = new Vector2(0, h * inv);

            foreach (var im in images) im.color = new Color(im.color.r, im.color.g, im.color.b, inv);
            foreach (var tx in texts)  tx.color = new Color(tx.color.r, tx.color.g, tx.color.b, inv);

            yield return null;
        }

        Destroy(gameObject);
        if (_current == this) _current = null;
    }

    private int ComputeBarPixelHeight()
    {
        if (_oneSixthEachBar)
            return Mathf.RoundToInt(Screen.height / 12f);  // chaque barre = 1/6

        if (barPixelHeight > 0)
            return barPixelHeight;                        // hauteur fixe

        return Mathf.RoundToInt(Screen.height / 12f);      // fallback
    }
}
