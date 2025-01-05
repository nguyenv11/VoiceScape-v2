using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

[Serializable]
public class DebugData
{
    public float[] audioBuffer;
    public float[] nsdfBuffer;
    public List<int> peakIndices;
    public float maxValue;
    public float clarityThreshold;
    public int selectedPeakIndex;
}

[RequireComponent(typeof(AudioSource))]
public class MPMAudioAnalyzer : MonoBehaviour
{
    [Header("Analysis Configuration")]
    [SerializeField] private int bufferSize = 2048;
    [Range(0.1f, 0.99f)]
    [SerializeField] private float clarityThreshold = 0.71f;  // Reduced from 0.93
    [Range(0.001f, 0.9f)]
    [SerializeField] private float noiseFloor = 0.001f;
    [SerializeField] private bool useKeyFrequencies = true;

    [Header("Voice Range")]
    public float minFrequency = 80f;      // Made public for visualizer
    public float maxFrequency = 1000f;    // Made public for visualizer
    
    [Header("Debug")]
    [SerializeField] private bool debugMode = true;
    [SerializeField] private bool showDetailedDebug = true;
    [SerializeField] private bool logPitchData = true;
    [SerializeField] private float debugUpdateInterval = 0.5f;
    [SerializeField] private bool visualizeBuffers = true;
    [SerializeField] private float visualizationUpdateRate = 0.1f;

    [Header("Test Configuration")]
    [SerializeField] private bool useTestTone = false;
    [SerializeField] private AudioClip testToneClip;

    [Header("Precision Settings")]
    [SerializeField] private float frequencySmoothing = 0.2f;    // Lower = more smoothing
    [Range(2, 10)]
    [SerializeField] private int medianWindowSize = 5;           // Window for median filtering
    [SerializeField] private float jumpThreshold = 20f;          // Minimum Hz change to register
    [SerializeField] private float octaveStabilityThreshold = 0.3f;  // How much freq can change

    // Public properties
    public float Frequency { get; private set; }
    public float Confidence { get; private set; }
    public float Clarity { get; private set; }
    public float Amplitude { get; private set; }
    public bool IsVoiceDetected => Amplitude > noiseFloor && Confidence > clarityThreshold;

    // Note frequencies for snapping
    private readonly List<float> keyFrequencies = new List<float>
    {
        // Common voice fundamental frequencies
        82.41f,   // E2
        87.31f,   // F2
        92.50f,   // F#2
        98.00f,   // G2
        103.83f,  // G#2
        110.00f,  // A2
        116.54f,  // A#2
        123.47f,  // B2
        130.81f,  // C3
        138.59f,  // C#3
        146.83f,  // D3
        155.56f,  // D#3
        164.81f,  // E3
        174.61f,  // F3
        185.00f,  // F#3
        196.00f,  // G3
        207.65f,  // G#3
        220.00f,  // A3
        233.08f,  // A#3
        246.94f,  // B3
        261.63f,  // C4 (middle C)
        277.18f,  // C#4
        293.66f,  // D4
        311.13f,  // D#4
        329.63f,  // E4
        349.23f,  // F4
        369.99f,  // F#4
        392.00f,  // G4
        415.30f,  // G#4
        440.00f,  // A4
        466.16f,  // A#4
        493.88f,  // B4
        523.25f   // C5
    };

    // Debug variables
    private float nextDebugTime = 0f;
    private float nextVisualizationUpdate = 0f;
    private string detailedDebugText = "";
    private float debugMaxAmplitude = 0f;
    private int samplesProcessed = 0;
    private int peaksFound = 0;
    private List<float> recentFrequencies = new List<float>();
    private const int MAX_RECENT_FREQUENCIES = 10;
    private DebugData currentDebugData;

    // Private buffers
    private AudioSource audioSource;
    private float[] audioBuffer;
    private float[] nsdfBuffer;
    private float[] lastFrequencies = new float[5];
    private int frequencyIndex = 0;
    private Queue<float> amplitudeHistory = new Queue<float>();
    private const int AMPLITUDE_HISTORY_LENGTH = 10;

    void Start()
    {
        InitializeComponents();
        InitializeMicrophone();

        if (debugMode)
        {
            Debug.Log($"[MPM] Started with buffer size: {bufferSize}, Sample rate: {AudioSettings.outputSampleRate}Hz");
        }
    }

