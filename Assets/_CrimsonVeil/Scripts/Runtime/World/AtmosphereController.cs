using UnityEngine;

namespace CrimsonVeil.World
{
    /// <summary>Pushes global mist and wind parameters to the CrimsonVeil shaders (works in edit mode).</summary>
    [ExecuteAlways]
    public sealed class AtmosphereController : MonoBehaviour
    {
        static readonly int MistColorId = Shader.PropertyToID("_CV_MistColor");
        static readonly int MistParamsId = Shader.PropertyToID("_CV_MistParams");
        static readonly int WindId = Shader.PropertyToID("_CV_Wind");

        [Header("Ground mist")]
        public Color mistColor = new Color(0.78f, 0.78f, 0.78f);
        [Tooltip("World height below which mist starts to pool.")]
        public float mistTop = 2f;
        public float mistFade = 6f;
        [Tooltip("Camera distance at which mist reaches full strength.")]
        public float mistDistance = 45f;
        [Range(0f, 1.5f)] public float mistStrength = 1f;

        [Header("Wind")]
        public float windStrength = 0.12f;
        public float windFrequency = 1.3f;
        public float gustScale = 0.15f;

        void OnEnable() => Apply();
        void OnValidate() => Apply();
        void Update() => Apply();

        public void Apply()
        {
            Shader.SetGlobalColor(MistColorId, mistColor);
            Shader.SetGlobalVector(MistParamsId, new Vector4(mistTop, mistFade, mistDistance, mistStrength));
            Shader.SetGlobalVector(WindId, new Vector4(windStrength, windFrequency, gustScale, 0f));
        }
    }
}
