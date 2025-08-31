using UnityEngine;

public class PlayerAbilitiesLoop : MonoBehaviour
{
    [Header("Nouveaux paramètres pour les capacités du joueur")]
    public bool newCanWallJump = true;
    public bool newCanDash = true;
    public bool newCanShoot = true;
    public bool newCanGrap = true;
    public bool newCanGlide = true;// Assurez-vous que le nom correspond bien à celui du composant PlayerMovement

    void Update()
    {
        // Recherche en continu tous les composants PlayerMovement dans la scène
        PlayerMovement[] players = FindObjectsOfType<PlayerMovement>();

        foreach (PlayerMovement player in players)
        {
            // Met à jour les capacités du joueur si elles ne correspondent pas aux nouveaux paramètres
            if (player.canWallJump != newCanWallJump)
                player.canWallJump = newCanWallJump;

            if (player.canDash != newCanDash)
                player.canDash = newCanDash;

            if (player.canShoot != newCanShoot)
                player.canShoot = newCanShoot;

            if (player.canGrap != newCanGrap)
                player.canGrap = newCanGrap;
            
            if (player.canGlide != newCanGlide)
                player.canGlide = newCanGlide;
        }
    }
}