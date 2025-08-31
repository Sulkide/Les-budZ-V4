using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Interactable : MonoBehaviour
{
    // Indique si un joueur est dans la zone d'interaction
    private bool playerInRange = false;
    // Référence au composant PlayerMovement de l'objet en collision
    private PlayerMovement player;

    private bool dontRead;
    
    // Assurez-vous que votre GameObject possède un collider configuré en "Is Trigger"
    private void OnTriggerStay2D(Collider2D collision)
    {
        
        // Vérifie que l'objet qui entre en collision a le composant PlayerMovement
        player = collision.GetComponent<PlayerMovement>();
        if (player != null)
        {
            if (player.useInputRegistered)
            {
                if (dontRead == false)
                {
                    StartCoroutine(TextBubble());
                }
                
            }
        }
    }

    private IEnumerator TextBubble()
    {
        dontRead = true;
        yield return new WaitForSeconds(3f);
        Debug.Log("Interaction détectée : le bouton 'Use' est pressé !");
        dontRead = false;

    }

}