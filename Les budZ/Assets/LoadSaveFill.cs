using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;


public class LoadSaveFill : MonoBehaviour
{
    public int nbSlot;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
        Debug.Log("test LoadSaveFill");
        
        string filePath = Application.persistentDataPath + "/slot" + nbSlot + ".json";

        if (!File.Exists(filePath))
        {
            Debug.Log("Sauvegarde introuvable : " + filePath + " création d'un nouvelle partie");
            GameManager.instance.fileID = nbSlot;
            GameManager.instance.newSceneLoad = true;
            GameManager.instance.newSaveFileCreated = true;
            SceneManager.LoadScene("LEVEL 1");
        }
        else
        {
            Debug.Log("Chargement de la file "+ nbSlot);
            GameManager.instance.Load(nbSlot);
        }
        
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
}
