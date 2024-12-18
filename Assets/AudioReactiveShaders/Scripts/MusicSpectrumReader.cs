using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Audio;


#if UNITY_EDITOR
using UnityEditor;
#endif

namespace AudioReactiveShader
{
    public class MusicSpectrumReader : MusicReader
    {
        [SerializeField] AUDIO_INPUT audio_input;

        #region editor
#if UNITY_EDITOR
        [CustomEditor(typeof(MusicSpectrumReader))]
        public class MusicReaderEditor : Editor
        {
            bool showHiddenVars;

            public override void OnInspectorGUI()
            {
                MusicSpectrumReader MSR = (MusicSpectrumReader)target;
                Undo.RecordObject(MSR, "MusicSpectrumReader changes");
                if (showHiddenVars) base.OnInspectorGUI();
                else
                {
                    Color color = new Color(.1f, .1f, .2f);
                    Rect headerArea = new Rect(0, 0, EditorGUIUtility.currentViewWidth, 35);
                    GUILayout.BeginArea(headerArea);
                    EditorGUILayout.Space(5);
                    EditorGUI.DrawRect(headerArea, color);
                    GUI.skin.label.fontSize = 15;
                    GUI.skin.label.fontStyle = FontStyle.BoldAndItalic;
                    GUILayout.Label("AUDIO REACTIVE SHADERS | music spectrum reader");
                    GUILayout.EndArea();
                    EditorGUILayout.Space(40);
                }

                MSR.audio_input = (AUDIO_INPUT)EditorGUILayout.EnumPopup("Input selection", MSR.audio_input);
                if (MSR.audio_input == AUDIO_INPUT.MixerGroup || MSR.audio_input == AUDIO_INPUT.MixerGroupWebGL)
                {
                    EditorGUILayout.LabelField("Choose your mixer group", EditorStyles.boldLabel);
                    MSR.targetMixerGroup = (AudioMixerGroup)EditorGUILayout.ObjectField("Target Mixer Group", MSR.targetMixerGroup, typeof(AudioMixerGroup), false);
                }
                EditorGUILayout.Space(5);
                if (MSR.audio_input != AUDIO_INPUT.AudioSourceWebGL && MSR.audio_input != AUDIO_INPUT.MixerGroupWebGL)
                {
                    EditorGUILayout.LabelField("0 to use the left channel or 1 to use the right channel", EditorStyles.boldLabel);
                    MSR.channelSelection = (int)EditorGUILayout.Slider(MSR.channelSelection, 0, 1);
                }
                    


                EditorGUILayout.Space(5);
                showHiddenVars = EditorGUILayout.Toggle("show hidden vars", showHiddenVars);

                if (GUI.changed)
                    EditorUtility.SetDirty(MSR);
            }
        }
#endif
        #endregion
        private void Awake()
        {
            rawSpectrumData = new float[128];
        }
        void OnEnable()
        {
            if (audio_input == AUDIO_INPUT.AudioSource || audio_input == AUDIO_INPUT.AudioSourceWebGL)
            {
                audioSource = GetComponent<AudioSource>();
                if (audioSource == null)
                {
                    Debug.LogWarning("AudioSource is not assigned.");
                }
            }

            else if(audio_input == AUDIO_INPUT.MixerGroup || audio_input == AUDIO_INPUT.MixerGroupWebGL)
            {
                refreshAudioSourcesOnMixerGroup();
            }

            groupedBands = new float[numBands];
            bandGroupsDistribution = new int[numBands];

            dinamicBandsDistribution();
        }
       

        void Update()
        {

            if (audio_input == AUDIO_INPUT.AudioSource)
            {
                // just works when an audio is playing
                if (audioSource.isPlaying)
                {
                    getAudiosourceData();
                }
            }

            else if (audio_input == AUDIO_INPUT.AudioListener)
            {
                AudioListener.GetSpectrumData(rawSpectrumData, channelSelection, FFTWindow.Rectangular);
                GroupSpectrumData();
            }

            else if (audio_input == AUDIO_INPUT.MixerGroup)
            {
                // just works when an audio is playing otherwise searchs for playing audiosources on the selected mixer group
                if (audioSource != null && audioSource.isPlaying)
                {
                    getAudiosourceData();
                }
                else
                {
                    searchForPlayingAudiosources();
                }
            }
            else if (audio_input == AUDIO_INPUT.AudioSourceWebGL)
            {
                if (audioSource.isPlaying)
                {
                    GetAudioClipSpectrumData();
                }
            }
            else if (audio_input == AUDIO_INPUT.MixerGroupWebGL)
            {
                if (audioSource != null && audioSource.isPlaying)
                {
                    GetAudioClipSpectrumData();
                }
                else
                {
                    searchForPlayingAudiosources();
                }
            }

        }

