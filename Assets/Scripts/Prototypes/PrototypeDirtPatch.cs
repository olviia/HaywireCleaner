using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PrototypeDirtPatch : MonoBehaviour
{

    public bool isInRange;
    public bool _isCleaned;
    [SerializeField] private GameObject collectSymbol;
    [SerializeField] private ParticleSystem particles;


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
    }

    public bool CanBeCleaned()
    {
        return isInRange && !_isCleaned;
    }
    

}
