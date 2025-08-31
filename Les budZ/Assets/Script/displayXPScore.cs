using System;
using TMPro;
using UnityEngine;

public class displayXPScore : MonoBehaviour
{
    private TMP_Text XPText;

    // Variables pour l'animation
    private int displayedXP;   // Valeur actuellement affichée
    private int startXP;       // Valeur au début de l'animation
    private int targetXP;      // Nouvelle valeur à atteindre
    private float timer;       // Minuteur pour contrôler la durée d'animation
    private float animationDuration = 1f; // Durée de l'animation en secondes

    // Seuil maximum : si GameManager.instance.XP dépasse ce cap, on affiche directement "999999"
    private const int cap = 9999999;

    private void Start()
    {
        XPText = GetComponent<TMP_Text>();

        // Initialisation avec la valeur actuelle du GameManager
        displayedXP = GameManager.instance.XP;
        startXP = displayedXP;
        targetXP = displayedXP;
        timer = animationDuration;

        // Affichage initial avec des zéros devant (7 caractères)
        XPText.text = displayedXP.ToString().PadLeft(7, '0');
    }

    private void Update()
    {
        // Si la nouvelle valeur dépasse le cap, on affiche directement "999999" (comme dans votre code original)
        if (GameManager.instance.XP > cap)
        {
            displayedXP = 999999;
            XPText.text = "999999";
            return;
        }
        else
        {
            // Détecter un changement dans la valeur de XP pour lancer une nouvelle animation
            if (GameManager.instance.XP != targetXP)
            {
                // Nouvelle animation : on commence à partir de la valeur affichée actuelle
                startXP = displayedXP;
                targetXP = GameManager.instance.XP;
                timer = 0f;
            }
            
            // Tant que l'animation n'est pas terminée (durée < 1 seconde)
            if (timer < animationDuration)
            {
                timer += Time.deltaTime;
                // Calcul d'un facteur (entre 0 et 1) pour l'interpolation linéaire
                float fraction = Mathf.Clamp01(timer / animationDuration);
                // Lerp linéaire entre startXP et targetXP, puis arrondi à l'entier le plus proche
                displayedXP = Mathf.RoundToInt(Mathf.Lerp(startXP, targetXP, fraction));
            }
            else
            {
                // Assurer que la valeur affichée est exactement celle cible à la fin de l'animation
                displayedXP = targetXP;
            }
            // Mettre à jour le texte avec complétion de zéros à gauche pour obtenir 7 caractères
            XPText.text = displayedXP.ToString().PadLeft(7, '0');
        }
    }
}
