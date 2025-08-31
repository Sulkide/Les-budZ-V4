using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Sulkide.Dialogue;
using D = Sulkide.Dialogue;

public class NPCmanager : MonoBehaviour
{
    private bool eventStart = false;

    [Header("Dialogue Runtime")]
    [SerializeField] private string defaultNpcName = "PNJ";
    private static readonly string[] UseActionNames = new[] { "Use" };

    // Options jouées par asset
    private readonly Dictionary<CharacterDialogueData, HashSet<int>> _usedOptions = new();
    // Indices visibles (UI -> data)
    private readonly List<int> _visibleOptionIndices = new();

    [SerializeField] private DummyAnimation npcAnim; // PNJ

    private DummyAnimation[] characterAnims;   // Sulkide, Darckox, MrSlow, Sulana
    private int _currentOptionSourceIndex = -1;

    [Header("Character Highlight Colors")]
    [SerializeField] private Color sulkideColor = Color.red;
    [SerializeField] private Color darckoxColor = Color.yellow;
    [SerializeField] private Color mrSlowColor  = Color.green;
    [SerializeField] private Color sulanaColor  = Color.blue;

    [Header("TopBar Colors")]
    [SerializeField] private Color topBarUnselectedBg = new(0.12f, 0.12f, 0.12f, 0.95f);
    [SerializeField] private Color topBarSelectedText = Color.white;
    [SerializeField] private Color topBarUnselectedText = new(0.85f, 0.85f, 0.85f, 1f);

    [Header("UI Timing")]
    [SerializeField] private float optionsOpenCooldown = 0.25f;
    private float optionsInputUnlockTime = 0f;

    private List<D.DialogueLine> activeLines;
    private int activeLineIndex = -1;
    private string activeNpcName = "PNJ";

    [Header("Caméra & Scène")]
    [SerializeField] private Transform newCameraPos;
    [SerializeField] private float newCameraFieldOfView = 40f;
    [SerializeField] private GameObject dummyHolder;

    [Header("Dummies")]
    [SerializeField] private GameObject sulkide;
    [SerializeField] private GameObject darckox;
    [SerializeField] private GameObject mrSlow;
    [SerializeField] private GameObject sulana;
    [SerializeField] private GameObject npc;

    [Header("Bandes noires (cubes)")]
    [SerializeField] private float barDistanceFromCamera = 0.5f;
    [SerializeField] private float barThickness = 0.01f;
    [SerializeField] private float barsCloseDuration = 0.5f;
    [SerializeField] private float barsOpenDuration = 0.5f;
    [SerializeField] private Material barsMaterial;

    [Header("Dialogue Data (ScriptableObjects)")]
    [SerializeField] private CharacterDialogueData sulkideData;
    [SerializeField] private CharacterDialogueData darckoxData;
    [SerializeField] private CharacterDialogueData mrSlowData;
    [SerializeField] private CharacterDialogueData sulanaData;

    [Header("Sélecteur visuel de personnage")]
    [SerializeField] private GameObject selectionIndicatorPrefab;
    [SerializeField] private float indicatorYOffset = 1.0f;
    [SerializeField] private AudioSource npcAudioSource;

    [Header("UI Navigation Tuning")]
    [SerializeField] private float axisDeadZone = 0.5f;
    [SerializeField] private float initialRepeatDelay = 0.30f;
    [SerializeField] private float repeatInterval = 0.12f;

    [Header("Character Switch Tuning")]
    [SerializeField] private float switchCooldown = 0.25f;
    private float nextSwitchAllowedTime = 0f;
    private bool CanSwitchNow() => Time.time >= nextSwitchAllowedTime;
    private void ArmSwitchCooldown() => nextSwitchAllowedTime = Time.time + switchCooldown;

    // repeat state
    private float hNextRepeatTime = 0f, vNextRepeatTime = 0f;
    private int hLastDir = 0, vLastDir = 0;

    // latches
    private readonly Dictionary<string, bool> _buttonLatch = new();

    // internals
    private Camera mainCam;
    private GameObject barRoot;
    private Transform topBar, bottomBar;

    private DummyAnimation animSulkide, animDarckox, animMrSlow, animSulana, animNpc;

    // UI
    private Canvas uiCanvas;
    private RectTransform topPanel, bottomPanel;
    private Button btnCharacter, btnTalk;
    private Text btnCharacterText, btnTalkText;
    private List<Text> optionTexts = new();
    private RectTransform optionsContainer;
    private GameObject responseBox;
    private Text responseNameText, responseText;
    private TypewriterEffect responseTyper;

    private enum UIState { Hidden, TopBar, Options, ShowingResponse }
    private UIState uiState = UIState.Hidden;
    private int topSelectionIndex = 1; // 0=Personnage, 1=Parler (Parler par défaut)
    private int selectedOptionIndex = 0;

    private struct CharacterSlot
    {
        public string name;
        public GameObject obj;
        public CharacterDialogueData data;
        public AudioSource audio;
    }
    private CharacterSlot[] characters;
    private int currentCharacter = 0;
    private GameObject indicatorInstance;

    private PlayerMovement currentPM;

    [Header("UI Font (optionnel)")]
    [SerializeField] private Font uiFontOverride;

    [Header("D-Pad Switching")]
    [SerializeField] private float dpadDeadZone = 0.5f;
    [SerializeField] private bool invertDpadHorizontal = false;
    [SerializeField] private bool invertDpadVertical = false;
    private int dpadLastXDir = 0;
    private int dpadLastYDir = 0;

    private void Awake()
    {
        mainCam = Camera.main;
        if (!mainCam) Debug.LogError("[NPCmanager] Aucune caméra MainCamera trouvée.");
    }

