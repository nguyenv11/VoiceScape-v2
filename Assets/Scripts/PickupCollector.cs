using UnityEngine;

[RequireComponent(typeof(SphereCollider))]
public class PickupCollector : MonoBehaviour
{
    private PickupManager pickupManager;
    private SphereCollider triggerCollider;
    
    [SerializeField] private float collectionRadius = 2f;
    [SerializeField] private bool showDebugSphere = true;

    private void Start()
    {
        pickupManager = FindFirstObjectByType<PickupManager>();
        if (pickupManager == null)
        {
            Debug.LogError("PickupCollector: Could not find PickupManager in scene!");
            enabled = false;
            return;
        }

        triggerCollider = GetComponent<SphereCollider>();
        triggerCollider.isTrigger = true;
        triggerCollider.radius = collectionRadius;

        Debug.LogError($"PickupCollector initialized with radius {collectionRadius}");
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.LogError($"Hit something: {other.gameObject.name}");
        
        // Get pickup index
        int index = pickupManager.GetPickupIndex(other.gameObject);
        Debug.LogError($"Found pickup at index: {index}");

        // Always try to collect when we hit a pickup
        pickupManager.OnPickupCollected(index);
    }

    private void OnDrawGizmos()
    {
        if (!showDebugSphere) return;
        
        Gizmos.color = new Color(1, 1, 0, 0.3f);
        Gizmos.DrawWireSphere(transform.position, collectionRadius);
    }
}