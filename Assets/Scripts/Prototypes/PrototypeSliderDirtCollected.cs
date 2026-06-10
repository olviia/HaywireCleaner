using UnityEngine;
using UnityEngine.UI;

namespace Prototypes
{
    public class PrototypeSliderDirtCollected:MonoBehaviour
    {
        private int cleaned;
        [SerializeField] private int total;
        [SerializeField] private GameObject finalpopup;

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

        void Update()
        {
            if (cleaned == total)
            {
                finalpopup.SetActive(true);
            }
        }
        
    }
}