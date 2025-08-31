using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Centre la caméra horizontalement entre les joueurs actifs (jusqu’à 4 joueurs),
/// avec option de prioriser le joueur le plus à droite.
/// </summary>
public class CameraCenterBetweenPlayers2D : MonoBehaviour
{
    [Tooltip("0 = centre exact, 1 = focus sur le joueur le plus à droite")]
    [Range(0f, 1f)]
    public float rightBias = 0f;

    void LateUpdate()
    {
        // Récupérer tous les joueurs actifs
        List<Transform> activePlayers = new List<Transform>();
        for (int i = 1; i <= 4; i++)
        {
            Transform pt = GetPlayerTransform(i);
            if (pt != null)
                activePlayers.Add(pt);
        }

        // Si aucun joueur, rien à faire
        if (activePlayers.Count == 0)
            return;

        // Déterminer min et max en X
        float minX = activePlayers[0].position.x;
        float maxX = minX;
        foreach (Transform p in activePlayers)
        {
            float x = p.position.x;
            if (x < minX) minX = x;
            if (x > maxX) maxX = x;
        }

        // Point milieu
        float midX = (minX + maxX) * 0.5f;
        // Appliquer bias vers la droite
        float newX = midX + rightBias * (maxX - midX);

        // Mettre à jour la position X de l'objet
        Vector3 newPos = transform.position;
        newPos.x = newX;
        transform.position = newPos;
    }

    private Transform GetPlayerTransform(int index)
    {
        switch (index)
        {
            case 1: return GameManager.instance.player1Location;
            case 2: return GameManager.instance.player2Location;
            case 3: return GameManager.instance.player3Location;
            case 4: return GameManager.instance.player4Location;
            default: return null;
        }
    }
}
