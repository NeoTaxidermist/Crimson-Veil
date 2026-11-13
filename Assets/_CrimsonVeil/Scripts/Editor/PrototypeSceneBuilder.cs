using System.Linq;
using CrimsonVeil.CameraSystem;
using CrimsonVeil.Combat;
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
    /// Rebuilds the prototype level from code so the look (ink greys + crimson) stays reproducible.
    /// Menu: Crimson Veil ▸ Build Prototype Scene.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        const string Root = "Assets/_CrimsonVeil";
        const string ScenePath = Root + "/Scenes/Prototype_Ruins.unity";
        const string MatDir = Root + "/Materials";
        const string HeroPath = Root + "/Art/Hero/Hero.fbx";
        const string WraithPath = Root + "/Art/Enemy/Wraith.fbx";

        static readonly Color FogColor = new Color(0.76f, 0.76f, 0.76f);

        // Depth layers (z) — camera sits at z = -13 looking +z.
        const float ZForeground = -3.2f;
        const float ZNear = 9f;
        const float ZMid = 28f;
        const float ZFar = 65f;
        const float ZSky = 130f;

        [MenuItem("Crimson Veil/Build Prototype Scene")]
        public static void Build()
        {
            ConfigureHeroImport();
            var m = CreateMaterials();
            var profile = CreateVolumeProfile();
            InstallInkGrade(m.inkGrade);

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // --- Atmosphere -------------------------------------------------
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 14f;
            RenderSettings.fogEndDistance = 170f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

            var lightGo = new GameObject("Moonlight");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.4f;
            lightGo.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            var atmosphere = new GameObject("Atmosphere").AddComponent<AtmosphereController>();
            atmosphere.mistColor = FogColor;
            atmosphere.mistTop = 1.5f;
            atmosphere.mistFade = 9f;
            atmosphere.mistDistance = 55f;

            // --- Playfield (z = 0) ------------------------------------------
            var level = Group("Level");
            Block(level, "Start", new Vector3(-2f, -0.5f, 0f), new Vector3(24f, 1f, 4f), m.inkPlay);
            Block(level, "StartCliff", new Vector3(-2f, -8f, 0f), new Vector3(23f, 14f, 3.8f), m.inkPlay);
            Block(level, "Step", new Vector3(13f, 0.75f, 0f), new Vector3(5f, 3.5f, 4f), m.inkPlay);
            Block(level, "GapLedge", new Vector3(23f, 0f, 0f), new Vector3(7f, 1f, 4f), m.inkPlay);
            Block(level, "GapLedgeRoot", new Vector3(23f, -6f, 0f), new Vector3(5f, 11f, 3.5f), m.inkPlay);
            Block(level, "HighLedge", new Vector3(31f, 3f, 0f), new Vector3(4f, 1f, 4f), m.inkPlay);
            Block(level, "Floor2", new Vector3(47f, -0.5f, 0f), new Vector3(22f, 1f, 4f), m.inkPlay);
            Block(level, "Floor2Cliff", new Vector3(47f, -8f, 0f), new Vector3(21f, 14f, 3.8f), m.inkPlay);
            Block(level, "FarLedge", new Vector3(81f, -0.5f, 0f), new Vector3(24f, 1f, 4f), m.inkPlay);
            Block(level, "FarLedgeCliff", new Vector3(81f, -8f, 0f), new Vector3(23f, 14f, 3.8f), m.inkPlay);
            Block(level, "EndWall", new Vector3(94f, 6f, 0f), new Vector3(2f, 14f, 4f), m.inkPlay);
            Block(level, "BackWall", new Vector3(-14f, 6f, 0f), new Vector3(2f, 14f, 4f), m.inkPlay);

            // Ruined arch the player runs through.
            Arch(level, "PlayArch", new Vector3(44f, 0f, 1.6f), 4.2f, 0.9f, 3.6f, 0.9f, 1.4f, m.inkPlay, 0.35f, 4);

            // Puzzle: a plank stands at the chasm edge, held up by a high crimson rope.
            // Crimson means "the veil can touch this" — only an arrow loosed upwards reaches it.
            BuildRopeBridge(level, m);
            new GameObject("Checkpoint").AddComponent<Checkpoint>().transform.position = new Vector3(71.5f, 0f, 0f);

            // Things to break and creatures to fight.
            var props = Group("Breakables", level);
            foreach (var (x, s) in new[] { (-1.2f, 1), (-0.4f, 2), (0.5f, 3), (37.5f, 4), (38.3f, 5), (73f, 6), (89f, 7) })
                Urn(props, new Vector3(x, 0f, 0f), s, m.inkPlay);
            var enemies = Group("Enemies");
            foreach (var x in new[] { 6f, 23f, 49f, 79f, 87f })
                Wraith(enemies, new Vector3(x, x > 20f && x < 27f ? 0.5f : 0f, 0f), m);

            // Rubble behind the play plane.
            int rs = 50;
            foreach (var x in new[] { -6f, 3f, 17f, 28f, 41f, 55f, 75f, 84f })
                Rubble(level, new Vector3(x, x > 10f && x < 16f ? 2.5f : (x > 20f && x < 27f ? 0.5f : 0f), 1.4f), rs++, m.inkPlay);

            // Grass along every walkable edge, in front of and behind the play plane.
            int seed = 10;
            foreach (var (x0, x1, y) in new[] { (-13f, 9f, 0f), (10.6f, 15.4f, 2.5f), (19.6f, 26.4f, 0.5f), (29.2f, 32.8f, 3.5f), (36f, 58f, 0f), (69.5f, 92.5f, 0f) })
            {
                for (float x = x0 + 1f; x < x1; x += 2.2f)
                {
                    Grass(level, new Vector3(x, y, -1.6f), seed++, 2.6f, new Vector2(0.15f, 0.6f), 26, m.grass);
                    Grass(level, new Vector3(x + 1f, y, 1.7f), seed++, 2.4f, new Vector2(0.2f, 0.9f), 22, m.grass);
                }
            }

            // Gallows with tattered crimson banners (left side, like the key art).
            var gallows = Group("Gallows", level);
            Block(gallows, "Post", new Vector3(-9.5f, 2.9f, 1.2f), new Vector3(0.35f, 5.8f, 0.35f), m.inkPlay, collider: false);
            Block(gallows, "Beam", new Vector3(-8.2f, 5.4f, 1.2f), new Vector3(3.2f, 0.3f, 0.3f), m.inkPlay, collider: false);
            Block(gallows, "Chain", new Vector3(-7.1f, 4.4f, 1.2f), new Vector3(0.04f, 2f, 0.04f), m.inkPlay, collider: false);
            Banner(gallows, new Vector3(-9.0f, 5.25f, 1.0f), 0.9f, 2.8f, 3, m.banner);
            Banner(gallows, new Vector3(-7.9f, 5.25f, 1.0f), 0.7f, 2.0f, 4, m.banner);

            // The crimson tree on the far ledge.
            var tree = Prop<TreeProp>(level, "CrimsonTree", new Vector3(52f, 0f, 1.8f), 21, new[] { m.inkPlay, m.leaf });
            tree.height = 11f; tree.trunkRadius = 0.55f; tree.depth = 5; tree.leavesPerTip = 18; tree.leafSize = 0.26f;
            tree.Rebuild();
            for (int i = 0; i < 6; i++)
                Grass(level, new Vector3(46f + i * 1.8f, 0f, 0.6f + (i % 2)), 300 + i, 2f, new Vector2(0.04f, 0.16f), 30, m.leaf, 0.12f);

            // --- Foreground silhouettes --------------------------------------
            var fg = Group("Foreground");
            for (int i = 0; i < 38; i++)
                Grass(fg, new Vector3(-14f + i * 2.9f, -2.4f + (i % 3) * 0.3f, ZForeground), 100 + i, 3.4f, new Vector2(0.6f, 1.9f), 40, m.grass, 0.07f);
            Block(fg, "BrokenPost", new Vector3(-4f, -1f, ZForeground - 0.4f), new Vector3(0.5f, 3.2f, 0.5f), m.inkFG, collider: false).transform.rotation = Quaternion.Euler(0, 0, 8f);
            Block(fg, "BrokenPost", new Vector3(33f, -1.2f, ZForeground - 0.4f), new Vector3(0.6f, 3f, 0.6f), m.inkFG, collider: false).transform.rotation = Quaternion.Euler(0, 0, -12f);

            // --- Near ruins ----------------------------------------------------
            var near = Group("NearRuins");
            float[] nearBreak = { 0f, 0.55f, 0.2f, 0.75f, 0f, 0.4f, 0.65f, 0.1f, 0.5f, 0.3f };
            for (int i = 0; i < 10; i++)
            {
                float x = -12f + i * 11.5f;
                Arch(near, "Arch", new Vector3(x, -1f, ZNear + (i % 2) * 2.5f), 3.6f + (i % 3) * 0.6f, 0.8f, 4.5f + (i % 2), 0.9f, 1.2f, m.inkNear, nearBreak[i], i);
                Rubble(near, new Vector3(x + 3.5f, -1f, ZNear + 1f), 200 + i, m.inkNear, 3.5f, 1.2f);
            }
            Banner(near, new Vector3(6.6f, 8.6f, ZNear - 0.4f), 1.1f, 3.6f, 9, m.banner);
            Banner(near, new Vector3(29f, 8.2f, ZNear + 2f), 1.0f, 3.0f, 10, m.banner);

            // --- Mid ruins + waterfall -----------------------------------------
            var mid = Group("MidRuins");
            for (int i = 0; i < 9; i++)
                Arch(mid, "Aqueduct", new Vector3(-30f + i * 9f, -2f, ZMid), 6f, 1.4f, 8f, 2f, 3f, m.inkMid, i == 3 ? 0.5f : 0f, i);
            Block(mid, "AqueductDeck", new Vector3(-12f, 15.8f, ZMid), new Vector3(30f, 2.2f, 3f), m.inkMid, collider: false);
            Block(mid, "AqueductDeck", new Vector3(19f, 15.8f, ZMid), new Vector3(22f, 2.2f, 3f), m.inkMid, collider: false);
            // Beyond the cliff the aqueduct is broken: lone, ruined arches.
            for (int i = 0; i < 5; i++)
                Arch(mid, "AqueductRuin", new Vector3(60f + i * 10f, -2f + (i % 2) * 1.5f, ZMid + (i % 2) * 3f), 6f, 1.4f, 7f + (i % 3), 2f, 3f, m.inkMid, 0.3f + 0.15f * (i % 3), i + 1);
            Block(mid, "Cliff", new Vector3(48f, 4f, ZMid - 2f), new Vector3(14f, 22f, 6f), m.inkMid, collider: false);
            Card(mid, "Waterfall", new Vector3(40.5f, 2f, ZMid - 3.4f), new Vector2(3.2f, 22f), m.waterfall);
            Card(mid, "WaterfallMist", new Vector3(40.5f, -6f, ZMid - 4f), new Vector2(14f, 6f), m.mistBand);

            // --- Far ruins -------------------------------------------------------
            var far = Group("FarRuins");
            float[] farBreak = { 0f, 0.3f, 0f, 0f, 0.6f, 0f, 0.2f, 0f, 0f, 0.45f, 0f, 0.7f, 0f, 0.25f };
            for (int i = 0; i < 14; i++)
                Arch(far, "FarAqueduct", new Vector3(-60f + i * 13f, -12f + (i % 3) * 1.5f, ZFar + (i % 2) * 8f), 8f, 2f, 14f, 3f, 4f, m.inkFar, farBreak[i], i);
            Block(far, "Tower", new Vector3(-22f, 10f, ZFar + 4f), new Vector3(6f, 40f, 6f), m.inkFar, collider: false);
            Block(far, "TowerTop", new Vector3(-22f, 31f, ZFar + 4f), new Vector3(8f, 2f, 8f), m.inkFar, collider: false);
            Block(far, "Tower", new Vector3(70f, 8f, ZFar + 10f), new Vector3(7f, 36f, 7f), m.inkFar, collider: false);
            Card(far, "FarWaterfall", new Vector3(14f, 0f, ZFar - 1.5f), new Vector2(4f, 30f), m.waterfall);
            Block(far, "DistantCliffs", new Vector3(0f, -10f, ZSky - 20f), new Vector3(400f, 24f, 4f), m.inkSky, collider: false);

            // --- Mist bands & light --------------------------------------------
            var mist = Group("Mist");
            Card(mist, "GroundMist", new Vector3(20f, -1.6f, 1.2f), new Vector2(120f, 2.6f), m.mistLow);
            Card(mist, "MistNear", new Vector3(20f, -1f, 5f), new Vector2(160f, 7f), m.mistBand);
            Card(mist, "MistMid", new Vector3(20f, 0f, 18f), new Vector2(220f, 12f), m.mistBand);
            Card(mist, "MistFar", new Vector3(20f, -2f, 45f), new Vector2(320f, 22f), m.mistBand);
            Card(mist, "MistSky", new Vector3(20f, 4f, 100f), new Vector2(500f, 40f), m.mistBand);
            for (int i = 0; i < 4; i++)
            {
                var shaft = Card(mist, "LightShaft", new Vector3(-6f + i * 17f, 12f, 22f + i * 6f), new Vector2(5f + i, 34f), m.shaft);
                shaft.transform.rotation = Quaternion.Euler(0f, 0f, 22f);
            }

            var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moon.name = "Moon";
            Object.DestroyImmediate(moon.GetComponent<Collider>());
            moon.transform.position = new Vector3(16f, 34f, ZSky);
            moon.transform.localScale = Vector3.one * 36f;
            moon.GetComponent<Renderer>().sharedMaterial = m.moon;

            var leaves = new GameObject("FallingLeaves");
            leaves.transform.position = new Vector3(45f, 10f, 0f);
            leaves.AddComponent<ParticleSystem>();
            leaves.GetComponent<ParticleSystemRenderer>().sharedMaterial = m.leafParticle;
            leaves.AddComponent<LeafFall>();
            var so = new SerializedObject(leaves.GetComponent<LeafFall>());
            so.FindProperty("area").vector3Value = new Vector3(100f, 1f, 6f);
            so.FindProperty("rate").floatValue = 12f;
            so.ApplyModifiedPropertiesWithoutUndo();

            // --- Player & camera ------------------------------------------------
            var fx = new GameObject("CombatFx").AddComponent<CombatFx>();
            SetRef(fx, "burstMaterial", m.wisp);
            var player = BuildPlayer(m);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.fieldOfView = 32f;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 400f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = FogColor;
            camGo.AddComponent<AudioListener>();
            var camData = camGo.AddComponent<UniversalAdditionalCameraData>();
            camData.renderPostProcessing = true;
            camData.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            camGo.AddComponent<CameraFollow25D>().Target = player.GetComponent<PlayerController>();
            camGo.transform.position = player.transform.position + new Vector3(0f, 2.4f, -13f);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[CrimsonVeil] Prototype scene built: " + ScenePath);
        }

        // ================================================================== player

        static GameObject BuildPlayer(Materials m)
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

            var anchor = new GameObject("VeilAnchor").transform;
            anchor.SetParent(player.transform, false);
            anchor.localPosition = new Vector3(0f, 1.5f, 0f);

            player.AddComponent<PlayerInputReader>();
            var pc = player.AddComponent<PlayerController>();
            SetRef(pc, "visualRoot", visual);

            var veil = player.AddComponent<VeilController>();
            SetRef(veil, "anchor", anchor);
            SetRef(veil, "player", pc);
            SetRef(veil, "arrowMaterial", m.veil);
            SetRef(player.AddComponent<VeilRenderer>(), "material", m.veil);
            SetRef(player.AddComponent<VeilWisps>(), "material", m.wisp);
            player.AddComponent<VeilCombat>();
            SetRef(player.AddComponent<PlayerHealth>(), "visualRoot", visual);

            var heroAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath);
            if (heroAsset != null)
            {
                var hero = (GameObject)PrefabUtility.InstantiatePrefab(heroAsset);
                hero.name = "Hero";
                hero.transform.SetParent(visual, false);
                foreach (var smr in hero.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.sharedMaterial = m.hero;
                    smr.updateWhenOffscreen = true;
                    smr.shadowCastingMode = ShadowCastingMode.Off;
                }
                var anim = player.AddComponent<HeroAnimator>();
                SetRef(anim, "player", pc);
                SetRef(anim, "veil", veil);
                SetRef(anim, "model", hero.transform);
                SetRef(anim, "veilAnchor", anchor);
            }
            else
            {
                Debug.LogWarning("[CrimsonVeil] Hero.fbx not found, using a capsule.");
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.transform.SetParent(visual, false);
                body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                body.GetComponent<Renderer>().sharedMaterial = m.hero;
            }
            return player;
        }

        static void ConfigureHeroImport()
        {
            foreach (var path in new[] { HeroPath, WraithPath })
            {
                if (!(AssetImporter.GetAtPath(path) is ModelImporter mi)) continue;
                bool dirty = mi.animationType != ModelImporterAnimationType.Generic || mi.importAnimation || mi.materialImportMode != ModelImporterMaterialImportMode.None;
                mi.animationType = ModelImporterAnimationType.Generic;
                mi.importAnimation = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.importCameras = false;
                mi.importLights = false;
                if (dirty) mi.SaveAndReimport();
            }
        }

        // ================================================================== materials

        struct Materials
        {
            public Material inkFG, inkPlay, inkNear, inkMid, inkFar, inkSky, grass, banner, leaf, hero;
            public Material mistBand, mistLow, waterfall, shaft, moon;
            public Material veil, wisp, leafParticle, inkGrade;
            public Material wraith, wraithEyes;
        }

        static Materials CreateMaterials()
        {
            var sil = Shader.Find("CrimsonVeil/Silhouette");
            var mist = Shader.Find("CrimsonVeil/Mist");
            var unlit = Shader.Find("Universal Render Pipeline/Unlit");
            var particles = Shader.Find("Universal Render Pipeline/Particles/Unlit");

            Material Ink(string n, float v, float mistStrength, float grain = 0.35f) =>
                Sil(n, sil, new Color(v, v, v * 1.04f), mistStrength, grain);

            var mats = new Materials
            {
                inkFG = Ink("InkForeground", 0.015f, 0f, 0.2f),
                inkPlay = Ink("InkPlay", 0.03f, 0.55f),
                inkNear = Ink("InkNear", 0.2f, 1.2f, 0.45f),
                inkMid = Ink("InkMid", 0.36f, 1.2f, 0.45f),
                inkFar = Ink("InkFar", 0.5f, 1.1f, 0.4f),
                inkSky = Ink("InkSky", 0.62f, 0.6f, 0.3f),
                wraith = Sil("WraithInk", sil, new Color(0.008f, 0.008f, 0.01f), 0.15f, 0.25f),
                wraithEyes = Sil("WraithEyes", sil, new Color(1f, 0.02f, 0.04f), 0f, 0f),
                grass = Wind(Sil("GrassInk", sil, new Color(0.02f, 0.02f, 0.022f), 0.4f, 0.2f), 1f, false),
                banner = Wind(Sil("BannerCrimson", sil, new Color(0.62f, 0.015f, 0.03f), 0.3f, 0.6f), 1.6f, true),
                leaf = Wind(Sil("LeafCrimson", sil, new Color(0.78f, 0.02f, 0.035f), 0.2f, 0.7f), 0.6f, false),
                hero = Sil("HeroInk", sil, new Color(0.012f, 0.012f, 0.014f), 0.12f, 0.15f),

                mistBand = MistMat("MistBand", mist, "_MODE_MIST", 0, new Color(0.8f, 0.8f, 0.8f), 0.55f, new Vector2(3f, 1.2f), new Vector2(0.025f, 0f), new Vector2(0.15f, 0.45f)),
                mistLow = MistMat("MistLow", mist, "_MODE_MIST", 0, new Color(0.78f, 0.78f, 0.78f), 0.4f, new Vector2(18f, 1.5f), new Vector2(0.01f, 0f), new Vector2(0.05f, 0.5f)),
                waterfall = MistMat("Waterfall", mist, "_MODE_WATERFALL", 2, new Color(0.88f, 0.88f, 0.88f), 0.85f, new Vector2(6f, 8f), new Vector2(0f, 1.1f), new Vector2(0.25f, 0.1f)),
                shaft = MistMat("LightShaft", mist, "_MODE_SHAFT", 1, new Color(1f, 1f, 1f), 0.16f, new Vector2(2f, 4f), new Vector2(0.01f, -0.02f), new Vector2(0.35f, 0.3f)),
                moon = Mat("Moon", unlit, new Color(3.4f, 3.4f, 3.3f)),

                veil = Transparent(Mat("Veil", particles, new Color(1.25f, 0.06f, 0.07f, 1f))),
                wisp = Transparent(Mat("VeilWisp", particles, new Color(1.1f, 0.05f, 0.06f, 1f))),
                leafParticle = Transparent(Mat("Leaf", particles, new Color(0.9f, 0.03f, 0.05f, 1f))),
                inkGrade = Mat("InkGrade", Shader.Find("CrimsonVeil/InkGrade"), Color.white),
            };
            mats.shaft.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mats.shaft.SetFloat("_DstBlend", (float)BlendMode.One);
            mats.wisp.SetTexture("_BaseMap", SpriteTexture("SoftDot", SoftDot));
            mats.leafParticle.SetTexture("_BaseMap", SpriteTexture("LeafShape", LeafShape));
            return mats;
        }

        // Soft round ink blot for wisps.
        static float SoftDot(float x, float y)
        {
            float d = Mathf.Sqrt(x * x + y * y);
            return Mathf.Clamp01(1f - d) * Mathf.Clamp01(1f - d) ;
        }

        // Pointed maple-ish leaf: a lens with three lobes.
        static float LeafShape(float x, float y)
        {
            float a = Mathf.Atan2(y, x);
            float r = Mathf.Sqrt(x * x + y * y);
            float lobes = 0.62f + 0.3f * Mathf.Pow(Mathf.Abs(Mathf.Cos(a * 2.5f)), 3f);
            float edge = lobes - r;
            return Mathf.Clamp01(edge * 18f);
        }

        static Texture2D SpriteTexture(string name, System.Func<float, float, float> alpha)
        {
            string path = $"{MatDir}/{name}.png";
            const int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = (x + 0.5f) / size * 2f - 1f, v = (y + 0.5f) / size * 2f - 1f;
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha(u, v)));
            }
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            if (AssetImporter.GetAtPath(path) is TextureImporter ti)
            {
                ti.alphaIsTransparency = true;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.mipmapEnabled = true;
                ti.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        static Material Sil(string name, Shader shader, Color color, float mist, float grain)
        {
            var m = Mat(name, shader, color);
            m.SetFloat("_MistStrength", mist);
            m.SetFloat("_GrainStrength", grain);
            // Only distant, lighter layers get a hint of moonlit top edge; near ink stays pure black.
            m.SetFloat("_EdgeLight", color.grayscale > 0.15f && color.r - color.g < 0.1f ? 0.03f : 0f);
            return m;
        }

        static Material Wind(Material m, float weight, bool pinTop)
        {
            m.SetFloat("_Wind", 1f);
            m.EnableKeyword("_WIND_ON");
            m.SetFloat("_WindWeight", weight);
            m.SetFloat("_PinTop", pinTop ? 1f : 0f);
            m.SetFloat("_Cull", 0f);
            return m;
        }

        static Material MistMat(string name, Shader shader, string keyword, int mode, Color color, float opacity, Vector2 scale, Vector2 scroll, Vector2 edge)
        {
            var m = Mat(name, shader, color);
            m.SetColor("_Color", color);
            m.SetFloat("_Mode", mode);
            foreach (var k in new[] { "_MODE_MIST", "_MODE_SHAFT", "_MODE_WATERFALL" }) m.DisableKeyword(k);
            m.EnableKeyword(keyword);
            m.SetFloat("_Opacity", opacity);
            m.SetVector("_NoiseScale", scale);
            m.SetVector("_Scroll", scroll);
            m.SetVector("_EdgeSoftness", edge);
            m.renderQueue = (int)RenderQueue.Transparent;
            return m;
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
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
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

        // ================================================================== rendering

        static void InstallInkGrade(Material material)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:UniversalRendererData", new[] { "Assets/Settings" }))
            {
                var data = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data == null) continue;
                var feature = data.rendererFeatures.OfType<FullScreenPassRendererFeature>().FirstOrDefault(f => f != null && f.name == "InkGrade");
                if (feature == null)
                {
                    feature = ScriptableObject.CreateInstance<FullScreenPassRendererFeature>();
                    feature.name = "InkGrade";
                    AssetDatabase.AddObjectToAsset(feature, data);
                    AssetDatabase.TryGetGUIDAndLocalFileIdentifier(feature, out string _, out long localId);

                    var so = new SerializedObject(data);
                    var list = so.FindProperty("m_RendererFeatures");
                    var map = so.FindProperty("m_RendererFeatureMap");
                    list.arraySize++;
                    list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = feature;
                    map.arraySize++;
                    map.GetArrayElementAtIndex(map.arraySize - 1).longValue = localId;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
                feature.passMaterial = material;
                feature.injectionPoint = FullScreenPassRendererFeature.InjectionPoint.AfterRenderingPostProcessing;
                feature.fetchColorBuffer = true;
                feature.SetActive(true);
                EditorUtility.SetDirty(feature);
                EditorUtility.SetDirty(data);
            }
            AssetDatabase.SaveAssets();
        }

        static VolumeProfile CreateVolumeProfile()
        {
            string path = Root + "/Settings/CrimsonVeil_Volume.asset";
            AssetDatabase.DeleteAsset(path);
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile, path);

            Add<Tonemapping>(profile).mode.Override(TonemappingMode.Neutral);

            var bloom = Add<Bloom>(profile);
            bloom.threshold.Override(0.95f);
            bloom.intensity.Override(1.1f);
            bloom.scatter.Override(0.75f);

            Add<ColorAdjustments>(profile).contrast.Override(10f);

            var vignette = Add<Vignette>(profile);
            vignette.intensity.Override(0.42f);
            vignette.smoothness.Override(0.55f);
            vignette.color.Override(Color.black);

            var grain = Add<FilmGrain>(profile);
            grain.type.Override(FilmGrainLookup.Medium4);
            grain.intensity.Override(0.22f);

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

        // ================================================================== props

        static Transform Group(string name, Transform parent = null)
        {
            var t = new GameObject(name).transform;
            if (parent != null) t.SetParent(parent, false);
            return t;
        }

        static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            go.isStatic = collider;
            return go;
        }

        static T Prop<T>(Transform parent, string name, Vector3 pos, int seed, Material[] mats) where T : ProceduralProp
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.Off;
            var p = go.AddComponent<T>();
            p.Configure(seed);
            return p;
        }

        static void Arch(Transform parent, string name, Vector3 pos, float span, float pillar, float spring, float crown, float depth, Material mat, float breakAmount = 0f, int seed = 0)
        {
            var a = Prop<ArchProp>(parent, name, pos, seed, new[] { mat });
            a.span = span; a.pillarWidth = pillar; a.springHeight = spring; a.crown = crown; a.depth = depth; a.breakAmount = breakAmount;
            a.Rebuild();
        }

        static void Rubble(Transform parent, Vector3 pos, int seed, Material mat, float width = 2.2f, float size = 0.6f)
        {
            var r = Prop<RubbleProp>(parent, "Rubble", pos, seed, new[] { mat });
            r.width = width; r.maxSize = size; r.blocks = 7 + seed % 5;
            r.Rebuild();
        }

        static void Urn(Transform parent, Vector3 pos, int seed, Material mat)
        {
            var u = Prop<UrnProp>(parent, "Urn", pos, seed, new[] { mat });
            u.height = 0.65f + (seed % 3) * 0.12f;
            u.Rebuild();
            var box = u.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true; // the hero walks past; the veil and arrows still hit it
            box.center = new Vector3(0f, u.height * 0.5f, 0f);
            box.size = new Vector3(0.6f, u.height, 0.6f);
            u.gameObject.AddComponent<Breakable>();
        }

        static void Wraith(Transform parent, Vector3 pos, Materials m)
        {
            var go = new GameObject("Wraith");
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var cap = go.AddComponent<CapsuleCollider>();
            cap.isTrigger = true;
            cap.center = new Vector3(0f, 1.05f, 0f);
            cap.height = 2f;
            cap.radius = 0.4f;
            go.AddComponent<Enemy>();

            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(WraithPath);
            Renderer eyes = null;
            if (asset != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                model.name = "Model";
                model.transform.SetParent(visual, false);
                foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.sharedMaterials = new[] { m.wraith, m.wraithEyes };
                    smr.updateWhenOffscreen = true;
                    smr.shadowCastingMode = ShadowCastingMode.Off;
                    eyes = smr;
                }
            }
            var anim = go.AddComponent<EnemyAnimator>();
            SetRef(anim, "visualRoot", visual);
            if (eyes != null) SetRef(anim, "eyes", eyes);
        }

        static void BuildRopeBridge(Transform parent, Materials m)
        {
            var root = Group("RopeBridgePuzzle", parent);

            var pivot = new GameObject("BridgeHinge").transform;
            pivot.SetParent(root, false);
            pivot.position = new Vector3(58.2f, 0f, 0f);
            var plank = Block(pivot, "Plank", Vector3.zero, new Vector3(0.45f, 11f, 2.6f), m.inkPlay);
            plank.isStatic = false;
            plank.transform.localPosition = new Vector3(0f, 5.5f, 0f);
            var hinge = pivot.gameObject.AddComponent<HingedBridge>();
            hinge.Configure(0f, -90f, 0.9f);

            var top = new GameObject("RopeTop").transform;
            top.SetParent(pivot, false);
            top.localPosition = new Vector3(-0.2f, 10.6f, 0f);

            Block(root, "RopePost", new Vector3(53.8f, 4.2f, 0.7f), new Vector3(0.4f, 8.4f, 0.4f), m.inkPlay, collider: false);
            var post = new GameObject("RopePostTop").transform;
            post.SetParent(root, false);
            post.position = new Vector3(54f, 8.2f, 0f);

            var rope = new GameObject("CrimsonRope").AddComponent<CuttableRope>();
            rope.transform.SetParent(root, false);
            rope.Configure(post, top, hinge, m.veil);
            var so = new SerializedObject(rope);
            so.FindProperty("start").objectReferenceValue = post;
            so.FindProperty("end").objectReferenceValue = top;
            so.FindProperty("bridge").objectReferenceValue = hinge;
            so.FindProperty("material").objectReferenceValue = m.veil;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void Grass(Transform parent, Vector3 pos, int seed, float width, Vector2 height, int blades, Material mat, float bladeWidth = 0.05f)
        {
            var g = Prop<GrassProp>(parent, "Grass", pos, seed, new[] { mat });
            g.width = width; g.heightRange = height; g.blades = blades; g.bladeWidth = bladeWidth;
            g.Rebuild();
        }

        static void Banner(Transform parent, Vector3 pos, float width, float height, int seed, Material mat)
        {
            var b = Prop<BannerProp>(parent, "Banner", pos, seed, new[] { mat });
            b.width = width; b.height = height;
            b.Rebuild();
        }

        static GameObject Card(Transform parent, string name, Vector3 center, Vector2 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            go.transform.SetParent(parent, false);
            go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
