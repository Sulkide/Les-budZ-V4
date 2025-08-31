using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using Input = UnityEngine.Windows.Input;

public class MenuManager : MonoBehaviour
{
    public PlayerInput playerControls;
    [SerializeField] private EventSystem eventController;
    [SerializeField] private GameObject FirstButton;
    [SerializeField] private GameObject EchapMenu;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private bool isOpen;
    private void Awake()
    {
        playerControls = GetComponent<PlayerInput>();
    }

    void Start()
    {
        isOpen = false;
        EchapMenu.SetActive(false);
        Time.timeScale = 1;
    }

    // Update is called once per frame
    void Update()
    {
        if (playerControls.actions["Escape"].WasPressedThisFrame())
        {
            isOpen = !isOpen;
            Debug.Log(isOpen);
            eventController.SetSelectedGameObject(FirstButton);
        }
        if (isOpen)
        {
            
            Time.timeScale = 0;
        }
        else
        {
            eventController.SetSelectedGameObject(null);
            Time.timeScale = 1;
        }
        EchapMenu.SetActive(isOpen);

        if (playerControls.actions["Cancel"].WasPressedThisFrame())
        {
            Close();
        }
        
  

       
    }

    public void Close()
    {
        isOpen = !isOpen;
        Debug.Log(isOpen);
        eventController.SetSelectedGameObject(null);
        
    }
    

    public void Play()
    {
        Time.timeScale = 1;
    }
   
    
}