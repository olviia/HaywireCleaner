using System;
using Core.Input;
using UnityEngine;

namespace Features.UI
{
    public class UIPopup:MonoBehaviour
    {
        private void OnEnable()=>
            InputRouter.Enter(InputContext.Menu);
        
        private void OnDisable() =>
            InputRouter.Exit(InputContext.Menu);
    }
}