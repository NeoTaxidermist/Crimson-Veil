using CrimsonVeil.CameraSystem;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using CrimsonVeil.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CrimsonVeil.EditorTools
{
    /// <summary>
    /// Rebuilds the grey-box prototype level from code so the look (greys + crimson) stays reproducible.
    /// Menu: Crimson Veil ▸ Build Prototype Scene.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        const string Root = "Assets/_CrimsonVeil";
        const string ScenePath = Root + "/Scenes/Prototype_Ruins.unity";
        const string MatDir = Root + "/Materials";

        static readonly Color FogColor = new Color(0.74f, 0.74f, 0.74f);

        [MenuItem("Crimson Veil/Build Prototype Scene")]
        public static void Build()
        {
            var mats = CreateMaterials();
            var profile = CreateVolumeProfile();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Atmosphere -----------------------------------------------
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 12f;
            RenderSettings.fogEndDistance = 95f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

            var lightGo = new GameObject("Moonlight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.6f;
            light.color = Color.white;
            lightGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            var volumeGo = new GameObject("Global Volume");
            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            // --- Level ----------------------------------------------------
            var level = new GameObject("Level").transform;
            var fg = mats.silhouette;
            Block(level, "Start", new Vector3(-2f, -0.5f, 0f), new Vector3(24f, 1f, 4f), fg);
            Block(level, "StartCliff", new Vector3(-2f, -6f, 0f), new Vector3(22f, 10f, 3.6f), fg);
            Block(level, "Step", new Vector3(13f, 0.75f, 0f), new Vector3(5f, 3.5f, 4f), fg);
            Block(level, "GapLedge", new Vector3(23f, 0.0f, 0f), new Vector3(7f, 1f, 4f), fg);
            Block(level, "HighLedge", new Vector3(31f, 3.0f, 0f), new Vector3(4f, 1f, 4f), fg);
            Block(level, "Floor2", new Vector3(46f, -0.5f, 0f), new Vector3(20f, 1f, 4f), fg);
            Block(level, "Floor2Cliff", new Vector3(46f, -6f, 0f), new Vector3(19f, 10f, 3.6f), fg);
            Block(level, "Pillar", new Vector3(42f, 2.2f, 0f), new Vector3(1f, 4.4f, 1f), fg);
            Block(level, "Lintel", new Vector3(44f, 5f, 0f), new Vector3(6f, 0.8f, 1.5f), fg);
            Block(level, "EndWall", new Vector3(57f, 6f, 0f), new Vector3(2f, 14f, 4f), fg);
            Block(level, "BackWall", new Vector3(-14f, 6f, 0f), new Vector3(2f, 14f, 4f), fg);

            // Foreground grass/rubble silhouettes (non-colliding, slightly in front).
            var deco = new GameObject("Foreground").transform;
            for (int i = 0; i < 14; i++)
            {
                float x = -12f + i * 5.1f;
                Deco(deco, "Rubble", new Vector3(x, -0.6f + (i % 3) * 0.15f, -2.6f), new Vector3(1.6f + i % 2, 0.6f + (i % 4) * 0.25f, 0.6f), fg);
            }

            // Mid-ground ruins (dark grey) and far ruins (lighter, fogged).
            var mid = new GameObject("Midground").transform;
            for (int i = 0; i < 6; i++)
                Arch(mid, new Vector3(-8f + i * 13f, 0f, 9f + (i % 2) * 3f), 5f + (i % 3), 4f, mats.mid);

            var far = new GameObject("Background").transform;
            for (int i = 0; i < 9; i++)
                Arch(far, new Vector3(-30f + i * 14f, -4f, 45f + (i % 3) * 6f), 16f + (i % 4) * 3f, 8f, mats.far);
            Deco(far, "Aqueduct", new Vector3(20f, 13f, 55f), new Vector3(140f, 3f, 4f), mats.far);

            var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moon.name = "Moon";
            Object.DestroyImmediate(moon.GetComponent<Collider>());
            moon.transform.position = new Vector3(8f, 26f, 85f);
            moon.transform.localScale = Vector3.one * 26f;
            moon.GetComponent<Renderer>().sharedMaterial = mats.moon;

            // Crimson accents: banners and drifting leaves.
            var accents = new GameObject("CrimsonAccents").transform;
            Deco(accents, "Banner", new Vector3(42.6f, 3.2f, -0.3f), new Vector3(0.7f, 2.2f, 0.05f), mats.crimson);
            Deco(accents, "Banner", new Vector3(45.4f, 3.4f, -0.3f), new Vector3(0.7f, 2.6f, 0.05f), mats.crimson);
            Deco(accents, "Banner", new Vector3(5f, 7f, 9.5f), new Vector3(1f, 3f, 0.05f), mats.crimson);

            var leaves = new GameObject("FallingLeaves");
            leaves.transform.SetParent(accents);
            leaves.transform.position = new Vector3(30f, 9f, 0f);
            leaves.AddComponent<ParticleSystem>();
            leaves.GetComponent<ParticleSystemRenderer>().sharedMaterial = mats.leaf;
            leaves.AddComponent<LeafFall>();
            var so = new SerializedObject(leaves.GetComponent<LeafFall>());
            so.FindProperty("area").vector3Value = new Vector3(60f, 1f, 6f);
            so.FindProperty("rate").floatValue = 10f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // --- Player ---------------------------------------------------
            var player = BuildPlayer(mats);

            // --- Camera ---------------------------------------------------
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 300f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = FogColor;
            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            var follow = camGo.AddComponent<CameraFollow25D>();
            follow.Target = player.GetComponent<PlayerController>();
            camGo.transform.position = player.transform.position + new Vector3(0f, 1.4f, -13f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[CrimsonVeil] Prototype scene built: " + ScenePath);
        }

        static GameObject BuildPlayer(Materials mats)
        {
            var player = new GameObject("Player");
            player.transform.position = new Vector3(-6f, 0.05f, 0f);
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.28f;
            cc.center = new Vector3(0f, 0.9f, 0f);
            cc.skinWidth = 0.03f;
            cc.stepOffset = 0.3f;
            cc.slopeLimit = 50f;

            var visual = new GameObject("Visual").transform;
            visual.SetParent(player.transform, false);
            Part(visual, PrimitiveType.Capsule, "Torso", new Vector3(0f, 1.1f, 0f), new Vector3(0.42f, 0.42f, 0.32f), mats.silhouette);
            Part(visual, PrimitiveType.Sphere, "Head", new Vector3(0f, 1.66f, 0f), Vector3.one * 0.3f, mats.silhouette);
            Part(visual, PrimitiveType.Capsule, "LegL", new Vector3(0.1f, 0.4f, 0f), new Vector3(0.16f, 0.42f, 0.16f), mats.silhouette);
            Part(visual, PrimitiveType.Capsule, "LegR", new Vector3(-0.1f, 0.4f, 0f), new Vector3(0.16f, 0.42f, 0.16f), mats.silhouette);

            var anchor = new GameObject("VeilAnchor").transform;
            anchor.SetParent(player.transform, false);
            anchor.localPosition = new Vector3(0f, 1.45f, 0f);

            player.AddComponent<PlayerInputReader>();
            var pc = player.AddComponent<PlayerController>();
            SetRef(pc, "visualRoot", visual);

            var veil = player.AddComponent<VeilController>();
            SetRef(veil, "anchor", anchor);
            SetRef(veil, "player", pc);
            SetRef(veil, "arrowMaterial", mats.veil);

            var vr = player.AddComponent<VeilRenderer>();
            SetRef(vr, "material", mats.veil);
            var wisps = player.AddComponent<VeilWisps>();
            SetRef(wisps, "material", mats.wisp);
            return player;
        }

        // ------------------------------------------------------------------ helpers

        struct Materials
        {
            public Material silhouette, mid, far, moon, crimson, veil, wisp, leaf;
        }

        static Materials CreateMaterials()
        {
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var particles = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            return new Materials
            {
                silhouette = Mat("Silhouette", unlit, new Color(0.03f, 0.03f, 0.035f)),
                mid = Mat("RuinsMid", unlit, new Color(0.2f, 0.2f, 0.21f)),
                far = Mat("RuinsFar", unlit, new Color(0.42f, 0.42f, 0.43f)),
                moon = Mat("Moon", unlit, new Color(3.2f, 3.2f, 3.1f)),
                crimson = Mat("Crimson", unlit, new Color(0.62f, 0.01f, 0.03f)),
                veil = Transparent(Mat("Veil", particles, new Color(1.25f, 0.06f, 0.07f, 1f))),
                wisp = Transparent(Mat("VeilWisp", particles, new Color(1.1f, 0.05f, 0.06f, 1f))),
                leaf = Transparent(Mat("Leaf", particles, new Color(0.9f, 0.03f, 0.05f, 1f))),
            };
        }

        static Material Mat(string name, Shader shader, Color color)
        {
            string path = $"{MatDir}/{name}.mat";
            var m = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (m == null)
            {
                m = new Material(shader);
                AssetDatabase.CreateAsset(m, path);
            }
            m.shader = shader;
            m.SetColor("_BaseColor", color);
            EditorUtility.SetDirty(m);
            return m;
        }

        static Material Transparent(Material m)
        {
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_Cull", 0f);
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetOverrideTag("RenderType", "Transparent");
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        static VolumeProfile CreateVolumeProfile()
        {
            string path = Root + "/Settings/CrimsonVeil_Volume.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            var tone = Add<Tonemapping>(profile);
            tone.mode.Override(TonemappingMode.Neutral);

            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(1f);
            bloom.intensity.Override(0.8f);
            bloom.scatter.Override(0.65f);

            var color = Add<ColorAdjustments>(profile);
            color.contrast.Override(18f);

            var vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.38f);
            vignette.smoothness.Override(0.5f);
            vignette.color.Override(Color.black);

            var grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium3);
            grain.intensity.Override(0.3f);

            EditorUtility.SetDirty(profile);
            AssetDatabase.SaveAssets();
            return profile;
        }

        static T Add<T>(VolumeProfile profile) where T : VolumeComponent
        {
            var c = profile.Add<T>(true);
            c.name = typeof(T).Name;
            c.hideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
            AssetDatabase.AddObjectToAsset(c, profile);
            return c;
        }

        static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            go.isStatic = true;
            return go;
        }

        static GameObject Deco(Transform parent, string name, Vector3 pos, Vector3 size, Material mat)
        {
            var go = Block(parent, name, pos, size, mat);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        static void Arch(Transform parent, Vector3 basePos, float height, float span, Material mat)
        {
            var arch = new GameObject("Arch").transform;
            arch.SetParent(parent, false);
            float w = span * 0.18f;
            Deco(arch, "PillarL", basePos + new Vector3(-span * 0.5f, height * 0.5f, 0f), new Vector3(w, height, w), mat);
            Deco(arch, "PillarR", basePos + new Vector3(span * 0.5f, height * 0.5f, 0f), new Vector3(w, height, w), mat);
            Deco(arch, "Top", basePos + new Vector3(0f, height + w * 0.5f, 0f), new Vector3(span + w, w * 1.2f, w), mat);
        }

        static void Part(Transform parent, PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = mat;
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
