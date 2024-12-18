using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
namespace AudioReactiveShader
{
    public class DinamicBandsAudioDataInterpreter : MonoBehaviour
    {
        [SerializeField] MusicReader MusicSpectrum;
        [Tooltip("use a value <= 0 to disable smooting")] [SerializeField] float smoothSpeed;
        [SerializeField] AnimationCurve ResponseAdjustment;
        [Range(1, 63)] [SerializeField] int bands = 10;
        float[] smoothedIntensisyValues;

        Renderer rend;
        Image img;
        ParticleSystem particles;
        Material mat;
        [SerializeField] MusicSpectrumReader.MATERIAL_OUTPUT MaterialOutput;
        public bool soundAffectsEmmisionRate;
        float startingEmmisionRate;



#if UNITY_EDITOR
        [CustomEditor(typeof(DinamicBandsAudioDataInterpreter))]
        public class DBADIEditor : Editor
        {
            bool showHiddenVars;

            public override void OnInspectorGUI()
            {
                DinamicBandsAudioDataInterpreter DBADI = (DinamicBandsAudioDataInterpreter)target;
                Undo.RecordObject(DBADI, "DinamicBandsAudioDataInterpreter changes");

                Color color = new Color(.1f, .1f, .2f);
                Rect headerArea = new Rect(0, 0, EditorGUIUtility.currentViewWidth, 35);
                GUILayout.BeginArea(headerArea);
                EditorGUILayout.Space(5);
                EditorGUI.DrawRect(headerArea, color);
                GUI.skin.label.fontSize = 15;
                GUI.skin.label.fontStyle = FontStyle.BoldAndItalic;
                GUILayout.Label("AUDIO REACTIVE SHADERS - Dinamic audio data interpreter");
                GUILayout.EndArea();
                EditorGUILayout.Space(40);

                EditorGUILayout.Space(5);
                base.OnInspectorGUI();

                if (GUI.changed)
                    EditorUtility.SetDirty(DBADI);
            }
        }
#endif
        //Set the bands lenght to the minimun required ammount
        private void Awake()
        {
            if (MusicSpectrum.numBands < bands) MusicSpectrum.numBands = bands;
        }

        //set the vars and bands position
        void Start()
        {
            smoothedIntensisyValues = new float[MusicSpectrum.numBands];
            if (MaterialOutput == MusicSpectrumReader.MATERIAL_OUTPUT.RENDERER)
            {
                rend = GetComponent<Renderer>();
                mat = rend.material;
            }
            else if (MaterialOutput == MusicSpectrumReader.MATERIAL_OUTPUT.PARTICLES)
            {
                particles = GetComponent<ParticleSystem>();
                setParticleSystem();
            }
            else
            {
                img = GetComponent<Image>();
                mat = img.material;
            }
            mat.SetFloat("_Bands", bands);
        }

        void setParticleSystem()
        {
            if (particles != null)
            {
                var partsRend = particles.GetComponent<ParticleSystemRenderer>().material;
                mat = partsRend;
                if (soundAffectsEmmisionRate)
                {
                    var partsEmmision = particles.emission;
                    startingEmmisionRate = partsEmmision.rateOverTime.constant;
                }
            }
            else Debug.LogWarning("no particle system found");
        }

        // send the final values to the shader.
        void Update()
        {
            if (smoothSpeed > 0)
            {
                for (int i = 0; i <= MusicSpectrum.numBands - 1; i++)
                {
                    smoothedIntensisyValues[i] = Mathf.Lerp(smoothedIntensisyValues[i], ResponseAdjustment.Evaluate(MusicSpectrum.groupedBands[i]), smoothSpeed * Time.deltaTime);
                }
                mat.SetFloatArray("_FreqLevels", smoothedIntensisyValues);

            }
            else
            {
                mat.SetFloatArray("_FreqLevels", MusicSpectrum.groupedBands);
            }

            if (particles != null && soundAffectsEmmisionRate)
            {
                var partsEmmision = particles.emission;
                partsEmmision.rateOverTime = startingEmmisionRate * smoothedIntensisyValues[0]*bands;
            }
        }

    }
}
