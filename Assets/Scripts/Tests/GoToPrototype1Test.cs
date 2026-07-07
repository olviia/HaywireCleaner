using System;
using Core;
using Core.Player;
using Core.SceneControls;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Tests
{
    public class GoToPrototype1Test:MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.GetComponentInParent<ActorHost>() != null)
            {
                SceneStateMachine.ChangeSceneTo(GameScene.Prototype1);
            }
        }
    }
}