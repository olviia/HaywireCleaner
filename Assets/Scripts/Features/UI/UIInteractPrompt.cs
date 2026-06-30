using Core.Events;
using Core.Player;
using TMPro;
using UnityEngine;

namespace Features.UI
{
    /// <summary>
    /// this prompt does not instantiate the prefab, it turns the
    /// prefab that is already there, on and off
    /// </summary>
    public class UIInteractPrompt:MonoBehaviour
    {
        [SerializeField] private GameObject container; //put the prefaB HERE
        [SerializeField] private TMP_Text label;
        //might be changed to images later
        [SerializeField] private TMP_Text button;
        [SerializeField] private UIInteractPromptDisplayRequestSO displayRequest;

        void OnEnable()
        {
            displayRequest.Show += OnShow;
            displayRequest.Hide += OnHide;
        }

        void OnDisable()
        {
            displayRequest.Show -= OnShow;
            displayRequest.Hide -= OnHide;
        }

        void OnShow(string labelText, Intent intent, Transform interactionObject)
        {
            
        }

        void OnHide()
        {
            
        }

        void LateUpdate()
        {
            //move somehow
        }


    }
}