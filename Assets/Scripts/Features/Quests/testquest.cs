using System;
using Core.Quests;
using UnityEngine;

namespace Features.Quests
{
    public class testquest:MonoBehaviour
    {
        void OnEnable()  => QuestInfo.Changed += Log;
        void OnDisable() => QuestInfo.Changed -= Log;

        void Log()
        {
            var snap = QuestInfo.Get(QuestInfo.TrackedId);
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