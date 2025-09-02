using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class PlayerScoreUI : MonoBehaviour
{
    [Header("Entry Prefab & Container")]
    [Tooltip("Prefab with: Image Icon, TextMeshProUGUI NameText, TextMeshProUGUI ScoreText")]
    public GameObject playerEntryPrefab;
    [Tooltip("Parent transform for instantiated entries (should NOT have a Layout Group)")]
    public RectTransform entriesContainer;

    [Header("Character Assets")]
    [Tooltip("Sprites in same order as GameManager.playerCharacterIs enum")]
    public Sprite[] characterIcons;
    [Tooltip("Names in same order as GameManager.playerCharacterIs enum")]
    public string[] characterNames;

    [Header("Layout")]
    [Tooltip("Horizontal spacing (in pixels) between each player icon")]
    public float iconSpacingX = 200f;

    private class PlayerEntry
    {
        public GameObject root;
        public Image icon;
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI scoreText;
        
        public TextMeshProUGUI maxLife;
        public TextMeshProUGUI currentLife;
    }

    // keyed by player slot number (1..4)
    private readonly Dictionary<int, PlayerEntry> entries = new Dictionary<int, PlayerEntry>();

    void Update()
    {
        var gm = GameManager.instance;
        if (gm == null) return;

        // Check presence for each slot
        for (int slot = 1; slot <= 4; slot++)
        {
            bool present = slot switch
            {
                1 => gm.isPlayer1present,
                2 => gm.isPlayer2present,
                3 => gm.isPlayer3present,
                4 => gm.isPlayer4present,
                _ => false
            };

            if (present && !entries.ContainsKey(slot))
            {
                CreateEntry(slot, gm);
            }
            else if (!present && entries.ContainsKey(slot))
            {
                Destroy(entries[slot].root);
                entries.Remove(slot);
            }
        }

        // Update all existing entries
        foreach (var kv in entries)
        {
            int slot = kv.Key;
            var entry = kv.Value;

            // Update character icon/name
            var charEnum = slot switch
            {
                1 => gm.player1Is,
                2 => gm.player2Is,
                3 => gm.player3Is,
                4 => gm.player4Is,
                _ => GameManager.playerCharacterIs.none
            };
            int idx = (int)charEnum;
            entry.icon.sprite = (idx >= 0 && idx < characterIcons.Length)
                                ? characterIcons[idx]
                                : null;

            entry.nameText.text = (idx >= 0 && idx < characterNames.Length && !string.IsNullOrEmpty(characterNames[idx]))
                                   ? characterNames[idx]
                                   : $"Joueur {slot}";

            // Update score
            int score = slot switch
            {
                1 => gm.player1Score,
                2 => gm.player2Score,
                3 => gm.player3Score,
                4 => gm.player4Score,
                _ => 0
            };
            entry.scoreText.text = score.ToString();
            
            
            
            int maxlife = slot switch
            {
                1 => gm.maxLife,
                2 => gm.maxLife,
                3 => gm.maxLife,
                4 => gm.maxLife,
                _ => 0
            };
            entry.maxLife.text = maxlife.ToString();
            
            
            int currentlife = slot switch
            {
                1 => gm.player1CurrentLife,
                2 => gm.player2CurrentLife,
                3 => gm.player3CurrentLife,
                4 => gm.player4CurrentLife,
                _ => 0
            };
            entry.currentLife.text = currentlife.ToString();
        }
    }

    private void CreateEntry(int slot, GameManager gm)
    {
        // Instantiate prefab
        var go = Instantiate(playerEntryPrefab, entriesContainer);
        var entry = new PlayerEntry
        {
            root = go,
            icon = go.transform.Find("Icon").GetComponent<Image>(),
            nameText = go.transform.Find("NameText").GetComponent<TextMeshProUGUI>(),
            scoreText = go.transform.Find("ScoreText").GetComponent<TextMeshProUGUI>(),
            maxLife = go.transform.Find("MaxLife").GetComponent<TextMeshProUGUI>(),
            currentLife = go.transform.Find("CurrentLife").GetComponent<TextMeshProUGUI>(),
        };

        // Position horizontally by slot
        var rt = go.GetComponent<RectTransform>();
        rt.anchoredPosition = new Vector2(iconSpacingX * (slot - 1), 0);

        // Initialize icon & name
        var charEnum = slot switch
        {
            1 => gm.player1Is,
            2 => gm.player2Is,
            3 => gm.player3Is,
            4 => gm.player4Is,
            _ => GameManager.playerCharacterIs.none
        };
        int idx = (int)charEnum;
        if (idx >= 0 && idx < characterIcons.Length)
            entry.icon.sprite = characterIcons[idx];
        entry.nameText.text = (idx >= 0 && idx < characterNames.Length && !string.IsNullOrEmpty(characterNames[idx]))
                               ? characterNames[idx]
                               : $"Joueur {slot}";
        entry.scoreText.text = "0";

        entries[slot] = entry;
    }
}
