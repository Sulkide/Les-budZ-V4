using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;
using Unity.Mathematics;
using Unity.VisualScripting;

public class EndLevelManager : MonoBehaviour
{
    public string sceneName;
    public int RespawnPointNumber;


    private bool monitoredBool = true;
    
    [Tooltip("Fenêtre de temps pour détecter le toggle (en secondes).")]
    public float detectionWindow = 0.5f;

    // Variables privées pour le suivi de l'état
    private bool previousState;
    private float falseStartTime = -1f;
    private int playerSelctor = 0;
    private float waitSelector;
    private float move;
    private bool use;
    private bool hasSelectPlayer;
    private string namePlayer;
    private bool playerIsSelected;// Valeur négative indique qu'aucun
    
    // Références vers les éléments nécessaires (à assigner depuis l'éditeur Unity)
    public GameObject endLevelCanvas;
    public Image flashImage;
    public Image fadeToBlackImage;
    public GameObject backgroundImage;
    public GameObject bonusPoint;
    public TextMeshProUGUI[] playerScoreTexts;   // Textes pour afficher les scores des joueurs
    public TextMeshProUGUI[] playerRankingTexts;   // Textes pour afficher les classements des joueurs
    public GameObject[] playerSprites;             // Images UI pour afficher le sprite du personnage de chaque joueur
    public Slider totalScoreSlider;
    public TextMeshProUGUI totalScoreText;
    public GameObject information;
    public TextMeshProUGUI bonusText;
    public TextMeshProUGUI MaxLevelPointText;// Prefab pour l'affichage du bonus de points (+15)

    // On suppose l'existence d'un GameManager singleton et d'un gestionnaire d'input (PlayerMovement)

    public float flashDuration = 0.5f;
    private float fadeDuration = 2f;
    private float displayDuration = 3f;
    private bool doBonusAnimation;// Durée de l'animation de comptage des scores en secondes

    // Classe interne pour stocker les informations de classement de chaque joueur
    private class PlayerRankingInfo
    {
        public int score;
        public Transform spriteTransform;
        public int playerNumber; // Ajout du numéro du joueur (1, 2, 3 ou 4)
    }

    // Fonction helper qui retourne le transform du sprite correspondant au personnage
    private Transform GetSpriteTransform(string playerChar)
    {
        switch (playerChar)
        {
            case "Sulkide":
                return playerSprites[0].transform;
            case "Darckox":
                return playerSprites[1].transform;
            case "Sulana":
                return playerSprites[2].transform;
            case "MrSlow":
                return playerSprites[3].transform;
            default:
                return null;
        }
    }

    public void Start()
    {
        flashImage.color = new Color(1f, 1f, 1f, 0f);
        fadeToBlackImage.color = new Color(0f, 0f, 0f, 0f);
        previousState = monitoredBool;
    }

    public void Use()
    {
        GameManager.instance.ChangeScene(sceneName, RespawnPointNumber);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        PlayerMovement pm = other.GetComponent<PlayerMovement>();
        if (pm != null)
        {
            pm.transform.position = this.transform.position;
            EventEndLevel(other, pm);
        }
    }

    // Méthode de fin de niveau à appeler (éventuellement via StartCoroutine depuis un trigger de fin de niveau)
    public void EventEndLevel(Collider2D other, PlayerMovement pm)
    {
        
        
        // 1. Activer le canvas de fin de niveau
        endLevelCanvas.gameObject.SetActive(true);

        // 2. Rendre les joueurs invincibles
        GameManager.instance.MakePlayerInvinsible();

        // 3. Jouer une animation de flash blanc (opacité 0 -> 1 -> 0 en 0.25s)
        flashImage.gameObject.SetActive(true);
        if (flashDuration > 0)
        {

            flashDuration -= Time.deltaTime;
            flashImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(flashImage.color.a, 1f, Time.deltaTime * 16));
            return;
        }

        backgroundImage.transform.DOMoveX(3000, 0.5f);

        // 4. Faire un fondu vers le noir sur 2 secondes avec fadeToBlackImage
        fadeToBlackImage.gameObject.SetActive(true);
        if (fadeDuration > 0)
        {
            fadeDuration -= Time.deltaTime;
            fadeToBlackImage.color =
                new Color(0f, 0f, 0f, Mathf.Lerp(fadeToBlackImage.color.a, 1f, Time.deltaTime * 6));
            totalScoreSlider.value = 0;
            return;
        }

