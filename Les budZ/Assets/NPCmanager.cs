using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Sulkide.Dialogue;
using D = Sulkide.Dialogue;
using UIGridLayoutGroup = UnityEngine.UI.GridLayoutGroup;
using UIVerticalLayoutGroup = UnityEngine.UI.VerticalLayoutGroup;

public class NPCmanager : MonoBehaviour
{
    private bool eventStart = false;

    [Header("Dialogue Runtime")]
    [SerializeField] private string defaultNpcName = "PNJ";
    private static readonly string[] UseActionNames = new[] { "Use" };

    // Options jouées par asset (Talk/Describe)
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

    [Header("Dialogue Data (Talk)")]
    [SerializeField] private CharacterDialogueData sulkideData;
    [SerializeField] private CharacterDialogueData darckoxData;
    [SerializeField] private CharacterDialogueData mrSlowData;
    [SerializeField] private CharacterDialogueData sulanaData;

    [Header("Dialogue Data (Describe)")]
    [SerializeField] private CharacterDialogueData sulkideDescribeData;
    [SerializeField] private CharacterDialogueData darckoxDescribeData;
    [SerializeField] private CharacterDialogueData mrSlowDescribeData;
    [SerializeField] private CharacterDialogueData sulanaDescribeData;

    [Header("Sélecteur visuel de personnage")]
    [SerializeField] private GameObject selectionIndicatorPrefab;
    [SerializeField] private float indicatorYOffset = 1.0f;
    [SerializeField] private AudioSource npcAudioSource;

    [Header("UI Navigation Tuning")]
    [SerializeField] private float axisDeadZone = 0.5f;
    [SerializeField] private float initialRepeatDelay = 0.30f;
    [SerializeField] private float repeatInterval = 0.12f;

    private Button btnDescribe;  private Text btnDescribeText;
    private Button btnUse;       private Text btnUseText;

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
    private KeyObjData pendingConsumedItem = null;
// Layouts pour optionsContainer
// Layouts pour optionsContainer
    private UIVerticalLayoutGroup optionsVLG;
    private UIGridLayoutGroup     optionsGrid;
    
    // Deux racines séparées, une pour liste et une pour grille
    private RectTransform optionsListRoot, optionsGridRoot;
    private UIVerticalLayoutGroup listVLG;
    private UIGridLayoutGroup     gridGLG;
    private bool usingGrid = false;
// --- Quitter ---
    [Header("Quit Dialogues par personnage")]
    public List<D.DialogueLine> quitWithSulkide;
    public List<D.DialogueLine> quitWithDarckox;
    public List<D.DialogueLine> quitWithMrSlow;
    public List<D.DialogueLine> quitWithSulana;

    private Button btnQuit; private Text btnQuitText;
    private bool quittingAfterConversation = false;

// Caméra sauvegarde
    private Vector3 savedCamPos;
    private Quaternion savedCamRot;
    private bool savedCamWasOrtho = false;
    private float savedCamFOV = 60f;
    private float savedOrthoSize = 5f;
    private bool preCamSaved = false;



    

    private enum UIState { Hidden, TopBar, Options, ShowingResponse }
    private UIState uiState = UIState.Hidden;
    private int topSelectionIndex = 1; // 0=Perso, 1=Parler, 2=Décrire, 3=Utiliser
    private int selectedOptionIndex = 0;

    // perso courants
    private struct CharacterSlot
    {
        public string name;
        public GameObject obj;
        public CharacterDialogueData talkData;
        public CharacterDialogueData describeData;
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

    // ====== INVENTAIRE / UTILISER ======
    // UI "Utiliser"
    private readonly List<Image> useItemIcons = new();
    private readonly List<KeyObjData> useItems = new();
    private int selectedUseIndex = 0;

    [System.Serializable] public class UseDialogueEntry
    {
        public KeyObjData item;
        public List<D.DialogueLine> lines;
        public bool consumeItem = false;   // <-- NOUVEAU
    
    }

    [Header("Use Dialogues par personnage")]
    public UseDialogueEntry[] useWithSulkide;
    public UseDialogueEntry[] useWithDarckox;
    public UseDialogueEntry[] useWithMrSlow;
    public UseDialogueEntry[] useWithSulana;

    // --- Give Item Popup ---
    [Header("Give Item UI")]
    [SerializeField] private Sprite defaultItemSprite;
    private GameObject givePopup;
    private Image giveIcon;
    private Text giveTitle;
    private Text giveDescription;
    private Text giveHint;

    private bool isShowingGivePopup = false;
    private bool givePopupShownThisLine = false;

    
    private enum OptionMode { Talk, Describe, Use }
    private OptionMode currentOptionsMode = OptionMode.Talk;

    private CharacterDialogueData GetDataForMode(int charIndex, OptionMode mode)
    {
        return mode == OptionMode.Describe
            ? characters[charIndex].describeData
            : characters[charIndex].talkData; // Talk par défaut (Use géré à part)
    }

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
        characters[0] = MakeSlot(sulkide, sulkideData,  sulkideDescribeData);
        characters[1] = MakeSlot(darckox, darckoxData,  darckoxDescribeData);
        characters[2] = MakeSlot(mrSlow,  mrSlowData,   mrSlowDescribeData);
        characters[3] = MakeSlot(sulana,  sulanaData,   sulanaDescribeData);

