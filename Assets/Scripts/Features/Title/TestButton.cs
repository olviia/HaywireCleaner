using Core;
using UnityEngine;

namespace Features.Title
{
    public class TestButton:MonoBehaviour
    {
        public void OnGoToGameplayButton()
        {
            SceneStateMachine.ChangeSceneTo(GameScene.Gameplay);
        }
    }
}