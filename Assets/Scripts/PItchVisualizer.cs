using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PitchVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MPMAudioAnalyzer audioAnalyzer;
    [SerializeField] private Transform currentPitchSphere;
    [SerializeField] public Transform targetPitchSphere;
    
    [Header("Layout")]
    [SerializeField] private float visualizerDistance = 1f;      // Distance in front
    [SerializeField] private float maxVerticalAngle = 20f;      // Reduced angle range
    [SerializeField] private float verticalOffset = -0.2f;      // Lower the whole visualization
    [SerializeField] private float sphereBaseScale = 0.05f;     // Current size seems good
    
    [Header("Visual Settings")]
    private Quaternion fixedRotation;                          // Store the initial rotation
    private Vector3 targetPosition;                            // Smoothed position target

    [Header("Frequency Settings")]
    [SerializeField] public float targetFrequency = 130.81f;    // Default to A4
    [SerializeField] private float frequencyTolerance = 5f;   // Hz tolerance
    
    [Header("Colors")]
    [SerializeField] private Color targetColor = new Color(0f, 1f, 0f, 0.8f);     // Bright green
    [SerializeField] private Color normalColor = new Color(0f, 0.5f, 1f, 0.8f);   // Sky blue
    [SerializeField] private Color closeColor = new Color(1f, 1f, 0f, 0.8f);      // Yellow

    [Header("Smoothing")]
    [SerializeField] private float positionSmoothTime = 0.1f;
    [SerializeField] private float colorSmoothTime = 0.1f;
    [SerializeField] private float scaleSmooth = 5f;

    [Header("Voice Range")]
    public float minFrequency = 80f;      // Adjusted for comfortable humming
    public float maxFrequency = 300f;      // Reduced from 1000Hz

    [Header("Alignment Feedback")]
    [SerializeField] private float perfectMatchThreshold = 2f;  // Hz difference for perfect match
    [SerializeField] private Color matchedColor = Color.red;    // Color when perfectly aligned

    [Header("Label References")]
    [SerializeField] private TMPro.TextMeshPro currentFrequencyLabel;
    [SerializeField] private TMPro.TextMeshPro confidenceLabel;
    [SerializeField] private TMPro.TextMeshPro minFreqLabel;
    [SerializeField] private TMPro.TextMeshPro maxFreqLabel;
    [SerializeField] private TMPro.TextMeshPro targetFrequencyLabel;
    [SerializeField] private float labelOffset = 0.15f;       // Distance from sphere 
    private Vector3 currentVelocity;
    private float currentScale = 1f;
    private Vector3 baseForward = Vector3.forward;
    private Transform cameraRig;

    [System.NonSerialized] private Material currentSphereMaterial;
    [System.NonSerialized] private Material targetMaterial;
    [System.NonSerialized] private Color currentColor;

     private void Start()
    {
        if (!ValidateComponents()) return;

        // Get reference to camera rig once
        cameraRig = Camera.main.transform.parent.parent;
        
        // Set initial fixed orientation
        transform.rotation = Quaternion.LookRotation(baseForward);
        fixedRotation = transform.rotation;

        InitializeMaterials();
        InitializePositions();
        SetupLabels();  
    }
    private void InitializePositions()
    {
        // Base position in front of rig
        Vector3 basePosition = cameraRig.position + (baseForward * visualizerDistance);
        transform.position = basePosition + Vector3.up * verticalOffset;
        
        // Set target sphere position
        targetPitchSphere.position = GetPositionForFrequency(targetFrequency);
    }

    private void SetupLabels()
    {        
        // Set up range labels
        if (minFreqLabel != null)
        {
            minFreqLabel.text = $"{minFrequency:F0}Hz";
            Vector3 minPos = GetPositionForFrequency(minFrequency);
            minFreqLabel.transform.position = minPos + Vector3.left * labelOffset;
        }
        
        if (maxFreqLabel != null)
        {
            maxFreqLabel.text = $"{maxFrequency:F0}Hz";
            Vector3 maxPos = GetPositionForFrequency(maxFrequency);
            maxFreqLabel.transform.position = maxPos + Vector3.left * labelOffset;
        }
        
        if (targetFrequencyLabel != null)
        {
            targetFrequencyLabel.text = $"{targetFrequency:F0}Hz";
            targetFrequencyLabel.transform.position = targetPitchSphere.position + Vector3.right * labelOffset;
        }
    }


    private bool ValidateComponents()
    {
        if (audioAnalyzer == null)
        {
            Debug.LogError("MPMAudioAnalyzer not assigned to PitchVisualizer");
            return false;
        }
        if (currentPitchSphere == null)
        {
            Debug.LogError("Current Pitch Sphere reference not set");
            return false;
        }
        if (targetPitchSphere == null)
        {
            Debug.LogError("Target Pitch Sphere reference not set");
            return false;
        }
        return true;
    }

    private void InitializeMaterials()
    {
        // Setup Current Sphere
        var renderer = currentPitchSphere.gameObject.GetComponent<MeshRenderer>();
        if (renderer == null)
        {
            if (currentPitchSphere.gameObject.GetComponent<MeshFilter>() == null)
            {
                var meshFilter = currentPitchSphere.gameObject.AddComponent<MeshFilter>();
                meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            }
            
            renderer = currentPitchSphere.gameObject.AddComponent<MeshRenderer>();
        }

        // Create material with proper URP settings
        currentSphereMaterial = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        currentSphereMaterial.SetFloat("_Surface", 1); // 0 = opaque, 1 = transparent
        currentSphereMaterial.SetFloat("_Blend", 0);   // 0 = alpha, 1 = premultiply
        currentSphereMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        currentSphereMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        currentSphereMaterial.SetFloat("_ZWrite", 0);
        currentSphereMaterial.renderQueue = 3000;
        currentSphereMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        renderer.sharedMaterial = currentSphereMaterial;
        currentColor = normalColor;
        currentSphereMaterial.color = currentColor;
        
        // Set initial scale
        currentPitchSphere.localScale = Vector3.one * sphereBaseScale;
        
        // Setup Target Sphere
        var targetRenderer = targetPitchSphere.gameObject.GetComponent<MeshRenderer>();
        if (targetRenderer == null)
        {
            if (targetPitchSphere.gameObject.GetComponent<MeshFilter>() == null)
            {
                var meshFilter = targetPitchSphere.gameObject.AddComponent<MeshFilter>();
                meshFilter.mesh = Resources.GetBuiltinResource<Mesh>("Sphere.fbx");
            }
            
            targetRenderer = targetPitchSphere.gameObject.AddComponent<MeshRenderer>();
        }

        // Create target material with proper URP settings
        targetMaterial = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
        targetMaterial.SetFloat("_Surface", 1);
        targetMaterial.SetFloat("_Blend", 0);
        targetMaterial.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
        targetMaterial.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        targetMaterial.SetFloat("_ZWrite", 0);
        targetMaterial.renderQueue = 3000;
        targetMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
        targetRenderer.sharedMaterial = targetMaterial;
        targetMaterial.color = targetColor;
        
        targetPitchSphere.localScale = Vector3.one * sphereBaseScale;
    }

    private void Update()
    {
        if (!ValidateComponents()) return;

        // Update position
        Vector3 targetPos = GetDesiredPosition();
        transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
        
        // Keep original rotation
        transform.rotation = fixedRotation;

        UpdateSpherePositions();
        UpdateLabels();
        UpdateLabelOrientations();
    }

    private Vector3 GetDesiredPosition()
    {
        if (cameraRig == null) return transform.position;
        
        // Get the camera's forward direction but only on the horizontal plane
        Vector3 cameraForward = Camera.main.transform.forward;
        Vector3 horizontalForward = Vector3.ProjectOnPlane(cameraForward, Vector3.up).normalized;
        
        // Position in front of camera, maintaining fixed height
        Vector3 position = cameraRig.position + (horizontalForward * visualizerDistance);
        position.y = cameraRig.position.y + verticalOffset;  // Use fixed vertical offset
        
        return position;
    }

    public Vector3 GetPositionForFrequency(float frequency)
    {
        // Calculate vertical offset based on frequency
        float normalizedFreq = (Mathf.Log(frequency) - Mathf.Log(minFrequency)) / 
                             (Mathf.Log(maxFrequency) - Mathf.Log(minFrequency));
        float angle = Mathf.Lerp(-maxVerticalAngle, maxVerticalAngle, normalizedFreq);
        float heightOffset = verticalOffset + Mathf.Tan(angle * Mathf.Deg2Rad) * visualizerDistance;
        
        // Position relative to rig
        Vector3 horizontalForward = Vector3.ProjectOnPlane(Camera.main.transform.forward, Vector3.up).normalized;
return cameraRig.position + (horizontalForward * visualizerDistance) + (Vector3.up * heightOffset);
    }

    private void UpdateSphereVisuals()
    {
        // Check pitch match
        float freqDifference = Mathf.Abs(audioAnalyzer.Frequency - targetFrequency);
        
        // Update colors based on distance from target frequency
        Color targetColorForState;
        if (freqDifference <= perfectMatchThreshold && audioAnalyzer.Confidence > 0.8f)
        {
            targetColorForState = matchedColor;
        }
        else if (freqDifference <= frequencyTolerance)
        {
            targetColorForState = targetColor;
        }
        else if (freqDifference <= frequencyTolerance * 3f)
        {
            targetColorForState = closeColor;
        }
        else
        {
            targetColorForState = normalColor;
        }

        // Smoothly transition colors
        currentColor = Color.Lerp(currentColor, targetColorForState, Time.deltaTime / colorSmoothTime);
        currentSphereMaterial.color = currentColor;

        // Update scale based on confidence
        float targetScale = Mathf.Lerp(0.8f, 1.2f, audioAnalyzer.Confidence);
        currentScale = Mathf.Lerp(currentScale, targetScale * sphereBaseScale, Time.deltaTime * scaleSmooth);
        currentPitchSphere.localScale = Vector3.one * currentScale;

        // Update target sphere color
        targetMaterial.color = targetColor;
        targetPitchSphere.localScale = Vector3.one * sphereBaseScale;
    }

    private void UpdateSpherePositions()
    {
        if (!audioAnalyzer.IsVoiceDetected) return;

        Vector3 targetPos = GetPositionForFrequency(audioAnalyzer.Frequency);
        currentPitchSphere.position = Vector3.SmoothDamp(
            currentPitchSphere.position,
            targetPos,
            ref currentVelocity,
            positionSmoothTime
        );

        // Set target sphere position
        targetPitchSphere.position = GetPositionForFrequency(targetFrequency);

        UpdateSphereVisuals();
    }

    private void UpdateLabels()
    {
        if (audioAnalyzer.IsVoiceDetected)
        {
            if (currentFrequencyLabel != null)
            {
                currentFrequencyLabel.text = $"{audioAnalyzer.Frequency:F0}Hz";
                currentFrequencyLabel.transform.position = currentPitchSphere.position + Vector3.right * labelOffset;
            }

            if (confidenceLabel != null)
            {
                confidenceLabel.text = $"{(audioAnalyzer.Confidence * 100):F0}%";
                confidenceLabel.transform.position = currentPitchSphere.position + Vector3.left * labelOffset;
            }
        }
        else
        {
            if (currentFrequencyLabel != null) currentFrequencyLabel.text = "";
            if (confidenceLabel != null) confidenceLabel.text = "";
        }

        // Update range labels
        if (minFreqLabel != null)
        {
            Vector3 minPos = GetPositionForFrequency(minFrequency);
            minFreqLabel.transform.position = minPos + Vector3.left * labelOffset;
            minFreqLabel.text = $"{minFrequency:F0}Hz";
        }
        
        if (maxFreqLabel != null)
        {
            Vector3 maxPos = GetPositionForFrequency(maxFrequency);
            maxFreqLabel.transform.position = maxPos + Vector3.left * labelOffset;
            maxFreqLabel.text = $"{maxFrequency:F0}Hz";
        }

        // Update target frequency label
        if (targetFrequencyLabel != null)
        {
            targetFrequencyLabel.transform.position = targetPitchSphere.position + Vector3.right * labelOffset;
            targetFrequencyLabel.text = $"{targetFrequency:F0}Hz";  // Add this line
        }
    }

    private void UpdateLabelOrientations()
    {
        if (Camera.main == null) return;

        Transform cameraTransform = Camera.main.transform;
        TextMeshPro[] labels = { currentFrequencyLabel, confidenceLabel, minFreqLabel, maxFreqLabel, targetFrequencyLabel };
        
        foreach (var label in labels)
        {
            if (label != null)
            {
                // Face the camera
                label.transform.LookAt(cameraTransform);
                label.transform.Rotate(0, 180, 0);  // Flip to face the camera
                
                // Ensure text stays centered
                label.alignment = TextAlignmentOptions.Center;
                label.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                label.rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                label.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            }
        }
    }

    private void OnDestroy()
    {
        if (currentSphereMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(currentSphereMaterial);
            else
                DestroyImmediate(currentSphereMaterial);
        }

        if (targetMaterial != null)
        {
            if (Application.isPlaying)
                Destroy(targetMaterial);
            else
                DestroyImmediate(targetMaterial);
        }
    }

}