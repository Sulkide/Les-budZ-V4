using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

[System.Serializable]
public class DialogueChoice {
    [Tooltip("Texte affiché pour cette option")]
    public string choiceText;
    [Tooltip("Index dans la liste de dialogue à atteindre si ce choix est sélectionné")]
    public int nextDialogueIndex;
}

[System.Serializable]
public class DialogueLine {
    [Tooltip("Texte à afficher (vous pouvez utiliser les tags Rich Text de TextMeshPro : <size>, <color>, <font>, etc.)")]
    public string text;
    [Tooltip("Offset spécifique pour cette ligne (en pixels)")]
    public Vector2 offset;
    [Tooltip("Transform personnalisé pour positionner la boîte de dialogue pour cette ligne (facultatif)")]
    public Transform customPosition;
    [Tooltip("Si vrai, le dialogue s'arrête ici, même s'il reste d'autres lignes")]
    public bool endDialogueHere = false;
    [Tooltip("Liste des choix de branchement pour cette ligne (optionnel)")]
    public List<DialogueChoice> choices;
}

public class DialogueTrigger : MonoBehaviour
{
    // Paramètres de la boîte de dialogue
    [Header("Choix du style de boîte de dialogue")]
    [Tooltip("Liste des prefabs de boîte de dialogue (différents styles)")]
    public List<GameObject> dialogueBoxPrefabs;
    [Tooltip("Index du style choisi dans la liste")]
    public int styleIndex = 0;
    [Tooltip("Transform par défaut pour positionner la boîte si aucun custom n'est défini")]
    public Transform defaultTargetTransform;
    [Tooltip("Offset global appliqué à la position de la boîte")]
    public Vector2 globalOffset = Vector2.zero;

    // Options de déclenchement et cooldown
    [Header("Options de déclenchement")]
    [Tooltip("Si vrai, le dialogue ne se déclenchera que si le booléen 'useInputRegistered' du PlayerMovement est à true dans la zone")]
    public bool requireInputOnTrigger = true;
    [Tooltip("Si vrai, le dialogue se déclenche une seule fois")]
    public bool triggerOnce = true;
    [Tooltip("Cooldown après la fin du dialogue pour permettre de le redéclencher (en secondes)")]
    public float dialogueCooldown = 1f;
    private bool triggered = false;
    // Permet d'empêcher le déclenchement avant la fin du cooldown
    private bool canTriggerDialogue = true;

    // Contenu du dialogue
    [Header("Contenu du dialogue")]
    [Tooltip("Liste des lignes de dialogue")]
    public List<DialogueLine> dialogueLines = new List<DialogueLine>();

    // Paramètres du typewriter et du fade
    [Header("Paramètres du typewriter et fade")]
    [Tooltip("Délai entre chaque lettre (en secondes)")]
    public float letterDelay = 0.05f;
    [Tooltip("Durée du fade in/out (en secondes)")]
    public float fadeDuration = 0.3f;

    // Options cinématiques
    [Header("Options cinématiques")]
    [Tooltip("Si vrai, la caméra zoome et se déplace pour centrer le locuteur pendant le dialogue")]
    public bool zoomOnSpeaker = false;
    [Tooltip("Taille orthographique de la caméra pour le zoom")]
    public float zoomSize = 3f;
    [Tooltip("Durée de la transition de zoom et de déplacement (en secondes)")]
    public float zoomTransitionDuration = 0.5f;
    [Tooltip("Si vrai, des bandes noires s'affichent pendant le dialogue")]
    public bool enableCinematicBars = false;
    [Tooltip("Durée de l'animation d'apparition/disparition des bandes (en secondes)")]
    public float barsMoveDuration = 0.5f;
    [Tooltip("Hauteur des bandes cinématographiques (en pixels)")]
    public float cinematicBarHeight = 100f;

    // Option pour geler le mouvement du joueur durant tout le dialogue
    [Header("Mouvement du joueur")]
    [Tooltip("Si vrai, le mouvement du joueur est désactivé pendant tout le dialogue")]
    public bool freezePlayerMovement = false;

    // Paramètres de la boîte de choix
    [Header("UI de choix")]
    [Tooltip("Prefab de la boîte de choix (ex. un Panel avec un Vertical Layout Group et jusqu’à 6 TextMeshProUGUI enfants)")]
    public GameObject choiceBoxPrefab;
    [Tooltip("Offset spécifique appliqué à la position de la boîte de choix, par rapport à la boîte de dialogue")]
    public Vector2 choiceBoxOffset;

