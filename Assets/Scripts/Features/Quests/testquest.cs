using System;
using Core.Quests;
using UnityEngine;

namespace Features.Quests
{
    public class testquest:MonoBehaviour
    {
        void OnEnable()  => QuestUI.Changed += Log;
        void OnDisable() => QuestUI.Changed -= Log;

        void Log()
        {
            var snap = QuestUI.Get(QuestUI.TrackedId);
            if (snap == null)
            {
                Debug.Log("[QuestProbe] nothing tracked");   
                return;
            }
            
            Debug.Log($"[QuestProbe] {snap.Title}  ({snap.Status})");        
            foreach (var o in snap.Objectives)    
            Debug.Log($"      {(o.Completed ? "[x]" : "[ ]")} {o.Text}"); 
        }

    }
}