using UnityEngine;

public class PlayerMovementController3rdPOV : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform currentPitchSphere;
    [SerializeField] private Transform cameraRig;
    
    [Header("Tilt Movement")]
    [SerializeField] private float maxTiltSpeed = 1.5f;         // Base movement speed
    [SerializeField] private float tiltDeadzone = 7f;          // Deadzone for stability
    [SerializeField] private float maxTiltAngle = 30f;         // Maximum tilt angle
    [SerializeField] private float movementSmoothTime = 0.5f;  // Movement smoothing
    [SerializeField] private float velocitySmoothFactor = 0.92f; // Additional velocity smoothing
    [SerializeField] private bool useWorldSpaceControl = true;  // Toggle between world and relative control
    
    [Header("Movement Bounds")]
    [SerializeField] private Vector2 roomBounds = new Vector2(3f, 3f);
    
    [Header("Debug")]
    [SerializeField] private bool showDebugGizmos = true;
    [SerializeField] private bool showTiltDebug = false;
    
    // Private references
    private Transform centerEyeAnchor;
    private Vector3 worldSpaceVelocity;
    private Vector3 lastForward;

    private void Start()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        if (cameraRig == null)
        {
            cameraRig = GameObject.Find("[BuildingBlock] Camera Rig")?.transform;
        }

        if (cameraRig != null)
        {
            var trackingSpace = cameraRig.Find("TrackingSpace");
            if (trackingSpace != null)
            {
                centerEyeAnchor = trackingSpace.Find("CenterEyeAnchor");
            }
        }

        if (centerEyeAnchor == null)
        {
            Debug.LogError("Could not find CenterEyeAnchor!");
            enabled = false;
            return;
        }

        // Store initial forward direction
        lastForward = Vector3.ProjectOnPlane(centerEyeAnchor.forward, Vector3.up).normalized;

        // Initialize sphere position in front of player
        if (currentPitchSphere != null)
        {
            // Position at same spot as target sphere
            Vector3 startPos = centerEyeAnchor.position + centerEyeAnchor.forward * 1f;
            startPos.y = currentPitchSphere.position.y; // Preserve height set by visualizer
            currentPitchSphere.position = startPos;

            // Remove any physics components that might interfere
            foreach (var collider in currentPitchSphere.GetComponents<Collider>())
            {
                Destroy(collider);
            }
            
            var rigidbody = currentPitchSphere.GetComponent<Rigidbody>();
            if (rigidbody != null) Destroy(rigidbody);
        }
    }

    private void Update()
    {
        if (!ValidateComponents()) return;
        
        UpdateTiltBasedMovement();
        ClampPosition();
    }

    private bool ValidateComponents()
    {
        return currentPitchSphere != null && centerEyeAnchor != null;
    }

    private void UpdateTiltBasedMovement()
    {
        // Get raw tilt angles from head orientation
        Vector3 headUp = centerEyeAnchor.up;
        float forwardTilt = Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(headUp, Vector3.right), Vector3.right);
        float rightTilt = Vector3.SignedAngle(Vector3.up, Vector3.ProjectOnPlane(headUp, Vector3.forward), -Vector3.forward);

        // Calculate base movement direction
        Vector3 targetVelocity = Vector3.zero;

        // Apply movement based on tilt
        if (Mathf.Abs(forwardTilt) > tiltDeadzone)
        {
            float tiltAmount = (Mathf.Abs(forwardTilt) - tiltDeadzone) / (maxTiltAngle - tiltDeadzone);
            tiltAmount = Mathf.Clamp01(tiltAmount);
            targetVelocity.z = maxTiltSpeed * tiltAmount * Mathf.Sign(forwardTilt);
        }

        if (Mathf.Abs(rightTilt) > tiltDeadzone)
        {
            float tiltAmount = (Mathf.Abs(rightTilt) - tiltDeadzone) / (maxTiltAngle - tiltDeadzone);
            tiltAmount = Mathf.Clamp01(tiltAmount);
            targetVelocity.x = maxTiltSpeed * tiltAmount * Mathf.Sign(rightTilt);
        }

        // Transform movement direction based on control mode
        if (!useWorldSpaceControl)
        {
            // Update forward reference (smoothly)
            Vector3 currentForward = Vector3.ProjectOnPlane(centerEyeAnchor.forward, Vector3.up).normalized;
            lastForward = Vector3.Lerp(lastForward, currentForward, Time.deltaTime * 5f);
            
            // Transform movement relative to view direction
            Quaternion rotation = Quaternion.LookRotation(lastForward, Vector3.up);
            targetVelocity = rotation * targetVelocity;
        }

        // Apply two-stage smoothing
        worldSpaceVelocity = Vector3.Lerp(worldSpaceVelocity, targetVelocity, Time.deltaTime / movementSmoothTime);
        worldSpaceVelocity *= velocitySmoothFactor;

        // Update position, preserving height
        Vector3 newPosition = currentPitchSphere.position + worldSpaceVelocity * Time.deltaTime;
        newPosition.y = currentPitchSphere.position.y;
        currentPitchSphere.position = newPosition;

        if (showTiltDebug)
        {
            Debug.Log($"Tilt - Forward: {forwardTilt:F1}°, Right: {rightTilt:F1}°, Velocity: {worldSpaceVelocity}");
        }
    }

    private void ClampPosition()
    {
        Vector3 position = currentPitchSphere.position;
        Vector3 rigPosition = cameraRig.position;
        
        position.x = Mathf.Clamp(position.x, rigPosition.x - roomBounds.x, rigPosition.x + roomBounds.x);
        position.z = Mathf.Clamp(position.z, rigPosition.z - roomBounds.y, rigPosition.z + roomBounds.y);
        
        currentPitchSphere.position = position;
    }

    private void OnDrawGizmos()
    {
        if (!showDebugGizmos || cameraRig == null) return;

        // Draw room bounds
        Gizmos.color = Color.yellow;
        Vector3 center = cameraRig.position;
        Vector3 size = new Vector3(roomBounds.x * 2, 4f, roomBounds.y * 2);
        Gizmos.DrawWireCube(center + Vector3.up * 2f, size);

        if (showTiltDebug && centerEyeAnchor != null)
        {
            // Draw head orientation
            Gizmos.color = Color.blue;
            Gizmos.DrawRay(centerEyeAnchor.position, centerEyeAnchor.up);
            // Draw control direction reference
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(centerEyeAnchor.position, useWorldSpaceControl ? Vector3.forward : lastForward);
            // Draw movement velocity
            Gizmos.color = Color.green;
            Gizmos.DrawRay(currentPitchSphere.position, worldSpaceVelocity);
        }
    }
}