        if (!npcAudioSource)
        {
            npcAudioSource = gameObject.AddComponent<AudioSource>();
            npcAudioSource.playOnAwake = false;
        }

        characterAnims = new[] { animSulkide, animDarckox, animMrSlow, animSulana };

        if (npcAnim == null) npcAnim = GetComponentInChildren<DummyAnimation>();
    }

    private CharacterSlot MakeSlot(GameObject go, CharacterDialogueData talk, CharacterDialogueData describe)
    {
        var slot = new CharacterSlot
        {
            obj = go,
            talkData = talk,
            describeData = describe,
            name = (talk != null && !string.IsNullOrEmpty(talk.characterName)) ? talk.characterName :
                   (describe != null && !string.IsNullOrEmpty(describe.characterName)) ? describe.characterName :
                   go.name,
            audio = go.GetComponent<AudioSource>()
        };
        if (!slot.audio)
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

    private void ClearOptionsContainer()
    {
        if (optionsListRoot)
            for (int i = optionsListRoot.childCount - 1; i >= 0; i--)
                Destroy(optionsListRoot.GetChild(i).gameObject);

        if (optionsGridRoot)
            for (int i = optionsGridRoot.childCount - 1; i >= 0; i--)
                Destroy(optionsGridRoot.GetChild(i).gameObject);

        optionTexts.Clear();
        useItemIcons.Clear();
        useItems.Clear();
    }


    
    private void OnTriggerStay2D(Collider2D other)
    {
        if (!eventStart && other.CompareTag("Target"))
        {
            
            
            var pm = other.GetComponent<PlayerMovement>();
            if (pm != null && pm.useInputRegistered)
            {
                eventStart = true;
                GameManager.instance?.MakePlayerInvisible(); // si tu as cette méthode
                EventStart(pm);
                Debug.Log("entre");
            }
        }
    }

    public void EventStart(PlayerMovement pm) => StartCoroutine(EventFlow(pm));

    // ----------------- Letterbox helpers (abrégés) -----------------
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

        // (optionnel) masquer tes players ici
        // GameManager.instance?.MakePlayerInvisible();

        if (mainCam && !preCamSaved)
        {
            savedCamPos = mainCam.transform.position;
            savedCamRot = mainCam.transform.rotation;
            savedCamWasOrtho = mainCam.orthographic;
            if (savedCamWasOrtho) savedOrthoSize = mainCam.orthographicSize;
            else                  savedCamFOV    = mainCam.fieldOfView;
            preCamSaved = true;
        }
        
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

        // EventSystem
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            es.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInBuild;
        }

