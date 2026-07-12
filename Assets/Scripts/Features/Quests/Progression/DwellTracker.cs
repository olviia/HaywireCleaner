using System;
using Core.Input;
using Core.Player;
using Core.SaveSystem;
using UnityEngine;

namespace Features.Quests.Progression
{
    public abstract class DwellTracker:MonoBehaviour
    {
        [SerializeField] private float requiredSeconds = 2f;
        [SerializeField] private float deadzone = 0.1f;

        private float accumulated;
        protected abstract string FactKey { get; }
        protected abstract float Intensity(); //how active am i right now

        void Update()
        {
            if (Intensity() > deadzone)
            {
                accumulated += Time.deltaTime;
                if (accumulated >= requiredSeconds)
                {
                    Debug.Log($"[Fact] fact key '{FactKey}' added");
                    WorldState.SetFlag(FactKey, true);
                    Destroy(gameObject); //crear itself from the scene
                }
            }
        }
    }
}