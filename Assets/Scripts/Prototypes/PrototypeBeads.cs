
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace Prototypes
{
    public class PrototypeBeads:MonoBehaviour
    {
        [SerializeField] private PrototypeDirtPatch[] patches;
        [SerializeField] private GameObject popup;
        [SerializeField] private float popupDuration  = 1f;
        [SerializeField] private GameObject flashLight;
        
        HashSet<int> beadIndexes = new HashSet<int>();
        

        private int collected;

        void Start()
        {
            while (beadIndexes.Count < 3)
            {
                beadIndexes.Add(Random.Range(0, patches.Length));
            }
        }

        public void CheckBead(PrototypeDirtPatch patch)
        {
            if (beadIndexes.Contains(Array.IndexOf(patches, patch)))
            {
                collected++;
                StartCoroutine(BeadPopup());
                Debug.Log("beads collected: " + collected);
                if (collected == 3)
                {
                    popupDuration *= 2;
                    
                    Debug.Log("3 beads collected");
                    //add light to robot
                    flashLight.SetActive(true);
                }
            }
        }
        
        IEnumerator BeadPopup()
        {
            popup.SetActive(true);
            popup.transform.localScale = Vector3.zero;
            Image popupImage = popup.GetComponent<Image>();
           
  
            // scale up
            float t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / (popupDuration*0.2f);
                popup.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one,   
                    t);
                yield return null;
            }

            yield return new WaitForSeconds(popupDuration*0.5f);

            // fade out
            t = 0f;
            while (t < 1f)
            {
                t += Time.deltaTime / (popupDuration*0.3f);
                Color c = popupImage.color;
                c.a = Mathf.Lerp(1f, 0f, t);
                popupImage.color = c;

                yield return null;
            }

            popup.SetActive(false);
        }

    }
}