using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using System.IO;
using System;
using TMPro;


public class GameManager : MonoBehaviour
{
    public static GameManager instance;
    
    
    
    
    [SerializeField] private PauseMenu pauseMenu;

    public int fileID;
    
    Camera mainCamera;
    private float elapsedTime = 0f;      
    public string gameTime = "00:00:00";
    public TMP_Text gameTimeText;


    public string realTime = "00:00:00";
    public TMP_Text realTimeText;
    private int lastSecond = -1;

    
    public string currentSceneName;
    
    public bool isPaused;

    public Transform respawnPoint;
    public string respawnPointName = "RespawnPoint (1)";
    public Vector3 checkPointPosition;
    public bool isCheckPointReached;

    public int maxScoreInLevel;
    public int currentMaxScoreInLevel;

    public int maxBluePrintInLevel;
    public int currentBluePrintInLevel;

    public bool recordPlayer1;
    public bool recordPlayer2;
    public bool recordPlayer3;
    public bool recordPlayer4;

    public int XP;

    public int Score;

    public int BluePrint;

    public int player1BluePrint;
    public int player2BluePrint;
    public int player3BluePrint;
    public int player4BluePrint;

    public int currentBluePrint;
    public int currentPlayer1BluePrint;
    public int currentPlayer2BluePrint;
    public int currentPlayer3BluePrint;
    public int currentPlayer4BluePrint;

    public int player1Score;
    public int player2Score;
    public int player3Score;
    public int player4Score;

    public int currentScore;
    public int currentPlayer1Score;
    public int currentPlayer2Score;
    public int currentPlayer3Score;
    public int currentPlayer4Score;

    public List<string> collectedCollectibles = new List<string>();

    // Liste temporaire qui stocke les collectibles récupérés depuis le dernier checkpoint
    public List<string> tempCollectedCollectibles = new List<string>();

    public List<string> collectedBluePrint = new List<string>();

    // Liste temporaire qui stocke les collectibles récupérés depuis le dernier checkpoint
    public List<string> tempCollectedBluePrint = new List<string>();
    
    public List<string> collectedBluePrintInTotal = new List<string>();

    public string currentMusicName;
    
    public bool newSaveFileLoaded = false;
    
    public bool newSceneLoad = true;
    
    public bool newSaveFileCreated = false;
    
    
    //public TMP_Text ScoreText;

    public int deathScore;
    public int player1DeathScore;
    public int player2DeathScore;
    public int player3DeathScore;
    public int player4DeathScore;

    public int killScore;
    public int player1KillScore;
    public int player2KillScore;
    public int player3KillScore;
    public int player4KillScore;

    public int player1MissJump;
    public int player2MissJump;
    public int player3MissJump;
    public int player4MissJump;
    
    public PlayerInputManager playerInputManager;
    public List<GameObject> playerPrefabs;
    public Transform parentForPlayers;
    private int nextPrefabIndex = 0;

    public bool isSulkidePresent;
    public bool isDarckoxPresent;
    public bool isSulanaPresent;
    public bool isSlowPresent;

    public bool isPlayer1present = false;
    public bool isPlayer2present = false;
    public bool isPlayer3present = false;
    public bool isPlayer4present = false;

    public PlayerMovement[] players = new PlayerMovement[4];

    public Transform player1Location;
    public Transform player2Location;
    public Transform player3Location;
    public Transform player4Location;

    public bool player1Bonus;
    public bool player2Bonus;
    public bool player3Bonus;
    public bool player4Bonus;

    public bool player1CurrentBonus;
    public bool player2CurrentBonus;
    public bool player3CurrentBonus;
    public bool player4CurrentBonus;

    public bool isPlayer1Dead;
    public bool isPlayer2Dead;
    public bool isPlayer3Dead;
    public bool isPlayer4Dead;

    public enum playerCharacterIs
    {
        Sulkide,
        Darckox,
        MrSlow,
        Sulana,
        none
    }

    public playerCharacterIs player1Is = playerCharacterIs.none;
    public playerCharacterIs player2Is = playerCharacterIs.none;
    public playerCharacterIs player3Is = playerCharacterIs.none;
    public playerCharacterIs player4Is = playerCharacterIs.none;

    public int test = 1;

    
    
