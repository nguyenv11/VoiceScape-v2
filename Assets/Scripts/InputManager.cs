using UnityEngine;

public class InputManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Canvas leftDisplayCanvas;
    [SerializeField] private Canvas rightDisplayCanvas;
    
    private AudioAnalyzer audioAnalyzer;
    private OVRCameraRig cameraRig;
    private Transform leftAnchor;
    private Transform rightAnchor;
    private Transform centerAnchor;
    
    private void Start()
    {
        audioAnalyzer = GetComponent<AudioAnalyzer>();
        cameraRig = Object.FindFirstObjectByType<OVRCameraRig>();
        
        if (cameraRig != null)
        {
            leftAnchor = cameraRig.leftHandAnchor;
            rightAnchor = cameraRig.rightHandAnchor;
            centerAnchor = cameraRig.centerEyeAnchor;
        }
        else
        {
            Debug.LogError("OVRCameraRig not found in scene!");
        }

        // Configure canvases
        if (leftDisplayCanvas != null)
        {
            leftDisplayCanvas.renderMode = RenderMode.WorldSpace;
        }
        if (rightDisplayCanvas != null)
        {
            rightDisplayCanvas.renderMode = RenderMode.WorldSpace;
        }
    }
    
    private void Update()
    {
        UpdateControllerPositions();
        HandleControllerInput();
    }
    
    private void UpdateControllerPositions()
    {
        if (cameraRig == null) return;

        // Update left display position
        if (leftDisplayCanvas != null && leftAnchor != null)
        {
            Vector3 leftPos = leftAnchor.position + (leftAnchor.forward * 0.1f);
            leftDisplayCanvas.transform.position = leftPos;
            leftDisplayCanvas.transform.LookAt(centerAnchor);
            leftDisplayCanvas.transform.Rotate(0, 180, 0); // Face user
        }
        
        // Update right display position
        if (rightDisplayCanvas != null && rightAnchor != null)
        {
            Vector3 rightPos = rightAnchor.position + (rightAnchor.forward * 0.1f);
            rightDisplayCanvas.transform.position = rightPos;
            rightDisplayCanvas.transform.LookAt(centerAnchor);
            rightDisplayCanvas.transform.Rotate(0, 180, 0); // Face user
        }
    }
    
    private void HandleControllerInput()
    {
        // Toggle displays with primary buttons (X and A)
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.LTouch))
        {
            leftDisplayCanvas?.gameObject.SetActive(!leftDisplayCanvas.gameObject.activeSelf);
        }
        
        if (OVRInput.GetDown(OVRInput.Button.One, OVRInput.Controller.RTouch))
        {
            rightDisplayCanvas?.gameObject.SetActive(!rightDisplayCanvas.gameObject.activeSelf);
        }

        // Haptic feedback based on amplitude
        if (audioAnalyzer != null)
        {
            float amplitude = audioAnalyzer.Amplitude;
            if (amplitude > 0.5f)
            {
                OVRInput.SetControllerVibration(0.5f, 0.5f, OVRInput.Controller.LTouch);
            }
        }
    }
}