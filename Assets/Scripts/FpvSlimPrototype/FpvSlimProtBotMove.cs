using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FpvSlimPrototype
{
    public class FpvSlimProtBotMove:MonoBehaviour
    {
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private float rotateSpeed = 120f;
    
        private Rigidbody _rb;
        private float _moveInput;
        private float _rotateInput;

        public bool canBeSlim;
        private bool isSlim;
        
        

        void Awake()
        {
            _rb = GetComponent<Rigidbody>();
        }


        private void FixedUpdate()
        {
            ReadInput();
            Rotate();
            Move();
        }

        private void Update()
        {
            if (Keyboard.current.spaceKey.wasReleasedThisFrame)
            {
                GoSlim();
            }
        }

        void ReadInput()
        {
            _moveInput = Keyboard.current.wKey.isPressed ? 1f :                        
                Keyboard.current.sKey.isPressed ? -0.35f : 0f; 
            _rotateInput =  Keyboard.current.dKey.isPressed ? 1f :                    
                Keyboard.current.aKey.isPressed ? -1f : 0f;
        }

        void Move()
        {
            Vector3 horizontalVelocity = transform.forward * (_moveInput * moveSpeed); 
            Vector3 velocity = _rb.linearVelocity;    
            velocity.x = horizontalVelocity.x;
            velocity.z = horizontalVelocity.z;
            
            
            _rb.linearVelocity = velocity;
        }

        void Rotate()
        {
            float yaw = _rotateInput * rotateSpeed * Time.fixedDeltaTime;
            Quaternion deltaRotation = Quaternion.Euler(0f, yaw, 0f);
            _rb.MoveRotation(_rb.rotation * deltaRotation);
        }

        public void GoSlim()
        {
            if (!canBeSlim) return;
            isSlim = !isSlim;
            Vector3 scale = transform.localScale;
            scale.y = isSlim ? 0.4f : 1f;
            transform.localScale = scale;
        }


    }
}