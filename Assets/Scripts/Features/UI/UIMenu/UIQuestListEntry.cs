using System;
using Core.Quests;
using TMPro;
using UnityEngine;

namespace Features.UI.UIMenu
{
    public class UIQuestListEntry:MonoBehaviour
    {
        [SerializeField] private TMP_Text title;
        [SerializeField] private TMP_Text description;
        [SerializeField] private Color completedColor;
        [SerializeField] private FontStyles style;
        
        public event Action<string> Clicked;

        private string id;

        public void Bind(QuestSnapshot snapshot)
        {
            id =  snapshot.Id;
            title.text = snapshot.Title;
            if (snapshot.StageStory.Count > 0)
                description.text = snapshot.StageStory[^1]; 

            if (snapshot.Status == QuestStatus.Completed)
            {
                title.color = completedColor;
                title.fontStyle = style;
            }
        }

        public void Click()
        {
            Clicked?.Invoke(id);
        }
        
        
    }
}