        // 5. Activer l'image de fond du menu de fin de niveau
        backgroundImage.SetActive(true);

        // 6. Assigner les scores via GameManager
        GameManager.instance.assigneScore();
        GameManager.instance.AssigneBluePrint();

        // 7. Récupérer les informations des joueurs depuis GameManager
        bool p1 = GameManager.instance.isPlayer1present;
        bool p2 = GameManager.instance.isPlayer2present;
        bool p3 = GameManager.instance.isPlayer3present;
        bool p4 = GameManager.instance.isPlayer4present;
        int score1 = p1 ? GameManager.instance.player1Score : 0;
        int score2 = p2 ? GameManager.instance.player2Score : 0;
        int score3 = p3 ? GameManager.instance.player3Score : 0;
        int score4 = p4 ? GameManager.instance.player4Score : 0;
        string char1 = p1 ? GameManager.instance.player1Is.ToString() : "";
        string char2 = p2 ? GameManager.instance.player2Is.ToString() : "";
        string char3 = p3 ? GameManager.instance.player3Is.ToString() : "";
        string char4 = p4 ? GameManager.instance.player4Is.ToString() : "";

        totalScoreSlider.gameObject.SetActive(true);
        totalScoreText.gameObject.SetActive(true);
        totalScoreSlider.transform.DOMoveY(1000, 1f);

        // Activer et animer les sprites en fonction du personnage de chacun
        if (p1)
        {
            if (char1 == "Sulkide")
            {
                playerSprites[0].gameObject.SetActive(true);
                playerSprites[0].transform.DOMoveY(500, 0.5f);
            }
            else if (char1 == "Darckox")
            {
                playerSprites[1].gameObject.SetActive(true);
                playerSprites[1].transform.DOMoveY(500, 0.5f);
            }
            else if (char1 == "Sulana")
            {
                playerSprites[2].gameObject.SetActive(true);
                playerSprites[2].transform.DOMoveY(500, 0.5f);
            }
            else if (char1 == "MrSlow")
            {
                playerSprites[3].gameObject.SetActive(true);
                playerSprites[3].transform.DOMoveY(500, 0.5f);
            }
        }

        if (p2)
        {
            if (char2 == "Sulkide")
            {
                playerSprites[0].gameObject.SetActive(true);
                playerSprites[0].transform.DOMoveY(500, 0.6f);
            }
            else if (char2 == "Darckox")
            {
                playerSprites[1].gameObject.SetActive(true);
                playerSprites[1].transform.DOMoveY(500, 0.6f);
            }
            else if (char2 == "Sulana")
            {
                playerSprites[2].gameObject.SetActive(true);
                playerSprites[2].transform.DOMoveY(500, 0.6f);
            }
            else if (char2 == "MrSlow")
            {
                playerSprites[3].gameObject.SetActive(true);
                playerSprites[3].transform.DOMoveY(500, 0.6f);
            }
        }

        if (p3)
        {
            if (char3 == "Sulkide")
            {
                playerSprites[0].gameObject.SetActive(true);
                playerSprites[0].transform.DOMoveY(500, 0.7f);
            }
            else if (char3 == "Darckox")
            {
                playerSprites[1].gameObject.SetActive(true);
                playerSprites[1].transform.DOMoveY(500, 0.7f);
            }
            else if (char3 == "Sulana")
            {
                playerSprites[2].gameObject.SetActive(true);
                playerSprites[2].transform.DOMoveY(500, 0.7f);
            }
            else if (char3 == "MrSlow")
            {
                playerSprites[3].gameObject.SetActive(true);
                playerSprites[3].transform.DOMoveY(500, 0.7f);
            }
        }

        if (p4)
        {
            if (char4 == "Sulkide")
            {
                playerSprites[0].gameObject.SetActive(true);
                playerSprites[0].transform.DOMoveY(500, 0.8f);
            }
            else if (char4 == "Darckox")
            {
                playerSprites[1].gameObject.SetActive(true);
                playerSprites[1].transform.DOMoveY(500, 0.8f);
            }
            else if (char4 == "Sulana")
            {
                playerSprites[2].gameObject.SetActive(true);
                playerSprites[2].transform.DOMoveY(500, 0.8f);
            }
            else if (char4 == "MrSlow")
            {
                playerSprites[3].gameObject.SetActive(true);
                playerSprites[3].transform.DOMoveY(500, 0.8f);
            }
        }

