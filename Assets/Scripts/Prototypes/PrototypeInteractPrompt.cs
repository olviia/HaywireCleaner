using System;
using UnityEngine;

public class PrototypeInteractPrompt : MonoBehaviour
{
    private Camera cam;

    void Start()
    {
        cam = Camera.main;
    }
    void Update()
    {
        Vector3 dir = cam.transform.position - transform.position;
        dir.y = 0;
        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        transform.localEulerAngles = new Vector3(0, angle, 0);
    
    }

}
