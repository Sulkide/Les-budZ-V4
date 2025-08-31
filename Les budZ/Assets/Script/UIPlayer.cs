using System;
using System.Reflection;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIPlayer : MonoBehaviour
{
    public int playerNumber;

    private RectTransform rectTransform;
    private int localScore = 0;

    private string characterName;
    private string curentCharacterName;

    private bool hasBonus;
    private bool hasCurrentlyBonus;

    private bool hasPlayedIntro = false;

    [Header("reference links")]
    public TMP_Text pointsText;
    public Image playerSprite;
    public Image bonusLifeSprite;

    public GameObject Yanchor;
    private float YanchorValue;

    private void Awake()
    {
        DisplayUIScore();
        rectTransform = GetComponent<RectTransform>();
        YanchorValue = Yanchor.transform.position.y;
    }

    private void Update()
    {
        // Construction dynamique des noms de champs en fonction de playerNumber
        string presentFieldName = "isPlayer" + playerNumber + "present";
        string bonusFieldName = "player" + playerNumber + "Bonus";
        string scoreFieldName = "player" + playerNumber + "Score";
        string characterFieldName = "player" + playerNumber + "Is";

        // Obtention du type de GameManager et accès aux champs dynamiques
        Type gmType = GameManager.instance.GetType();

        FieldInfo presentField = gmType.GetField(presentFieldName, BindingFlags.Instance | BindingFlags.Public);
        if (presentField == null)
        {
            Debug.LogError("Champ " + presentFieldName + " introuvable dans GameManager");
            return;
        }
        bool isPresent = (bool)presentField.GetValue(GameManager.instance);

        if (isPresent)
        {
            // Récupération du bonus
            FieldInfo bonusField = gmType.GetField(bonusFieldName, BindingFlags.Instance | BindingFlags.Public);
            if (bonusField == null)
            {
                Debug.LogError("Champ " + bonusFieldName + " introuvable dans GameManager");
                return;
            }
            hasBonus = (bool)bonusField.GetValue(GameManager.instance);

            DisplayUIGeneral();

            // Récupération du score
            FieldInfo scoreField = gmType.GetField(scoreFieldName, BindingFlags.Instance | BindingFlags.Public);
            if (scoreField == null)
            {
                Debug.LogError("Champ " + scoreFieldName + " introuvable dans GameManager");
                return;
            }
            int score = (int)scoreField.GetValue(GameManager.instance);
            if (localScore != score)
            {
                localScore = score;
                DisplayUIScore();
            }

            // Récupération du type de personnage
            FieldInfo characterField = gmType.GetField(characterFieldName, BindingFlags.Instance | BindingFlags.Public);
            if (characterField == null)
            {
                Debug.LogError("Champ " + characterFieldName + " introuvable dans GameManager");
                return;
            }
            var playerChar = characterField.GetValue(GameManager.instance);

            // Mise à jour du nom de personnage selon le type
            if (playerChar.Equals(GameManager.playerCharacterIs.Sulkide))
            {
                characterName = "sulkide";
            }
            else if (playerChar.Equals(GameManager.playerCharacterIs.Sulana))
            {
                characterName = "sulana";
            }
            else if (playerChar.Equals(GameManager.playerCharacterIs.MrSlow))
            {
                characterName = "mrSlow";
            }
            else if (playerChar.Equals(GameManager.playerCharacterIs.Darckox))
            {
                characterName = "darckox";
            }

            if (curentCharacterName != characterName)
            {
                curentCharacterName = characterName;
                DisplayUISprite(curentCharacterName);
            }

            if (hasCurrentlyBonus != hasBonus)
            {
                hasCurrentlyBonus = hasBonus;
                DisplayHealthBonus(hasCurrentlyBonus);
                
            }
        }
        else
        {
            pointsText.gameObject.SetActive(false);
            playerSprite.gameObject.SetActive(false);
            bonusLifeSprite.gameObject.SetActive(false);

            hasPlayedIntro = false;
        }
    }

    void DisplayUIScore()
    {
        pointsText.rectTransform.DOLocalJump(pointsText.rectTransform.localPosition, 3f, 1, 0.2f).SetEase(Ease.OutBounce);
        pointsText.rectTransform.DOShakeScale(0.5f, 0.2f).OnComplete(() =>
        {
            pointsText.rectTransform.DOScale(Vector3.one, 0.1f).SetEase(Ease.InOutQuad);
        });
        pointsText.text = ":" + localScore.ToString();
    }

    void DisplayUISprite(string characterName)
    {
        playerSprite.rectTransform.DOShakeRotation(0.5f, 60f).OnComplete(() =>
        {
            playerSprite.rectTransform.DORotate(Vector3.zero, 0.1f).SetEase(Ease.InOutQuad);
        });
        playerSprite.rectTransform.DOShakeScale(0.5f, 0.2f).OnComplete(() =>
        {
            playerSprite.rectTransform.DOScale(new Vector3(0.15f, 0.15f, 0.15f), 0.1f).SetEase(Ease.InOutQuad);
        });

        if (characterName == "sulkide")
        {
            playerSprite.color = Color.red;
        }
        else if (characterName == "sulana")
        {
            playerSprite.color = Color.blue;
        }
        else if (characterName == "mrSlow")
        {
            playerSprite.color = Color.green;
        }
        else if (characterName == "darckox")
        {
            playerSprite.color = Color.yellow;
        }
    }

    void DisplayHealthBonus(bool hasBonus)
    {
        if (hasBonus)
        {
            bonusLifeSprite.gameObject.SetActive(true);

            bonusLifeSprite.rectTransform.DOShakeRotation(0.5f, 60f).OnComplete(() =>
            {
                bonusLifeSprite.rectTransform.DORotate(Vector3.zero, 0.1f).SetEase(Ease.InOutQuad);
            });
            bonusLifeSprite.rectTransform.DOShakeScale(0.5f, 0.2f).OnComplete(() =>
            {
                bonusLifeSprite.rectTransform.DOScale(new Vector3(0.2f, 0.2f, 0.2f), 0.5f).SetEase(Ease.InOutQuad);
            });
        }
        else
        {
            bonusLifeSprite.rectTransform.DOShakeRotation(0.5f, 60f).OnComplete(() =>
            {
                bonusLifeSprite.rectTransform.DORotate(Vector3.zero, 0.5f).SetEase(Ease.InOutQuad);
            });
            bonusLifeSprite.rectTransform.DOShakeScale(0.5f, 0.2f).OnComplete(() =>
            {
                bonusLifeSprite.rectTransform.DOScale(new Vector3(0.2f, 0.2f, 0.2f), 0.5f).SetEase(Ease.InOutQuad).OnComplete(() =>
                {
                    bonusLifeSprite.gameObject.SetActive(false);
                });
            });
        }
    }

    void DisplayUIGeneral()
    {
        if (!hasPlayedIntro)
        {
            pointsText.gameObject.SetActive(true);
            playerSprite.gameObject.SetActive(true);
            bonusLifeSprite.gameObject.SetActive(true);

            rectTransform.DOMove(new Vector3(rectTransform.position.x, YanchorValue, rectTransform.position.z), 0.5f).SetEase(Ease.OutElastic);
            // Jouer l'animation d'intro

            hasPlayedIntro = true;
        }
    }
}
