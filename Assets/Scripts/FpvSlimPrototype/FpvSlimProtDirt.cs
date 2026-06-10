using Prototypes;
using UnityEngine;

namespace FpvSlimPrototype
{
    public class FpvSlimProtDirt:MonoBehaviour
    {
        public bool isInRange;
        public bool _isCleaned;
        [SerializeField] private GameObject collectSymbol;
        [SerializeField] private ParticleSystem particles;
        [SerializeField] private PrototypeSliderDirtCollected slider;
        [SerializeField] private FpvSlimProtBeads beads;


        // Update is called once per frame
        void Update()
        {
            if (isInRange && !_isCleaned)
            {
                collectSymbol.SetActive(true);
            }
            else
            {
                collectSymbol.SetActive(false);
            }
        }

        public void Clean()
        {
            collectSymbol.SetActive(false);
            transform.GetComponent<MeshRenderer>().enabled = false;
            particles.Play();
            _isCleaned = true;
            slider.OnDirtCleaned();

            beads.CheckBead(this);
        }

        public bool CanBeCleaned()
        {
            return isInRange && !_isCleaned;
        }
    }
}