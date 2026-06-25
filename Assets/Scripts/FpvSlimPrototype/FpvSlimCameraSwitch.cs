using Prototypes;
using UnityEngine;

namespace FpvSlimPrototype
{
    public class FpvSlimCameraSwitch:MonoBehaviour
    {
        
        [SerializeField] private GameObject outline;
        private void OnTriggerEnter(Collider other)
        { Debug.Log($"Trigger enter: {other.gameObject.name} (tag: {other.tag})");  
            if (other.tag.Equals("PrototypeSlimArea"))
            {
                Camera.main.GetComponent<PrototypeCamera>().enabled = false;
                Camera.main.GetComponent<FpvSlimProtCamera>().enabled = true;
                outline.SetActive(true);
            }
        }



        private void OnTriggerExit(Collider other)
        {
            if (other.tag.Equals("PrototypeSlimArea"))
            {
                Camera.main.GetComponent<PrototypeCamera>().enabled = true;
                Camera.main.GetComponent<FpvSlimProtCamera>().enabled = false;
                outline.SetActive(false);
                
            }
        }
    }
}