using UnityEngine;
using UnityEngine.UI;

namespace Prototypes
{
    public class PrototypeSliderDirtCollected:MonoBehaviour
    {
        private int cleaned;
        [SerializeField] private int total;

        private Slider slider;

        void Awake()
        {
            slider = GetComponent<Slider>();
        }
        
        public void OnDirtCleaned()
        {
            cleaned++;
            Debug.Log((float)cleaned/total);
            slider.value = (float)cleaned/total;
        }
    }
}