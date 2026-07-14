using System;
using Core.Input;
using UnityEngine;

namespace Features.UI
{
    /// <summary>
    /// listens to input on tab or start, and opens/closes menu. has to also
    /// push the menu context
    /// </summary>
    
    enum MenuState{Closed, Opened}
    public class UIMenuController: MonoBehaviour
    {
        [SerializeField] private GameObject panel; //our menu prefab that we will turn on and off
        
        private int lastTabIndex; //to return back to the last closed tab
        private MenuState state = MenuState.Closed;

        private void Start()
        {
            //jsut in case somebody forgot to turn it off on the scene
            panel.SetActive(false);
            state = MenuState.Closed;

        }

        private void OnEnable()
        {
            MenuInput.ToggleMenu += OnToggle;
        }

        private void OnDisable()
        {
            InputRouter.Exit(InputContext.Menu);
            MenuInput.ToggleMenu -= OnToggle;
        }

        private void OnToggle()
        {
            if (state == MenuState.Opened)
                Close();
            else if(InputRouter.ActiveContext == InputContext.Gameplay)
                Open();
        }

        private void Open()
        {
            state = MenuState.Opened;
            panel.SetActive(true);
            InputRouter.Enter(InputContext.Menu);
        }

        private void Close()
        {
            state = MenuState.Closed;
            panel.SetActive(false);
            InputRouter.Exit(InputContext.Menu);
        }
    }
}