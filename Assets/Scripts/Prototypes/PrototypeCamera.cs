using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PrototypeCamera : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float height;
    [SerializeField] private float distance = 0f;
    [SerializeField] private float sensitivity = 2f;
    [SerializeField] private float minPitch = -20f;
    [SerializeField] private float maxPitch = 60f;

    private float _yaw;
    private float _pitch;

    void Awake()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void LateUpdate()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();
        _yaw += mouseDelta.x * sensitivity;
        _pitch -= mouseDelta.y * sensitivity;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);
        
        Quaternion yawOnly = Quaternion.Euler(0f, _yaw, 0f);
        transform.position = target.position - yawOnly * Vector3.forward * distance + height* Vector3.up;
        
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

    }
}