    // Références internes
    private GameObject dialogueBoxInstance;
    private TextMeshProUGUI dialogueText;
    private CanvasGroup canvasGroup;
    private Camera mainCamera;
    private float originalCameraSize;
    private Vector3 originalCameraPosition;
    private bool isDialogueRunning = false;
    private CameraFlip3D2D multiCameraController;

    // Variable pour recevoir le résultat de la branche
    private int choiceResult;

    // Références aux bandes cinématiques créées dynamiquement
    private RectTransform topBar;
    private RectTransform bottomBar;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera != null)
        {
            originalCameraSize = mainCamera.fieldOfView;
            originalCameraPosition = mainCamera.transform.position;
            multiCameraController = mainCamera.GetComponent<CameraFlip3D2D>();
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (isDialogueRunning || !canTriggerDialogue)
            return;
        if (triggered && triggerOnce)
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null)
        {
            if (!requireInputOnTrigger)
            {
                triggered = true;
                StartDialogue(player);
            }
        }
    }

    void OnTriggerStay2D(Collider2D other)
    {
        if (isDialogueRunning || !canTriggerDialogue)
            return;
        if (!requireInputOnTrigger || (triggered && triggerOnce))
            return;

        PlayerMovement player = other.GetComponent<PlayerMovement>();
        if (player != null && player.useInputRegistered)
        {
            triggered = true;
            StartDialogue(player);
        }
    }

    void StartDialogue(PlayerMovement player)
    {
        // Dès le début, empêcher tout nouveau déclenchement
        canTriggerDialogue = false;
        isDialogueRunning = true;

        // Geler le mouvement du joueur, si activé
        if (freezePlayerMovement)
        {
            foreach (var p in FindObjectsOfType<PlayerMovement>())
                p.areControllsRemoved = true;
        }

        // Si ZoomOnSpeaker est activé, désactiver le contrôleur de caméra et démarrer le zoom et le déplacement
        if (zoomOnSpeaker && multiCameraController != null)
        {
            multiCameraController.enabled = false;
            Vector3 startCamPos = mainCamera.transform.position;
            // On utilise le target pour centrer la caméra (on utilise defaultTargetTransform ou le Player)
            Transform target = (defaultTargetTransform != null) ? defaultTargetTransform : player.transform;
            // Conserver la composante z actuelle
            Vector3 panTarget = new Vector3(target.position.x, target.position.y, startCamPos.z);
            StartCoroutine(SmoothZoomAndPan(mainCamera.fieldOfView, zoomSize, startCamPos, panTarget, zoomTransitionDuration));
        }

        if (enableCinematicBars)
        {
            CreateCinematicBars();
            StartCoroutine(ShowCinematicBars());
        }

        if (dialogueBoxPrefabs != null && dialogueBoxPrefabs.Count > 0)
        {
            if (styleIndex < 0 || styleIndex >= dialogueBoxPrefabs.Count)
                styleIndex = 0;
            dialogueBoxInstance = Instantiate(dialogueBoxPrefabs[styleIndex], transform.parent);
            dialogueText = dialogueBoxInstance.GetComponentInChildren<TextMeshProUGUI>();
            canvasGroup = dialogueBoxInstance.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = dialogueBoxInstance.AddComponent<CanvasGroup>();
        }
        Transform targetPos = (defaultTargetTransform != null) ? defaultTargetTransform : player.transform;
        UpdateDialogueBoxPosition(targetPos, globalOffset);
        StartCoroutine(RunDialogue(player));
    }

    IEnumerator RunDialogue(PlayerMovement player)
    {
        int i = 0;
        while (i < dialogueLines.Count)
        {
            DialogueLine line = dialogueLines[i];
            Transform target = (line.customPosition != null) ? line.customPosition : player.transform;
            UpdateDialogueBoxPosition(target, globalOffset + line.offset);
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 0, 1, fadeDuration));
            yield return new WaitUntil(() => !player.useInputRegistered);
            dialogueText.text = line.text;
            dialogueText.maxVisibleCharacters = 0;
            dialogueText.ForceMeshUpdate();
            int totalCharacters = dialogueText.textInfo.characterCount;
            bool skipped = false;
            for (int visibleCount = 0; visibleCount <= totalCharacters; visibleCount++)
            {
                dialogueText.maxVisibleCharacters = visibleCount;
                if (player.useInputRegistered)
                {
                    dialogueText.maxVisibleCharacters = totalCharacters;
                    player.useInputRegistered = false;
                    skipped = true;
                    break;
                }
                yield return new WaitForSeconds(letterDelay);
            }
            if (skipped)
                yield return new WaitForSeconds(0.1f);

            if (line.endDialogueHere)
            {
                yield return new WaitUntil(() => player.useInputRegistered);
                player.useInputRegistered = false;
                yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1, 0, fadeDuration));
                break;
            }
            else if (line.choices != null && line.choices.Count > 0)
            {
                yield return StartCoroutine(HandleDialogueChoices(player, line));
                i = choiceResult;
                continue;
            }
            else
            {
                yield return new WaitUntil(() => player.useInputRegistered);
                player.useInputRegistered = false;
            }
            yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1, 0, fadeDuration));
            i++;
        }
        yield return StartCoroutine(FadeCanvasGroup(canvasGroup, 1, 0, fadeDuration));
        Destroy(dialogueBoxInstance);
        isDialogueRunning = false;

        if (zoomOnSpeaker && mainCamera != null)
        {
            Vector3 currentCamPos = mainCamera.transform.position;
            // Retour à la position et taille d'origine
            yield return StartCoroutine(SmoothZoomAndPan(mainCamera.fieldOfView, originalCameraSize, currentCamPos, originalCameraPosition, zoomTransitionDuration));
            if (multiCameraController != null)
                multiCameraController.enabled = true;
        }
        if (enableCinematicBars)
        {
            yield return StartCoroutine(HideCinematicBars());
        }

        if (freezePlayerMovement)
        {
            foreach (var p in FindObjectsOfType<PlayerMovement>())
                p.areControllsRemoved = false;
        }

        if (!triggerOnce)
        {
            yield return new WaitForSeconds(dialogueCooldown);
            triggered = false;
            canTriggerDialogue = true;
        }
    }

    IEnumerator HandleDialogueChoices(PlayerMovement player, DialogueLine currentLine)
    {
        GameObject choiceBoxInstance = Instantiate(choiceBoxPrefab, dialogueBoxInstance.transform.parent);
        RectTransform dialogueRect = dialogueBoxInstance.GetComponent<RectTransform>();
        RectTransform choiceRect = choiceBoxInstance.GetComponent<RectTransform>();
        float dialogueBottomY = dialogueRect.position.y - dialogueRect.rect.height * (1 - dialogueRect.pivot.y);
        float choiceY = dialogueBottomY - choiceRect.rect.height * choiceRect.pivot.y;
        Vector3 finalPosition = new Vector3(dialogueRect.position.x, choiceY, dialogueRect.position.z);
        finalPosition += new Vector3(choiceBoxOffset.x, choiceBoxOffset.y, 0f);
        choiceRect.position = finalPosition;
        TextMeshProUGUI[] choiceTexts = choiceBoxInstance.GetComponentsInChildren<TextMeshProUGUI>();
        int choiceCount = currentLine.choices.Count;
        for (int j = 0; j < choiceTexts.Length; j++)
        {
            if (j < choiceCount)
            {
                choiceTexts[j].text = currentLine.choices[j].choiceText;
                choiceTexts[j].gameObject.SetActive(true);
            }
            else
            {
                choiceTexts[j].gameObject.SetActive(false);
            }
        }
        int currentSelection = 0;
        Color normalColor = Color.gray;
        Color highlightColor = Color.black;
        void UpdateChoiceHighlight()
        {
            for (int j = 0; j < choiceCount; j++)
            {
                choiceTexts[j].color = (j == currentSelection) ? highlightColor : normalColor;
            }
        }
        UpdateChoiceHighlight();
        bool selectionMade = false;
        while (!selectionMade)
        {
            Vector2 moveInput = player.moveInput;
            if (moveInput.y > 0.5f)
            {
                currentSelection = Mathf.Max(currentSelection - 1, 0);
                UpdateChoiceHighlight();
                yield return new WaitForSeconds(0.2f);
            }
            else if (moveInput.y < -0.5f)
            {
                currentSelection = Mathf.Min(currentSelection + 1, choiceCount - 1);
                UpdateChoiceHighlight();
                yield return new WaitForSeconds(0.2f);
            }
            if (player.useInputRegistered)
            {
                selectionMade = true;
                player.useInputRegistered = false;
            }
            yield return null;
        }
        DialogueChoice selectedChoice = currentLine.choices[currentSelection];
        Destroy(choiceBoxInstance);
        choiceResult = selectedChoice.nextDialogueIndex;
        yield return null;
    }

    // Coroutine combinée pour effectuer un zoom et déplacer la caméra en douceur
    IEnumerator SmoothZoomAndPan(float startSize, float endSize, Vector3 startPos, Vector3 targetPos, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            mainCamera.fieldOfView = Mathf.Lerp(startSize, endSize, elapsed / duration);
            // Conserver la composante z d'origine
            Vector3 newPos = Vector3.Lerp(startPos, new Vector3(targetPos.x, targetPos.y, startPos.z), elapsed / duration);
            mainCamera.transform.position = newPos;
            yield return null;
        }
        mainCamera.fieldOfView = endSize;
        mainCamera.transform.position = new Vector3(targetPos.x, targetPos.y, startPos.z);
    }

    void UpdateDialogueBoxPosition(Transform target, Vector2 offset)
    {
        if (dialogueBoxInstance != null && target != null)
        {
            Canvas parentCanvas = dialogueBoxInstance.GetComponentInParent<Canvas>();
            RectTransform rect = dialogueBoxInstance.GetComponent<RectTransform>();
            if (parentCanvas != null && parentCanvas.renderMode == RenderMode.WorldSpace)
                rect.position = target.position + (Vector3)offset;
            else
            {
                Vector2 screenPos = Camera.main.WorldToScreenPoint(target.position);
                screenPos += offset;
                rect.position = screenPos;
            }
        }
    }

    IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
    {
        float t = 0f;
        cg.alpha = start;
        while (t < duration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Lerp(start, end, t / duration);
            yield return null;
        }
        cg.alpha = end;
    }

    // Création automatique des bandes noires sur le Canvas parent
    void CreateCinematicBars()
    {
        Canvas parentCanvas = null;
        if (dialogueBoxInstance != null)
            parentCanvas = dialogueBoxInstance.GetComponentInParent<Canvas>();
        if (parentCanvas == null && defaultTargetTransform != null)
            parentCanvas = defaultTargetTransform.GetComponentInParent<Canvas>();
        if (parentCanvas == null)
            parentCanvas = FindObjectOfType<Canvas>();

        if (parentCanvas == null)
        {
            Debug.LogError("Aucun Canvas n'a été trouvé pour créer les bandes cinématiques.");
            return;
        }

        GameObject topBarObj = new GameObject("TopBar");
        topBarObj.transform.SetParent(parentCanvas.transform, false);
        topBar = topBarObj.AddComponent<RectTransform>();
        topBar.anchorMin = new Vector2(0, 1);
        topBar.anchorMax = new Vector2(1, 1);
        topBar.pivot = new Vector2(0.5f, 1);
        topBar.sizeDelta = new Vector2(0, cinematicBarHeight);
        Image topImage = topBarObj.AddComponent<Image>();
        topImage.color = Color.black;
        topBar.anchoredPosition = new Vector2(0, cinematicBarHeight);

        GameObject bottomBarObj = new GameObject("BottomBar");
        bottomBarObj.transform.SetParent(parentCanvas.transform, false);
        bottomBar = bottomBarObj.AddComponent<RectTransform>();
        bottomBar.anchorMin = new Vector2(0, 0);
        bottomBar.anchorMax = new Vector2(1, 0);
        bottomBar.pivot = new Vector2(0.5f, 0);
        bottomBar.sizeDelta = new Vector2(0, cinematicBarHeight);
        Image bottomImage = bottomBarObj.AddComponent<Image>();
        bottomImage.color = Color.black;
        bottomBar.anchoredPosition = new Vector2(0, -cinematicBarHeight);
    }

    IEnumerator ShowCinematicBars()
    {
        Vector2 topInitial = topBar.anchoredPosition;
        Vector2 topTarget = new Vector2(0, 0);
        Vector2 bottomInitial = bottomBar.anchoredPosition;
        Vector2 bottomTarget = new Vector2(0, 0);
        float t = 0f;
        while (t < barsMoveDuration)
        {
            t += Time.deltaTime;
            topBar.anchoredPosition = Vector2.Lerp(topInitial, topTarget, t / barsMoveDuration);
            bottomBar.anchoredPosition = Vector2.Lerp(bottomInitial, bottomTarget, t / barsMoveDuration);
            yield return null;
        }
        topBar.anchoredPosition = topTarget;
        bottomBar.anchoredPosition = bottomTarget;
    }

    IEnumerator HideCinematicBars()
    {
        Vector2 topInitial = topBar.anchoredPosition;
        Vector2 topTarget = new Vector2(0, cinematicBarHeight);
        Vector2 bottomInitial = bottomBar.anchoredPosition;
        Vector2 bottomTarget = new Vector2(0, -cinematicBarHeight);
        float t = 0f;
        while (t < barsMoveDuration)
        {
            t += Time.deltaTime;
            topBar.anchoredPosition = Vector2.Lerp(topInitial, topTarget, t / barsMoveDuration);
            bottomBar.anchoredPosition = Vector2.Lerp(bottomInitial, bottomTarget, t / barsMoveDuration);
            yield return null;
        }
        topBar.anchoredPosition = topTarget;
        bottomBar.anchoredPosition = bottomTarget;
        Destroy(topBar.gameObject);
        Destroy(bottomBar.gameObject);
        topBar = null;
        bottomBar = null;
    }
}