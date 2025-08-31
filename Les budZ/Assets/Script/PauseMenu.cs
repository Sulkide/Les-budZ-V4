using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject startSelectedButton;

    private void Awake()
    {
        gameObject.SetActive(false);
    }

    public bool Toggle()
    {
        if (gameObject.activeSelf)
        {
            Close();
            return false;
        }
        else
        {
            Open();
            return true;
        }
    }

    private void Open()
    {

        EventSystem.current.SetSelectedGameObject(startSelectedButton);
        gameObject.SetActive(true);
        Time.timeScale = 0;
    }

    private void Close()
    {
        Time.timeScale = 1;
        gameObject.SetActive(false);
    }

    public void ReturnToMenu()
    {
        if (GameManager.instance) Destroy(GameManager.instance.gameObject);
        Time.timeScale = 1;
        SceneManager.LoadScene("TitleScreen");
    }
    
    public void Quit()
    {
        Application.Quit();
    }
}
