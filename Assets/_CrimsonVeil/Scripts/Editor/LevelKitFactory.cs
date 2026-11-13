using CrimsonVeil.CameraSystem;
using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using CrimsonVeil.Veil;
using CrimsonVeil.World;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace CrimsonVeil.EditorTools
{
    /// <summary>
    /// Builds every level-kit object from code. Used to (re)generate the prefabs the Level Editor places,
    /// and by the prototype scene builder — one source of truth for the look.
    /// </summary>
    public static class LevelKitFactory
    {
        public const string HeroPath = LevelKitMaterials.Root + "/Art/Hero/Hero.fbx";
        public const string WraithPath = LevelKitMaterials.Root + "/Art/Enemy/Wraith.fbx";
        public static readonly Color FogColor = new Color(0.76f, 0.76f, 0.76f);

        static LevelKitMaterials M => LevelKitMaterials.Get();

        // ================================================================== scene setup

        public static void ApplyRenderSettings()
        {
            RenderSettings.skybox = null;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = FogColor;
            RenderSettings.fogStartDistance = 14f;
            RenderSettings.fogEndDistance = 170f;
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);
            LevelKitMaterials.InstallInkGrade(M.inkGrade);
        }

        /// <summary>Camera, light, post-processing, atmosphere, combat FX and the moon.</summary>
        public static GameObject Essentials()
        {
            var root = new GameObject("Level Essentials");

            var light = new GameObject("Moonlight").AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 0.4f;
            light.transform.SetParent(root.transform, false);
            light.transform.rotation = Quaternion.Euler(35f, -30f, 0f);

            var volume = new GameObject("Global Volume").AddComponent<Volume>();
            volume.transform.SetParent(root.transform, false);
            volume.isGlobal = true;
            volume.sharedProfile = LevelKitMaterials.VolumeProfile();

            var atmosphere = new GameObject("Atmosphere").AddComponent<AtmosphereController>();
            atmosphere.transform.SetParent(root.transform, false);
            atmosphere.mistColor = FogColor;
            atmosphere.mistTop = 1.5f;
            atmosphere.mistFade = 9f;
            atmosphere.mistDistance = 55f;

            var fx = new GameObject("CombatFx").AddComponent<CombatFx>();
            fx.transform.SetParent(root.transform, false);
            SetRef(fx, "burstMaterial", M.burst);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            camGo.transform.SetParent(root.transform, false);
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
            camGo.AddComponent<CameraFollow25D>();
            camGo.transform.position = new Vector3(0f, 2.4f, -13f);

            var moon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            moon.name = "Moon";
            Object.DestroyImmediate(moon.GetComponent<Collider>());
            moon.transform.SetParent(root.transform, false);
            moon.transform.position = new Vector3(16f, 30f, 130f);
            moon.transform.localScale = Vector3.one * 46f;
            moon.GetComponent<Renderer>().sharedMaterial = M.moon;
            return root;
        }

        // ================================================================== gameplay

        public static GameObject Player(Vector3 pos)
        {
            var player = new GameObject("Player");
            player.transform.position = pos;
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.8f; cc.radius = 0.28f; cc.center = new Vector3(0f, 0.9f, 0f);
            cc.skinWidth = 0.03f; cc.stepOffset = 0.3f; cc.slopeLimit = 50f;

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
            SetRef(veil, "arrowMaterial", M.rope);
            SetRef(player.AddComponent<VeilRenderer>(), "material", M.veil);
            SetRef(player.AddComponent<VeilWisps>(), "material", M.wisp);
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
                    smr.sharedMaterial = M.hero;
                    smr.updateWhenOffscreen = true;
                    smr.shadowCastingMode = ShadowCastingMode.Off;
                }
                var anim = player.AddComponent<HeroAnimator>();
                SetRef(anim, "player", pc);
                SetRef(anim, "veil", veil);
                SetRef(anim, "model", hero.transform);
                SetRef(anim, "veilAnchor", anchor);
                SetRef(player.AddComponent<SpringBones>(), "root", hero.transform);
                var handBone = CrimsonVeil.Core.RigJoint.FindDeep(hero.transform, "Hand.R");
                if (handBone != null) SetRef(veil, "hand", handBone);
                var drawBone = CrimsonVeil.Core.RigJoint.FindDeep(hero.transform, "Hand.L");
                if (drawBone != null) SetRef(veil, "drawHand", drawBone);
                SetRef(veil, "stringMaterial", M.rope);
            }
            return player;
        }

        public static GameObject Spawn(Vector3 pos)
        {
            var go = new GameObject("PlayerSpawn");
            go.transform.position = pos;
            go.AddComponent<PlayerSpawn>();
            return go;
        }

        public static GameObject Terrain(Vector3 top, float width, float height, TerrainStyle style = TerrainStyle.Rock, int seed = 1, bool grass = true)
        {
            var go = new GameObject(height > width * 1.5f ? "Wall" : "Platform");
            go.transform.position = top;
            var t = go.AddComponent<TerrainBlock>();
            t.width = width; t.height = height; t.style = style; t.seed = seed;
            t.grassOnTop = grass;
            t.hangingRoots = height < 6f;
            t.bodyMaterial = M.inkPlay;
            t.grassMaterial = M.grass;
            t.Rebuild();
            return go;
        }

        public static GameObject Urn(Vector3 pos, int seed = 1)
        {
            var u = Prop<UrnProp>(null, "Urn", pos, seed, new[] { M.inkPlay });
            u.height = 0.65f + (seed % 3) * 0.12f;
            u.Rebuild();
            var box = u.gameObject.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0f, u.height * 0.5f, 0f);
            box.size = new Vector3(0.6f, u.height, 0.6f);
            u.gameObject.AddComponent<Breakable>();
            return u.gameObject;
        }

        public static GameObject Wraith(Vector3 pos)
        {
            var go = new GameObject("Wraith");
            go.transform.position = pos;
            var cap = go.AddComponent<CapsuleCollider>();
            cap.isTrigger = true; cap.center = new Vector3(0f, 1.05f, 0f); cap.height = 2f; cap.radius = 0.4f;
            go.AddComponent<Enemy>();
            var visual = new GameObject("Visual").transform;
            visual.SetParent(go.transform, false);
            Renderer eyes = null;
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(WraithPath);
            if (asset != null)
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                model.name = "Model";
                model.transform.SetParent(visual, false);
                foreach (var smr in model.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    smr.sharedMaterials = new[] { M.wraith, M.wraithEyes };
                    smr.updateWhenOffscreen = true;
                    smr.shadowCastingMode = ShadowCastingMode.Off;
                    eyes = smr;
                }
            }
            var anim = go.AddComponent<EnemyAnimator>();
            SetRef(anim, "visualRoot", visual);
            if (eyes != null) SetRef(anim, "eyes", eyes);
            return go;
        }

        public static GameObject Checkpoint(Vector3 pos)
        {
            var go = new GameObject("Checkpoint");
            go.transform.position = pos;
            go.AddComponent<Checkpoint>();
            return go;
        }

        public static GameObject CameraZone(Vector3 pos, float width = 20f)
        {
            var go = new GameObject("CameraZone");
            go.transform.position = pos;
            var z = go.AddComponent<CameraZone>();
            z.minX = -width * 0.5f;
            z.maxX = width * 0.5f;
            z.offset = new Vector3(0f, 4f, -16.5f);
            return go;
        }

        /// <summary>Plank standing at <paramref name="hinge"/> that falls to the right when its crimson rope is cut.</summary>
        public static GameObject RopeBridge(Vector3 hinge, float length = 11f)
        {
            var root = new GameObject("RopeBridge");
            root.transform.position = hinge;

            var pivot = new GameObject("BridgeHinge").transform;
            pivot.SetParent(root.transform, false);
            var plank = Block(pivot, "Plank", Vector3.zero, new Vector3(0.45f, length, 2.6f), M.inkPlay);
            plank.transform.localPosition = new Vector3(0f, length * 0.5f, 0f);
            var bridge = pivot.gameObject.AddComponent<HingedBridge>();
            bridge.Configure(0f, -90f, 0.9f);

            var top = new GameObject("RopeTop").transform;
            top.SetParent(pivot, false);
            top.localPosition = new Vector3(-0.2f, Mathf.Min(7.2f, length * 0.65f), 0f);

            Block(root.transform, "RopePost", Vector3.zero, new Vector3(0.4f, 7f, 0.4f), M.inkPlay, false).transform.localPosition = new Vector3(-4.4f, 3.5f, 0.7f);
            var post = new GameObject("RopePostTop").transform;
            post.SetParent(root.transform, false);
            post.localPosition = new Vector3(-4.2f, 6.8f, 0f);

            var rope = new GameObject("CrimsonRope").AddComponent<CuttableRope>();
            rope.transform.SetParent(root.transform, false);
            rope.Configure(post, top, bridge, M.rope);
            var so = new SerializedObject(rope);
            so.FindProperty("start").objectReferenceValue = post;
            so.FindProperty("end").objectReferenceValue = top;
            so.FindProperty("bridge").objectReferenceValue = bridge;
            so.FindProperty("material").objectReferenceValue = M.rope;
            so.ApplyModifiedPropertiesWithoutUndo();

            var zone = new GameObject("CameraZone").AddComponent<CameraZone>();
            zone.transform.SetParent(root.transform, false);
            zone.minX = -16f;
            zone.maxX = 4f;
            zone.offset = new Vector3(0f, 4f, -16.5f);
            return root;
        }

        // ================================================================== decoration (layered)

        public static GameObject Arch(Vector3 pos, LevelLayer layer, float span = 4f, float spring = 4.5f, float breakAmount = 0f, int seed = 0)
        {
            float scale = LayerScale(layer);
            var a = Prop<ArchProp>(null, breakAmount > 0f ? "BrokenArch" : "Arch", Layered(pos, layer), seed, new[] { M.ForLayer(layer) });
            a.span = span * scale; a.pillarWidth = 0.8f * scale; a.springHeight = spring * scale; a.crown = 0.9f * scale; a.depth = 1.2f * scale;
            a.breakAmount = breakAmount;
            a.Rebuild();
            Tag(a.gameObject, layer);
            return a.gameObject;
        }

        public static GameObject Column(Vector3 pos, LevelLayer layer, float height = 6f, bool broken = false, int seed = 0)
        {
            float scale = LayerScale(layer);
            var c = Prop<ColumnProp>(null, broken ? "BrokenColumn" : "Column", Layered(pos, layer), seed, new[] { M.ForLayer(layer) });
            c.height = height * scale; c.radius = 0.38f * scale; c.broken = broken;
            c.Rebuild();
            Tag(c.gameObject, layer);
            return c.gameObject;
        }

        public static GameObject Rubble(Vector3 pos, LevelLayer layer, int seed = 0, float width = 2.4f, float size = 0.6f)
        {
            float scale = LayerScale(layer);
            var r = Prop<RubbleProp>(null, "Rubble", Layered(pos, layer), seed, new[] { M.ForLayer(layer) });
            r.width = width * scale; r.maxSize = size * scale; r.blocks = 7 + seed % 5;
            r.Rebuild();
            Tag(r.gameObject, layer);
            return r.gameObject;
        }

        public static GameObject Banner(Vector3 top, LevelLayer layer, float width = 0.9f, float height = 2.8f, int seed = 1)
        {
            var b = Prop<BannerProp>(null, "Banner", Layered(top, layer), seed, new[] { M.banner });
            b.width = width; b.height = height;
            b.Rebuild();
            Tag(b.gameObject, layer);
            return b.gameObject;
        }

        public static GameObject Gallows(Vector3 foot, LevelLayer layer = LevelLayer.Playfield)
        {
            var root = new GameObject("Gallows");
            root.transform.position = Layered(foot, layer) + new Vector3(0f, 0f, layer == LevelLayer.Playfield ? 1.2f : 0f);
            var mat = M.ForLayer(layer);
            Block(root.transform, "Post", Vector3.zero, new Vector3(0.35f, 5.8f, 0.35f), mat, false).transform.localPosition = new Vector3(0f, 2.9f, 0f);
            Block(root.transform, "Beam", Vector3.zero, new Vector3(3.2f, 0.3f, 0.3f), mat, false).transform.localPosition = new Vector3(1.3f, 5.4f, 0f);
            Block(root.transform, "Brace", Vector3.zero, new Vector3(0.18f, 1.4f, 0.18f), mat, false).transform.localPosition = new Vector3(0.45f, 4.75f, 0f);
            root.transform.Find("Brace").localRotation = Quaternion.Euler(0f, 0f, -45f);
            Block(root.transform, "Chain", Vector3.zero, new Vector3(0.04f, 2f, 0.04f), mat, false).transform.localPosition = new Vector3(2.4f, 4.4f, 0f);
            var b1 = Banner(Vector3.zero, layer, 0.9f, 2.8f, 3); b1.transform.SetParent(root.transform, false); b1.transform.localPosition = new Vector3(0.5f, 5.25f, -0.2f);
            var b2 = Banner(Vector3.zero, layer, 0.7f, 2.0f, 4); b2.transform.SetParent(root.transform, false); b2.transform.localPosition = new Vector3(1.6f, 5.25f, -0.2f);
            Tag(root, layer);
            return root;
        }

        public static GameObject CrimsonTree(Vector3 foot, LevelLayer layer = LevelLayer.Playfield, int seed = 21, float height = 11f)
        {
            var t = Prop<TreeProp>(null, "CrimsonTree", Layered(foot, layer) + new Vector3(0f, 0f, layer == LevelLayer.Playfield ? 1.8f : 0f), seed, new[] { M.ForLayer(layer), M.leaf });
            t.height = height; t.trunkRadius = 0.55f * height / 11f; t.depth = 6; t.leavesPerTip = 16; t.leafSize = 0.3f;
            t.Rebuild();
            // A carpet of fallen leaves around the roots.
            for (int i = 0; i < 6; i++)
            {
                var g = Grass(foot + new Vector3(-4f + i * 1.6f, 0f, 0f), layer, 300 + seed + i, 2f, new Vector2(0.04f, 0.16f), 30, M.leaf, 0.12f);
                g.transform.SetParent(t.transform, true);
                g.transform.position = new Vector3(g.transform.position.x, foot.y, t.transform.position.z - 1.2f + (i % 2));
            }
            Tag(t.gameObject, layer);
            return t.gameObject;
        }

        public static GameObject Grass(Vector3 pos, LevelLayer layer, int seed = 1, float width = 3f, Vector2? height = null, int blades = 30, Material mat = null, float bladeWidth = 0.06f)
        {
            var g = Prop<GrassProp>(null, "Grass", Layered(pos, layer), seed, new[] { mat != null ? mat : M.grass });
            g.width = width; g.heightRange = height ?? new Vector2(0.4f, 1.4f); g.blades = blades; g.bladeWidth = bladeWidth;
            g.Rebuild();
            Tag(g.gameObject, layer);
            return g.gameObject;
        }

        public const string RuinDir = LevelKitMaterials.Root + "/Art/Ruins/";

        /// <summary>Modelled ruin piece from Blender (RuinWall, Tower, RuinStairs), tinted for its layer.</summary>
        public static GameObject Ruin(string model, Vector3 pos, LevelLayer layer)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(RuinDir + model + ".fbx");
            GameObject go;
            if (asset != null)
            {
                go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
                go.name = model;
            }
            else go = new GameObject(model);
            go.transform.position = Layered(pos, layer);
            go.transform.localScale = Vector3.one * LayerScale(layer);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                r.sharedMaterial = M.ForLayer(layer);
                r.shadowCastingMode = ShadowCastingMode.Off;
            }
            Tag(go, layer);
            return go;
        }

        public static GameObject Fern(Vector3 pos, LevelLayer layer, int seed = 1, float size = 1.6f, int fronds = 9)
        {
            var f = Prop<FernProp>(null, "Fern", Layered(pos, layer), seed, new[] { M.grass });
            f.size = size; f.fronds = fronds;
            f.Rebuild();
            Tag(f.gameObject, layer);
            return f.gameObject;
        }

        public static GameObject Waterfall(Vector3 top, LevelLayer layer = LevelLayer.Mid, float width = 3.2f, float height = 22f)
        {
            var root = new GameObject("Waterfall");
            root.transform.position = Layered(top, layer);
            var fall = Card(root.transform, "Water", new Vector3(0f, -height * 0.5f, -0.4f), new Vector2(width, height), M.waterfall);
            Card(root.transform, "Spray", new Vector3(0f, -height, -0.8f), new Vector2(width * 4f, Mathf.Min(6f, height * 0.3f)), M.mistBand);
            Tag(root, layer);
            return root;
        }

        public static GameObject MistBand(Vector3 center, LevelLayer layer, float width = 120f, float height = 6f)
        {
            var go = Card(null, "MistBand", Layered(center, layer) + new Vector3(0f, 0f, -2f), new Vector2(width, height), layer == LevelLayer.Playfield ? M.mistLow : M.mistBand);
            Tag(go, layer);
            return go;
        }

        public static GameObject LightShaft(Vector3 center, LevelLayer layer = LevelLayer.Mid, float width = 6f, float height = 34f)
        {
            var go = Card(null, "LightShaft", Layered(center, layer), new Vector2(width, height), M.shaft);
            go.transform.rotation = Quaternion.Euler(0f, 0f, 22f);
            Tag(go, layer);
            return go;
        }

        public static GameObject FallingLeaves(Vector3 center, float width = 60f)
        {
            var leaves = new GameObject("FallingLeaves");
            leaves.transform.position = center;
            leaves.AddComponent<ParticleSystem>();
            leaves.GetComponent<ParticleSystemRenderer>().sharedMaterial = M.leafParticle;
            leaves.AddComponent<LeafFall>();
            var so = new SerializedObject(leaves.GetComponent<LeafFall>());
            so.FindProperty("area").vector3Value = new Vector3(width, 1f, 6f);
            so.FindProperty("rate").floatValue = width * 0.15f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return leaves;
        }

        // ================================================================== helpers

        /// <summary>Further layers are drawn bigger so they read at distance.</summary>
        public static float LayerScale(LevelLayer layer)
        {
            switch (layer)
            {
                case LevelLayer.Mid: return 1.6f;
                case LevelLayer.Far: return 2.4f;
                case LevelLayer.Sky: return 3.5f;
                default: return 1f;
            }
        }

        public static Vector3 Layered(Vector3 pos, LevelLayer layer) => new Vector3(pos.x, pos.y, DepthLayer.Z(layer) + (layer == LevelLayer.Playfield ? pos.z : 0f));

        /// <summary>Moves an object to a layer: z position and ink value of all its silhouette renderers.</summary>
        public static void ApplyLayer(GameObject go, LevelLayer layer)
        {
            var tag = go.GetComponent<DepthLayer>();
            if (tag == null) tag = go.AddComponent<DepthLayer>();
            var from = tag.layer;
            tag.layer = layer;
            var p = go.transform.position;
            go.transform.position = new Vector3(p.x, p.y, DepthLayer.Z(layer));
            go.transform.localScale *= LayerScale(layer) / LayerScale(from);
            var fromMat = M.ForLayer(from);
            foreach (var r in go.GetComponentsInChildren<Renderer>())
            {
                var mats = r.sharedMaterials;
                for (int i = 0; i < mats.Length; i++)
                    if (mats[i] == fromMat || mats[i] == M.inkFG || mats[i] == M.inkPlay || mats[i] == M.inkNear || mats[i] == M.inkMid || mats[i] == M.inkFar || mats[i] == M.inkSky)
                        mats[i] = M.ForLayer(layer);
                r.sharedMaterials = mats;
            }
            foreach (var pp in go.GetComponentsInChildren<ProceduralProp>()) pp.Rebuild();
        }

        static void Tag(GameObject go, LevelLayer layer)
        {
            var t = go.GetComponent<DepthLayer>();
            if (t == null) t = go.AddComponent<DepthLayer>();
            t.layer = layer;
        }

        public static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 size, Material mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            go.transform.localScale = size;
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        public static T Prop<T>(Transform parent, string name, Vector3 pos, int seed, Material[] mats) where T : ProceduralProp
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = pos;
            var r = go.AddComponent<MeshRenderer>();
            r.sharedMaterials = mats;
            r.shadowCastingMode = ShadowCastingMode.Off;
            var p = go.AddComponent<T>();
            p.Configure(seed);
            return p;
        }

        public static GameObject Card(Transform parent, string name, Vector3 center, Vector2 size, Material mat)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.name = name;
            Object.DestroyImmediate(go.GetComponent<Collider>());
            if (parent != null) go.transform.SetParent(parent, false);
            if (parent != null) go.transform.localPosition = center; else go.transform.position = center;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var r = go.GetComponent<Renderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = ShadowCastingMode.Off;
            return go;
        }

        public static void SetRef(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            so.FindProperty(field).objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