    void Start()
    {
        mainCamera = Camera.main;
        
        Screen.SetResolution(3840, 2160, FullScreenMode.FullScreenWindow);

        if (!playerInputManager)
        {
            playerInputManager = GetComponent<PlayerInputManager>();
        }
        
        if (realTimeText == null)
        {
            Debug.LogError("Clock text reference is missing.");
            return;
        }

        UpdateDisplay(DateTime.Now);
        // On peut démarrer une coroutine pour mise à jour efficace
        StartCoroutine(RefreshLoop());
        
        System.Collections.IEnumerator RefreshLoop()
        {
            while (true)
            {
                DateTime now = DateTime.Now; // heure locale de l’ordinateur
                if (now.Second != lastSecond)
                {
                    UpdateDisplay(now);
                    lastSecond = now.Second;
                }

                // Petite attente pour ne pas surcharger ; on utilise realtime pour ignorer timeScale
                yield return new WaitForSecondsRealtime(0.1f);
            }
        }
        
    }

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (newSaveFileCreated)
        {
            SoundManager.Instance.PlayMusic("Bourée (Steven Wilson Remix)");
            
            newSaveFileCreated = false;
        }
        
        if (newSaveFileLoaded)
        {
            SoundManager.Instance.PlayMusic(currentMusicName);
            Debug.Log("test0");
            
            newSaveFileLoaded = false;
        }
        
        if (newSceneLoad && !newSaveFileLoaded)
        {
            if (currentMaxScoreInLevel > 0)
            {
                maxScoreInLevel = currentMaxScoreInLevel;
            }
            else
            {
                MaxScoreLevel();
            }

            if (currentBluePrintInLevel > 0)
            {
                maxBluePrintInLevel = currentBluePrintInLevel;
            }
            else
            {
                MaxBluePrintLevel();
            }

            if (SoundManager.Instance.gameObject.transform.GetChild(0).gameObject.GetComponent<AudioSource>().clip != null)
            {
                currentMusicName = SoundManager.Instance.gameObject.transform.GetChild(0).gameObject.GetComponent<AudioSource>().clip.name;
            }
            else
            {
                currentMusicName = "EMPTY";
            }
            
            Debug.Log("test1");
            
            newSceneLoad = false;
        }
        
        Debug.Log(currentMusicName);
        
        ResetElementsOnLoad();
        ForcePlayerJoin();
        SpawnPlayers();

        Debug.Log("test2");
        
        // À chaque rechargement de scène, détruit les collectibles dont l'ID figure dans la liste permanente
        Collectible[] collectibles = FindObjectsOfType<Collectible>();
        foreach (Collectible c in collectibles)
        {
            if (collectedCollectibles.Contains(c.uniqueId))
            {
                Destroy(c.gameObject);
            }
        }

        BluePrint[] BluePrints = FindObjectsOfType<BluePrint>();
        foreach (BluePrint c in BluePrints)
        {
            if (collectedBluePrint.Contains(c.uniqueId))
            {
                Destroy(c.gameObject);
            }
        }
        
        currentSceneName = scene.name;
        
