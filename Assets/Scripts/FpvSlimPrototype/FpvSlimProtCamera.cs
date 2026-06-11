using System;
using UnityEngine;

namespace FpvSlimPrototype
{
    public class FpvSlimProtCamera: MonoBehaviour
    {
        [SerializeField] private Transform placeholder;


        private void LateUpdate()
        {
            transform.position = placeholder.position;
            transform.rotation = placeholder.rotation;
        }
    }
}