using TMPro;
using UnityEngine;

public class saveDisplayer : MonoBehaviour
{
    [SerializeField, Range(1,4)]
    private int slotToRead = 1;

    public TMP_Text displayText;
    
    void Start()
    {
        displayText = gameObject.GetComponent<TMP_Text>();
        
        GameManager.GameData data = SaveLoader.LoadGameData(slotToRead);
        if (data != null)
        {
            displayText.text = slotToRead.ToString() + " - " + data.currentSceneName + " [" + data.gameTime + "]";
        }
        else
        {
            displayText.text = slotToRead.ToString() + " - nouvelle partie ";
        }
    }
}
