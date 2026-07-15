using Core.Input;
using UnityEngine;

namespace Features.UI.UIMenu
{
    public class GamePauseInMenu:MonoBehaviour
    {
        private void OnEnable() => InputRouter.ContextChangedTo += Apply;

        private void OnDisable()
        {
            InputRouter.ContextChangedTo -= Apply;
            Time.timeScale = 1f;
        }

        private void Apply(InputContext context) => Time.timeScale =
            context switch
            {
                InputContext.Menu => 0f,
                _ => 1f,
            };
    }
}