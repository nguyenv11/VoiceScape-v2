using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif
namespace AudioReactiveShader
{
    public class audioDataInterpreter : MonoBehaviour
    {
        [SerializeField] MusicReader MusicSpectrum;
        [Tooltip("use a value <= 0 to disable smooting")] [SerializeField] float smoothSpeed;
        [SerializeField] AnimationCurve ResponseAdjustment;
        [Range(0, 5)] public float Low;
        [Range(0, 5)] public float MidLow;
        [Range(0, 5)] public float Mid;
        [Range(0, 5)] public float MidHigh;
        [Range(0, 5)] public float High;

        Renderer rend;
        Image img;
        ParticleSystem particles;
        Material mat;
        [SerializeField] MusicSpectrumReader.MATERIAL_OUTPUT MaterialOutput;


        public bool soundAffectsParticlesEmmisionRate;
        float startingEmmisionRate;

        private int MidLowPosition;
        private int MidPosition;
        private int MidHighPosition;
        private int HighPosition;

#if UNITY_EDITOR
        [CustomEditor(typeof(audioDataInterpreter))]
        public class AudioInterpreterEditor : Editor
        {
            bool showHiddenVars;

            public override void OnInspectorGUI()
            {
                audioDataInterpreter ADI = (audioDataInterpreter)target;
                Undo.RecordObject(ADI, "audioDataInterpreter changes");

                Color color = new Color(.1f, .1f, .2f);
                Rect headerArea = new Rect(0, 0, EditorGUIUtility.currentViewWidth, 35);
                GUILayout.BeginArea(headerArea);
                EditorGUILayout.Space(5);
                EditorGUI.DrawRect(headerArea, color);
                GUI.skin.label.fontSize = 15;
                GUI.skin.label.fontStyle = FontStyle.BoldAndItalic;
                GUILayout.Label("AUDIO REACTIVE SHADERS - Audio data interpreter");
                GUILayout.EndArea();
                EditorGUILayout.Space(40);

                EditorGUILayout.Space(5);
                base.OnInspectorGUI();

                if (GUI.changed)
                    EditorUtility.SetDirty(ADI);
            }
        }
#endif
        //Set the bands lenght to the minimun required ammount
        private void Awake()
        {
            if (MusicSpectrum.numBands < 5) MusicSpectrum.numBands = 5;
        }
         //set the vars and bands position
        void Start()
        {
           
            //set the bands range
            MidLowPosition = (int)Mathf.Floor(MusicSpectrum.numBands / 4 - 1);
            MidPosition = (int)Mathf.Floor(MusicSpectrum.numBands / 2 - 1);
            MidHighPosition = (int)Mathf.Floor(MusicSpectrum.numBands * .75f - 1);
            HighPosition = (int)Mathf.Floor(MusicSpectrum.numBands - 1);


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
        }

        void setParticleSystem()
        {
            if (particles != null)
            {
                var partsRend = particles.GetComponent<ParticleSystemRenderer>().material;
                mat = partsRend;
                if (soundAffectsParticlesEmmisionRate)
                {
                    var partsEmmision = particles.emission;
                    startingEmmisionRate = partsEmmision.rateOverTime.constant;
                }
            }
            else Debug.LogWarning("no particle system found");
        }

        // get the audio data and fill it in the correspondet vars
        void Update()
        {
            if (smoothSpeed > 0)
            {
                Low = Mathf.Lerp(Low, ResponseAdjustment.Evaluate(MusicSpectrum.groupedBands[0]), smoothSpeed * Time.deltaTime);
                MidLow = Mathf.Lerp(MidLow, ResponseAdjustment.Evaluate(MusicSpectrum.groupedBands[MidLowPosition]), smoothSpeed * Time.deltaTime);
                Mid = Mathf.Lerp(Mid, ResponseAdjustment.Evaluate(MusicSpectrum.groupedBands[MidPosition]), smoothSpeed * Time.deltaTime);
                MidHigh = Mathf.Lerp(MidHigh, ResponseAdjustment.Evaluate(MusicSpectrum.groupedBands[MidHighPosition]), smoothSpeed * Time.deltaTime);
                High = Mathf.Lerp(High, ResponseAdjustment.Evaluate(MusicSpectrum.groupedBands[HighPosition]), smoothSpeed * Time.deltaTime);
            }
            else
            {
                Low = MusicSpectrum.groupedBands[0];
                MidLow = MusicSpectrum.groupedBands[MidLowPosition];
                Mid = MusicSpectrum.groupedBands[MidPosition];
                MidHigh = MusicSpectrum.groupedBands[MidHighPosition];
                High = MusicSpectrum.groupedBands[HighPosition];
            }
            if (particles !=null && soundAffectsParticlesEmmisionRate)
            {
                var partsEmmision = particles.emission;
                partsEmmision.rateOverTime = startingEmmisionRate *(Low+MidLow+Mid+MidHigh+High);
            }
        }
        // send the final values to the shader.
        private void FixedUpdate()
        {
            mat.SetFloat("_Low", Low);
            mat.SetFloat("_MidLow", MidLow);
            mat.SetFloat("_Mid", Mid);
            mat.SetFloat("_MidHigh", MidHigh);
            mat.SetFloat("_High", High);
        }
    }
}

