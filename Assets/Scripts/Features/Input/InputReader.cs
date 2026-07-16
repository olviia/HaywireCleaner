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
        private InputActionMap cinematic;
        private InputActionMap menu;
        
        private readonly Dictionary<Intent, InputAction> map = new();
        
        //here cach the actions that we get from intentbindings map
        //add new when intentBindings has more input
        private InputAction move;
        private InputAction interact;

        //for cutscenes
        private InputAction skip;
        
        //for UI menu
        private InputAction toggleMenu;
        private InputAction confirm;
        private void Awake()
        {
            player = actions.FindActionMap("Player");
            cinematic = actions.FindActionMap("Cinematic");
            menu = actions.FindActionMap("UI");
            
            move = actions.FindAction("Move");
            interact = actions.FindAction("Interact");
            
            skip = actions.FindAction("Skip");
            
            toggleMenu = actions.FindAction("ToggleMenu");
            confirm = actions.FindAction("Confirm");
            
            map[Intent.Move] = move;
            map[Intent.Interact] = interact;

            interact.performed += OnInteractPerformed;

            skip.performed += OnSkipPerformed;
            
            toggleMenu.performed += OnToggleMenuPerformed;
            confirm.started += OnConfirmStarted;
            confirm.canceled += OnConfirmCanceled;
            
            
            glyphs = new InputGlyphProvider(map, actions);
            GlyphInput.Register(glyphs); //give the glyphs to the core
        }

        private void OnEnable()
        {
            InputRouter.ContextChangedTo += Apply;
            menu.Enable(); //always turned on for ui clicking
            Apply(InputRouter.ActiveContext);
        }

        private void OnDisable() => InputRouter.ContextChangedTo -= Apply;

        private void OnDestroy()
        {
            interact.performed -= OnInteractPerformed;
            skip.performed -= OnSkipPerformed;
            toggleMenu.performed -= OnToggleMenuPerformed;
            confirm.started -= OnConfirmStarted;
            confirm.canceled -= OnConfirmCanceled;
            glyphs?.Dispose();
        }

        //continious actions go into update
        void Update()
        {
            ModuleInput.RaiseMove(move.ReadValue<Vector2>());
        }
        private void Apply(InputContext context)
        {
            player.Disable();
            cinematic.Disable();

            var activeMap = context switch
            {
                InputContext.Menu => menu,
                InputContext.Cutscene => cinematic,
                InputContext.Gameplay => player,
                _ => player
            };
            activeMap.Enable();
        }

        //discrete actions are raising events
        void OnInteractPerformed(InputAction.CallbackContext context)
        {
            ModuleInput.RaiseInteract();
            ModuleInput.RaiseStopCharging();
        }

        
        //cutscenes
        void OnSkipPerformed(InputAction.CallbackContext _) => CutsceneInput.RaiseSkip();

        
        
        //menu
        void OnToggleMenuPerformed(InputAction.CallbackContext _) => MenuInput.RaiseToggleMenu();
        void OnConfirmStarted(InputAction.CallbackContext _) => MenuInput.RaiseConfirmDown();
        void OnConfirmCanceled(InputAction.CallbackContext _) => MenuInput.RaiseConfirmUp();

        
    }
}