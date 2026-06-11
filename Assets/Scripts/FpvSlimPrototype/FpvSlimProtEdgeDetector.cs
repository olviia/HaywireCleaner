using System;
using UnityEngine;

namespace FpvSlimPrototype
{
    public class FpvSlimProtEdgeDetector:MonoBehaviour
    {
        [SerializeField] GameObject image;
        [SerializeField] private float checkDistance = 0.1f;
        



        private void Update()
        {
            Vector3 halfExtents = transform.localScale * 0.5f;
            bool blocked = Physics.BoxCast(transform.position, halfExtents,
                transform.forward,
                out RaycastHit hit, transform.rotation, checkDistance);

            if(blocked)
            {
                image.SetActive(true);
            } else{
                image.SetActive(false);
            }
        }
    }
}