    void Update()
    {
        if (!ValidateAudioSource()) return;
        
        audioSource.GetOutputData(audioBuffer, 0);
        ProcessAudioData();
        
        if (debugMode && Time.time >= nextDebugTime)
        {
            UpdateDebugInfo();
            nextDebugTime = Time.time + debugUpdateInterval;
        }
    }

    private bool ValidateAudioSource()
    {
        if (audioSource == null)
        {
            Debug.LogError("[MPM] Audio Source is null!");
            return false;
        }

        if (!audioSource.isPlaying)
        {
            Debug.LogError("[MPM] Audio Source is not playing!");
            return false;
        }

        return true;
    }

    private void InitializeComponents()
    {
        audioSource = GetComponent<AudioSource>();
        audioBuffer = new float[bufferSize];
        nsdfBuffer = new float[bufferSize];

        for (int i = 0; i < AMPLITUDE_HISTORY_LENGTH; i++)
        {
            amplitudeHistory.Enqueue(0f);
        }

        currentDebugData = new DebugData
        {
            audioBuffer = new float[bufferSize],
            nsdfBuffer = new float[bufferSize],
            peakIndices = new List<int>()
        };
    }

    private void InitializeMicrophone()
    {
        // If using test tone, skip microphone initialization
        if (useTestTone && testToneClip != null)
        {
            audioSource.clip = testToneClip;
            audioSource.loop = true;
            audioSource.volume = 0.5f;  // Set a reasonable default volume
            audioSource.Play();
            Debug.Log("[MPM] Using test tone instead of microphone");
            return;
        }

        if (Microphone.devices.Length == 0)
        {
            Debug.LogError("[MPM] No microphone found!");
            return;
        }

        string deviceName = Microphone.devices[0];
        Debug.Log($"[MPM] Using microphone: {deviceName}");
        
        try
        {
            audioSource.clip = Microphone.Start(deviceName, true, 1, AudioSettings.outputSampleRate);
            audioSource.loop = true;
            audioSource.volume = 1.0f;

            while (!(Microphone.GetPosition(deviceName) > 0)) { }
            audioSource.Play();

            Debug.Log($"[MPM] Microphone initialized successfully at {AudioSettings.outputSampleRate}Hz");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MPM] Microphone initialization failed: {e.Message}");
        }
    }

    private void ProcessAudioData()
    {
        samplesProcessed++;

        if (debugMode)
        {
            Debug.Log($"[MPM DEBUG] === START PROCESSING ===");
            Debug.Log($"[MPM DEBUG] Buffer length: {audioBuffer.Length}, First few samples: {string.Join(", ", audioBuffer.Take(5).Select(x => x.ToString("F4")))}");
        }

        // Copy buffer for visualization if needed
        if (visualizeBuffers && Time.time >= nextVisualizationUpdate)
        {
            Array.Copy(audioBuffer, currentDebugData.audioBuffer, bufferSize);
            nextVisualizationUpdate = Time.time + visualizationUpdateRate;
        }

        // Pre-processing with gain
        float preGain = 20.0f;
        float maxSample = 0f;
        for (int i = 0; i < audioBuffer.Length; i++)
        {
            audioBuffer[i] *= preGain;
            maxSample = Mathf.Max(maxSample, Mathf.Abs(audioBuffer[i]));
        }

        if (debugMode)
        {
            Debug.Log($"[MPM] Max sample amplitude: {maxSample:F4}");
        }

        // Calculate RMS amplitude
        float sumSquared = 0f;
        debugMaxAmplitude = 0f;
        
        for (int i = 0; i < audioBuffer.Length; i++)
        {
            float sample = audioBuffer[i];
            sumSquared += sample * sample;
            debugMaxAmplitude = Mathf.Max(debugMaxAmplitude, Mathf.Abs(sample));
        }
        
        float rms = Mathf.Sqrt(sumSquared / bufferSize);
        
        // Update amplitude with moving average
        amplitudeHistory.Dequeue();
        amplitudeHistory.Enqueue(rms);
        Amplitude = amplitudeHistory.Average();

        if (debugMode && showDetailedDebug)
        {
            Debug.Log($"[MPM] Buffer: MaxAmp={debugMaxAmplitude:F4}, RMS={rms:F4}, Amplitude={Amplitude:F4}");
        }

        // Process if above noise floor
        if (Amplitude > noiseFloor)
        {
            CalculateNSDF();
            float rawFrequency = FindFrequency();

            if (Confidence > clarityThreshold)
            {
                UpdateFrequency(rawFrequency);
            }
            else if (debugMode)
            {
                Debug.Log($"[MPM] Low confidence: {Confidence:F2}");
            }
        }
        else if (debugMode)
        {
            Debug.Log($"[MPM] Below noise floor: {Amplitude:F4}");
            Confidence = 0f;
            Clarity = 0f;
        }
    }

    private void CalculateNSDF()
    {
        System.Diagnostics.Stopwatch sw = null;
        if (debugMode) 
        {
            sw = System.Diagnostics.Stopwatch.StartNew();
            Debug.Log("[MPM] Starting NSDF calculation...");
        }

        float maxValue = float.MinValue;
        currentDebugData.peakIndices.Clear();
        
        // Calculate NSDF
        for (int tau = 0; tau < bufferSize; tau++)
        {
            float acf = 0f;
            float m0 = 0f;
            float m1 = 0f;

            for (int i = 0; i < bufferSize - tau; i++)
            {
                acf += audioBuffer[i] * audioBuffer[i + tau];
                m0 += audioBuffer[i] * audioBuffer[i];
                m1 += audioBuffer[i + tau] * audioBuffer[i + tau];
            }

            nsdfBuffer[tau] = 2 * acf / (m0 + m1 + float.Epsilon);
            
            if (nsdfBuffer[tau] > maxValue)
            {
                maxValue = nsdfBuffer[tau];
            }

            if (debugMode && tau < 5)
            {
                Debug.Log($"[MPM] NSDF[{tau}] = {nsdfBuffer[tau]:F4} (acf: {acf:F4}, m0: {m0:F4}, m1: {m1:F4})");
            }
        }

        // Normalize relative to the maximum value
        for (int i = 0; i < bufferSize; i++)
        {
            nsdfBuffer[i] = nsdfBuffer[i] / (maxValue + float.Epsilon);
        }

        if (debugMode)
        {
            Debug.Log($"[MPM] Max NSDF value before normalization: {maxValue:F4}");
            Debug.Log($"[MPM] First 5 normalized NSDF values: {string.Join(", ", nsdfBuffer.Take(5).Select(x => x.ToString("F4")))}");
            
            if (sw != null)
            {
                sw.Stop();
                Debug.Log($"[MPM] NSDF calculation took {sw.ElapsedMilliseconds}ms");
            }
        }
    }

    private float FindFrequency()
    {
        if (debugMode)
        {
            Debug.Log("[MPM] Starting frequency detection...");
        }

        // Find maximum value
        float maxValue = float.MinValue;
        int maxIndex = 0;
        
        for (int i = 0; i < bufferSize; i++)
        {
            if (nsdfBuffer[i] > maxValue)
            {
                maxValue = nsdfBuffer[i];
                maxIndex = i;
            }
        }

        if (debugMode)
        {
            Debug.Log($"[MPM] Max NSDF value: {maxValue:F4} at index {maxIndex}");
        }

        currentDebugData.maxValue = maxValue;
        currentDebugData.clarityThreshold = clarityThreshold * maxValue;

        // Find first peak above threshold
        int selectedPeakIndex = 0;
        bool foundPeak = false;
        peaksFound = 0;
        
        if (debugMode)
        {
            Debug.Log($"[MPM] Looking for peaks above threshold: {clarityThreshold * maxValue:F4}");
        }

        for (int i = 2; i < bufferSize - 1; i++)
        {
            if (nsdfBuffer[i] > nsdfBuffer[i - 1] && nsdfBuffer[i] >= nsdfBuffer[i + 1])
            {
                peaksFound++;
                
                if (debugMode)
                {
                    Debug.Log($"[MPM] Found peak at index {i}: {nsdfBuffer[i]:F4}");
                }
                
                if (visualizeBuffers)
                {
                    currentDebugData.peakIndices.Add(i);
                }

                if (!foundPeak && nsdfBuffer[i] > clarityThreshold * maxValue)
                {
                    selectedPeakIndex = i;
                    foundPeak = true;
                    if (debugMode)
                    {
                        Debug.Log($"[MPM] Selected peak at index {i}: {nsdfBuffer[i]:F4}");
                    }
                }
            }
        }

        if (!foundPeak || selectedPeakIndex == 0)
        {
            if (debugMode)
            {
                Debug.Log("[MPM] No valid peak found");
            }
            Confidence = 0f;
            Clarity = 0f;
            return 0f;
        }

        currentDebugData.selectedPeakIndex = selectedPeakIndex;

       // Parabolic interpolation
        float alpha = nsdfBuffer[selectedPeakIndex - 1];
        float beta = nsdfBuffer[selectedPeakIndex];
        float gamma = nsdfBuffer[selectedPeakIndex + 1];
        float delta = 0.5f * (alpha - gamma) / (alpha - 2f * beta + gamma);
        
        Confidence = maxValue;
        Clarity = nsdfBuffer[selectedPeakIndex];

        float refinedPeriod = selectedPeakIndex + delta;
        float frequency = AudioSettings.outputSampleRate / refinedPeriod;

        return Mathf.Clamp(frequency, minFrequency, maxFrequency);
    }

    private void UpdateFrequency(float rawFrequency)
    {
        // Add octave stability check
        float lastFreq = lastFrequencies.Length > 0 ? lastFrequencies[frequencyIndex] : rawFrequency;
        if (lastFreq > 0)
        {
            // Check if new frequency is close to an octave jump
            while (rawFrequency > lastFreq * 1.8f) // If more than ~1.8x higher
                rawFrequency *= 0.5f;  // Drop an octave
            while (rawFrequency < lastFreq * 0.6f) // If less than ~0.6x lower
                rawFrequency *= 2.0f;  // Raise an octave
        }
        float processedFrequency = useKeyFrequencies ? 
            SnapToNearestKeyFrequency(rawFrequency) : 
            rawFrequency;

        // Update frequency history
        lastFrequencies[frequencyIndex] = processedFrequency;
        frequencyIndex = (frequencyIndex + 1) % lastFrequencies.Length;
        Frequency = MedianFrequency();

        // Update recent frequencies for debugging
        recentFrequencies.Add(Frequency);
        if (recentFrequencies.Count > MAX_RECENT_FREQUENCIES)
        {
            recentFrequencies.RemoveAt(0);
        }

        if (logPitchData)
        {
            Debug.Log($"[MPM] Freq={Frequency:F1}Hz, Conf={Confidence:F2}, Clear={Clarity:F2}");
        }
    }

    private float SnapToNearestKeyFrequency(float frequency)
    {
        float minDiff = float.MaxValue;
        float closest = frequency;

        foreach (float key in keyFrequencies)
        {
            float diff = Mathf.Abs(frequency - key);
            if (diff < minDiff)
            {
                minDiff = diff;
                closest = key;
            }
        }
        return closest;
    }

    private float MedianFrequency()
    {
        float[] sortedFreqs = new float[lastFrequencies.Length];
        lastFrequencies.CopyTo(sortedFreqs, 0);
        System.Array.Sort(sortedFreqs);
        return sortedFreqs[sortedFreqs.Length / 2];
    }

    private void UpdateDebugInfo()
    {
        detailedDebugText = $"MPM Analysis:\n" +
                           $"Buffer Size: {bufferSize}\n" +
                           $"Sample Rate: {AudioSettings.outputSampleRate}Hz\n" +
                           $"Frequency: {Frequency:F1}Hz\n" +
                           $"Raw Amplitude: {debugMaxAmplitude:F4}\n" +
                           $"Smoothed Amplitude: {Amplitude:F4}\n" +
                           $"Confidence: {Confidence:F2}\n" +
                           $"Clarity: {Clarity:F2}\n" +
                           $"Peaks Found: {peaksFound}\n" +
                           $"Samples Processed: {samplesProcessed}\n" +
                           $"Recent Frequencies: {string.Join(", ", recentFrequencies.Select(f => f.ToString("F1")))}";
    }

    private void OnGUI()
    {
        if (!debugMode) return;

        GUILayout.BeginArea(new Rect(10, 10, 300, showDetailedDebug ? 400 : 100));
        
        // Basic info
        GUILayout.Label($"Frequency: {Frequency:F1} Hz");
        GUILayout.Label($"Confidence: {Confidence:F2}");
        GUILayout.Label($"Clarity: {Clarity:F2}");
        GUILayout.Label($"Amplitude: {Amplitude:F3}");

        // Detailed debug info
        if (showDetailedDebug)
        {
            GUILayout.Box(detailedDebugText, GUILayout.Width(280));
        }
        
        GUILayout.EndArea();
    }

    public DebugData GetDebugData()
    {
        return currentDebugData;
    }
}