        // Canvas + RectTransform (FORCÉ)
        var canvasGO = new GameObject("DialogueCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvasRT = canvasGO.GetComponent<RectTransform>(); // <-- parent garanti
        uiCanvas = canvasGO.GetComponent<Canvas>();
        uiCanvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        int uiLayer = LayerMask.NameToLayer("UI");
        if (uiLayer >= 0) canvasGO.layer = uiLayer;
        canvasGO.hideFlags = HideFlags.DontSave | HideFlags.DontSaveInBuild;

        // Top / Bottom panels (parent = canvasRT, jamais null)
        topPanel = CreatePanel("TopBarUI", canvasRT, new Color(0, 0, 0, 1f));
        AnchorTop(topPanel);

        bottomPanel = CreatePanel("BottomBarUI", canvasRT, new Color(0, 0, 0, 1f));
        AnchorBottom(bottomPanel);

        // --- Boutons du top ---
        (btnCharacter, btnCharacterText) = CreateButton("BtnCharacter", topPanel, new Vector2(200, 48), new Vector2(12,  -12));
        btnCharacterText.text = "Personnage";
        btnCharacter.onClick.AddListener(SwitchCharacterNext);

        (btnTalk, btnTalkText) = CreateButton("BtnTalk", topPanel, new Vector2(200, 48), new Vector2(224, -12));
        btnTalkText.text = "Parler";
        btnTalk.onClick.AddListener(() => OpenOptions(OptionMode.Talk));

        (btnDescribe, btnDescribeText) = CreateButton("BtnDescribe", topPanel, new Vector2(200, 48), new Vector2(436, -12));
        btnDescribeText.text = "Décrire";
        btnDescribe.onClick.AddListener(() => OpenOptions(OptionMode.Describe));

        (btnUse, btnUseText) = CreateButton("BtnUse", topPanel, new Vector2(200, 48), new Vector2(648, -12));
        btnUseText.text = "Utiliser";
        btnUse.onClick.AddListener(() => OpenOptions(OptionMode.Use));

        (btnQuit, btnQuitText) = CreateButton("BtnQuit", topPanel, new Vector2(200, 48), new Vector2(860, -12));
        btnQuitText.text = "Quitter";
        btnQuit.onClick.AddListener(StartQuitFlow);

        
        // --- Options container (texte / inventaire) ---
        var optionsGO = new GameObject("OptionsContainer", typeof(RectTransform));
        optionsGO.transform.SetParent(bottomPanel, false);
        optionsContainer = optionsGO.GetComponent<RectTransform>();
        optionsContainer.anchorMin = new Vector2(0, 0);
        optionsContainer.anchorMax = new Vector2(1, 1);
        optionsContainer.offsetMin = new Vector2(20, 20);
        optionsContainer.offsetMax = new Vector2(-20, -20);

        // --- Racine LISTE (Vertical) ---
        var listGO = new GameObject("ListRoot", typeof(RectTransform));
        listGO.transform.SetParent(optionsContainer, false);
        optionsListRoot = listGO.GetComponent<RectTransform>();
        optionsListRoot.anchorMin = new Vector2(0, 0);
        optionsListRoot.anchorMax = new Vector2(1, 1);
        optionsListRoot.offsetMin = Vector2.zero;
        optionsListRoot.offsetMax = Vector2.zero;
        listVLG = listGO.AddComponent<UIVerticalLayoutGroup>();
        listVLG.spacing = 2f;
        listVLG.childControlHeight = true;
        listVLG.childControlWidth  = true;
        listVLG.childForceExpandHeight = false;
        listVLG.childForceExpandWidth  = false;
        listVLG.padding = new RectOffset(10, 10, 10, 10);
        optionsListRoot.gameObject.SetActive(true);

// --- Racine GRILLE (Grid) ---
        var gridGO = new GameObject("GridRoot", typeof(RectTransform));
        gridGO.transform.SetParent(optionsContainer, false);
        optionsGridRoot = gridGO.GetComponent<RectTransform>();
        optionsGridRoot.anchorMin = new Vector2(0, 0);
        optionsGridRoot.anchorMax = new Vector2(1, 1);
        optionsGridRoot.offsetMin = Vector2.zero;
        optionsGridRoot.offsetMax = Vector2.zero;
        gridGLG = gridGO.AddComponent<UIGridLayoutGroup>();
        gridGLG.startCorner     = GridLayoutGroup.Corner.UpperLeft;
        gridGLG.startAxis       = GridLayoutGroup.Axis.Horizontal;
        gridGLG.constraint      = UIGridLayoutGroup.Constraint.FixedRowCount;
        gridGLG.constraintCount = 1;
        gridGLG.childAlignment  = TextAnchor.UpperLeft;
        gridGLG.padding         = new RectOffset(10, 10, 10, 10);
        gridGLG.spacing         = new Vector2(24f, 0f);
        optionsGridRoot.gameObject.SetActive(false);

        



// ⚠️ NE PAS créer le Grid ici. On le créera quand on en a besoin.
        //optionsGrid = null;
        

        // --- Response box ---
        responseBox = CreatePanel("ResponseBox", bottomPanel, new Color(0, 0, 0, 0)).gameObject;
        var rb = responseBox.GetComponent<RectTransform>();
        rb.anchorMin = new Vector2(0, 0);
        rb.anchorMax = new Vector2(1, 1);
        rb.offsetMin = new Vector2(20, 20);
        rb.offsetMax = new Vector2(-20, -20);

        var nameGO = new GameObject("SpeakerName", typeof(RectTransform), typeof(Text));
        nameGO.transform.SetParent(responseBox.transform, false);
        var nameRT = nameGO.GetComponent<RectTransform>();
        nameRT.anchorMin = new Vector2(0, 1);
        nameRT.anchorMax = new Vector2(1, 1);
        nameRT.pivot = new Vector2(0, 1);
        nameRT.offsetMin = new Vector2(0, -36);
        nameRT.offsetMax = new Vector2(0, 0);
        responseNameText = nameGO.GetComponent<Text>();
        responseNameText.font = ResolveUIFont();
        responseNameText.fontSize = 22;
        responseNameText.color = Color.white;
        responseNameText.alignment = TextAnchor.UpperLeft;
        responseNameText.text = "PNJ";

        var textGO = new GameObject("ResponseText", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(responseBox.transform, false);
        var textRT = textGO.GetComponent<RectTransform>();
        textRT.anchorMin = new Vector2(0, 0);
        textRT.anchorMax = new Vector2(1, 1);
        textRT.offsetMin = Vector2.zero;
        textRT.offsetMax = new Vector2(0, -40);
        responseText = textGO.GetComponent<Text>();
        responseText.font = ResolveUIFont();
        responseText.fontSize = 24;
        responseText.color = Color.white;
        responseText.alignment = TextAnchor.UpperLeft;
        // ➜ Ajouter TypewriterEffect APRÈS création/assignation des Texts
        responseTyper = textGO.AddComponent<TypewriterEffect>();

        responseBox.SetActive(false);

        // --- GiveItemPopup ---
        givePopup = CreatePanel("GiveItemPopup", bottomPanel, new Color(0, 0, 0, 0.90f)).gameObject;
        var gp = givePopup.GetComponent<RectTransform>();
        gp.anchorMin = new Vector2(0.5f, 0.5f);
        gp.anchorMax = new Vector2(0.5f, 0.5f);
        gp.pivot     = new Vector2(0.5f, 0.5f);
        gp.sizeDelta = new Vector2(680, 220);
        gp.anchoredPosition = Vector2.zero;
        givePopup.SetActive(false);

        var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        iconGO.transform.SetParent(givePopup.transform, false);
        var iconRT = iconGO.GetComponent<RectTransform>();
        iconRT.anchorMin = new Vector2(0, 0.5f);
        iconRT.anchorMax = new Vector2(0, 0.5f);
        iconRT.pivot     = new Vector2(0, 0.5f);
        iconRT.sizeDelta = new Vector2(128, 128);
        iconRT.anchoredPosition = new Vector2(24, 0);
        giveIcon = iconGO.GetComponent<Image>();
        giveIcon.preserveAspect = true;

        giveTitle = new GameObject("Title", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        giveTitle.transform.SetParent(givePopup.transform, false);
        var titleRT = giveTitle.GetComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0, 1);
        titleRT.anchorMax = new Vector2(1, 1);
        titleRT.offsetMin = new Vector2(168, -60);
        titleRT.offsetMax = new Vector2(-20, -16);
        giveTitle.font = ResolveUIFont();
        giveTitle.fontSize = 30;
        giveTitle.color = Color.white;
        giveTitle.alignment = TextAnchor.UpperLeft;

        giveDescription = new GameObject("Description", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        giveDescription.transform.SetParent(givePopup.transform, false);
        var descRT = giveDescription.GetComponent<RectTransform>();
        descRT.anchorMin = new Vector2(0, 0);
        descRT.anchorMax = new Vector2(1, 1);
        descRT.offsetMin = new Vector2(168, 20);
        descRT.offsetMax = new Vector2(-20, -70);
        giveDescription.font = ResolveUIFont();
        giveDescription.fontSize = 22;
        giveDescription.color = new Color(0.9f, 0.9f, 0.9f, 1f);
        giveDescription.alignment = TextAnchor.UpperLeft;

        giveHint = new GameObject("Hint", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
        giveHint.transform.SetParent(givePopup.transform, false);
        var hintRT = giveHint.GetComponent<RectTransform>();
        hintRT.anchorMin = new Vector2(0, 0);
        hintRT.anchorMax = new Vector2(1, 0);
        hintRT.offsetMin = new Vector2(20, 16);
        hintRT.offsetMax = new Vector2(-20, 56);
        giveHint.font = ResolveUIFont();
        giveHint.fontSize = 18;
        giveHint.color = new Color(1f, 1f, 1f, 0.85f);
        giveHint.alignment = TextAnchor.LowerRight;
        giveHint.text = "Appuyez sur Utiliser pour continuer";

        // Masquer le bas au départ
        HideBottom();

        // Navigation: off
        var navNone = new Navigation { mode = Navigation.Mode.None };
        btnCharacter.navigation = navNone;
        btnTalk.navigation      = navNone;
        btnDescribe.navigation  = navNone;
        btnUse.navigation       = navNone;
        btnQuit.navigation = navNone;

    }


    
    private void SetOptionsLayoutMode(bool horizontalGrid, int itemCount = 0)
    {
        if (!optionsContainer) return;

        usingGrid = horizontalGrid;

        if (optionsListRoot) optionsListRoot.gameObject.SetActive(!horizontalGrid);
        if (optionsGridRoot) optionsGridRoot.gameObject.SetActive(horizontalGrid);

        // Ajuste la largeur des cellules si on est en grille
        if (horizontalGrid && gridGLG && optionsGridRoot)
        {
            float containerW = optionsGridRoot.rect.width;
            if (containerW <= 0f) containerW = Screen.width - 40f;
            float padLR    = gridGLG.padding.left + gridGLG.padding.right;
            float spacingX = gridGLG.spacing.x;
            float available = Mathf.Max(0f, containerW - padLR - spacingX * Mathf.Max(0, itemCount - 1));
            float cellW = (itemCount > 0) ? available / itemCount : 420f;
            gridGLG.cellSize = new Vector2(Mathf.Clamp(cellW, 64f, 520f), 72f);
        }

        if (optionsContainer.gameObject.activeInHierarchy)
            LayoutRebuilder.ForceRebuildLayoutImmediate(optionsContainer);
    }







    private void ShowGiveItemPopup(KeyObjData item)
    {
        if (item == null || givePopup == null) return;

        // Ajoute à l’inventaire (une fois)
        GameManager.instance?.AddKeyObject(item);

        // UI
        giveIcon.sprite = item.icon ? item.icon : defaultItemSprite;
        giveTitle.text  = string.IsNullOrEmpty(item.displayName) ? item.id : item.displayName;

        // On suppose que KeyObjData possède un champ 'description'
        string desc = item.description; 
        giveDescription.text = string.IsNullOrEmpty(desc) ? "" : desc;

        givePopup.SetActive(true);
        isShowingGivePopup = true;
    }

    private void HideGiveItemPopup()
    {
        if (!givePopup) return;
        givePopup.SetActive(false);
        isShowingGivePopup = false;
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

        if (initialIndex >= 0)
            topSelectionIndex = Mathf.Clamp(initialIndex, 0, 4);

        SetTopButtonsInteractable(true);
        HighlightTopButton();
        ResetInputLatchAll();
        ResetAxisRepeatState();

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(btnTalk ? btnTalk.gameObject : null);
    }

    private void HideBottom()
    {
        if (!bottomPanel) return;
        bottomPanel.gameObject.SetActive(false);
        if (optionsContainer) optionsContainer.gameObject.SetActive(false);
        if (responseBox) responseBox.SetActive(false);
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
            if (currentOptionsMode == OptionMode.Use)
                BuildUseList();
            else
                BuildOptionsForCurrentCharacter(currentOptionsMode);

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

        // X : -1/ +1 pour se déplacer entre 0..3
        int hEdge = AxisEdge(ref hLastDir, x, axisDeadZone);
        if (hEdge != 0)
        {
            topSelectionIndex = Mathf.Clamp(topSelectionIndex + hEdge, 0, 4);
            HighlightTopButton();
        }

        // Y : haut = changer de perso / bas = confirmer
// Y : bas = confirmer (PLUS de changement de perso avec haut)
        int vEdge = AxisEdge(ref vLastDir, y, axisDeadZone);
        if (vEdge < 0)
        {
            ConfirmTopSelection();
            return;
        }

        

        // Gâchettes pour changer de perso
        if (PressedOnce("SelectL", "Selectl") && CanSwitchNow()) { SwitchCharacterNext(); ArmSwitchCooldown(); return; }
        if (PressedOnce("SelectR") && CanSwitchNow()) { SwitchCharacterPrev(); ArmSwitchCooldown(); return; }

        // Bouton Use = confirmer
        if (PressedOnceUse()) ConfirmTopSelection();
    }

    private void ConfirmTopSelection()
    {
        switch (topSelectionIndex)
        {
            case 0: SwitchCharacterNext(); break;            // Personnage
            case 1: OpenOptions(OptionMode.Talk); break;     // Parler
            case 2: OpenOptions(OptionMode.Describe); break; // Décrire
            case 3: OpenOptions(OptionMode.Use); break;      // Utiliser
            case 4: StartQuitFlow(); break;                  // Quitter
        }
    }


    private void HighlightTopButton()
    {
        if (!btnCharacter || !btnTalk || !btnDescribe || !btnUse) return;

        var selectedBg   = GetCurrentHighlightColor();
        var deselectedBg = topBarUnselectedBg;

        var imgChar = btnCharacter.GetComponent<Image>();
        var imgTalk = btnTalk.GetComponent<Image>();
        var imgDesc = btnDescribe.GetComponent<Image>();
        var imgUse  = btnUse.GetComponent<Image>();
        var imgQuit = btnQuit.GetComponent<Image>();
        if (imgChar) imgChar.color = (topSelectionIndex == 0) ? selectedBg : deselectedBg;
        if (imgTalk) imgTalk.color = (topSelectionIndex == 1) ? selectedBg : deselectedBg;
        if (imgDesc) imgDesc.color = (topSelectionIndex == 2) ? selectedBg : deselectedBg;
        if (imgUse)  imgUse.color  = (topSelectionIndex == 3) ? selectedBg : deselectedBg;
        if (imgQuit) imgQuit.color = (topSelectionIndex == 4) ? selectedBg : deselectedBg;
        if (btnCharacterText) btnCharacterText.color = (topSelectionIndex == 0) ? topBarSelectedText : topBarUnselectedText;
        if (btnTalkText)      btnTalkText.color      = (topSelectionIndex == 1) ? topBarSelectedText : topBarUnselectedText;
        if (btnDescribeText)  btnDescribeText.color  = (topSelectionIndex == 2) ? topBarSelectedText : topBarUnselectedText;
        if (btnUseText)       btnUseText.color       = (topSelectionIndex == 3) ? topBarSelectedText : topBarUnselectedText;
        if (btnQuitText) btnQuitText.color = (topSelectionIndex == 4) ? topBarSelectedText : topBarUnselectedText;
    }

    private void OpenOptions(OptionMode mode)
    {
        currentOptionsMode = mode;

        if (mode == OptionMode.Use)
        {
            ShowOptions();                 // ⬅️ D'ABORD on active le panneau + container
            BuildUseList();                // ⬅️ ENSUITE on remplit et on bascule en grille
            selectedUseIndex = 0;
            HighlightUseItem();
            return;
        }

        BuildOptionsForCurrentCharacter(mode);
        ShowOptions();
        selectedOptionIndex = 0;
        HighlightOption();
    }


    // ------------------------------ Options : Talk/Describe ------------------------------
    private void HandleOptionsInput()
    {
        // Gâchettes
        if (PressedOnce("SelectL", "Selectl") && CanSwitchNow()) { SwitchCharacterNext(); ArmSwitchCooldown(); return; }
        if (PressedOnce("SelectR") && CanSwitchNow()) { SwitchCharacterPrev(); ArmSwitchCooldown(); return; }

        // D-pad X (droite => next / gauche => prev)

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

        if (currentOptionsMode == OptionMode.Use)
        {
            // NAV HORIZONTALE (gauche/droite) + VERTICALE (remonter au TopBar)
            int xStep = DpadEdgeX();
            if (xStep == 0) xStep = AxisEdge(ref hLastDir, currentPM.moveInput.x, axisDeadZone);

            if (xStep != 0 && useItems.Count > 0)
            {
                selectedUseIndex = Mathf.Clamp(selectedUseIndex + xStep, 0, Mathf.Max(0, useItems.Count - 1));
                HighlightUseItem();
                return;
            }

            int yStep = DpadEdgeY();
            if (yStep == 0) yStep = AxisEdge(ref vLastDir, currentPM.moveInput.y, axisDeadZone);
            if (yStep != 0)
            {
                if (useItems.Count == 0) { ShowTopBarUI(3); return; }
                if (yStep > 0 && selectedUseIndex == 0) { ShowTopBarUI(3); return; } // haut => remonter
                // bas/haut n’affectent pas l’index (on garde la rangée unique)
            }

            // Valider
            if (PressedOnceUse() && useItems.Count > 0)
            {
                LaunchUseDialogue(useItems[selectedUseIndex]);
            }
            return;
        }


        // Stick X : droite => next / gauche => prev


        // Y : naviguer liste
        int dpy = DpadEdgeY();
        if (dpy != 0) { NavigateOptions(-dpy); }
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
        if (_visibleOptionIndices.Count == 0) { ShowTopBarUI( (currentOptionsMode==OptionMode.Talk)?1:2 ); return; }
        if (delta < 0 && selectedOptionIndex == 0) { ShowTopBarUI( (currentOptionsMode==OptionMode.Talk)?1:2 ); return; }
        MoveOption(delta);
    }

    private void BuildOptionsForCurrentCharacter(OptionMode mode)
    {
        // clear anciens
        ClearOptionsContainer();
        _visibleOptionIndices.Clear();
        
        // Talk / Describe => layout vertical
        SetOptionsLayoutMode(false);


        var data = GetDataForMode(currentCharacter, mode);
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
                // Cherche une ligne Player sinon première ligne non vide
                D.DialogueLine firstPlayer = opt.lines.Find(
                    l => l.speaker == D.Speaker.Player && !string.IsNullOrWhiteSpace(l.text)
                );
                if (firstPlayer != null) label = firstPlayer.text;
                else if (opt.lines.Count > 0) label = string.IsNullOrWhiteSpace(opt.lines[0].text) ? "(...)" : opt.lines[0].text;
                else label = "(...)";
            }

            _visibleOptionIndices.Add(i);
            optionTexts.Add(CreateOptionText(label));
        }

        if (_visibleOptionIndices.Count == 0)
            optionTexts.Add(CreateOptionText("(Aucune option)"));
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
        go.transform.SetParent(optionsListRoot ? optionsListRoot : optionsContainer, false);
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
        var data = GetDataForMode(currentCharacter, currentOptionsMode);

        if (data == null || data.dialogueOptions == null || data.dialogueOptions.Count == 0 || _visibleOptionIndices.Count == 0)
        {
            ShowTopBarUI( (currentOptionsMode==OptionMode.Talk)?1:2 );
            return;
        }

        selectedOptionIndex = Mathf.Clamp(selectedOptionIndex, 0, _visibleOptionIndices.Count - 1);
        int sourceIndex = _visibleOptionIndices[selectedOptionIndex];
        var opt = data.dialogueOptions[sourceIndex];

        _currentOptionSourceIndex = sourceIndex;

        string npcName = !string.IsNullOrEmpty(data.npcDisplayName) ? data.npcDisplayName : defaultNpcName;
        StartConversation(opt.lines, npcName);
    }

    // ====== UTILISER : inventaire et mapping ======
    private void BuildUseList()
    {
        
        
        // nettoyer UI précédente
        ClearOptionsContainer(); 

        var gm = GameManager.instance;
        var list = gm != null ? gm.GetKeyItems() : null;
        
        if (list == null || list.Count == 0)
        {
            SetOptionsLayoutMode(false);
            optionTexts.Add(CreateOptionText("(Inventaire vide)"));
            return;
        }
        
        int count = list.Count;
        SetOptionsLayoutMode(true, count);

        for (int i = 0; i < list.Count; i++)
        {
            var data = list[i];
            if (!data) continue;

            var row = new GameObject("UseItemRow", typeof(RectTransform));
            row.transform.SetParent(optionsGridRoot, false); // ← ICI (et plus sur optionsContainer)
            var rt = row.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot     = new Vector2(0, 1);
            rt.sizeDelta = new Vector2(0, 72);

            var iconGO = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconGO.transform.SetParent(row.transform, false);
            var irt = iconGO.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0.5f);
            irt.anchorMax = new Vector2(0, 0.5f);
            irt.pivot     = new Vector2(0, 0.5f);
            irt.sizeDelta = new Vector2(64, 64);
            irt.anchoredPosition = new Vector2(0, 0);
            var icon = iconGO.GetComponent<Image>();
            icon.sprite = data.icon;
            icon.preserveAspect = true;

            var label = new GameObject("Label", typeof(RectTransform), typeof(Text)).GetComponent<Text>();
            label.transform.SetParent(row.transform, false);
            var lrt = label.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0, 0);
            lrt.anchorMax = new Vector2(1, 1);
            lrt.offsetMin = new Vector2(72, 0);
            lrt.offsetMax = new Vector2(0, 0);
            label.font     = ResolveUIFont();
            label.fontSize = 24;
            label.alignment = TextAnchor.MiddleLeft;
            label.color = Color.white;
            label.text  = string.IsNullOrEmpty(data.displayName) ? data.id : data.displayName;

            useItemIcons.Add(icon);
            useItems.Add(data);
        }

        
    }

    private void HighlightUseItem()
    {
        for (int i = 0; i < useItemIcons.Count; i++)
        {
            if (!useItemIcons[i]) continue;
            useItemIcons[i].color = (i == selectedUseIndex) ? GetCurrentHighlightColor() : Color.white;
        }
    }

    private UseDialogueEntry FindUseEntry(KeyObjData item, int charIndex)
    {
        UseDialogueEntry[] table = null;
        switch (charIndex)
        {
            case 0: table = useWithSulkide; break;
            case 1: table = useWithDarckox; break;
            case 2: table = useWithMrSlow;  break;
            case 3: table = useWithSulana;  break;
        }
        if (table != null)
        {
            for (int i = 0; i < table.Length; i++)
                if (table[i] != null && table[i].item == item)
                    return table[i];
        }
        return null;
    }


    private void LaunchUseDialogue(KeyObjData item)
    {
        var entry = FindUseEntry(item, currentCharacter);

        if (entry == null || entry.lines == null || entry.lines.Count == 0)
        {
            var fallback = new List<D.DialogueLine> {
                new D.DialogueLine { speaker = D.Speaker.NPC, text = "Je ne vois pas l'intérêt de me montrer ça." }
            };
            pendingConsumedItem = null;               // rien à consommer
            StartConversation(fallback, activeNpcName);
            return;
        }

        // ➜ On consommera l’objet seulement si la case est cochée
        pendingConsumedItem = entry.consumeItem ? item : null;

        string npcName = string.IsNullOrEmpty(activeNpcName) ? defaultNpcName : activeNpcName;
        StartConversation(entry.lines, npcName);
    }



    // ------------------------------ Conversation ------------------------------
    private void StartConversation(List<D.DialogueLine> lines, string npcName)
    {
        if (lines == null || lines.Count == 0) { ShowTopBarUI(1); return; }

        activeLines = lines;
        activeLineIndex = 0;
        activeNpcName = string.IsNullOrEmpty(npcName) ? defaultNpcName : npcName;

        ShowResponse();
        ShowCurrentLine();
    }

    private DummyAnimation GetDummyForSpeaker(D.Speaker speaker)
    {
        switch (speaker)
        {
            case D.Speaker.Player:
                return (currentCharacter >= 0 && currentCharacter < characterAnims.Length) ? characterAnims[currentCharacter] : null;
            case D.Speaker.NPC:     return npcAnim;
            case D.Speaker.Sulkide: return animSulkide;
            case D.Speaker.Darckox: return animDarckox;
            case D.Speaker.MrSlow:  return animMrSlow;
            case D.Speaker.Sulana:  return animSulana;
            default: return null;
        }
    }

    private void PlayDummyAnimation(D.AnimationKind kind, D.Speaker speaker)
    {
        if (kind == D.AnimationKind.None) return;

        var dummy = GetDummyForSpeaker(speaker);
        if (dummy == null) return;

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

        givePopupShownThisLine = false;  // reset par ligne

        
        var line = activeLines[activeLineIndex];

        // Nom dans la box
        switch (line.speaker)
        {
            case D.Speaker.Player:
                responseNameText.text = characters[currentCharacter].name;
                responseNameText.color = GetCurrentHighlightColor();
                break;
            case D.Speaker.NPC:
                responseNameText.text = activeNpcName; responseNameText.color = Color.gray; break;
            case D.Speaker.Sulkide:
                responseNameText.text = sulkideData ? sulkideData.characterName : "Sulkide"; responseNameText.color = sulkideColor; break;
            case D.Speaker.Darckox:
                responseNameText.text = darckoxData ? darckoxData.characterName : "Darckox"; responseNameText.color = darckoxColor; break;
            case D.Speaker.MrSlow:
                responseNameText.text = mrSlowData ? mrSlowData.characterName : "MrSlow"; responseNameText.color = mrSlowColor; break;
            case D.Speaker.Sulana:
                responseNameText.text = sulanaData ? sulanaData.characterName : "Sulana"; responseNameText.color = sulanaColor; break;
        }

        // Audio
        if (line.audio != null)
        {
            if (line.speaker == D.Speaker.Player)
            {
                var a = characters[currentCharacter].audio;
                if (a) a.PlayOneShot(line.audio);
            }
            else if (line.speaker == D.Speaker.NPC)
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
        
        // Donner un item si la ligne le demande
// (on suppose que D.DialogueLine possède: public KeyObjData giveItem;)
        if (!givePopupShownThisLine && line.giveItem != null)
        {
            ShowGiveItemPopup(line.giveItem);
            givePopupShownThisLine = true;
        }

    }

    private void HandleResponseInput()
    {
        // Switch perso pendant la réponse

        if (PressedOnce("SelectL", "Selectl") && CanSwitchNow()) { SwitchCharacterPrev(); ArmSwitchCooldown(); return; }
        if (PressedOnce("SelectR") && CanSwitchNow()) { SwitchCharacterNext(); ArmSwitchCooldown(); return; }

        if (PressedOnceUse())
        {
            if (activeLines == null || activeLines.Count == 0) { ShowTopBarUI(1); return; }

            var line = activeLines[Mathf.Clamp(activeLineIndex, 0, activeLines.Count - 1)];

            if (responseTyper.IsTyping())
            {
                responseTyper.StopAndShowAll(line.text ?? "");
                return;
            }
            
            // Si la carte "objet reçu" est affichée, on la ferme d'abord
            if (isShowingGivePopup)
            {
                HideGiveItemPopup();
                return; // on ne passe pas à la ligne suivante tant que la carte n'est pas fermée
            }
            
            // Avancer
            activeLineIndex++;
            if (activeLineIndex >= activeLines.Count)
            {
                if (quittingAfterConversation)
                {
                    quittingAfterConversation = false;
                    ResetConversationAnimations();
                    StartCoroutine(ExitDialogueSequence());
                    return;
                }

                if (currentOptionsMode != OptionMode.Use)
                {
                    var data = GetDataForMode(currentCharacter, currentOptionsMode);
                    if (data != null && _currentOptionSourceIndex >= 0)
                        MarkOptionUsed(data, _currentOptionSourceIndex);
                }
                else
                {
                    if (pendingConsumedItem != null)
                    {
                        GameManager.instance?.RemoveKeyObject(pendingConsumedItem);
                        pendingConsumedItem = null;
                    }
                    BuildUseList();
                }

                ResetConversationAnimations();
                ShowTopBarUI(currentOptionsMode == OptionMode.Use ? 3 : 1);
                return;
            }


            // Avant la ligne suivante, on remet tout Idle pour éviter un locuteur “bloqué”
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
        if (btnDescribe)  btnDescribe.interactable  = interactable;
        if (btnUse)       btnUse.interactable       = interactable;
        if (btnQuit) btnQuit.interactable = interactable;

    }

    private void OnDisable()  { CleanupRuntimeObjects(); }
    private void OnDestroy()  { CleanupRuntimeObjects(); }

    private void CleanupRuntimeObjects()
    {
        // Bars
        if (barRoot != null)
        {
            if (Application.isEditor) DestroyImmediate(barRoot);
            else Destroy(barRoot);
            barRoot = null; topBar = null; bottomBar = null;
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
            btnCharacter = btnTalk = btnDescribe = btnUse = null;
            btnCharacterText = btnTalkText = btnDescribeText = btnUseText = null;
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
    
    private List<D.DialogueLine> GetQuitLinesForCurrentCharacter()
    {
        switch (currentCharacter)
        {
            case 0: return (quitWithSulkide != null && quitWithSulkide.Count > 0) ? quitWithSulkide
                       : new List<D.DialogueLine>{ new D.DialogueLine{ speaker=D.Speaker.Sulkide, text="On s'capte plus tard." } };
            case 1: return (quitWithDarckox != null && quitWithDarckox.Count > 0) ? quitWithDarckox
                       : new List<D.DialogueLine>{ new D.DialogueLine{ speaker=D.Speaker.Darckox, text="Ça marche, à +." } };
            case 2: return (quitWithMrSlow != null && quitWithMrSlow.Count > 0) ? quitWithMrSlow
                       : new List<D.DialogueLine>{ new D.DialogueLine{ speaker=D.Speaker.MrSlow, text="Très bien, à bientôt." } };
            case 3: return (quitWithSulana != null && quitWithSulana.Count > 0) ? quitWithSulana
                       : new List<D.DialogueLine>{ new D.DialogueLine{ speaker=D.Speaker.Sulana, text="D'accord, on se revoit." } };
            default: return new List<D.DialogueLine>{ new D.DialogueLine{ speaker=D.Speaker.NPC, text="À la prochaine." } };
        }
    }

    private void StartQuitFlow()
    {
        // Lancer un mini dialogue selon le perso, puis on quittera à la fin
        var lines = GetQuitLinesForCurrentCharacter();
        quittingAfterConversation = true;
        // Nom de NPC déjà utilisé ailleurs
        string npcName = string.IsNullOrEmpty(activeNpcName) ? defaultNpcName : activeNpcName;
        StartConversation(lines, npcName);
    }

    private IEnumerator ExitDialogueSequence()
    {
        // Fermer les bandes noires au milieu (transition comme l’intro)
        EnsureBarsExist();
        yield return AnimateBarsClose(barsCloseDuration);

        // Rendre le joueur visible
        GameManager.instance?.MakePlayervisible();

        // Restaurer la caméra d’origine
        if (mainCam && preCamSaved)
        {
            mainCam.transform.SetPositionAndRotation(savedCamPos, savedCamRot);
            if (savedCamWasOrtho) mainCam.orthographicSize = savedOrthoSize;
            else                  mainCam.fieldOfView      = savedCamFOV;
        }

        // Nettoyage UI/bars
        CleanupRuntimeObjects();
        preCamSaved = false;

        // Remettre l’état
        uiState = UIState.Hidden;
        eventStart = false;
        currentPM = null;

        // (Optionnel) masquer les dummies si tu veux
        if (dummyHolder) dummyHolder.SetActive(false);
    }

}
