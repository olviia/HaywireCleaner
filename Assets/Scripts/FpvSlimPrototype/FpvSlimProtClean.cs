using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FpvSlimPrototype
{
    public class FpvSlimProtClean:MonoBehaviour
    {
        [SerializeField] private float swipeSpeed = 0.12f;
        [SerializeField] private Transform swipeObject;
        private FpvSlimProtDirt dirt;
        
        private void OnTriggerEnter(Collider other)
        {
            if (other.tag.Equals("PrototypeDirt"))
            {
                dirt = other.GetComponent<FpvSlimProtDirt>();
                if(!dirt._isCleaned) dirt.isInRange = true;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (other.tag.Equals("PrototypeDirt"))
            {
                if (dirt == other.GetComponent<FpvSlimProtDirt>())
                {
                    if(!dirt._isCleaned) dirt.isInRange = false;
                    dirt = null;
                }
            }
        }
        
        void Update()
        {
            if (Keyboard.current.eKey.isPressed)
            {
                if (dirt != null && dirt.CanBeCleaned())
                {
                    dirt.Clean();

                    StartCoroutine(Swipe(dirt.transform.position));
                }
            }
            
        }

        IEnumerator Swipe(Vector3 swipeDirection)
        {
            Vector3 startWorld = swipeObject.position;
            Vector3 startLocal = swipeObject.localPosition;
            
            float t = 0f;

            while (t < 1f)
            {
                t+=Time.deltaTime/swipeSpeed;
                swipeObject.position = Vector3.Lerp(startWorld, swipeDirection, t);
                yield return null;
            }
            Vector3 returnFromLocal = swipeObject.localPosition;

            t = 0;
            while (t < 1f)
            {
                t+=Time.deltaTime/swipeSpeed;
                swipeObject.localPosition = Vector3.Lerp(returnFromLocal, startLocal, t);
                yield return null;
            }
        }
    
    }
}