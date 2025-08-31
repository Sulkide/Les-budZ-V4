using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    [SerializeField] private GameObject startSelectedButton;
    private void Start()
    {
        EventSystem.current.SetSelectedGameObject(startSelectedButton);
    }

    public void File1()
    {
        SceneManager.LoadScene("LoadScene1");
    }
    
    public void File2()
    {
        SceneManager.LoadScene("LoadScene2");
    }
    
    public void File3()
    {
        SceneManager.LoadScene("LoadScene3");
    }
    
    public void File4()
    {
        SceneManager.LoadScene("LoadScene4");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
