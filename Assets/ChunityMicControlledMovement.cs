using UnityEngine;
using UnityEngine.Serialization;

public class ChunityMicControlledMovement : MonoBehaviour
{
    //public ChuckSubInstance chuckInstance; // Reference to the Chunity ChuckSubInstance
    public float sensitivity = 100.0f;    // Adjusts how sensitive the movement is to microphone input
    public float threshold = 0.1f;       // Minimum volume required to move the object up
    public float moveSpeed = 5.0f;       // Speed at which the object moves
    [FormerlySerializedAs("downPosition")] public Vector3 downLocalPosition;        // Position when the object moves down
    [FormerlySerializedAs("upPosition")] public Vector3 upLocalPosition;          // Position when the object moves up
    public GameObject targetGo;           // The GameObject to be moved
    public AudioSource audioSource;       // AudioSource to play the microphone input
    
    private float currentVolume = 0.0f;
    //private Chuck.FloatCallback myFloatCallback;

    // Microphone API variables
    private AudioClip micClip;
    private float[] micSamples;
    public bool useUnityMic = true;
    public int sampleRate = 44100;

    void Start()
    {
        // Start the microphone if using Unity's Microphone API
        if (useUnityMic)
        {
            micClip = Microphone.Start(null, true, 1, sampleRate); // 1-second loop buffer
            audioSource.clip = micClip;
            audioSource.loop = true; // Loop the microphone audio
            while (!(Microphone.GetPosition(null) > 0)) {} // Wait until the microphone starts
            Debug.Log("Microphone started!: " + Microphone.devices[0]);
            audioSource.Play(); // Play the microphone input through the AudioSource
            micSamples = new float[sampleRate];
        }

        // Initialize ChucK code for analyzing microphone input
        /*
         chuckInstance.RunCode(@"
            global float rms;

            // Connect the microphone (adc) to a Gain object
            adc => Gain g => dac;

            // Set the gain level
            0.5 => g.gain;

            // Variables for RMS calculation
            512 => int bufferSize;            // Number of samples to process
            float buffer[bufferSize];         // Array to hold samples

            while (true)
            {
                0 => rms; // Reset RMS for each loop

                // Fill buffer with samples and compute squared sum
                for (0 => int i; i < bufferSize; i++)
                {
                    adc.last() => buffer[i];
                    rms + buffer[i] * buffer[i] => rms;
                }

                // Compute the RMS value
                Math.sqrt(rms / bufferSize) => rms;

                // Advance time
                10::ms => now;
            }
        ");*/

        //myFloatCallback = chuckInstance.CreateGetFloatCallback(MyFloatCallbackFunction);
    }

    // [SerializeField] private float latestValue;
    // private void MyFloatCallbackFunction(CK_FLOAT obj)
    // {
    //     latestValue = (float)obj;
    // }

    void Update()
    {
        if (useUnityMic && Microphone.IsRecording(null))
        {
            // Get the microphone data from Unity's Microphone API
            micClip.GetData(micSamples, 0);

            // Calculate the RMS value using Unity's microphone data
            float rms = 0.0f;
            for (int i = 0; i < micSamples.Length; i++)
            {
                rms += micSamples[i] * micSamples[i];
            }
            rms = Mathf.Sqrt(rms / micSamples.Length);

            // Update current volume based on Unity's microphone input
            currentVolume = rms * sensitivity;
        }
        /*
        else
        {
            // Get the latest value from ChucK if not using Unity's Microphone API
            chuckInstance.GetFloat("rms", myFloatCallback);
            currentVolume = latestValue * sensitivity;
        }
        */

        if (currentVolume > threshold)
        {
            // Move the object up when the volume exceeds the threshold
            targetGo.transform.localPosition = 
                Vector3.Lerp
                (
                    targetGo.transform.localPosition,
                    upLocalPosition,
                    Time.deltaTime * moveSpeed
                );
        }
        else
        {
            // Move the object down when the volume is below the threshold
            targetGo.transform.localPosition = 
                Vector3.Lerp
                    (targetGo.transform.localPosition,
                        downLocalPosition,
                        Time.deltaTime * moveSpeed / 2
                    );
        }
    }

    void OnDestroy()
    {
        if (useUnityMic && Microphone.IsRecording(null))
        {
            Microphone.End(null); // Stop recording when the script is destroyed
        }
    }
}