        // 8. Si le parent de "other" est "Player X (clone)", ajouter 15 points et instancier l'effet bonus
        if (other != null && other.transform.parent != null && !doBonusAnimation)
        {
            string parentName = other.transform.parent.name;
            for (int j = 1; j <= 4; j++)
            {
                string playerCloneName = "Player " + j + "(Clone)";
                if (parentName == playerCloneName)
                {


                    if (j == 1)
                    {
                        GameManager.instance.addScore(15, parentName);
                        score1 += 15;
                        bonusPoint.gameObject.transform.position = playerSprites[0].gameObject.transform.position;
                        bonusPoint.gameObject.transform.DOMoveY(playerSprites[0].gameObject.transform.position.y + 700,
                            0.5f);
                        bonusPoint.gameObject.transform.DOShakeRotation(0.5f, 90f).OnComplete(() =>
                        {
                            bonusPoint.gameObject.transform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad);
                        });
                        bonusPoint.gameObject.transform.DOShakeScale(0.5f, 1.5f).OnComplete(() =>
                        {
                            bonusPoint.gameObject.transform.DOScale(new Vector3(1f, 1f, 1f), 0.3f)
                                .SetEase(Ease.InOutQuad);
                        });
                        doBonusAnimation = true;

                    }
                    else if (j == 2)
                    {
                        GameManager.instance.addScore(15, parentName);
                        score2 += 15;
                        bonusPoint.gameObject.transform.position = playerSprites[1].gameObject.transform.position;
                        bonusPoint.gameObject.transform.DOMoveY(playerSprites[1].gameObject.transform.position.y + 700,
                            0.5f);
                        bonusPoint.gameObject.transform.DOShakeRotation(0.5f, 90f).OnComplete(() =>
                        {
                            bonusPoint.gameObject.transform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad);
                        });
                        bonusPoint.gameObject.transform.DOShakeScale(0.5f, 1.5f).OnComplete(() =>
                        {
                            bonusPoint.gameObject.transform.DOScale(new Vector3(1f, 1f, 1f), 0.3f)
                                .SetEase(Ease.InOutQuad);
                        });
                        doBonusAnimation = true;
                    }
                    else if (j == 3)
                    {
                        GameManager.instance.addScore(15, parentName);
                        score3 += 15;
                        bonusPoint.gameObject.transform.position = playerSprites[2].gameObject.transform.position;
                        bonusPoint.gameObject.transform.DOMoveY(playerSprites[2].gameObject.transform.position.y + 700,
                            0.5f);
                        bonusPoint.gameObject.transform.DOShakeRotation(0.5f, 90f).OnComplete(() =>
                        {
                            bonusPoint.gameObject.transform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad);
                        });
                        bonusPoint.gameObject.transform.DOShakeScale(0.5f, 1.5f).OnComplete(() =>
                        {
                            bonusPoint.gameObject.transform.DOScale(new Vector3(1f, 1f, 1f), 0.3f)
                                .SetEase(Ease.InOutQuad);
                        });
                        doBonusAnimation = true;
                    }
                    else if (j == 4)
                    {
                        GameManager.instance.addScore(15, parentName);
                        score4 += 15;
                        bonusPoint.gameObject.transform.position = playerSprites[3].gameObject.transform.position;
                        bonusPoint.gameObject.transform.DOMoveY(playerSprites[3].gameObject.transform.position.y + 700,
                            0.5f);
                        bonusPoint.gameObject.transform.DOShakeRotation(0.5f, 90f).OnComplete(() =>
                        {
                            bonusPoint.gameObject.transform.DORotate(Vector3.zero, 0.3f).SetEase(Ease.InOutQuad);
                        });
                        bonusPoint.gameObject.transform.DOShakeScale(0.5f, 1.5f).OnComplete(() =>
                        {
                            bonusPoint.gameObject.transform.DOScale(new Vector3(1f, 1f, 1f), 0.3f)
                                .SetEase(Ease.InOutQuad);
                        });
                        doBonusAnimation = true;
                    }