    private void Start()
    {
        if (dummyHolder) dummyHolder.SetActive(false);

        animSulkide = sulkide.transform.GetChild(0).GetComponent<DummyAnimation>();
        animDarckox = darckox.transform.GetChild(0).GetComponent<DummyAnimation>();
        animMrSlow  = mrSlow.transform.GetChild(0).GetComponent<DummyAnimation>();
        animSulana  = sulana.transform.GetChild(0).GetComponent<DummyAnimation>();
        animNpc     = npc.transform.GetChild(0).GetComponent<DummyAnimation>();

        characters = new CharacterSlot[4];
        characters[0] = MakeSlot(sulkide, sulkideData);
        characters[1] = MakeSlot(darckox, darckoxData);
        characters[2] = MakeSlot(mrSlow,  mrSlowData);
        characters[3] = MakeSlot(sulana,  sulanaData);

        if (!npcAudioSource)
        {
            npcAudioSource = gameObject.AddComponent<AudioSource>();
            npcAudioSource.playOnAwake = false;
        }

        characterAnims = new[] { animSulkide, animDarckox, animMrSlow, animSulana };

        if (npcAnim == null) npcAnim = GetComponentInChildren<DummyAnimation>();
    }

    private CharacterSlot MakeSlot(GameObject go, CharacterDialogueData data)
    {
        var slot = new CharacterSlot
        {
            obj = go,
            data = data,
            name = data != null && !string.IsNullOrEmpty(data.characterName) ? data.characterName : (go ? go.name : "Char"),
            audio = go ? go.GetComponent<AudioSource>() : null
        };
        if (slot.audio == null && go != null)
        {
            slot.audio = go.AddComponent<AudioSource>();
            slot.audio.playOnAwake = false;
        }
        return slot;
    }

    private Font ResolveUIFont()
    {
        if (uiFontOverride != null) return uiFontOverride;
        try { return Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); }
        catch
        {
            try { return Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Verdana", "Helvetica", "Liberation Sans" }, 16); }
            catch { return Font.CreateDynamicFontFromOSFont("Arial", 16); }
        }
    }

