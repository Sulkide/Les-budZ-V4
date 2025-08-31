using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MovingPlatform : MonoBehaviour 
{ 
    [Header("Option de plateforme one way")] 
    [Tooltip("Si vrai, la plateforme permettra de passer par le bas (one way).")] 
    public bool oneWayPlatform = false;

    [Header("Option de plateforme top way")]
    [Tooltip("Si vrai, la plateforme permettra de descendre au travers depuis le dessus quand le joueur appuie vers le bas.")]
    public bool TopWayPlatform = false;

    [Header("Points de déplacement")]
    [Tooltip("Liste des points (transforms) que la plateforme doit atteindre.")]
    public List<Transform> points;

    [Header("Paramètres de déplacement")]
    [Tooltip("Vitesse de déplacement de la plateforme.")]
    public float speed = 3f;
    [Tooltip("Temps d'arrêt à chaque point avant de passer au suivant.")]
    public float waitTime = 1f;
    [Header("Décélération aux extrémités")]
    [Tooltip("Distance à partir de laquelle la plateforme commence à ralentir")]
    public float decelerationDistance = 1f;
    [Tooltip("Vitesse minimale lors de la décélération")]
    public float minDecelerationSpeed = 0.5f;

    [Header("Options de loop")]
    [Tooltip("Si vrai, la plateforme fera un aller-retour.")]
    public bool loop = false;
    [Tooltip("Si vrai et loop activé, la plateforme boucle indéfiniment.")]
    public bool loopInfinite = false;
    [Tooltip("Nombre d'aller-retour à effectuer avant de se détruire (si loop non infini).")]
    public int loopCount = 1;

    [Header("Option de destruction")]
    [Tooltip("Si vrai, la plateforme se détruira après avoir terminé son parcours.")]
    public bool destroyOnFinish = false;

    [Header("Option de portage des objets")]
    [Tooltip("Si vrai, le tag de l'objet sera changé en \"MovingPlatform\", sinon il sera \"Untagged\".")]
    public bool carryObjects = false;

    [Header("Option de plateforme tombante")]
    [Tooltip("Si vrai, la plateforme deviendra tombante après contact avec le joueur.")]
    public bool fallingPlatform = false;
    [Tooltip("Délai avant que la plateforme ne commence à vibrer puis à tomber.")]
    public float fallDelay = 2f;
    [Tooltip("Durée de la vibration avant la chute.")]
    public float vibrationDuration = 0.5f;
    [Tooltip("Intensité de la vibration.")]
    public float vibrationMagnitude = 0.1f;

    [Header("Option de réapparition pour plateforme tombante")]
    [Tooltip("Si vrai, la plateforme réapparaîtra après être tombée.")]
    public bool respawnAfterFall = true;
    [Tooltip("Délai avant que la plateforme réapparaisse après être tombée.")]
    public float respawnDelay = 2f;

    [Header("Option destructible")]
    [Tooltip("Si vrai, l'objet est destructible lorsqu'il est percuté par un Player avec le tag 'Dash'.")]
    public bool destructible = false;
    [Tooltip("GameObject à désactiver lorsque l'objet est détruit.")]
    public GameObject objectToDisable;
    [Tooltip("GameObject à activer lorsque l'objet est détruit.")]
    public GameObject objectToActivate;
    [Tooltip("Délai avant la destruction après collision.")]
    public float destructionDelay = 1f;
    [Tooltip("Force appliquée pour propulser les enfants activés.")]
    public float destructibleForce = 5f;

    [Header("Option de démarrage sur contact")]
    [Tooltip("Si vrai, la plateforme commencera à bouger lorsqu'un joueur entre en contact avec elle.")]
    public bool startOnContact = false;

    // Variables internes pour la gestion du mouvement et de la vibration
    private Vector3 basePosition;
    private Vector3 lastBasePosition;  // Position de base fournie par le mouvement de la plateforme
    private bool isVibrating = false;
    private Vector3 vibrationOffset = Vector3.zero;

    // Compteur de boucles complètes (aller-retour)
    private int loopsDone = 0;
    // Flag indiquant que la chute a commencé et que la physique doit prendre le relais
    private bool fallingStarted = false;
    // Pour éviter de lancer plusieurs fois la coroutine de chute
    private bool fallCoroutineStarted = false;
    // Pour sauvegarder les contraintes initiales du Rigidbody2D (utilisées lors du respawn)
    private RigidbodyConstraints2D initialConstraints;

    private Vector3 initialPosition;
    // La position de base utilisée par le mouvement de la plateforme

    private Rigidbody2D rb;

    // Pour éviter de réappliquer IgnoreCollision à chaque frame sur le même collider
    private HashSet<Collider2D> ignoredColliders = new HashSet<Collider2D>();

    // Flag pour s'assurer que le mouvement est démarré une seule fois
    private bool movementStarted = false;

    private void Start()
    {
        // Si destructible, activer/désactiver les GameObjects correspondants dès le départ
        if (destructible)
        {
            if (objectToDisable != null)
                objectToDisable.SetActive(false);
            if (objectToActivate != null)
                objectToActivate.SetActive(true);
        }
        
        // Sauvegarder la position initiale et l'utiliser comme basePosition
        initialPosition = transform.position;
        basePosition = initialPosition;

        // Récupérer le Rigidbody2D et sauvegarder ses contraintes initiales
        rb = GetComponent<Rigidbody2D>();
        initialConstraints = rb.constraints;

        // Changer le tag de l'objet en fonction de la valeur de carryObjects
        gameObject.tag = carryObjects ? "MovingPlatform" : "Untagged";

        // Sauvegarde de la position de départ
        basePosition = transform.position;
        lastBasePosition = basePosition;
        
        // Configuration de l'effector pour oneWayPlatform (si true, la plateforme sera one-way et permettra de passer par le bas)
        if (oneWayPlatform)
        {
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.usedByEffector = true;
            }
            else
            {
                Debug.LogWarning("Aucun Collider2D trouvé pour configurer la plateforme one way.");
            }
            if (GetComponent<PlatformEffector2D>() == null)
            {
                gameObject.AddComponent<PlatformEffector2D>();
            }
        }
        else
        {
            // Si oneWayPlatform est false, on s'assure que l'effector n'est pas utilisé,
            // de sorte que la plateforme reste solide par dessous.
            Collider2D collider = GetComponent<Collider2D>();
            if (collider != null)
            {
                collider.usedByEffector = false;
            }
        }

        // Pour l'option TopWayPlatform, on active le drop-through depuis le dessus
        // sans modifier la solidité par dessous si oneWayPlatform est false.
        if (TopWayPlatform)
        {
            if (oneWayPlatform)
            {
                // L'effector est déjà configuré par oneWayPlatform.
            }
            else
            {
                // On ne modifie pas le comportement par défaut du collider pour éviter le passage par dessous.
                Debug.Log("TopWayPlatform activé sans OneWayPlatform : la plateforme restera solide par le bas et n'autorisera le drop-through que depuis le dessus.");
            }
        }
        
        if (!loop && (points == null || points.Count < 1))
        {
            Debug.LogWarning("Il faut au moins un point dans la liste pour déplacer la plateforme." + GameManager.instance.realTime);
            return;
        }

        // Si l'option loop est activée, ajouter la position de spawn en tête du chemin.
        if (loop)
        {
            GameObject spawnPoint = new GameObject("SpawnPoint");
            spawnPoint.transform.position = transform.position;
            spawnPoint.hideFlags = HideFlags.HideInHierarchy;

            List<Transform> newPoints = new List<Transform>();
            newPoints.Add(spawnPoint.transform);
            if (points != null)
                newPoints.AddRange(points);
            points = newPoints;
        }
        else
        {
            // Positionner la basePosition sur le premier point
            basePosition = points[0].position;
        }

        // Démarrer la coroutine de déplacement immédiatement si l'option StartOnContact est désactivée.
        if (!startOnContact)
        {
            movementStarted = true;
            StartCoroutine(MovePlatform());
        }
    }

    // Dans Update, la position affichée est la somme de la basePosition et de l'offset de vibration (si actif)
    private void Update()
    {
        if (!fallingStarted)
        {
            transform.position = basePosition + (isVibrating ? vibrationOffset : Vector3.zero);
        }
    }

    // Gestion des collisions (pour options destructible, plateforme tombante et démarrage sur contact)
    private void OnCollisionEnter2D(Collision2D collision)
    {
        // Si l'option StartOnContact est activée, démarrer le déplacement lors du contact avec le joueur
        if (startOnContact && !movementStarted && collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            movementStarted = true;
            StartCoroutine(MovePlatform());
        }

        if (destructible)
        {
            if (collision.gameObject.layer == LayerMask.NameToLayer("Player"))
            {
                PlayerMovement playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
                if (playerMovement != null && playerMovement.isDashing)
                {
                    Debug.Log("Destructible");
                    gameObject.layer = LayerMask.NameToLayer("Default");

                    Vector2 pushDirection = playerMovement.moveInput;

                    foreach (Transform child in objectToActivate.transform)
                    {
                        Rigidbody2D childRb = child.GetComponent<Rigidbody2D>();
                        if (childRb != null)
                        {
                            float randX = Random.Range(0, 3);
                            float randY = Random.Range(0, 3);
                            float rand = Random.Range(0, 20);
                            Vector2 randVec = new Vector2(randX, randY);
                            
                            childRb.constraints = RigidbodyConstraints2D.None;
                            childRb.AddForce((pushDirection + randVec) * (destructibleForce + rand), ForceMode2D.Impulse);
                            
                            // Appliquer une rotation sur l'axe Z avec une vitesse aléatoire
                            float rotationSpeed = Random.value < 0.5f ? rand : -rand;
                            childRb.angularVelocity = rotationSpeed;
                        }
                    }
                    StartCoroutine(DestroyAfterDelay());
                    return;
                }
            }
        }
        if (fallingPlatform && !fallCoroutineStarted && collision.gameObject.layer == LayerMask.NameToLayer("Player"))
        {
            fallCoroutineStarted = true;
            StartCoroutine(FallPlatform());
        }
    }

    // --- Gestion de collision pour TopWayPlatform --- 
    // Permet au joueur de passer au travers par le haut lorsqu'il appuie vers le bas,
    // sans affecter la solidité par dessous si oneWayPlatform est false.
    private void OnCollisionStay2D(Collision2D collision)
    {
        if (TopWayPlatform)
        {
            PlayerMovement playerMovement = collision.gameObject.GetComponent<PlayerMovement>();
            if (playerMovement != null && playerMovement.moveInput.y < -0.5f)
            {
                // Vérifier que le joueur se trouve au-dessus de la plateforme
                if (collision.transform.position.y > transform.position.y)
                {
                    Collider2D platformCollider = GetComponent<Collider2D>();
                    Collider2D playerCollider = collision.collider;
                    if (platformCollider != null && playerCollider != null && !ignoredColliders.Contains(playerCollider))
                    {
                        Physics2D.IgnoreCollision(platformCollider, playerCollider, true);
                        ignoredColliders.Add(playerCollider);
                        StartCoroutine(ReenableCollision(platformCollider, playerCollider, 0.5f));
                    }
                }
            }
        }
    }

    // Réactive la collision après un délai et retire le collider de la liste
    IEnumerator ReenableCollision(Collider2D platformCollider, Collider2D playerCollider, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (platformCollider != null && playerCollider != null)
        {
            Physics2D.IgnoreCollision(platformCollider, playerCollider, false);
            ignoredColliders.Remove(playerCollider);
        }
    }
    // --- Fin de la gestion TopWayPlatform ---

    // Coroutine qui gère le déplacement selon les points
    IEnumerator MovePlatform()
    {
        int index = 0;
        int direction = 1;            // 1 = vers la fin, -1 = vers le début
        bool didFullRoundTrip = false; // ← déclarer ici
        loopsDone = 0;                // remets à zéro si besoin

        while (true)
        {
            if (fallingStarted)
                yield break;

            Transform target = points[index];

            float distance;
            while ((distance = Vector3.Distance(basePosition, target.position)) > 0.01f)
            {
                if (fallingStarted)
                    yield break;

                // Sauvegarde de l'ancienne position
                lastBasePosition = basePosition;

                // Calcul de la vitesse courante
                float currentSpeed = speed;
                bool isEndPoint = (index == 0 || index == points.Count - 1);
                if (isEndPoint && distance < decelerationDistance)
                {
                    float t = distance / decelerationDistance; 
                    currentSpeed = Mathf.Lerp(minDecelerationSpeed, speed, t);
                }

                // Déplacement
                basePosition = Vector3.MoveTowards(
                    basePosition,
                    target.position,
                    currentSpeed * Time.deltaTime
                );

                // Mise à jour du tag selon le delta de mouvement
                Vector3 delta = basePosition - lastBasePosition;
                if (Mathf.Approximately(delta.x, 0f) && !Mathf.Approximately(delta.y, 0f))
                {
                    // Mouvement purement vertical
                    gameObject.tag = "Untagged";
                }
                else if (!Mathf.Approximately(delta.x, 0f))
                {
                    // Mouvement ayant un composant horizontal
                    gameObject.tag = carryObjects ? "MovingPlatform" : "Untagged";
                }

                yield return null;
            }

            // On aligne parfaitement
            basePosition = target.position;
            yield return new WaitForSeconds(waitTime);

            // --- Gestion du ping-pong / loop ---
            if (loop)
            {
                if (index == points.Count - 1)
                {
                    direction = -1;
                }
                else if (index == 0 && direction == -1)
                {
                    // on vient de faire un aller-retour complet
                    didFullRoundTrip = true;
                    loopsDone++;

                    if (!loopInfinite && loopsDone >= loopCount)
                    {
                        if (startOnContact)
                        {
                            movementStarted = false;
                            yield break;
                        }
                        if (destroyOnFinish)
                            Destroy(gameObject);
                        yield break;
                    }

                    direction = 1;
                }

                index += direction;
            }
            else
            {
                // sans loop : on parcourt une seule fois
                if (index < points.Count - 1)
                {
                    index++;
                }
                else
                {
                    if (startOnContact)
                    {
                        movementStarted = false;
                        yield break;
                    }
                    if (destroyOnFinish)
                        Destroy(gameObject);
                    yield break;

                }

             
            }
        }
    }


    // Coroutine gérant la vibration puis la chute
    IEnumerator FallPlatform()
    {
        yield return new WaitForSeconds(fallDelay);
        
        isVibrating = true;
        float elapsed = 0f;
        while (elapsed < vibrationDuration)
        {
            vibrationOffset = new Vector3(Random.Range(-vibrationMagnitude, vibrationMagnitude),
                                          Random.Range(-vibrationMagnitude, vibrationMagnitude),
                                          0);
            elapsed += Time.deltaTime;
            yield return null;
        }
  
        isVibrating = false;
        vibrationOffset = Vector3.zero;
        fallingStarted = true;

        if (rb != null)
        {
            gameObject.layer = LayerMask.NameToLayer("Phantom");
            rb.constraints = RigidbodyConstraints2D.None;
        }
        
        if (respawnAfterFall)
        {
            StartCoroutine(RespawnPlatform());
        }
    }

    // Coroutine de respawn qui réinitialise la position, les contraintes et la rotation Z
    IEnumerator RespawnPlatform()
    {
        yield return new WaitForSeconds(respawnDelay);
        basePosition = initialPosition;
        transform.position = initialPosition;
        transform.rotation = Quaternion.Euler(0, 0, 0);

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.angularVelocity = 0f;
            rb.constraints = initialConstraints;
        }

        fallingStarted = false;
        fallCoroutineStarted = false;
        loopsDone = 0;
        
        gameObject.layer = LayerMask.NameToLayer("Ground");
        
        // Au respawn, si l'option StartOnContact est désactivée, redémarrer le mouvement,
        // sinon, attendre un nouveau contact avec le joueur.
        if (!startOnContact)
        {
            movementStarted = true;
            StartCoroutine(MovePlatform());
        }
        else
        {
            movementStarted = false;
        }
    }

    // Coroutine de destruction différée
    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(destructionDelay);
        Destroy(gameObject);
    }
}
