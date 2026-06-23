using System;
using Core;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Features.Input
{
    public class InputReader:MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions;

        private InputActionMap player;
        private InputAction move;
        private InputAction interact;

        private void OnEnable()
        {
            player = actions.FindActionMap("Player");
            move = actions.FindAction("Move");
            interact = actions.FindAction("Interact");

            interact.performed += OnInteractPerformed;
            player.Enable();
        }

        void OnDisable()
        {
            interact.performed -= OnInteractPerformed;
        }

        
        //continious actions go into update
        void Update()
        {
            PlayerCommands.SetMove(move.ReadValue<Vector2>());
        }

        //discrete actions are raising events
        void OnInteractPerformed(InputAction.CallbackContext context)
        {
            PlayerCommands.RaiseInteract();
        }
    }
}