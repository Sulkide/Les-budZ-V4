using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("Rail de déplacement")]
    [Tooltip("Si laissé vide, prendra automatiquement tous les enfants de la caméra.")]
    public List<Transform> railPoints;
    
    [Header("Offset général")]
    [Tooltip("Offset X/Y toujours appliqué à la caméra, même sur le rail.")]
    public Vector2 globalOffset = Vector2.zero;
    
    [Header("Paramètres de zoom (perspective)")]
    [Tooltip("Champ de vision minimal (en degrés).")]
    public float minZoom = 30f;
    [Tooltip("Champ de vision maximal (en degrés).")]
    public float maxZoom = 60f;
    [Tooltip("Vitesse d'interpolation du changement de FOV.")]
    public float distanceZoomSpeed = 2f;
    [Tooltip("Vitesse de zoom-out liée au mouvement.")]
    public float moveZoomOutSpeed = 3f;
    [Tooltip("Multiplicateur de la vitesse pour le zoom dynamique.")]
    public float speedZoomMultiplier = 0.1f;
    [Range(0f, 0.5f)]
    [Tooltip("Marge de viewport (fraction de la largeur) à laisser libre de chaque côté.")]
    public float viewportMargin = 0.1f;

    [Header("Offset équidistance")]
    [Tooltip("Décalage proportionnel à la distance horizontale entre joueurs.")]
    public float offsetMultiplier = 0.2f;

    [Header("Suivi rail (smooth)")]
    [Tooltip("Vitesse d'interpolation pour le suivi du rail.")]
    public float followSpeed = 5f;

    [Header("Recentrage après délai")]
    [Tooltip("Temps avant de recentrer quand on est complètement zoomé-out.")]
    public float recenterDelay = 2f;
    [Tooltip("Vitesse d'interpolation du recentrage.")]
    public float recenterSpeed = 2f;
    
    [Header("Gestion des joueurs hors-rail")]
    [Tooltip("Distance verticale max tolérée entre un joueur et le rail.")]
    public float maxVerticalOffsetDistance = 2f;
    [Tooltip("Temps en secondes avant d'activer le suivi Y hors rail.")]
    public float timeBeforeYAdjust = 1f;
    [Tooltip("Vitesse d'interpolation du Y lorsque hors-rail.")]
    public float strayYAdjustSpeed = 3f;
    [Tooltip("Offset X/Y appliqué à la caméra en mode stray (hors-rail).")]
    public Vector2 strayOffset = Vector2.zero;

    // ** Nouveau : un timer par joueur pour son dépassement vertical **
    private float[] strayTimers = new float[4];
    private bool   isStrayMode = false;
    private Camera cam;
    private Vector3 lastRightPos = Vector3.zero;
    private float lastCamX = float.NegativeInfinity;
    private float recenterTimer = 0f;
    private bool isRecentering = false;

    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam.orthographic)
            Debug.LogWarning("CameraFollow2DPerspective nécessite une caméra en perspective.");

        // Récupère automatiquement les points du rail si aucun assigné
        if (railPoints == null || railPoints.Count == 0)
        {
            railPoints = new List<Transform>();
            foreach (Transform child in transform)
                railPoints.Add(child);
        }
        if (railPoints.Count < 2)
        {
            Debug.LogError("Il faut au moins 2 points pour définir le rail !");
            enabled = false;
            return;
        }
        foreach (var pt in railPoints)
            pt.SetParent(null);

        // Initialise le FOV à la valeur minimale
        cam.fieldOfView = minZoom;
        lastRightPos = Vector3.positiveInfinity;
        lastCamX = transform.position.x;
        
        for (int i = 0; i < strayTimers.Length; i++)
            strayTimers[i] = 0f;
    }

    void LateUpdate()
    {
        var gm = GameManager.instance;
        if (gm == null) return;

        // --- 1) Mode Recording ? ---
        if (gm.recordPlayer1 && gm.player1Location != null) { FollowRecordedPlayer(gm.player1Location); return; }
        if (gm.recordPlayer2 && gm.player2Location != null) { FollowRecordedPlayer(gm.player2Location); return; }
        if (gm.recordPlayer3 && gm.player3Location != null) { FollowRecordedPlayer(gm.player3Location); return; }
        if (gm.recordPlayer4 && gm.player4Location != null) { FollowRecordedPlayer(gm.player4Location); return; }

        // --- 2) Comportement normal sur rail ---
        Transform[] locs = { gm.player1Location, gm.player2Location, gm.player3Location, gm.player4Location };
        bool[] dead     = { gm.isPlayer1Dead,   gm.isPlayer2Dead,    gm.isPlayer3Dead,    gm.isPlayer4Dead   };

        int alive = 0;
        Vector3 leftPos = Vector3.zero, rightPos = Vector3.zero;
        float minX = 0f, maxX = 0f;

        for (int i = 0; i < 4; i++)
        {
            var loc = locs[i];
            if (loc == null || dead[i]) continue;
            float x = loc.position.x;
            if (alive == 0)
            {
                minX = maxX = x;
                leftPos = rightPos = loc.position;
            }
            else
            {
                if (x < minX) { minX = x; leftPos = loc.position; }
                if (x > maxX) { maxX = x; rightPos = loc.position; }
            }
            alive++;
        }
        if (alive == 0) return;

        // --- calcul du mouvement de la caméra ---
        float camX      = transform.position.x;
        float camDeltaX = camX - lastCamX;
        bool camMovedRight = (lastCamX != float.NegativeInfinity) && (camDeltaX > 0f);
        float camSpeed = camDeltaX / Time.deltaTime;
        lastCamX = camX;

        // Target monde (équidistance ou centré si 1 joueur)
        float fullDistX = (alive > 1) ? (maxX - minX) : 0f;
        Vector3 worldTarget = (alive == 1)
            ? rightPos
            : ((leftPos + rightPos) * 0.5f + Vector3.right * (fullDistX * offsetMultiplier));

        // Projection sur le rail
        Vector3 targetEqui  = ProjectOntoRail(worldTarget);
        Vector3 targetRight = ProjectOntoRail(rightPos);

        // ---  Détection de la dérive en Y de chaque joueur  ---
        isStrayMode = false;
        float sumProjY = 0f, sumPlayerY = 0f;
        int   countStrayed = 0;

        for (int i = 0; i < 4; i++)
        {
            var loc = locs[i];
            if (loc == null || dead[i]) { strayTimers[i] = 0f; continue; }

            Vector3 proj = ProjectOntoRail(loc.position);
            float deltaY = Mathf.Abs(loc.position.y - proj.y);

            if (deltaY > maxVerticalOffsetDistance)
            {
                strayTimers[i] += Time.deltaTime;
                if (strayTimers[i] >= timeBeforeYAdjust)
                {
                    isStrayMode = true;
                    sumProjY   += proj.y;
                    sumPlayerY += loc.position.y;
                    countStrayed++;
                }
            }
            else
            {
                strayTimers[i] = 0f;
            }
        }

// Si mode stray, override du Y de targetEqui
        if (isStrayMode && countStrayed > 0)
        {
            float avgProjY   = sumProjY   / countStrayed;
            float avgPlayerY = sumPlayerY / countStrayed;
            float targetY    = (avgProjY + avgPlayerY) * 0.5f;
            targetEqui.y     = Mathf.Lerp(
                transform.position.y,
                targetY,
                strayYAdjustSpeed * Time.deltaTime
            );
        }

        
        for (int i = 0; i < GameManager.instance.players.Length; i++)
        {
            if (GameManager.instance.players[i] == null) break;

            if (i < 1) // un joueur sur le terrain
            {
                //Debug.Log("il n'y a qu'un joueur");
            }
            else // plus de un joueur sur le terrain
            {
                //Debug.Log("il y a plusieur joueur");
            }
            
            if (GameManager.instance.players[i].lastOnGroundTime > 0)
            {
                //Debug.Log("le joueur " + i + "est sur le sols"  );
            }
            else
            {
                //Debug.Log("le joueur " + i + ",'n'est pas sur le sols");
            }
            
            //Debug.Log(GameManager.instance.players[i].transform.position); // donne la position du ou des joueurs
            
            
            
        }
        
        
        
        // --- ZOOM basé sur la projection perspective ---
        if (camMovedRight)
        {
            // distance entre la caméra et le plan des joueurs (supposé z=0)
            float d = Mathf.Abs(transform.position.z);
            // FOV statique pour englober fullDistX avec marges
            float denom = 2f * d * cam.aspect * (1f - 2f * viewportMargin);
            float staticFov = (denom > 0f)
                ? 2f * Mathf.Rad2Deg * Mathf.Atan(fullDistX / denom)
                : maxZoom;
            float targetFovDist = Mathf.Clamp(staticFov, minZoom, maxZoom);

            // zoom dynamique proportionnel à la vitesse
            float dynamicFov = cam.fieldOfView + camSpeed * speedZoomMultiplier * Time.deltaTime;
            float targetFovMove = Mathf.Min(dynamicFov, maxZoom);

            // on combine les deux objectifs (le plus large FOV = zoom-out le plus fort)
            float fovGoal = Mathf.Max(targetFovDist, targetFovMove);

            cam.fieldOfView = Mathf.Lerp(
                cam.fieldOfView,
                fovGoal,
                distanceZoomSpeed * Time.deltaTime
            );
        }

        // Recentrage si besoin
        bool leftVisible    = IsWorldXInView(minX);
        bool shouldRecenter = (cam.fieldOfView >= maxZoom - 0.01f) && !leftVisible && alive > 1;
        if (shouldRecenter)
        {
            recenterTimer += Time.deltaTime;
            if (recenterTimer >= recenterDelay) isRecentering = true;
        }
        else
        {
            recenterTimer = 0f;
            isRecentering = false;
        }

        // Application de la position finale
    // Application de la position finale avec ajustement Y hors-rail
    
    // ——— Application finale lissée avec offset hors-rail ———

// 1) Récupère la position actuelle
    Vector3 curr = transform.position;

// 2) Cibles rail
    float railX = targetEqui.x;
    float railY = ProjectOntoRail(worldTarget).y;

// 3) Cibles stray (mi-chemin entre joueurs et rail)
    float strayX = ( (leftPos.x + rightPos.x) * 0.5f ) + strayOffset.x;
    float strayY = ( (sumProjY / countStrayed) + (sumPlayerY / countStrayed) ) * 0.5f + strayOffset.y;

// 4) Choix de la cible selon le mode
    float targetXoffset = isStrayMode ? strayX : railX;
    float targetYoffset = isStrayMode ? strayY : railY;
    
    targetXoffset += globalOffset.x;
    targetYoffset += globalOffset.y;

// 5) Lerp lissé en X et en Y
    float newX = Mathf.Lerp(curr.x, targetXoffset, followSpeed * Time.deltaTime);
    float newY = Mathf.Lerp(curr.y, targetYoffset, strayYAdjustSpeed * Time.deltaTime);

// 6) Application
    transform.position = new Vector3(newX, newY, curr.z);

    lastRightPos = rightPos;
    }

    private void FollowRecordedPlayer(Transform player)
    {
        Vector3 curr   = transform.position;
        Vector3 target = new Vector3(player.position.x, player.position.y, curr.z);
        transform.position = Vector3.Lerp(curr, target, recenterSpeed * Time.deltaTime);
    }

    private Vector3 ProjectOntoRail(Vector3 point)
    {
        Vector3 best = railPoints[0].position;
        float bestDist = float.MaxValue;
        for (int i = 0; i < railPoints.Count - 1; i++)
        {
            Vector3 A = railPoints[i].position;
            Vector3 B = railPoints[i + 1].position;
            Vector3 AB = B - A;
            float sq = AB.sqrMagnitude;
            if (sq < 1e-4f) continue;
            float t = Mathf.Clamp01(Vector3.Dot(point - A, AB) / sq);
            Vector3 proj = A + t * AB;
            float d2 = (point - proj).sqrMagnitude;
            if (d2 < bestDist)
            {
                bestDist = d2;
                best = proj;
            }
        }
        return best;
    }

    private bool IsWorldXInView(float x)
    {
        // demi-largeur du frustum à la profondeur z=0
        float d = Mathf.Abs(transform.position.z);
        float halfW = d * Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * cam.aspect;
        float cx    = transform.position.x;
        return x >= cx - halfW && x <= cx + halfW;
    }
}
