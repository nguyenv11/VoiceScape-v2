using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class PitchVisualizer : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MPMAudioAnalyzer audioAnalyzer;
    [SerializeField] private Transform currentPitchSphere;
    [SerializeField] private Transform targetPitchSphere;
    
    [Header("Layout")]
    [SerializeField] private float visualizerDistance = 1f;      // Distance in front
    [SerializeField] private float maxVerticalAngle = 20f;      // Reduced angle range
    [SerializeField] private float verticalOffset = -0.2f;      // Lower the whole visualization
    [SerializeField] private float sphereBaseScale = 0.05f;     // Current size seems good
    
    [Header("Visualization Settings")]
    [SerializeField] private float targetFrequency = 130.81f;    // Default to A4
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
    public float minFrequency = 100f;      // Adjusted for comfortable humming
    public float maxFrequency = 600f;      // Reduced from 1000Hz

    [Header("Alignment Feedback")]
    [SerializeField] private float perfectMatchThreshold = 2f;  // Hz difference for perfect match
    [SerializeField] private Color matchedColor = Color.red;    // Color when perfectly aligned
    [SerializeField] private float matchHoldTime = 0.5f;       // How long to maintain match

    private float matchTimer = 0f;
    private bool isMatched = false;

    private Material currentSphereMaterial;
    private Vector3 currentVelocity;
    private float currentScale = 1f;
    private Color currentColor;

    // Text components
    private TextMeshPro frequencyLabel;
    private TextMeshPro confidenceLabel;
    private TextMeshPro minFreqLabel;
    private TextMeshPro maxFreqLabel;

    [Header("Text Customization")]
    [SerializeField] private float fontSize = 1f;           // Starting with a larger size
    [SerializeField] private float labelOffset = 0.15f;       // Distance from spheres
    [SerializeField] private float labelSize = 0.002f;            // Scale for the label transform
    
    
    private void Start()
    {
        if (!ValidateComponents()) 
        {
            enabled = false;
            return;
        }

        InitializeMaterials();
        InitializeLabels();

        // Set initial positions
        if (currentPitchSphere != null && targetPitchSphere != null)
        {
            Vector3 initialPos = GetPositionForFrequency(audioAnalyzer.minFrequency);
            currentPitchSphere.position = initialPos;
            targetPitchSphere.position = GetPositionForFrequency(targetFrequency);
            
            // Update labels with initial positions
            UpdateLabels();
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
            Debug.Log("[PitchVisualizer] Added MeshRenderer to currentPitchSphere");
        }

        // Create material directly
        currentSphereMaterial = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        if (currentSphereMaterial.shader == null)
        {
            // Fallback to standard shader
            currentSphereMaterial = new Material(Shader.Find("Standard"));
        }
        
        Debug.Log($"[PitchVisualizer] Using shader: {currentSphereMaterial.shader.name}");
        
        renderer.sharedMaterial = currentSphereMaterial;
        currentColor = normalColor;
        currentSphereMaterial.color = currentColor;
        
        // Set initial scale
        currentPitchSphere.localScale = Vector3.one * sphereBaseScale;
        
        // Setup Target Sphere with similar process
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

        var targetMaterial = new Material(currentSphereMaterial); // Use same shader
        targetRenderer.sharedMaterial = targetMaterial;
        targetMaterial.color = targetColor;
        
        targetPitchSphere.localScale = Vector3.one * sphereBaseScale;
    }

    private void InitializeLabels()
    {
        GameObject labelsParent = new GameObject("Labels");
        labelsParent.transform.parent = null;
        labelsParent.transform.localScale = Vector3.one;  // Reset to 1
        labelsParent.transform.SetParent(transform, false);

        // Adjust label offset to be more readable
        labelOffset = 0.15f;  // Closer to spheres

        // Create labels
        minFreqLabel = CreateLabel(labelsParent, $"{audioAnalyzer.minFrequency}Hz", GetPositionForFrequency(audioAnalyzer.minFrequency));
        maxFreqLabel = CreateLabel(labelsParent, $"{audioAnalyzer.maxFrequency}Hz", GetPositionForFrequency(audioAnalyzer.maxFrequency));
        
        // Dynamic labels
        Vector3 initialPos = targetPitchSphere.position;
        frequencyLabel = CreateLabel(labelsParent, "0 Hz", initialPos + Vector3.right * labelOffset);
        confidenceLabel = CreateLabel(labelsParent, "0.00", initialPos + Vector3.left * labelOffset);

        // Ensure none of the labels inherit the small sphere scale
        foreach(var label in new[] { minFreqLabel, maxFreqLabel, frequencyLabel, confidenceLabel })
        {
            if (label != null)
            {
                label.transform.localScale = Vector3.one;
            }
        }
    }

    private TextMeshPro CreateLabel(GameObject parent, string text, Vector3 position)
    {
        GameObject labelObj = new GameObject($"Label_{text}");
        labelObj.transform.SetParent(parent.transform, false);  
        labelObj.transform.position = position;
        
        var tmp = labelObj.AddComponent<TextMeshPro>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontStyle = FontStyles.Bold;
        
        // Simple outline for visibility
        tmp.outlineWidth = 0.1f;
        tmp.outlineColor = Color.black;

        // Set a reasonable default size for the rect transform
        tmp.rectTransform.sizeDelta = new Vector2(2f, 0.5f);

        return tmp;
    }

    private void Update()
    {
        if (!ValidateComponents()) return;
        
        UpdateSpherePosition();
        UpdateLabels();
        UpdateLabelOrientations();
    }

    private Vector3 GetPositionForFrequency(float frequency)
    {
        // Get camera position as reference
        Vector3 cameraPos = Camera.main.transform.position;
        Vector3 forward = Camera.main.transform.forward;
        Vector3 up = Camera.main.transform.up;

        // Create base position in front of camera
        Vector3 basePos = cameraPos + (forward * visualizerDistance) + (up * verticalOffset);

        // Convert frequency to logarithmic scale between 0 and 1
        float normalizedFreq = (Mathf.Log(frequency) - Mathf.Log(audioAnalyzer.minFrequency)) / 
                            (Mathf.Log(audioAnalyzer.maxFrequency) - Mathf.Log(audioAnalyzer.minFrequency));
        
        // Map to vertical position using smaller angle range
        float angle = Mathf.Lerp(-maxVerticalAngle, maxVerticalAngle, normalizedFreq);
        float heightOffset = Mathf.Tan(angle * Mathf.Deg2Rad) * visualizerDistance;
        
        // Move up/down from base position
        return basePos + (up * heightOffset);
    }

    private void UpdateSpherePosition()
    {
        if (!audioAnalyzer.IsVoiceDetected) return;

        Vector3 targetPos = GetPositionForFrequency(audioAnalyzer.Frequency);
        currentPitchSphere.position = Vector3.SmoothDamp(
            currentPitchSphere.position,
            targetPos,
            ref currentVelocity,
            positionSmoothTime
        );

        // Check for perfect match
        float freqDifference = Mathf.Abs(audioAnalyzer.Frequency - targetFrequency);
        
        if (freqDifference <= perfectMatchThreshold && audioAnalyzer.Confidence > 0.8f)
        {
            matchTimer += Time.deltaTime;
            if (matchTimer >= matchHoldTime)
            {
                isMatched = true;
                currentColor = matchedColor;
                currentSphereMaterial.color = currentColor;
            }
        }
        else
        {
            matchTimer = 0f;
            isMatched = false;
            
            // Regular color updates
            Color targetColorForState = freqDifference <= frequencyTolerance ? targetColor :
                                    freqDifference <= frequencyTolerance * 3f ? closeColor :
                                    normalColor;

            currentColor = Color.Lerp(currentColor, targetColorForState, Time.deltaTime / colorSmoothTime);
            currentSphereMaterial.color = currentColor;
        }

        // Update scale based on confidence
        float targetScale = Mathf.Lerp(0.8f, 1.2f, audioAnalyzer.Confidence);
        currentScale = Mathf.Lerp(currentScale, targetScale * sphereBaseScale, Time.deltaTime * scaleSmooth);
        currentPitchSphere.localScale = Vector3.one * currentScale;
    }

    private void UpdateLabels()
    {
        if (audioAnalyzer.IsVoiceDetected)
        {
            if (frequencyLabel != null)
            {
                frequencyLabel.text = $"{audioAnalyzer.Frequency:F0}Hz";
                frequencyLabel.transform.position = currentPitchSphere.position + Vector3.right * labelOffset;
                frequencyLabel.fontSize = fontSize;  // Apply current font size
            }

            if (confidenceLabel != null)
            {
                confidenceLabel.text = $"{(audioAnalyzer.Confidence * 100):F0}%";
                confidenceLabel.transform.position = currentPitchSphere.position + Vector3.left * labelOffset;
                confidenceLabel.fontSize = fontSize;  // Apply current font size
            }
        }
        else
        {
            if (frequencyLabel != null) frequencyLabel.text = "";
            if (confidenceLabel != null) confidenceLabel.text = "";
        }

        // Update min/max labels too
        if (minFreqLabel != null)
        {
            minFreqLabel.fontSize = fontSize;
            minFreqLabel.transform.position = GetPositionForFrequency(minFrequency) + Vector3.left * labelOffset;
        }
        if (maxFreqLabel != null)
        {
            maxFreqLabel.fontSize = fontSize;
            maxFreqLabel.transform.position = GetPositionForFrequency(maxFrequency) + Vector3.left * labelOffset;
        }
    }

    private void UpdateLabelOrientations()
    {
        if (Camera.main == null) return;

        Transform cameraTransform = Camera.main.transform;
        TextMeshPro[] labels = { frequencyLabel, confidenceLabel, minFreqLabel, maxFreqLabel };
        
        foreach (var label in labels)
        {
            if (label != null)
            {
                label.transform.LookAt(cameraTransform);
                label.transform.Rotate(0, 180, 0);
            }
        }
    }
}