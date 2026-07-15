using System.Text;
using Core.Quests;
using TMPro;
using UnityEngine;

namespace Features.UI.UIMenu
{
    public class UIQuestDetailPanel:MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Color completedColor;

        public void Show(QuestSnapshot snapshot)
        {
            title.text = snapshot.Title;
            string open = $"<color=#{ColorUtility.ToHtmlStringRGB(completedColor)}><s>";
            const string close = "</s></color>";

            var sb = new StringBuilder();
            int last = snapshot.StageStory.Count - 1;
            bool isActive = snapshot.Status == QuestStatus.Active && last >= 0;

            if (isActive)
            {
                sb.AppendLine(snapshot.StageStory[last]);
                foreach(var obj in snapshot.Objectives)
                    sb.AppendLine("   " + (obj.Completed?open + obj.Text + close: obj.Text));

                for (int i = last - 1; i >= 0; i--)
                {
                    sb.AppendLine(open + snapshot.StageStory[i] + close);
                }
            }
            else
            {
                for(int i = last; i>=0; i--)
                    sb.AppendLine(open + snapshot.StageStory[i] + close);
            }
            
            description.text = sb.ToString();
        }
    }
}