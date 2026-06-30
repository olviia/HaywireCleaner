using System.Collections.Generic;
using Core.Input;
using Core.Player;
using UnityEngine;
using UnityEngine.InputSystem;


namespace Features.Input
{
    
    /// <summary>
    /// routes input to the player
    /// </summary>
    public class InputReader:MonoBehaviour
    {
        [SerializeField] private InputActionAsset actions; 
        private InputGlyphProvider glyphs; //we'll get the map here from installer

        private InputActionMap player;
        private readonly Dictionary<Intent, InputAction> map = new();
        
        //here cach the actions that we get from intentbindings map
        //add new when intentBindings has more input
        private InputAction move;
        private InputAction interact;

        private void Awake()
        {
            player = actions.FindActionMap("Player");
            
            move = actions.FindAction("Move");
            interact = actions.FindAction("Interact");
            
            map[Intent.Move] = move;
            map[Intent.Interact] = interact;

            interact.performed += OnInteractPerformed;
            
            glyphs = new InputGlyphProvider(map, actions);
            GlyphInput.Register(glyphs); //give the glyphs to the core
        }

        private void OnEnable()=> player.Enable();
        
        private void OnDisable() => player.Disable();

        private void OnDestroy()
        {
            interact.performed -= OnInteractPerformed;
            glyphs?.Dispose();
        }

        //continious actions go into update
        void Update()
        {
            ModuleInput.RaiseMove(move.ReadValue<Vector2>());
        }

        //discrete actions are raising events
        void OnInteractPerformed(InputAction.CallbackContext context)
                                        => ModuleInput.RaiseInteract();
        
    }
}