        void getAudiosourceData()
        {
            if (audioSource != null) audioSource.GetSpectrumData(rawSpectrumData, channelSelection, FFTWindow.Rectangular);
            GroupSpectrumData();
        }

        private void GetAudioClipSamples(float[] samples)
        {
            audioSource.clip.GetData(samples, audioSource.timeSamples);
        }
        private void GetAudioClipSpectrumData()
        {
            GetAudioClipSamples(clipSamples);
            FFT(clipSamples);

            // Process the FFT result into spectrum data
            for (int i = 0; i < rawSpectrumData.Length / 2; i++)
            {
                rawSpectrumData[i] = Mathf.Sqrt(clipSamples[2 * i] * clipSamples[2 * i] + clipSamples[2 * i + 1] * clipSamples[2 * i + 1]);
            }
            GroupSpectrumData();
        }

        //group bands according to the selected bands value to show in the eq
        void GroupSpectrumData()
        {

            // Reset grouped bands
            for (int i = 0; i < numBands; i++)
            {
                groupedBands[i] = 0;
            }

            int startIndex = 0;
            for (int i = 0; i < numBands; i++)
            {
                int size;
                size = bandGroupsDistribution[i];

                // Sum up magnitudes of bands in this group
                for (int j = 0; j < size; j++)
                {

                    if (size <= 1) groupedBands[i] += rawSpectrumData[startIndex + j];
                    else
                    {
                        if(audio_input == AUDIO_INPUT.AudioSourceWebGL || audio_input == AUDIO_INPUT.MixerGroupWebGL)
                        {
                            groupedBands[i] += .01f * rawSpectrumData[startIndex + j] / size;
                        }
                        else
                        {
                            groupedBands[i] += rawSpectrumData[startIndex + j] / size;
                        }
                    }
                }

                startIndex += size;
            }

        }

        void dinamicBandsDistribution()
        {
            int totalAdded = 0;
            int progressionAmp = totalSpectrum / (numBands * 4);
            int progressionStart = progressionAmp * ((numBands + 2) / -2);

            for (int i = 0; i <= numBands - 1; i++)
            {
                progressionStart += 1 * progressionAmp;

                totalAdded += (totalSpectrum / numBands) + progressionStart;
                bandGroupsDistribution[i] = (64 / numBands) + progressionStart;

                if ((totalSpectrum / numBands) + progressionStart < 1)
                {
                    bandGroupsDistribution[i] = 1;
                    totalAdded += -((totalSpectrum / numBands) + progressionStart) + 1;
                }
            }
            if (totalAdded < 64)
            {
                bandGroupsDistribution[numBands - 1] += (totalSpectrum - totalAdded);
            }
            if (totalAdded > 64)
            {
                bandGroupsDistribution[numBands - 1] -= (totalAdded - totalSpectrum);
            }
        }
        public void refreshAudioSourcesOnMixerGroup()
        {
            audioSourcesInGroup = new List<AudioSource>();
            FindAudioSourcesOnMixerGroup(targetMixerGroup);
            searchForPlayingAudiosources();
        }
        void FindAudioSourcesOnMixerGroup(AudioMixerGroup mixerGroup)
        {
            AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();

            foreach (AudioSource audioSource in allAudioSources)
            {
                if (audioSource.outputAudioMixerGroup == mixerGroup)
                {
                    audioSourcesInGroup.Add(audioSource);
                }
            }
        }
        void searchForPlayingAudiosources()
        {
            foreach (AudioSource AS in audioSourcesInGroup)
            {
                if (AS.isPlaying)
                {
                    audioSource = AS;
                    return;
                }
            }
        }
    }
}
