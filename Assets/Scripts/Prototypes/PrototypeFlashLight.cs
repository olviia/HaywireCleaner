using UnityEngine;
using UnityEngine.InputSystem;

namespace Prototypes
{
    public class PrototypeFlashLight:MonoBehaviour
    {
        
        void Update()
        {
            Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());      
            transform.rotation = Quaternion.LookRotation(ray.direction);

        }
    }
}