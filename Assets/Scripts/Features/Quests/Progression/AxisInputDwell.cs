using Core.Input;
using Core.Player;
using Core.SaveSystem;
using UnityEngine;

namespace Features.Quests.Progression
{
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