    private void Update()
    {
        if (uiState == UIState.Hidden || currentPM == null) return;

        switch (uiState)
        {
            case UIState.TopBar:        HandleTopBarInput();      break;
            case UIState.Options:       HandleOptionsInput();     break;
            case UIState.ShowingResponse: HandleResponseInput();  break;
        }

        UpdateIndicatorPosition();
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!eventStart && other.CompareTag("Target"))
        {
            Debug.Log("[NPCmanager] OnTriggerStay2D");
            
            var pm = other.GetComponent<PlayerMovement>();
            if (pm != null && pm.useInputRegistered)
            {
                eventStart = true;
                GameManager.instance.MakePlayerInvisible();
                EventStart(pm);
            }
        }
    }

    public void EventStart(PlayerMovement pm) => StartCoroutine(EventFlow(pm));

    // ----------------- Letterbox helpers (inchangés, compacts) -----------------
    private void EnsureBarsExist()
    {
        if (!Application.isPlaying) return;

        if (mainCam == null) return;
        if (barRoot == null)
        {
            barRoot = new GameObject("LetterboxBars");
            barRoot.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInBuild;
            barRoot.transform.SetParent(mainCam.transform, false);
            barRoot.transform.localPosition = Vector3.zero;
            barRoot.transform.localRotation = Quaternion.identity;
        }

        if (topBar == null) topBar = CreateBar("TopBar");
        if (bottomBar == null) bottomBar = CreateBar("BottomBar");
        barRoot.SetActive(true);
    }
    private Transform CreateBar(string name)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInBuild;
        go.name = name;
        go.transform.SetParent(barRoot.transform, false);
        var col = go.GetComponent<Collider>(); if (col) Destroy(col);
        var mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            if (!barsMaterial)
            {
                var mat = new Material(Shader.Find("Unlit/Color"));
                mat.color = Color.black;
                mr.sharedMaterial = mat;
            }
            else mr.sharedMaterial = barsMaterial;
        }
        return go.transform;
    }
    private void ComputeFrustum(float dist, out float width, out float height)
    {
        if (mainCam.orthographic) { height = 2f * mainCam.orthographicSize; width = height * mainCam.aspect; }
        else
        {
            float h = 2f * dist * Mathf.Tan(mainCam.fieldOfView * 0.5f * Mathf.Deg2Rad);
            height = h; width = h * mainCam.aspect;
        }
    }
    private void SetBarsOutsideImmediate()
    {
        if (mainCam == null) return;
        ComputeFrustum(barDistanceFromCamera, out float w, out float h);
        float halfH = h * 0.5f;
        Vector3 barScale = new(w, halfH, barThickness);
        float outsideOffset = (h * 0.5f) + (halfH * 0.5f);
        Vector3 topPos = new(0f, +outsideOffset, barDistanceFromCamera);
        Vector3 botPos = new(0f, -outsideOffset, barDistanceFromCamera);
        topBar.DOKill(); bottomBar.DOKill();
        topBar.localScale = barScale; bottomBar.localScale = barScale;
        topBar.localPosition = topPos; bottomBar.localPosition = botPos;
    }
    private void SetBarsClosedImmediate()
    {
        if (mainCam == null) return;
        ComputeFrustum(barDistanceFromCamera, out float w, out float h);
        float halfH = h * 0.5f;
        Vector3 barScale = new(w, halfH, barThickness);
        Vector3 topPos = new(0f, +halfH * 0.5f, barDistanceFromCamera);
        Vector3 botPos = new(0f, -halfH * 0.5f, barDistanceFromCamera);
        topBar.DOKill(); bottomBar.DOKill();
        topBar.localScale = barScale; bottomBar.localScale = barScale;
        topBar.localPosition = topPos; bottomBar.localPosition = botPos;
    }
    private IEnumerator AnimateBarsClose(float duration)
    {
        if (mainCam == null) yield break;
        ComputeFrustum(barDistanceFromCamera, out float w, out float h);
        float halfH = h * 0.5f;
        Vector3 targetScale = new(w, halfH, barThickness);
        Vector3 topTarget = new(0f, +halfH * 0.5f, barDistanceFromCamera);
        Vector3 botTarget = new(0f, -halfH * 0.5f, barDistanceFromCamera);
        topBar.DOKill(); bottomBar.DOKill();
        var seq = DOTween.Sequence();
        seq.Join(topBar.DOScale(targetScale, duration).SetEase(Ease.InOutSine));
        seq.Join(bottomBar.DOScale(targetScale, duration).SetEase(Ease.InOutSine));
        seq.Join(topBar.DOLocalMove(topTarget, duration).SetEase(Ease.InOutSine));
        seq.Join(bottomBar.DOLocalMove(botTarget, duration).SetEase(Ease.InOutSine));
        yield return seq.WaitForCompletion();
    }
    private void AnimateBarsToLetterbox(float ratio, float duration)
    {
        if (mainCam == null) return;
        ComputeFrustum(barDistanceFromCamera, out float w, out float h);
        float targetH = Mathf.Clamp01(ratio) * h;
        Vector3 targetScale = new(w, targetH, barThickness);
        float edgeOffset = (h * 0.5f) - (targetH * 0.5f);
        Vector3 topPos = new(0f, +edgeOffset, barDistanceFromCamera);
        Vector3 botPos = new(0f, -edgeOffset, barDistanceFromCamera);
        topBar.DOKill(); bottomBar.DOKill();
        topBar.DOScale(targetScale, duration).SetEase(Ease.InOutSine);
        bottomBar.DOScale(targetScale, duration).SetEase(Ease.InOutSine);
        topBar.DOLocalMove(topPos, duration).SetEase(Ease.InOutSine);
        bottomBar.DOLocalMove(botPos, duration).SetEase(Ease.InOutSine);
    }
    // ---------------------------------------------------------------------------

    private IEnumerator EventFlow(PlayerMovement pm)
    {
        currentPM = pm;

        EnsureBarsExist();
        SetBarsOutsideImmediate();
        yield return AnimateBarsClose(barsCloseDuration);

        GameManager.instance?.MakePlayerInvinsible();

        if (newCameraPos && mainCam)
        {
            mainCam.transform.SetPositionAndRotation(newCameraPos.position, newCameraPos.rotation);
            if (!mainCam.orthographic) mainCam.fieldOfView = newCameraFieldOfView;
            SetBarsClosedImmediate();
        }

        if (dummyHolder) dummyHolder.SetActive(true);
        animSulkide?.Idle(); animSulana?.Idle(); animMrSlow?.Idle(); animDarckox?.Idle(); animNpc?.Idle();

        yield return new WaitForSeconds(1f);

        AnimateBarsToLetterbox(1f / 6f, barsOpenDuration);

        EnsureUIExists();
        SetupTopBottomHeights(1f / 6f);
        ShowTopBarUI(1); // "Parler" par défaut
        InitIndicator();
        UpdateCharacterButtonText();
    }

    // ------------------------------ UI runtime ------------------------------
    private void EnsureUIExists()
    {
        if (!Application.isPlaying) return;
        
        if (uiCanvas != null) return;

        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInBuild;

            
        }

        var canvasGO = MarkDontSave(new GameObject("DialogueCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster)));

        uiCanvas = canvasGO.GetComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.layer = LayerMask.NameToLayer("UI");

        
        canvasGO.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInBuild;

        
        topPanel = CreatePanel("TopBarUI", uiCanvas.transform as RectTransform, new Color(0, 0, 0, 1f));
        AnchorTop(topPanel);

        var (btn1, txt1) = CreateButton("BtnCharacter", topPanel, new Vector2(200, 48), new Vector2(12, -12));
        btnCharacter = btn1; btnCharacterText = txt1; btnCharacterText.text = "Sulkide";
        btnCharacter.onClick.AddListener(SwitchCharacterNext);

        var (btn2, txt2) = CreateButton("BtnTalk", topPanel, new Vector2(200, 48), new Vector2(224, -12));
        btnTalk = btn2; btnTalkText = txt2; btnTalkText.text = "Parler";
        btnTalk.onClick.AddListener(OpenOptions);

        bottomPanel = CreatePanel("BottomBarUI", uiCanvas.transform as RectTransform, new Color(0, 0, 0, 1f));
        AnchorBottom(bottomPanel);

        var optionsGO = new GameObject("OptionsContainer", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        optionsGO.transform.SetParent(bottomPanel, false);
        optionsContainer = optionsGO.GetComponent<RectTransform>();
        optionsContainer.anchorMin = new(0, 0);
        optionsContainer.anchorMax = new(1, 1);
        optionsContainer.offsetMin = new(20, 20);
        optionsContainer.offsetMax = new(-20, -20);
        var vlg = optionsGO.GetComponent<VerticalLayoutGroup>();
        vlg.spacing = 8f;
        vlg.childControlHeight = true; vlg.childControlWidth = true;
        vlg.childForceExpandHeight = false; vlg.childForceExpandWidth = false;
        var csf = optionsGO.GetComponent<ContentSizeFitter>();
        csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        csf.verticalFit   = ContentSizeFitter.FitMode.Unconstrained;

        responseBox = CreatePanel("ResponseBox", bottomPanel, new Color(0, 0, 0, 0)).gameObject;
        var rb = responseBox.GetComponent<RectTransform>();
        rb.anchorMin = new(0, 0); rb.anchorMax = new(1, 1);
        rb.offsetMin = new(20, 20); rb.offsetMax = new(-20, -20);

        var nameGO = new GameObject("SpeakerName", typeof(RectTransform), typeof(Text));
        nameGO.transform.SetParent(responseBox.transform, false);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new(0, 1); nameRT.anchorMax = new(1, 1);
        nameRT.pivot = new(0, 1);
        nameRT.offsetMin = new(0, -36); nameRT.offsetMax = new(0, 0);
        responseNameText = nameGO.GetComponent<Text>();
        responseNameText.font = ResolveUIFont();
        responseNameText.fontSize = 22;
        responseNameText.color = Color.white;
        responseNameText.alignment = TextAnchor.UpperLeft;
        responseNameText.text = "PNJ";

        var textGO = new GameObject("ResponseText", typeof(RectTransform), typeof(Text), typeof(TypewriterEffect));
        textGO.transform.SetParent(responseBox.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new(0, 0); textRT.anchorMax = new(1, 1);
        textRT.offsetMin = Vector2.zero; textRT.offsetMax = new Vector2(0, -40);
        responseText = textGO.GetComponent<Text>();
        responseText.font = ResolveUIFont();
        responseText.fontSize = 24;
        responseText.color = Color.white;
        responseText.alignment = TextAnchor.UpperLeft;
        responseTyper = textGO.GetComponent<TypewriterEffect>();
        responseBox.SetActive(false);

        HideBottom();

        var navNone = new Navigation { mode = Navigation.Mode.None };
        btnCharacter.navigation = navNone;
        btnTalk.navigation = navNone;
    }

    private T MarkDontSave<T>(T go) where T : UnityEngine.Object
    {
        if (go is GameObject g) g.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInBuild;
        else if (go is Component c) c.gameObject.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInBuild;
        return go;
    }
    
    private RectTransform CreatePanel(string name, RectTransform parent, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        var img = go.GetComponent<Image>();
        img.color = color;
        return rt;
    }

    private (Button, Text) CreateButton(string name, RectTransform parent, Vector2 size, Vector2 topLeftOffset)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = go.GetComponent<RectTransform>();
        rt.pivot = new(0, 1); rt.anchorMin = new(0, 1); rt.anchorMax = new(0, 1);
        rt.sizeDelta = size; rt.anchoredPosition = topLeftOffset;

        var img = go.GetComponent<Image>();
        img.color = new(0.1f, 0.1f, 0.1f, 0.9f);

        var btn = go.GetComponent<Button>();

        var txtGO = new GameObject("Text", typeof(RectTransform), typeof(Text));
        txtGO.transform.SetParent(go.transform, false);
        var trt = txtGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
        trt.offsetMin = new(10, 6); trt.offsetMax = new(-10, -6);
        var txt = txtGO.GetComponent<Text>();
        txt.font = ResolveUIFont();
        txt.fontSize = 20; txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleCenter;
        txt.text = name;

        return (btn, txt);
    }

    private void AnchorTop(RectTransform rt)
    {
        rt.anchorMin = new(0, 1);
        rt.anchorMax = new(1, 1);
        rt.pivot = new(0.5f, 1f);
        rt.offsetMin = new(0, -100);
        rt.offsetMax = new(0, 0);
        rt.anchoredPosition = Vector2.zero;
    }

    private void AnchorBottom(RectTransform rt)
    {
        rt.anchorMin = new(0, 0);
        rt.anchorMax = new(1, 0);
        rt.pivot = new(0.5f, 0f);
        rt.offsetMin = new(0, 0);
        rt.offsetMax = new(0, 100);
        rt.anchoredPosition = Vector2.zero;
    }

    private void SetupTopBottomHeights(float ratio)
    {
        float h = Mathf.Round(Screen.height * ratio);
        var topOffMax = topPanel.offsetMax; topOffMax.y = 0;
        var topOffMin = topPanel.offsetMin; topOffMin.y = -h;
        topPanel.offsetMin = topOffMin; topPanel.offsetMax = topOffMax;

        var botOffMin = bottomPanel.offsetMin; botOffMin.y = 0;
        var botOffMax = bottomPanel.offsetMax; botOffMax.y = h;
        bottomPanel.offsetMin = botOffMin; bottomPanel.offsetMax = botOffMax;
    }

    private void ShowTopBarUI(int initialIndex = -1)
    {
        uiCanvas.enabled = true;
        topPanel.gameObject.SetActive(true);
        HideBottom();
        uiState = UIState.TopBar;

        // Par défaut : "Parler"
        topSelectionIndex = (initialIndex >= 0) ? Mathf.Clamp(initialIndex, 0, 1) : 1;

        SetTopButtonsInteractable(true);
        HighlightTopButton();
        ResetInputLatchAll();
        ResetAxisRepeatState();

        // Force la sélection du bouton "Parler"
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(btnTalk ? btnTalk.gameObject : null);
    }

    private void HideBottom()
    {
        bottomPanel.gameObject.SetActive(false);
        optionsContainer.gameObject.SetActive(false);
        responseBox.SetActive(false);
    }

    private void ShowOptions()
    {
        bottomPanel.gameObject.SetActive(true);
        optionsContainer.gameObject.SetActive(true);
        responseBox.SetActive(false);

        SetTopButtonsInteractable(false);
        ResetInputLatchAll();
        ResetAxisRepeatState();
        optionsInputUnlockTime = Time.time + optionsOpenCooldown;
        ArmSwitchCooldown();
        uiState = UIState.Options;
    }

    private void ShowResponse()
    {
        bottomPanel.gameObject.SetActive(true);
        optionsContainer.gameObject.SetActive(false);
        responseBox.SetActive(true);

        SetTopButtonsInteractable(false);
        ResetInputLatchAll();
        ResetAxisRepeatState();
        uiState = UIState.ShowingResponse;
    }

    private void ResetInputLatchAll()
    {
        foreach (var name in UseActionNames) _buttonLatch[name] = false;
    }

    private void ResetAxisRepeatState()
    {
        hLastDir = 0; vLastDir = 0;
        hNextRepeatTime = 0f; vNextRepeatTime = 0f;
        dpadLastXDir = 0; dpadLastYDir = 0;
    }

    // -------------------------- Indicateur & perso courant --------------------------
    private void InitIndicator()
    {
        if (!selectionIndicatorPrefab || indicatorInstance != null) return;
        indicatorInstance = Instantiate(selectionIndicatorPrefab);
        indicatorInstance.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInBuild;
        indicatorInstance.name = "SelectedCharacterIndicator";
        UpdateIndicatorPosition();
        indicatorInstance.SetActive(true);
    }

    private void UpdateIndicatorPosition()
    {
        if (indicatorInstance == null) return;
        var t = characters[currentCharacter].obj ? characters[currentCharacter].obj.transform : null;
        if (t == null) return;

        float y = indicatorYOffset;
        var sr = t.GetComponentInChildren<SpriteRenderer>();
        if (sr != null) y = sr.bounds.size.y * 0.5f + indicatorYOffset;
        else
        {
            var r = t.GetComponentInChildren<Renderer>();
            if (r != null) y = r.bounds.size.y * 0.5f + indicatorYOffset;
        }

        indicatorInstance.transform.position = t.position + Vector3.up * y;
        indicatorInstance.transform.rotation = Quaternion.identity;
    }

    private void SwitchCharacterNext()
    {
        currentCharacter = (currentCharacter + 1) % characters.Length;
        OnCharacterChanged();
    }

    private void SwitchCharacterPrev()
    {
        currentCharacter = (currentCharacter - 1 + characters.Length) % characters.Length;
        OnCharacterChanged();
    }

    private void UpdateCharacterButtonText()
    {
        if (btnCharacterText) btnCharacterText.text = characters[currentCharacter].name;
    }

    private void OnCharacterChanged()
    {
        UpdateCharacterButtonText();
        UpdateIndicatorPosition();

        if (uiState == UIState.Options)
        {
            BuildOptionsForCurrentCharacter();
            selectedOptionIndex = Mathf.Clamp(selectedOptionIndex, 0, Mathf.Max(0, optionTexts.Count - 1));
            HighlightOption();
            optionsInputUnlockTime = Time.time + optionsOpenCooldown;
            ResetAxisRepeatState();
            foreach (var name in UseActionNames) _buttonLatch[name] = IsPressed(name);
        }
        if (uiState == UIState.ShowingResponse) ResetConversationAnimations();

        HighlightTopButton();
    }

    private void ResetConversationAnimations()
    {
        if (npcAnim) npcAnim.Idle();
        if (characterAnims != null)
            foreach (var a in characterAnims) if (a) a.Idle();
    }

    private Color GetCurrentHighlightColor()
    {
        return currentCharacter switch
        {
            0 => sulkideColor,
            1 => darckoxColor,
            2 => mrSlowColor,
            3 => sulanaColor,
            _ => Color.yellow,
        };
    }

    // ------------------------------ Inputs helpers ------------------------------
    private InputAction GetAction(string actionName)
    {
        if (string.IsNullOrEmpty(actionName)) return null;
        if (currentPM == null || currentPM.playerControls == null) return null;
        var asset = currentPM.playerControls.actions;
        return asset?.FindAction(actionName, throwIfNotFound: false);
    }
    private bool IsPressed(string actionName)
    {
        var act = GetAction(actionName);
        return act != null && act.IsPressed();
    }
    private bool PressedOnce(params string[] actionNames)
    {
        bool result = false;
        foreach (var name in actionNames)
        {
            if (string.IsNullOrEmpty(name)) continue;
            bool pressedNow = IsPressed(name);
            bool last = _buttonLatch.TryGetValue(name, out var l) ? l : false;
            bool down = pressedNow && !last;
            _buttonLatch[name] = pressedNow;
            result |= down;
        }
        return result;
    }
    private bool PressedOnceUse() => PressedOnce(UseActionNames);

    private int AxisEdge(ref int lastDir, float axis, float deadZone)
    {
        int dir = (axis > deadZone) ? +1 : (axis < -deadZone) ? -1 : 0;
        if (dir == 0) { if (lastDir != 0) lastDir = 0; return 0; }
        if (lastDir == 0) { lastDir = dir; return dir; }
        return 0;
    }

    private Vector2 ReadDpad()
    {
        var act = GetAction("Dpad");
        return act != null ? act.ReadValue<Vector2>() : Vector2.zero;
    }
    private int DpadEdgeX()
    {
        float x = ReadDpad().x;
        if (invertDpadHorizontal) x = -x;
        int dir = (x > dpadDeadZone) ? +1 : (x < -dpadDeadZone) ? -1 : 0;
        if (dir == 0) { if (dpadLastXDir != 0) dpadLastXDir = 0; return 0; }
        if (dpadLastXDir == 0) { dpadLastXDir = dir; return dir; }
        return 0;
    }
    private int DpadEdgeY()
    {
        float y = ReadDpad().y;
        if (invertDpadVertical) y = -y;
        int dir = (y > dpadDeadZone) ? +1 : (y < -dpadDeadZone) ? -1 : 0;
        if (dir == 0) { if (dpadLastYDir != 0) dpadLastYDir = 0; return 0; }
        if (dpadLastYDir == 0) { dpadLastYDir = dir; return dir; }
        return 0;
    }

    // ------------------------------ TopBar ------------------------------
    private void HandleTopBarInput()
    {
        float x = currentPM.moveInput.x;
        float y = currentPM.moveInput.y;

        // X : Personnage (0) <-> Parler (1)
        int hEdge = AxisEdge(ref hLastDir, x, axisDeadZone);
        if (hEdge > 0) { topSelectionIndex = 1; HighlightTopButton(); }
        else if (hEdge < 0) { topSelectionIndex = 0; HighlightTopButton(); }

        // Y : Haut = changer de perso, Bas = confirmer
        int vEdge = AxisEdge(ref vLastDir, y, axisDeadZone);
        if (vEdge > 0 && CanSwitchNow())
        {
            SwitchCharacterNext();
            ArmSwitchCooldown();
        }
        else if (vEdge < 0)
        {
            if (topSelectionIndex == 0) SwitchCharacterNext();
            else OpenOptions();
            return;
        }

        // D-pad gauche/droite = switch persos
        int dpadX = DpadEdgeX();
        if (dpadX != 0 && CanSwitchNow())
        {
            if (dpadX > 0) SwitchCharacterNext(); else SwitchCharacterPrev();
            ArmSwitchCooldown();
            return;
        }

        // Gâchettes
        if (PressedOnce("SelectR") && CanSwitchNow()) { SwitchCharacterNext(); ArmSwitchCooldown(); return; }
        if (PressedOnce("SelectL", "Selectl") && CanSwitchNow()) { SwitchCharacterPrev(); ArmSwitchCooldown(); return; }

        // Use
        if (PressedOnceUse())
        {
            if (topSelectionIndex == 0) SwitchCharacterNext();
            else OpenOptions();
        }
    }

    private void HighlightTopButton()
    {
        if (!btnCharacter || !btnTalk) return;
        var selectedBg = GetCurrentHighlightColor();
        var deselectedBg = topBarUnselectedBg;

        var imgChar = btnCharacter.GetComponent<Image>();
        var imgTalk = btnTalk.GetComponent<Image>();

        if (topSelectionIndex == 0)
        {
            if (imgChar) imgChar.color = selectedBg;
            if (imgTalk) imgTalk.color = deselectedBg;
            if (btnCharacterText) btnCharacterText.color = topBarSelectedText;
            if (btnTalkText)      btnTalkText.color      = topBarUnselectedText;
        }
        else
        {
            if (imgChar) imgChar.color = deselectedBg;
            if (imgTalk) imgTalk.color = selectedBg;
            if (btnCharacterText) btnCharacterText.color = topBarUnselectedText;
            if (btnTalkText)      btnTalkText.color      = topBarSelectedText;
        }
    }

    private void OpenOptions()
    {
        BuildOptionsForCurrentCharacter();
        ShowOptions();
        selectedOptionIndex = 0;
        HighlightOption();
    }

    // ------------------------------ Options ------------------------------
    private void HandleOptionsInput()
    {
        // Gâchettes
        if (PressedOnce("SelectR") && CanSwitchNow()) { SwitchCharacterNext(); ArmSwitchCooldown(); return; }
        if (PressedOnce("SelectL", "Selectl") && CanSwitchNow()) { SwitchCharacterPrev(); ArmSwitchCooldown(); return; }

        // D-pad X (droite => next / gauche => prev)
        int dpadX = DpadEdgeX();
        if (dpadX != 0 && CanSwitchNow())
        {
            if (dpadX > 0) SwitchCharacterNext(); else SwitchCharacterPrev();
            ArmSwitchCooldown();
            return;
        }

        // Verrou ouverture
        if (Time.time < optionsInputUnlockTime)
        {
            float yy = currentPM.moveInput.y;
            float xx = currentPM.moveInput.x;
            vLastDir = (yy > axisDeadZone) ? +1 : (yy < -axisDeadZone) ? -1 : 0;
            hLastDir = (xx > axisDeadZone) ? +1 : (xx < -axisDeadZone) ? -1 : 0;
            foreach (var name in UseActionNames) _buttonLatch[name] = IsPressed(name);
            return;
        }

        // Stick X : droite => next / gauche => prev
        int hEdge = AxisEdge(ref hLastDir, currentPM.moveInput.x, axisDeadZone);
        if (hEdge != 0 && CanSwitchNow())
        {
            if (hEdge > 0) SwitchCharacterNext(); else SwitchCharacterPrev();
            ArmSwitchCooldown();
        }

        // Y : naviguer liste
        int dpadY = DpadEdgeY();
        if (dpadY != 0) { NavigateOptions(-dpadY); }
        else
        {
            int vEdge = AxisEdge(ref vLastDir, currentPM.moveInput.y, axisDeadZone);
            if (vEdge != 0) NavigateOptions(-vEdge);
        }

        // Valider
        if (PressedOnceUse()) SelectCurrentOption();
    }

    private void NavigateOptions(int delta)
    {
        if (_visibleOptionIndices.Count == 0) { ShowTopBarUI(0); return; }
        if (delta < 0 && selectedOptionIndex == 0) { ShowTopBarUI(0); return; }
        MoveOption(delta);
    }

    private void BuildOptionsForCurrentCharacter()
    {
        foreach (var t in optionTexts) if (t) Destroy(t.gameObject);
        optionTexts.Clear();
        _visibleOptionIndices.Clear();

        var data = characters[currentCharacter].data;
        if (data == null || data.dialogueOptions == null || data.dialogueOptions.Count == 0)
        {
            optionTexts.Add(CreateOptionText("(Aucune option)"));
            return;
        }

        for (int i = 0; i < data.dialogueOptions.Count; i++)
        {
            if (!IsOptionVisible(data, i)) continue;
            var opt = data.dialogueOptions[i];
            string label = opt.optionLabel;

            if (string.IsNullOrWhiteSpace(label))
            {
                D.DialogueLine firstPlayer = opt.lines.Find(
                    l => l.speaker == Speaker.Player && !string.IsNullOrWhiteSpace(l.text)
                );
                if (firstPlayer != null) label = firstPlayer.text;
                else if (opt.lines.Count > 0) label = string.IsNullOrWhiteSpace(opt.lines[0].text) ? "(...)" : opt.lines[0].text;
                else label = "(...)";
            }

            _visibleOptionIndices.Add(i);
            optionTexts.Add(CreateOptionText(label));
        }

        if (_visibleOptionIndices.Count == 0)
        {
            optionTexts.Add(CreateOptionText("(Aucune option)"));
        }
    }

    private bool IsOptionVisible(CharacterDialogueData data, int optIndex)
    {
        if (data == null) return false;
        if (optIndex < 0 || optIndex >= data.dialogueOptions.Count) return false;

        var opt = data.dialogueOptions[optIndex];
        _usedOptions.TryGetValue(data, out var playedSet);
        bool usedThis = playedSet != null && playedSet.Contains(optIndex);

        if (opt.hideAfterUse && usedThis) return false;
        if (!opt.hiddenInitially) return true;

        bool revealed =
            opt.revealedByOptionIndex >= 0 &&
            playedSet != null &&
            playedSet.Contains(opt.revealedByOptionIndex);

        return revealed;
    }

    private Text CreateOptionText(string content)
    {
        var go = new GameObject("Option", typeof(RectTransform), typeof(Text));
        go.transform.SetParent(optionsContainer, false);
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new(0, 1); rt.anchorMax = new(1, 1);
        rt.pivot = new(0, 1); rt.sizeDelta = new(0, 36);

        var txt = go.GetComponent<Text>();
        txt.font = ResolveUIFont();
        txt.fontSize = 24; txt.color = Color.white;
        txt.alignment = TextAnchor.MiddleLeft;
        txt.text = content;
        return txt;
    }

    private void MoveOption(int delta)
    {
        selectedOptionIndex = Mathf.Clamp(selectedOptionIndex + delta, 0, Mathf.Max(0, optionTexts.Count - 1));
        HighlightOption();
    }

    private void HighlightOption()
    {
        for (int i = 0; i < optionTexts.Count; i++)
        {
            if (!optionTexts[i]) continue;
            optionTexts[i].color = (i == selectedOptionIndex) ? GetCurrentHighlightColor() : Color.white;
        }
    }

    private void SelectCurrentOption()
    {
        var data = characters[currentCharacter].data;
        if (data == null || data.dialogueOptions == null || data.dialogueOptions.Count == 0 || _visibleOptionIndices.Count == 0)
        {
            ShowTopBarUI(1);
            return;
        }

        selectedOptionIndex = Mathf.Clamp(selectedOptionIndex, 0, _visibleOptionIndices.Count - 1);
        int sourceIndex = _visibleOptionIndices[selectedOptionIndex];
        var opt = data.dialogueOptions[sourceIndex];

        _currentOptionSourceIndex = sourceIndex;

        string npcName = !string.IsNullOrEmpty(data.npcDisplayName) ? data.npcDisplayName : defaultNpcName;
        StartConversation(opt.lines, npcName);
    }

    private void StartConversation(List<D.DialogueLine> lines, string npcName)
    {
        if (lines == null || lines.Count == 0) { ShowTopBarUI(1); return; }

        activeLines = lines;
        activeLineIndex = 0;
        activeNpcName = string.IsNullOrEmpty(npcName) ? defaultNpcName : npcName;

        ShowResponse();
        ShowCurrentLine();
    }

    private DummyAnimation GetDummyForSpeaker(Speaker speaker)
    {
        switch (speaker)
        {
            case Speaker.Player:
                return (currentCharacter >= 0 && currentCharacter < characterAnims.Length) ? characterAnims[currentCharacter] : null;
            case Speaker.NPC:     return npcAnim;
            case Speaker.Sulkide: return animSulkide;
            case Speaker.Darckox: return animDarckox;
            case Speaker.MrSlow:  return animMrSlow;
            case Speaker.Sulana:  return animSulana;
            default: return null;
        }
    }

    private void PlayDummyAnimation(D.AnimationKind kind, Speaker speaker)
    {
        if (kind == D.AnimationKind.None) return;

        var dummy = GetDummyForSpeaker(speaker);
        if (dummy == null) return;

        // Reset d’abord
        dummy.Idle();

        switch (kind)
        {
            case D.AnimationKind.Idle:             dummy.Idle(); break;
            case D.AnimationKind.TalkingNormal:    dummy.TalkingNormal(); break;
            case D.AnimationKind.TalkingHappy:     dummy.TalkingHappy(); break;
            case D.AnimationKind.TalkingSad:       dummy.TalkingSad(); break;
            case D.AnimationKind.TalkingAngry:     dummy.TalkingAngry(); break;
            case D.AnimationKind.TalkingStress:    dummy.TalkingStress(); break;
            case D.AnimationKind.Shocked:          dummy.Shocked(); break;
            case D.AnimationKind.Giving:           dummy.Giving(); break;
        }
    }


    private void StopAllDialogueAudio()
    {
        if (npcAudioSource) npcAudioSource.Stop();
        if (characters != null)
            for (int i = 0; i < characters.Length; i++)
                if (characters[i].audio) characters[i].audio.Stop();
    }

    private void ShowCurrentLine()
    {
        if (activeLines == null || activeLineIndex < 0 || activeLineIndex >= activeLines.Count) return;

        var line = activeLines[activeLineIndex];

        // Nom dans la box
        switch (line.speaker)
        {
            case Speaker.Player:
                responseNameText.text = characters[currentCharacter].name;
                responseNameText.color = GetCurrentHighlightColor();
                break;
            case Speaker.NPC:
                responseNameText.text = activeNpcName; responseNameText.color = Color.gray; break;
            case Speaker.Sulkide:
                responseNameText.text = sulkideData ? sulkideData.characterName : "Sulkide"; responseNameText.color = sulkideColor; break;
            case Speaker.Darckox:
                responseNameText.text = darckoxData ? darckoxData.characterName : "Darckox"; responseNameText.color = darckoxColor; break;
            case Speaker.MrSlow:
                responseNameText.text = mrSlowData ? mrSlowData.characterName : "MrSlow"; responseNameText.color = mrSlowColor; break;
            case Speaker.Sulana:
                responseNameText.text = sulanaData ? sulanaData.characterName : "Sulana"; responseNameText.color = sulanaColor; break;
        }

        // Audio
        if (line.audio != null)
        {
            if (line.speaker == Speaker.Player)
            {
                var a = characters[currentCharacter].audio;
                if (a) a.PlayOneShot(line.audio);
            }
            else if (line.speaker == Speaker.NPC)
            {
                if (npcAudioSource) npcAudioSource.PlayOneShot(line.audio);
            }
            else
            {
                var anim = GetDummyForSpeaker(line.speaker);
                var go = anim ? anim.gameObject : null;
                var a = go ? go.GetComponent<AudioSource>() : null;
                if (a) a.PlayOneShot(line.audio);
            }
        }

        // Anim
        PlayDummyAnimation(line.animation, line.speaker);

        // Typewriter
        responseTyper.SetSpeed(45f);
        responseTyper.StartTyping(line.text ?? "");
    }

    private void HandleResponseInput()
    {
        // Switch perso pendant la réponse
        int dpadStep = DpadEdgeX();
        if (dpadStep != 0 && CanSwitchNow())
        {
            if (dpadStep > 0) SwitchCharacterNext(); else SwitchCharacterPrev();
            ArmSwitchCooldown();
            return;
        }
        if (PressedOnce("SelectR") && CanSwitchNow()) { SwitchCharacterPrev(); ArmSwitchCooldown(); return; }
        if (PressedOnce("SelectL", "Selectl") && CanSwitchNow()) { SwitchCharacterNext(); ArmSwitchCooldown(); return; }

        if (PressedOnceUse())
        {
            if (activeLines == null || activeLines.Count == 0) { ShowTopBarUI(1); return; }

            var line = activeLines[Mathf.Clamp(activeLineIndex, 0, activeLines.Count - 1)];

            if (responseTyper.IsTyping())
            {
                responseTyper.StopAndShowAll(line.text ?? "");
                return;
            }

            // Avancer
            activeLineIndex++;
            if (activeLineIndex >= activeLines.Count)
            {
                // Marquer l’option jouée
                var data = characters[currentCharacter].data;
                if (data != null && _currentOptionSourceIndex >= 0)
                    MarkOptionUsed(data, _currentOptionSourceIndex);

                // Tout remettre Idle (important si un autre perso que le sélectionné parlait)
                ResetConversationAnimations();
                ShowTopBarUI(1);
                return;
            }

            // Avant d’afficher la prochaine ligne, remettre tous les dummies en Idle
            // pour éviter qu’un locuteur précédent reste en anim.
            ResetConversationAnimations();
            ShowCurrentLine();
        }
    }

    private void MarkOptionUsed(CharacterDialogueData data, int optIndex)
    {
        if (data == null || optIndex < 0) return;
        if (!_usedOptions.TryGetValue(data, out var set))
        {
            set = new HashSet<int>();
            _usedOptions[data] = set;
        }
        set.Add(optIndex);
    }

    private void SetTopButtonsInteractable(bool interactable)
    {
        if (btnCharacter) btnCharacter.interactable = interactable;
        if (btnTalk)      btnTalk.interactable      = interactable;
    }
    
    private void OnDisable()
    {
        CleanupRuntimeObjects();
    }

    private void OnDestroy()
    {
        CleanupRuntimeObjects();
    }

    private void CleanupRuntimeObjects()
    {
        // Bars
        if (barRoot != null)
        {
            if (Application.isEditor) DestroyImmediate(barRoot);
            else Destroy(barRoot);
            barRoot = null;
            topBar = null;
            bottomBar = null;
        }

        // UI
        if (uiCanvas != null)
        {
            if (Application.isEditor) DestroyImmediate(uiCanvas.gameObject);
            else Destroy(uiCanvas.gameObject);
            uiCanvas = null;
            topPanel = bottomPanel = null;
            optionsContainer = null;
            responseBox = null;
            btnCharacter = btnTalk = null;
            btnCharacterText = btnTalkText = null;
            optionTexts.Clear();
        }

        // Indicator
        if (indicatorInstance != null)
        {
            if (Application.isEditor) DestroyImmediate(indicatorInstance);
            else Destroy(indicatorInstance);
            indicatorInstance = null;
        }
    }

}
