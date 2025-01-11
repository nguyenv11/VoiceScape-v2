using UnityEngine;

public class TestPersistence : MonoBehaviour 
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Transform sphereTransform;
    
    void Start()
    {
        Debug.Log("Test script started");
    }
}