        if (fileID != 0)
        {
            Save(fileID);
        }
        
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            ReloadScene();
        }
        
        
        
        UpdatePlayTime();

        //ScoreText.text = Score.ToString();

        if (player1Location)
        {
            isPlayer1present = true;
        }

        if (player2Location)
        {
            isPlayer2present = true;
        }

        if (player3Location)
        {
            isPlayer3present = true;
        }

        if (player4Location)
        {
            isPlayer4present = true;
        }
    }

    private void SpawnPlayers()
    {
        Vector3 spawnPosition = isCheckPointReached ? checkPointPosition : respawnPoint.position;
        
        foreach (var player in players)
        {
            if (player)
            {
                player.transform.position = spawnPosition;
            }
        }
    }

    public void ChangeScene(string sceneName, int respawnPointNameNumber)
    {
        
        
        isCheckPointReached = false;
        respawnPointName = "RespawnPoint (" + respawnPointNameNumber + ")";
        DestroyChildrenOfParentForPlayers();
        AllPlayerDeathReset();
        resetScoreToZero();
        foreach (string id in tempCollectedCollectibles)
        {
            if (!collectedCollectibles.Contains(id))
            {
                collectedCollectibles.Add(id);
            }
        }

        foreach (string id in tempCollectedBluePrint)
        {
            if (!collectedBluePrint.Contains(id))
            {
                collectedBluePrint.Add(id);
            }
        }

        AssigneBonusHealth();

        maxScoreInLevel = 0;
        maxBluePrintInLevel = 0;
        currentBluePrintInLevel = 0;
        currentMaxScoreInLevel = 0;
        
        foreach (string id in tempCollectedBluePrint)
        {
            if (!collectedBluePrintInTotal.Contains(id))
            {
                collectedBluePrintInTotal.Add(id);
            }
        }
        
        foreach (string id in collectedBluePrint)
        {
            if (!collectedBluePrintInTotal.Contains(id))
            {
                collectedBluePrintInTotal.Add(id);
            }
        }
        
        tempCollectedBluePrint.Clear();
        tempCollectedCollectibles.Clear();
        collectedBluePrint.Clear();

        
        newSceneLoad = true;
        
        
        SceneManager.LoadScene(sceneName);
        
        Debug.Log("test3");
        
    }

    public void UpdateCheckPoint(Vector3 checkPointPosition)
    {
        this.checkPointPosition = checkPointPosition;
        isCheckPointReached = true;
        
        // Lorsque le checkpoint est atteint, transfère les collectibles collectés temporairement dans la liste permanente
        foreach (string id in tempCollectedCollectibles)
        {
            if (!collectedCollectibles.Contains(id))
            {
                collectedCollectibles.Add(id);
            }
        }

        foreach (string id in tempCollectedBluePrint)
        {
            if (!collectedBluePrint.Contains(id))
            {
                collectedBluePrint.Add(id);
            }
        }

        AssigneBonusHealth();
        assigneScore();
        AssigneBluePrint();
        
        currentMusicName = SoundManager.Instance.gameObject.transform.GetChild(0).gameObject.GetComponent<AudioSource>().clip.name;
        
        // Vide la liste temporaire après le transfert
        tempCollectedBluePrint.Clear();
        tempCollectedCollectibles.Clear();
        
        Save(fileID);
    }

    public void ReloadScene()
    {
        currentMaxScoreInLevel = maxScoreInLevel;
        currentBluePrintInLevel = maxBluePrintInLevel;
        resetAllScoreLevel();
        resetAllBluePrintScore();
        tempCollectedBluePrint.Clear();
        tempCollectedCollectibles.Clear();
        respawnPointName = "RespawnPoint (1)";
        string currentSceneName = SceneManager.GetActiveScene().name;
        DestroyChildrenOfParentForPlayers();
        AllPlayerDeathReset();
        SceneManager.LoadScene(currentSceneName);
    }

    public void DestroyChildrenOfParentForPlayers()
    {
        if (parentForPlayers == null)
        {
            Debug.LogWarning("parentForPlayers n'est pas défini.");
            return;
        }

        for (int i = parentForPlayers.childCount - 1; i >= 0; i--)
        {
            Destroy(parentForPlayers.GetChild(i).gameObject);
        }
    }

    void ResetElementsOnLoad()
    {
        parentForPlayers = null;
        
        GameObject respawnRef = FindObjectByName(respawnPointName);
        if (respawnRef)
        {
            respawnPoint = respawnRef.transform;
        }

        GameObject AllParentRef = FindObjectByName("AllPlayer");
        if (AllParentRef)
        {
            parentForPlayers = AllParentRef.transform;
        }


        if (isPlayer1present)
        {
            player1Location = respawnPoint?.transform;
        }
    }

    private GameObject FindObjectByName(string objectName)
    {
        GameObject foundObject = GameObject.Find(objectName);
        if (foundObject == null)
        {
            Debug.LogWarning("L'objet '" + objectName + "' n'a pas été trouvé dans la scène.");
        }

        return foundObject;
    }


    void ForcePlayerJoin()
    {
        test += 1;

        playerInputManager.playerPrefab = null;
        nextPrefabIndex = 0;

        if (isPlayer1present)
        {
            playerInputManager.playerPrefab = playerPrefabs[0];

            players[0] = Instantiate(playerInputManager.playerPrefab, respawnPoint.position, Quaternion.identity)
                .GetComponentInChildren<PlayerMovement>();

            playerInputManager.playerPrefab = playerPrefabs[1];
            nextPrefabIndex = 2;
        }

        if (isPlayer2present)
        {
            playerInputManager.playerPrefab = playerPrefabs[1];
            players[1] = Instantiate(playerInputManager.playerPrefab, respawnPoint.position, Quaternion.identity)
                .GetComponentInChildren<PlayerMovement>();
            playerInputManager.playerPrefab = playerPrefabs[2];
            nextPrefabIndex = 3;
        }

        if (isPlayer3present)
        {
            playerInputManager.playerPrefab = playerPrefabs[2];
            players[2] = Instantiate(playerInputManager.playerPrefab, respawnPoint.position, Quaternion.identity)
                .GetComponentInChildren<PlayerMovement>();
            playerInputManager.playerPrefab = playerPrefabs[3];
            nextPrefabIndex = 4;
        }

        if (isPlayer4present)
        {
            playerInputManager.playerPrefab = playerPrefabs[3];
            players[3] = Instantiate(playerInputManager.playerPrefab, respawnPoint.position, Quaternion.identity)
                .GetComponentInChildren<PlayerMovement>();
            playerInputManager.playerPrefab = playerPrefabs[4];
            nextPrefabIndex = 5;
        }

        //playerInputManager.onPlayerJoined += OnPlayerJoined;
    }

    public void UpdatePlayers()
    {
        
    }

    void OnPlayerJoined(PlayerInput player)
    {
        if (playerPrefabs.Count > 0)
        {
            playerInputManager.playerPrefab = playerPrefabs[nextPrefabIndex];
            nextPrefabIndex = (nextPrefabIndex + 1) % playerPrefabs.Count;
        }

        if (parentForPlayers != null)
        {
            player.transform.SetParent(parentForPlayers);

            if (isPlayer1Dead == false && isPlayer1present)
            {
                player.transform.position = player1Location.position;
            }
            else if (isPlayer2Dead == false && isPlayer2present)
            {
                player.transform.position = player2Location.position;
            }
            else if (isPlayer3Dead == false && isPlayer3present)
            {
                player.transform.position = player3Location.position;
            }
            else if (isPlayer4Dead == false && isPlayer4present)
            {
                player.transform.position = player4Location.position;
            }
            else
            {
                player.transform.position = respawnPoint.position;
            }
        }
    }

    public void addScore(int score, string playerName)
    {
        Score += score;

        if (playerName == "Player 1" || playerName == "Player 1(Clone)")
        {
            player1Score += score;
        }

        if (playerName == "Player 2" || playerName == "Player 2(Clone)")
        {
            player2Score += score;
        }

        if (playerName == "Player 3" || playerName == "Player 3(Clone)")
        {
            player3Score += score;
        }

        if (playerName == "Player 4" || playerName == "Player 4(Clone)")
        {
            player4Score += score;
        }
    }

    public void addBluPrint(int score, string playerName)
    {
        BluePrint += score;

        if (playerName == "Player 1" || playerName == "Player 1(Clone)")
        {
            player1BluePrint += score;
        }

        if (playerName == "Player 2" || playerName == "Player 2(Clone)")
        {
            player2BluePrint += score;
        }

        if (playerName == "Player 3" || playerName == "Player 3(Clone)")
        {
            player3BluePrint += score;
        }

        if (playerName == "Player 4" || playerName == "Player 4(Clone)")
        {
            player4BluePrint += score;
        }
    }


    public void addDeathScore(int score, string playerName)
    {
        deathScore += score;
        if (playerName == "Player 1" || playerName == "Player 1(Clone)")
        {
            player1DeathScore += score;
        }

        if (playerName == "Player 2" || playerName == "Player 2(Clone)")
        {
            player2DeathScore += score;
        }

        if (playerName == "Player 3" || playerName == "Player 3(Clone)")
        {
            player3DeathScore += score;
        }

        if (playerName == "Player 4" || playerName == "Player 4(Clone)")
        {
            player4DeathScore += score;
        }
    }

    public void addKillScore(int score, string playerName)
    {
        killScore += score;
        if (playerName == "Player 1" || playerName == "Player 1(Clone)")
        {
            player1KillScore += score;
        }

        if (playerName == "Player 2" || playerName == "Player 2(Clone)")
        {
            player2KillScore += score;
        }

        if (playerName == "Player 3" || playerName == "Player 3(Clone)")
        {
            player3KillScore += score;
        }

        if (playerName == "Player 4" || playerName == "Player 4(Clone)")
        {
            player4KillScore += score;
        }
    }

    public void addMissJumpScore(int score, string playerName)
    {
        if (playerName == "Player 1" || playerName == "Player 1(Clone)")
        {
            player1MissJump += score;
        }

        if (playerName == "Player 2" || playerName == "Player 2(Clone)")
        {
            player2MissJump += score;
        }

        if (playerName == "Player 3" || playerName == "Player 3(Clone)")
        {
            player3MissJump += score;
        }

        if (playerName == "Player 4" || playerName == "Player 4(Clone)")
        {
            player4MissJump += score;
        }
    }

    public void addXP(int xp)
    {
        XP += xp;
    }

    public void addOrRemovePlayerBonus(string parentName, bool hasBonus)
    {
        if (parentName == "Player 1" || parentName == "Player 1(Clone)")
        {
            player1Bonus = hasBonus;
        }

        if (parentName == "Player 2" || parentName == "Player 2(Clone)")
        {
            player2Bonus = hasBonus;
        }

        if (parentName == "Player 3" || parentName == "Player 3(Clone)")
        {
            player3Bonus = hasBonus;
        }

        if (parentName == "Player 4" || parentName == "Player 4(Clone)")
        {
            player4Bonus = hasBonus;
        }
    }

    public void FindPlayer(string parentName, Transform playerLoc, PlayerMovement playerMovement)
    {
        if (parentName == "Player 1" || parentName == "Player 1(Clone)")
        {
            player1Location = playerLoc;
            players[0] = playerMovement;
        }

        if (parentName == "Player 2" || parentName == "Player 2(Clone)")
        {
            player2Location = playerLoc;
            players[1] = playerMovement;
        }

        if (parentName == "Player 3" || parentName == "Player 3(Clone)")
        {
            player3Location = playerLoc;
            players[2] = playerMovement;
        }

        if (parentName == "Player 4" || parentName == "Player 4(Clone)")
        {
            player4Location = playerLoc;
            players[3] = playerMovement;
        }
    }

    public void PlayerDeadCheck(string parentName, bool isDead)
    {
        if (parentName == "Player 1" || parentName == "Player 1(Clone)")
        {
            isPlayer1Dead = isDead;
        }

        if (parentName == "Player 2" || parentName == "Player 2(Clone)")
        {
            isPlayer2Dead = isDead;
        }

        if (parentName == "Player 3" || parentName == "Player 3(Clone)")
        {
            isPlayer3Dead = isDead;
        }

        if (parentName == "Player 4" || parentName == "Player 4(Clone)")
        {
            isPlayer4Dead = isDead;
        }
    }


    public void AllPlayerDeathReset()
    {
        isPlayer1Dead = false;
        isPlayer2Dead = false;
        isPlayer3Dead = false;
        isPlayer4Dead = false;
    }

    public void CharacterCheck(string parentName, string characterName)
    {
        if (parentName == "Player 1" || parentName == "Player 1(Clone)")
        {
            if (characterName == "Sulkide")
            {
                player1Is = playerCharacterIs.Sulkide;
            }
            else if (characterName == "Darckox")
            {
                player1Is = playerCharacterIs.Darckox;
            }
            else if (characterName == "Mr Slow")
            {
                player1Is = playerCharacterIs.MrSlow;
            }
            else if (characterName == "Sulana")
            {
                player1Is = playerCharacterIs.Sulana;
            }
        }

        if (parentName == "Player 2" || parentName == "Player 2(Clone)")
        {
            if (characterName == "Sulkide")
            {
                player2Is = playerCharacterIs.Sulkide;
            }
            else if (characterName == "Darckox")
            {
                player2Is = playerCharacterIs.Darckox;
            }
            else if (characterName == "Mr Slow")
            {
                player2Is = playerCharacterIs.MrSlow;
            }
            else if (characterName == "Sulana")
            {
                player2Is = playerCharacterIs.Sulana;
            }
        }

        if (parentName == "Player 3" || parentName == "Player 3(Clone)")
        {
            if (characterName == "Sulkide")
            {
                player3Is = playerCharacterIs.Sulkide;
            }
            else if (characterName == "Darckox")
            {
                player3Is = playerCharacterIs.Darckox;
            }
            else if (characterName == "Mr Slow")
            {
                player3Is = playerCharacterIs.MrSlow;
            }
            else if (characterName == "Sulana")
            {
                player3Is = playerCharacterIs.Sulana;
            }
        }

        if (parentName == "Player 4" || parentName == "Player 4(Clone)")
        {
            if (characterName == "Sulkide")
            {
                player4Is = playerCharacterIs.Sulkide;
            }
            else if (characterName == "Darckox")
            {
                player4Is = playerCharacterIs.Darckox;
            }
            else if (characterName == "Mr Slow")
            {
                player4Is = playerCharacterIs.MrSlow;
            }
            else if (characterName == "Sulana")
            {
                player4Is = playerCharacterIs.Sulana;
            }
        }
    }

    public void MakePlayerInvinsible()
    {
        foreach (var player in players)
        {
            if (!player) continue;
            player.gameObject.layer = LayerMask.NameToLayer("Default");
            player.areControllsRemoved = true;
        }
    }
    
    public void MakePlayerInvisible()
    {
        foreach (var player in players)
        {
            if (!player) continue;
            player.gameObject.layer = LayerMask.NameToLayer("Default");
            mainCamera.gameObject.GetComponent<CameraFollow2D>().enabled = false;
            player.gameObject.transform.GetChild(0).gameObject.SetActive(false);
            player.areControllsRemoved = true;
        }
    }
    
    public void MakePlayervisible()
    {
        foreach (var player in players)
        {
            if (!player) continue;
            player.gameObject.layer = LayerMask.NameToLayer("Player");
            mainCamera.gameObject.GetComponent<CameraFollow2D>().enabled = true;
            player.gameObject.transform.GetChild(0).gameObject.SetActive(true);
            player.areControllsRemoved = false;
        }
    }

    public void resetAllScoreLevel()
    {
        Score = currentScore;
        player1Score = currentPlayer1Score;
        player2Score = currentPlayer2Score;
        player3Score = currentPlayer3Score;
        player4Score = currentPlayer4Score;
    }

    public void resetAllBluePrintScore()
    {
        BluePrint = currentBluePrint;

        player1BluePrint = currentPlayer1BluePrint;
        player2BluePrint = currentPlayer2BluePrint;
        player3BluePrint = currentPlayer3BluePrint;
        player4BluePrint = currentPlayer4BluePrint;
    }

    public void resetScoreToZero()
    {
        Score = 0;
        player1Score = 0;
        player2Score = 0;
        player3Score = 0;
        player4Score = 0;
        
        currentScore = 0;
        currentPlayer1Score = 0;
        currentPlayer2Score = 0;
        currentPlayer3Score = 0;
        currentPlayer4Score = 0;
        
        BluePrint = 0;
        player1BluePrint = 0;
        player2BluePrint = 0;
        player3BluePrint = 0;
        player4BluePrint = 0;

        currentBluePrint = 0;
        currentPlayer1Score = 0;
        currentPlayer2Score = 0;
        currentPlayer3Score = 0;
        currentPlayer4Score = 0;

    }

    public void assigneScore()
    {
        currentScore = Score;
        currentPlayer1Score = player1Score;
        currentPlayer2Score = player2Score;
        currentPlayer3Score = player3Score;
        currentPlayer4Score = player4Score;
    }

    public void AssigneBluePrint()
    {
        currentBluePrint = BluePrint;
        currentPlayer1BluePrint = player1BluePrint;
        currentPlayer2BluePrint = player2BluePrint;
        currentPlayer3BluePrint = player3BluePrint;
        currentPlayer4BluePrint = player4BluePrint;
    }

    public void AssigneBonusHealth()
    {
        player1CurrentBonus = player1Bonus;
        player2CurrentBonus = player2Bonus;
        player3CurrentBonus = player3Bonus;
        player4CurrentBonus = player4Bonus;
    }

    public void DisableOffScreenDeath()
    {
        foreach (var player in players)
        {
            if (player) player.deactivateOnOffScreen = true;
        }
    }

    public void MaxScoreLevel()
    {
        Collectible[] collectibles = FindObjectsOfType<Collectible>();
        foreach (Collectible c in collectibles)
        {
            if (c.gameObject.GetComponent<Collectible>().score > 0)
            {
                maxScoreInLevel += c.gameObject.GetComponent<Collectible>().score;
            }
        }
    }


    public BluePrint[] BluePrintslist;

    public void MaxBluePrintLevel()
    {
        BluePrint[] BluePrints = FindObjectsOfType<BluePrint>();
        foreach (BluePrint c in BluePrints)
        {
            if (c.gameObject.GetComponent<BluePrint>().score > 0)
            {
                maxBluePrintInLevel += c.gameObject.GetComponent<BluePrint>().score;
            }

            BluePrintslist = BluePrints;
        }
    }
    
    #region Pause Menu

    public void TogglePause()
    {
        isPaused = pauseMenu.Toggle();
    }
    
    #endregion
    
    
    private void UpdatePlayTime()
    {
        if (isPaused) return;

        elapsedTime += Time.deltaTime;

        TimeSpan ts = TimeSpan.FromSeconds(elapsedTime);
        int hours   = ts.Hours;
        int minutes = ts.Minutes;
        int seconds = ts.Seconds;

        gameTime = $"{hours:00}:{minutes:00}:{seconds:00}";
        gameTimeText.text = gameTime;
    }
    
    void UpdateDisplay(DateTime dt)
    {
        // Format 24h HH:mm:ss avec zéros de tête
        string formatted = dt.ToString("HH:mm:ss");
        realTime = formatted;
        realTimeText.text = formatted;
    }

    // Optionnel : exposer une méthode pour forcer une resynchronisation (utile si on veut
    // détecter un saut système important)
    public void ForceUpdate()
    {
        DateTime now = DateTime.Now;
        UpdateDisplay(now);
        lastSecond = now.Second;
    }

    
    
    [System.Serializable]
    public class GameData
    {
        public int fileID;

        public float  elapsedTime;  
        public string gameTime;
        
        public string currentSceneName;
        
        public Transform respawnPoint;
        public string respawnPointName;
        public Vector3 checkPointPosition;
        public bool isCheckPointReached;

        public int maxScoreInLevel;
        public int currentMaxScoreInLevel;

        public int maxBluePrintInLevel;
        public int currentBluePrintInLevel;

        public bool recordPlayer1;
        public bool recordPlayer2;
        public bool recordPlayer3;
        public bool recordPlayer4;

        public int XP;

        public int Score;

        public int BluePrint;

        public int player1BluePrint;
        public int player2BluePrint;
        public int player3BluePrint;
        public int player4BluePrint;

        public int currentBluePrint;
        public int currentPlayer1BluePrint;
        public int currentPlayer2BluePrint;
        public int currentPlayer3BluePrint;
        public int currentPlayer4BluePrint;

        public int player1Score;
        public int player2Score;
        public int player3Score;
        public int player4Score;

        public int currentScore;
        public int currentPlayer1Score;
        public int currentPlayer2Score;
        public int currentPlayer3Score;
        public int currentPlayer4Score;

        public List<string> collectedCollectibles = new List<string>();

        // Liste temporaire qui stocke les collectibles récupérés depuis le dernier checkpoint
        public List<string> tempCollectedCollectibles = new List<string>();

        public List<string> collectedBluePrint = new List<string>();

        // Liste temporaire qui stocke les collectibles récupérés depuis le dernier checkpoint
        public List<string> tempCollectedBluePrint = new List<string>();
    
        public List<string> collectedBluePrintInTotal = new List<string>();

        public string currentMusicName;
        
        public bool newSceneLoad = true;
    }
    
    public void Save(int slot)
    {
        GameData data = new GameData();
        
        data.fileID = fileID;
        
        data.elapsedTime = elapsedTime;
        data.gameTime = gameTime;
        
        data.currentSceneName = currentSceneName;
        
        data.respawnPoint = respawnPoint;
        data.respawnPointName = respawnPointName;
        data.checkPointPosition = checkPointPosition;
        data.isCheckPointReached = isCheckPointReached;
        
        data.maxScoreInLevel = maxScoreInLevel;
        data.currentMaxScoreInLevel = currentMaxScoreInLevel;
        data.maxBluePrintInLevel = maxBluePrintInLevel;
        data.currentBluePrintInLevel = currentBluePrintInLevel;
        data.recordPlayer1 =  recordPlayer1;
        data.recordPlayer2 =  recordPlayer2;
        data.recordPlayer3 =  recordPlayer3;    
        data.recordPlayer4 =  recordPlayer4;
        
        data.XP = XP;
        data.Score = Score;
        data.BluePrint = BluePrint;
        
        data.player1BluePrint = player1BluePrint;
        data.player2BluePrint = player2BluePrint;
        data.player3BluePrint = player3BluePrint;
        data.player4BluePrint = player4BluePrint;
        data.currentBluePrint = currentBluePrint;
        data.currentPlayer1BluePrint = currentPlayer1BluePrint;
        data.currentPlayer2BluePrint = currentPlayer2BluePrint;
        data.currentPlayer3BluePrint = currentPlayer3BluePrint;
        data.currentPlayer4BluePrint = currentPlayer4BluePrint;
        data.player1Score = player1Score;
        data.player2Score = player2Score;
        data.player3Score = player3Score;
        data.player4Score = player4Score;
        data.currentScore = currentScore;
        data.currentPlayer1Score = currentPlayer1Score;
        data.currentPlayer2Score = currentPlayer2Score;
        data.currentPlayer3Score = currentPlayer3Score;
        data.currentPlayer4Score = currentPlayer4Score;
        
        data.collectedCollectibles = new List<string>(collectedCollectibles);
        data.tempCollectedCollectibles = new List<string>(tempCollectedCollectibles);
        data.collectedBluePrint = new List<string>(collectedBluePrint);
        data.tempCollectedBluePrint = new List<string>(tempCollectedBluePrint);
        data.collectedBluePrintInTotal = new List<string>(collectedBluePrintInTotal);
        
        data.currentMusicName = currentMusicName;
        
        data.newSceneLoad = newSceneLoad;
        
        string json = JsonUtility.ToJson(data, true);
        
        string filePath = Application.persistentDataPath + "/slot" + slot + ".json";
        
        File.WriteAllText(filePath, json);
        Debug.Log("Jeu sauvegardé dans " + filePath);
    }
    
    public void Load(int slot)
    {
        string filePath = Application.persistentDataPath + "/slot" + slot + ".json";

        if (!File.Exists(filePath))
        {
            Debug.LogWarning("Sauvegarde introuvable : " + filePath);
            return;
        }
        
        string json = File.ReadAllText(filePath);
        GameData data = JsonUtility.FromJson<GameData>(json);
        
        fileID = data.fileID;
        
        elapsedTime = data.elapsedTime; 
        gameTime = data.gameTime;
        
        currentSceneName = data.currentSceneName;
        respawnPointName = data.respawnPointName;
        checkPointPosition = data.checkPointPosition;
        isCheckPointReached = data.isCheckPointReached;
        maxScoreInLevel = data.maxScoreInLevel;
        currentMaxScoreInLevel = data.currentMaxScoreInLevel;
        maxBluePrintInLevel = data.maxBluePrintInLevel;
        currentBluePrintInLevel = data.currentBluePrintInLevel;
        recordPlayer1 = data.recordPlayer1;
        recordPlayer2 = data.recordPlayer2;
        recordPlayer3 = data.recordPlayer3;
        recordPlayer4 = data.recordPlayer4;
        XP = data.XP;
        Score = data.Score;
        BluePrint = data.BluePrint;
        
        player1BluePrint = data.player1BluePrint;
        player2BluePrint = data.player2BluePrint;
        player3BluePrint = data.player3BluePrint;
        player4BluePrint = data.player4BluePrint;
        currentBluePrint = data.currentBluePrint;
        currentPlayer1BluePrint = data.currentPlayer1BluePrint;
        currentPlayer2BluePrint = data.currentPlayer2BluePrint;
        currentPlayer3BluePrint = data.currentPlayer3BluePrint;
        currentPlayer4BluePrint = data.currentPlayer4BluePrint;

        player1Score = data.player1Score;
        player2Score = data.player2Score;
        player3Score = data.player3Score;
        player4Score = data.player4Score;
        currentScore = data.currentScore;
        currentPlayer1Score = data.currentPlayer1Score;
        currentPlayer2Score = data.currentPlayer2Score;
        currentPlayer3Score = data.currentPlayer3Score;
        currentPlayer4Score = data.currentPlayer4Score;

        collectedCollectibles = data.collectedCollectibles    ?? new List<string>();
        tempCollectedCollectibles = data.tempCollectedCollectibles?? new List<string>();
        collectedBluePrint = data.collectedBluePrint       ?? new List<string>();
        tempCollectedBluePrint = data.tempCollectedBluePrint   ?? new List<string>();
        collectedBluePrintInTotal = data.collectedBluePrintInTotal?? new List<string>();

        currentMusicName = data.currentMusicName;

        newSaveFileLoaded = true;
        
        newSceneLoad = false;
        SceneManager.LoadScene(currentSceneName);
    }

    
}