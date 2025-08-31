using System.Text.RegularExpressions;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class RecordingZone : MonoBehaviour
{
    [Tooltip("Le layer utilisé pour les joueurs (par ex. \"Player\").")]
    public string playerLayerName = "Player";

    // Regex pour extraire le numéro du Player dans le parent,
    // format exact : "Player X(Clone)" où X ∈ {1,2,3,4}
    private static readonly Regex playerNameRegex =
        new Regex(@"^Player\s*([1-4])\(Clone\)$", RegexOptions.Compiled);

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        GetComponent<SpriteRenderer>().color = new Color(1f, 1f, 1f, 0f);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        TryRecordPlayer(other, true);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        TryRecordPlayer(other, false);
    }

    private void TryRecordPlayer(Collider2D other, bool entering)
    {
        // 1) Vérifier le layer
        if (other.gameObject.layer != LayerMask.NameToLayer(playerLayerName))
            return;

        // 2) Vérifier la présence du component PlayerMovement
        var pm = other.GetComponent<PlayerMovement>();
        if (pm == null)
            return;

        var gm = GameManager.instance;
        if (gm == null)
            return;

        // 3) Extraire le numéro via ParentName (doit être "Player X(Clone)")
        string parentName = pm.parentName;  
        var m = playerNameRegex.Match(parentName);
        if (!m.Success)
            return;

        int index = int.Parse(m.Groups[1].Value);

        if (entering)
        {
            // 4) Si aucun enregistrement n'est déjà actif, activer celui-ci
            if (gm.recordPlayer1 || gm.recordPlayer2 ||
                gm.recordPlayer3 || gm.recordPlayer4)
            {
                return;
            }

            switch (index)
            {
                case 1: gm.recordPlayer1 = true; break;
                case 2: gm.recordPlayer2 = true; break;
                case 3: gm.recordPlayer3 = true; break;
                case 4: gm.recordPlayer4 = true; break;
            }
        }
        else
        {
            // 5) À la sortie, désactiver seulement si c'est celui qui était enregistré
            switch (index)
            {
                case 1:
                    if (gm.recordPlayer1) gm.recordPlayer1 = false;
                    break;
                case 2:
                    if (gm.recordPlayer2) gm.recordPlayer2 = false;
                    break;
                case 3:
                    if (gm.recordPlayer3) gm.recordPlayer3 = false;
                    break;
                case 4:
                    if (gm.recordPlayer4) gm.recordPlayer4 = false;
                    break;
            }
        }
    }
}
