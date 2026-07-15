using System;
using System.Text;
using Core.Quests;
using TMPro;
using UnityEngine;

namespace Features.UI
{
    public class HUDQuest:MonoBehaviour
    {
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text objectives;
        [SerializeField] private Color completedColor;

        
        private void OnEnable()
        {
            QuestInfo.Changed += Refresh;
        }

        private void OnDisable()
        {
            QuestInfo.Changed -= Refresh;
        }

        private void Refresh()
        {
            var snapshot = QuestInfo.Get(QuestInfo.TrackedId);
            if (snapshot == null)
            {
                canvasGroup.alpha = 0f;
                return;
            }
            title.text = snapshot.Title;
            var sb = new StringBuilder();
            foreach (var line in snapshot.Objectives)
                sb.AppendLine(line.Completed ? Strike(line.Text) : line.Text);

            objectives.text  = sb.ToString();
            canvasGroup.alpha = 1f;
        }

        private string Strike(string text) =>
            $"<color=#{ColorUtility.ToHtmlStringRGB(completedColor)}><s>{text}</s></color>";
    }
}