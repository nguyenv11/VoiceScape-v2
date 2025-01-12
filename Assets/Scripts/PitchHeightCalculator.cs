using UnityEngine;

public static class PitchHeightCalculator
{
    // Core height parameters
    public static float BaseHeight = 0.5f;
    public static float MaxHeight = 2.5f;
    public static float MinFrequency = 80f;
    public static float MaxFrequency = 300f;
    public static float MaxVerticalAngle = 300f;
    public static float VerticalOffset = 300f;
    public static float VisualizerDistance = 300f;

    // Calculate basic height without release behavior
    public static float GetHeightForFrequency(float frequency)
    {
        // Ensure frequency is in valid range
        frequency = Mathf.Clamp(frequency, MinFrequency, MaxFrequency);
        
        // Calculate normalized frequency using logarithmic scale
        float normalizedFreq = (Mathf.Log(frequency) - Mathf.Log(MinFrequency)) / 
                             (Mathf.Log(MaxFrequency) - Mathf.Log(MinFrequency));
        normalizedFreq = Mathf.Clamp01(normalizedFreq);
    
        // Calculate final height
        float heightFromPitch = normalizedFreq * (MaxHeight - BaseHeight);
        return BaseHeight + heightFromPitch;
    }
    
    public static Vector3 GetPositionForFrequency(float frequency)
    {
        // Calculate vertical offset based on frequency
        float normalizedFreq = (Mathf.Log(frequency) - Mathf.Log(MinFrequency)) / 
                               (Mathf.Log(MaxFrequency) - Mathf.Log(MinFrequency));
        float angle = Mathf.Lerp(-MaxVerticalAngle, MaxVerticalAngle, normalizedFreq);
        float heightOffset = VerticalOffset + Mathf.Tan(angle * Mathf.Deg2Rad) + VisualizerDistance;
        
        // Position relative to parent
        //Vector3 horizontalForward = Vector3.ProjectOnPlane(-transform.parent.right, Vector3.up).normalized;
        return (Vector3.up * heightOffset);
    }

    // Initialize with current settings
    public static void Initialize(float baseHeight, float maxHeight, float minFreq, float maxFreq)
    {
        BaseHeight = baseHeight;
        MaxHeight = maxHeight;
        MinFrequency = minFreq;
        MaxFrequency = maxFreq;
    }
    
    public static void Initialize(float baseHeight, float maxHeight, float minFreq, float maxFreq, float maxVerticalAngle, float verticalOffset, float visualizerDistance)
    {
        BaseHeight = baseHeight;
        MaxHeight = maxHeight;
        MinFrequency = minFreq;
        MaxFrequency = maxFreq;
        MaxVerticalAngle = maxVerticalAngle;
        VerticalOffset = verticalOffset;
        VisualizerDistance = visualizerDistance;
    }
}