                    break;

                }
            }
        }

        // 9. Animation progressive des scores sur 4 secondes
        int totalScoreInLevel = GameManager.instance.maxScoreInLevel + 15;
        int totalScore = GameManager.instance.Score;
        totalScoreSlider.maxValue = totalScoreInLevel;
        MaxLevelPointText.text = totalScoreInLevel.ToString();

        bool prevInputState = pm.useInputRegistered;
        monitoredBool = pm.useInputRegistered;

        if (displayDuration > 0)
        {
            if (prevInputState)
                displayDuration -= Time.deltaTime;
            else
                displayDuration -= Time.deltaTime / 4;

            float fractionSlider = Mathf.Clamp01(displayDuration / 4);
            int displayedXPSlider = Mathf.RoundToInt(Mathf.Lerp(totalScore, 0, fractionSlider));
            totalScoreSlider.value = displayedXPSlider;
            totalScoreText.text = displayedXPSlider.ToString();

            if (p1)
            {
                float fraction = Mathf.Clamp01(displayDuration / 4);
                int displayedXP = Mathf.RoundToInt(Mathf.Lerp(score1, 0, fraction));
                if (char1 == "Sulkide")
                {
                    playerScoreTexts[0].gameObject.SetActive(true);
                    playerScoreTexts[0].text = displayedXP.ToString();
                }
                else if (char1 == "Darckox")
                {
                    playerScoreTexts[1].gameObject.SetActive(true);
                    playerScoreTexts[1].text = displayedXP.ToString();
                }
                else if (char1 == "Sulana")
                {
                    playerScoreTexts[2].gameObject.SetActive(true);
                    playerScoreTexts[2].text = displayedXP.ToString();
                }
                else if (char1 == "MrSlow")
                {
                    playerScoreTexts[3].gameObject.SetActive(true);
                    playerScoreTexts[3].text = displayedXP.ToString();
                }
            }

            if (p2)
            {
                float fraction = Mathf.Clamp01(displayDuration / 4);
                int displayedXP = Mathf.RoundToInt(Mathf.Lerp(score2, 0, fraction));
                if (char2 == "Sulkide")
                {
                    playerScoreTexts[0].gameObject.SetActive(true);
                    playerScoreTexts[0].text = displayedXP.ToString();
                }
                else if (char2 == "Darckox")
                {
                    playerScoreTexts[1].gameObject.SetActive(true);
                    playerScoreTexts[1].text = displayedXP.ToString();
                }
                else if (char2 == "Sulana")
                {
                    playerScoreTexts[2].gameObject.SetActive(true);
                    playerScoreTexts[2].text = displayedXP.ToString();
                }
                else if (char2 == "MrSlow")
                {
                    playerScoreTexts[3].gameObject.SetActive(true);
                    playerScoreTexts[3].text = displayedXP.ToString();
                }
            }

            if (p3)
            {
                float fraction = Mathf.Clamp01(displayDuration / 4);
                int displayedXP = Mathf.RoundToInt(Mathf.Lerp(score3, 0, fraction));
                if (char3 == "Sulkide")
                {
                    playerScoreTexts[0].gameObject.SetActive(true);
                    playerScoreTexts[0].text = displayedXP.ToString();
                }
                else if (char3 == "Darckox")
                {
                    playerScoreTexts[1].gameObject.SetActive(true);
                    playerScoreTexts[1].text = displayedXP.ToString();
                }
                else if (char3 == "Sulana")
                {
                    playerScoreTexts[2].gameObject.SetActive(true);
                    playerScoreTexts[2].text = displayedXP.ToString();
                }
                else if (char3 == "MrSlow")
                {
                    playerScoreTexts[3].gameObject.SetActive(true);
                    playerScoreTexts[3].text = displayedXP.ToString();
                }
            }

            if (p4)
            {
                float fraction = Mathf.Clamp01(displayDuration / 4);
                int displayedXP = Mathf.RoundToInt(Mathf.Lerp(score4, 0, fraction));
                if (char4 == "Sulkide")
                {
                    playerScoreTexts[0].gameObject.SetActive(true);
                    playerScoreTexts[0].text = displayedXP.ToString();
                }
                else if (char4 == "Darckox")
                {
                    playerScoreTexts[1].gameObject.SetActive(true);
                    playerScoreTexts[1].text = displayedXP.ToString();
                }
                else if (char4 == "Sulana")
                {
                    playerScoreTexts[2].gameObject.SetActive(true);
                    playerScoreTexts[2].text = displayedXP.ToString();
                }
                else if (char4 == "MrSlow")
                {
                    playerScoreTexts[3].gameObject.SetActive(true);
                    playerScoreTexts[3].text = displayedXP.ToString();
                }
            }

            return;
        }

        // 10. Classement final : création de la liste des joueurs actifs
        List<PlayerRankingInfo> playersRanking = new List<PlayerRankingInfo>();

        if (p1)
            playersRanking.Add(new PlayerRankingInfo
                { score = score1, spriteTransform = GetSpriteTransform(char1), playerNumber = 1 });
        if (p2)
            playersRanking.Add(new PlayerRankingInfo
                { score = score2, spriteTransform = GetSpriteTransform(char2), playerNumber = 2 });
        if (p3)
            playersRanking.Add(new PlayerRankingInfo
                { score = score3, spriteTransform = GetSpriteTransform(char3), playerNumber = 3 });
        if (p4)
            playersRanking.Add(new PlayerRankingInfo
                { score = score4, spriteTransform = GetSpriteTransform(char4), playerNumber = 4 });

        // Trier par score décroissant (le meilleur score en premier)
        playersRanking.Sort((a, b) => b.score.CompareTo(a.score));



        if (playersRanking.Count > 1 && !hasSelectPlayer)
        {
            
            
            switch (playersRanking[0].playerNumber)
            {
                case 1:
                    move = GameManager.instance.player1Location.gameObject.GetComponent<PlayerMovement>()
                        .moveInput.x;
                    use = GameManager.instance.player1Location.gameObject.GetComponent<PlayerMovement>()
                        .useInputRegistered;
                    bonusText.text = "Selon "+ GameManager.instance.player1Location.gameObject.name +", qui merite son bonus ?";
                    bonusText.gameObject.SetActive(true);

                    break;
                case 2:
                    
                    move = GameManager.instance.player2Location.gameObject.GetComponent<PlayerMovement>()
                        .moveInput.x;
                    use = GameManager.instance.player2Location.gameObject.GetComponent<PlayerMovement>()
                        .useInputRegistered;
                    bonusText.text = "Selon "+ GameManager.instance.player2Location.gameObject.name +", qui merite son bonus ?";
                    bonusText.gameObject.SetActive(true);

                    break;
                case 3:

                    move = GameManager.instance.player3Location.gameObject.GetComponent<PlayerMovement>()
                        .moveInput.x;
                    use = GameManager.instance.player3Location.gameObject.GetComponent<PlayerMovement>()
                        .useInputRegistered;
                    bonusText.text = "Selon "+ GameManager.instance.player3Location.gameObject.name +", qui merite son bonus ?";
                    bonusText.gameObject.SetActive(true);
                    
                    break;
                case 4:
                    
                    move = GameManager.instance.player4Location.gameObject.GetComponent<PlayerMovement>()
                        .moveInput.x;
                    use = GameManager.instance.player4Location.gameObject.GetComponent<PlayerMovement>()
                        .useInputRegistered;
                    bonusText.text = "Selon "+ GameManager.instance.player4Location.gameObject.name +", qui merite son bonus ?";
                    bonusText.gameObject.SetActive(true);

                    break;
            }

            //modifie le code ICI
            
            List<int> activeIndices = new List<int>();
            for (int k = 0; k < playerSprites.Length; k++)
            {
                if (playerSprites[k].gameObject.activeSelf)
                    activeIndices.Add(k);
            }

            if (activeIndices.Count > 0)
            {
                if (move > 0f && waitSelector <= 0f)
                {
                    playerSelctor++;
                    if (playerSelctor >= activeIndices.Count) playerSelctor = 0;
                    waitSelector = 0.2f;
                }
                else if (move < 0f && waitSelector <= 0f)
                {
                    playerSelctor--;
                    if (playerSelctor < 0) playerSelctor = activeIndices.Count - 1;
                    waitSelector = 0.2f;
                }
                else
                {
                    waitSelector -= Time.deltaTime;
                }

       
                foreach (var spr in playerSprites)
                    spr.GetComponent<Highlight>().enabled = false;

           
                int realIndex = activeIndices[playerSelctor];
                playerSprites[realIndex].GetComponent<Highlight>().enabled = true;
                namePlayer = playerSprites[realIndex].gameObject.name;
                
                if (!use)
                {
                    playerIsSelected = true;
                }

                if (use && playerIsSelected)
                {
                    Debug.Log("test");
                    if (namePlayer == "Sulkide")
                    {
                        if (char1 == "Sulkide")
                        {
                            if ((GameManager.instance.player1Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player1Bonus) = true;
                            }
                        }

                        if (char2 == "Sulkide")
                        {
                            if ((GameManager.instance.player2Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player2Bonus) = true;
                            }
                        }

                        if (char3 == "Sulkide")
                        {
                            if ((GameManager.instance.player3Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player3Bonus) = true;
                            }
                        }

                        if (char4 == "Sulkide")
                        {
                            if ((GameManager.instance.player4Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player4Bonus) = true;
                            }
                        }
                    }
                    else if (namePlayer == "Darckox")
                    {
                        if (char1 == "Darckox")
                        {
                            if ((GameManager.instance.player1Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player1Bonus) = true;
                            }
                        }

                        if (char2 == "Darckox")
                        {
                            if ((GameManager.instance.player2Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player2Bonus) = true;
                            }
                        }

                        if (char3 == "Darckox")
                        {
                            if ((GameManager.instance.player3Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player3Bonus) = true;
                            }
                        }

                        if (char4 == "Darckox")
                        {
                            if ((GameManager.instance.player4Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player4Bonus) = true;
                            }
                        }
                    }
                    else if (namePlayer == "Sulana")
                    {
                        if (char1 == "Sulana")
                        {
                            if ((GameManager.instance.player1Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player1Bonus) = true;
                            }
                        }

                        if (char2 == "Sulana")
                        {
                            if ((GameManager.instance.player2Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player2Bonus) = true;
                            }
                        }

                        if (char3 == "Sulana")
                        {
                            if ((GameManager.instance.player3Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player3Bonus) = true;
                            }
                        }

                        if (char4 == "Sulana")
                        {
                            if ((GameManager.instance.player4Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player4Bonus) = true;
                            }
                        }
                    }
                    else if (namePlayer == "Mr Slow")
                    {
                        if (char1 == "MrSlow")
                        {
                            if ((GameManager.instance.player1Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player1Bonus) = true;
                            }
                        }

                        if (char2 == "MrSlow")
                        {
                            if ((GameManager.instance.player2Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player2Bonus) = true;
                            }
                        }

                        if (char3 == "MrSlow")
                        {
                            if ((GameManager.instance.player3Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player3Bonus) = true;
                            }
                        }

                        if (char4 == "MrSlow")
                        {
                            if ((GameManager.instance.player4Location.gameObject.GetComponent<PlayerMovement>()
                                    .HasCurrentlyHealthbonus))
                            {
                                GameManager.instance.addXP(1000);
                            }
                            else
                            {
                                (GameManager.instance.player4Bonus) = true;
                            }
                        }
                    }
                    
                    hasSelectPlayer = true;
                    bonusText.gameObject.SetActive(false);
                }
            
            }
        }
        else if (playersRanking.Count <= 1)
        {
            hasSelectPlayer = true;
        }


        // Afficher et positionner les textes de classement pour chaque joueur actif
        for (int i = 0; i < playerRankingTexts.Length; i++)
        {
            if (i < playersRanking.Count)
            {
                playerRankingTexts[i].gameObject.SetActive(true);
                playerRankingTexts[i].transform.position = playersRanking[i].spriteTransform.position;
                switch (i)
                {
                    case 0:
                        playerRankingTexts[i].text = "1er";
                        break;
                    case 1:
                        playerRankingTexts[i].text = "2e";
                        break;
                    case 2:
                        playerRankingTexts[i].text = "3e";
                        break;
                    case 3:
                        playerRankingTexts[i].text = "4e";
                        break;
                }
            }
            else
            {
                playerRankingTexts[i].gameObject.SetActive(false);
            }
        }

        if (hasSelectPlayer)
        {
            information.SetActive(true);

            if (previousState && !monitoredBool)
            {
                falseStartTime = Time.time;
            }

            // Détecte la transition de false à true
            if (!previousState && monitoredBool)
            {
                // Vérifier que la transition s'effectue en moins de detectionWindow secondes
                if (falseStartTime >= 0 && Time.time - falseStartTime <= detectionWindow)
                {
                    Use();
                }

                // Réinitialiser le chronomètre après la transition
                falseStartTime = -1f;
            }

            // Si le booléen reste false et que trop de temps s'est écoulé, réinitialiser la détection
            if (!monitoredBool && falseStartTime >= 0 && Time.time - falseStartTime > detectionWindow)
            {
                falseStartTime = -1f;
            }

            // Mettre à jour l'état précédent pour la prochaine frame
            previousState = monitoredBool;
        }



    }
}
