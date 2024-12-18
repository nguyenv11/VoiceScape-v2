using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;


namespace AudioReactiveShader
{
    public abstract class MusicReader : MonoBehaviour
    {
        public enum AUDIO_INPUT
        {
            AudioSource,
            AudioListener,
            MixerGroup,
            AudioSourceWebGL,
            MixerGroupWebGL,
        }
        public enum MATERIAL_OUTPUT
        {
            RENDERER,
            PARTICLES,
            CANVAS_IMG
        }

        [SerializeField] int _channelSelection;
        [HideInInspector] public int totalSpectrum = 64;
        AudioSource _audioSource;
        [HideInInspector] public int _numBands;
        [HideInInspector] public AudioMixerGroup targetMixerGroup;
        /*[HideInInspector]*/ public List<AudioSource> audioSourcesInGroup;


        public float[] rawSpectrumData;
        [HideInInspector] public int[] bandGroupsDistribution;
        public float[] groupedBands;
        public float[] clipSamples = new float[256];


        public AudioSource audioSource { get { return _audioSource; } set { _audioSource = value; } }
        public int numBands { get { return _numBands; } set { _numBands = value; } }
        public int channelSelection { get { return _channelSelection; } set { _channelSelection = value; } }

        public static void FFT(float[] data)
        {
            int n = data.Length / 2;
            int m = (int)Mathf.Log(n, 2);

            for (int i = 1, j = 0; i < n; i++)
            {
                int bit = n >> 1;
                for (; j >= bit; bit >>= 1)
                {
                    j -= bit;
                }
                j += bit;

                if (i < j)
                {
                    int realIndex1 = 2 * i;
                    int imagIndex1 = realIndex1 + 1;
                    int realIndex2 = 2 * j;
                    int imagIndex2 = realIndex2 + 1;

                    float tempReal = data[realIndex1];
                    float tempImag = data[imagIndex1];
                    data[realIndex1] = data[realIndex2];
                    data[imagIndex1] = data[imagIndex2];
                    data[realIndex2] = tempReal;
                    data[imagIndex2] = tempImag;
                }
            }

            for (int length = 2; length <= n; length <<= 1)
            {
                float angle = 2 * Mathf.PI / length;
                float wlenX = Mathf.Cos(angle);
                float wlenY = Mathf.Sin(angle);
                for (int i = 0; i < n; i += length)
                {
                    float wX = 1;
                    float wY = 0;
                    for (int j = 0; j < length / 2; j++)
                    {
                        int evenIndex = 2 * (i + j);
                        int oddIndex = evenIndex + length;

                        float evenReal = data[evenIndex];
                        float evenImag = data[evenIndex + 1];
                        float oddReal = data[oddIndex];
                        float oddImag = data[oddIndex + 1];

                        float tempReal = oddReal * wX - oddImag * wY;
                        float tempImag = oddReal * wY + oddImag * wX;

                        data[evenIndex] = evenReal + tempReal;
                        data[evenIndex + 1] = evenImag + tempImag;
                        data[oddIndex] = evenReal - tempReal;
                        data[oddIndex + 1] = evenImag - tempImag;

                        float tempWX = wX * wlenX - wY * wlenY;
                        wY = wX * wlenY + wY * wlenX;
                        wX = tempWX;
                    }
                }
            }

        }
    }

}
