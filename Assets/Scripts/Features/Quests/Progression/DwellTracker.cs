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
                    WorldState.SetFlag(FactKey, true);
                    Destroy(gameObject); //crear itself from the scene
                }
            }
        }

        public class AxisInputDwell : DwellTracker
        {
            private enum Axis {Vertical, Horizontal}

            [SerializeField] private Axis axis;
            private Vector2 latestMove;
            protected override string FactKey => 
                    axis == Axis.Vertical? FactKeys.TutorialPlayerMoved: FactKeys.TutorialPlayerRotated;
            protected override float Intensity() => 
                    Mathf.Abs(axis == Axis.Vertical? latestMove.y : latestMove.x);

            void OnEnable() => ModuleInput.OnIntent += OnIntent;
            void OnDisable() => ModuleInput.OnIntent -= OnIntent;

            private void OnIntent(Intent intent, Vector2 value)
            {
                if(intent == Intent.Move) latestMove = value;
            }
        }
    }
}