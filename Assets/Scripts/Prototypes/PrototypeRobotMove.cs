using UnityEngine;
using UnityEngine.InputSystem;

public class PrototypeRobotMove : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotateSpeed = 120f;
    
    private CharacterController _controller;
    private float _moveInput;
    private float _rotateInput;

    void Awake()
    {
        _controller = GetComponent<CharacterController>();
    }

    void Update()
    {
        ReadInput();
        Rotate();
        Move();
    }

    void ReadInput()
    {
         _moveInput = Keyboard.current.wKey.isPressed ? 1f :                        
             Keyboard.current.sKey.isPressed ? -0.1f : 0f; 
         _rotateInput =  Keyboard.current.dKey.isPressed ? 1f :                    
             Keyboard.current.aKey.isPressed ? -1f : 0f;
    }

    void Move()
    {
        Vector3 velocity = _moveInput * moveSpeed*transform.forward;
        velocity.y = -9.81f;
        _controller.Move(velocity*Time.deltaTime);
    }

    void Rotate()
    {
        transform.Rotate(Vector3.up, _rotateInput*rotateSpeed * Time.deltaTime);
    }
}
