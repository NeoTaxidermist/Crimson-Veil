using System.Collections.Generic;
using System.IO;
using CrimsonVeil.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

namespace CrimsonVeil.EditorTools
{
    /// <summary>
    /// Motion capture onto the hero. The Mixamo sources (ArtSource/mocap, copied to Art/Mocap - both outside git:
    /// their licence lets the motion be used in the game, not the files be passed on) are imported as humanoid
    /// animation; a humanoid avatar is built for the hero in code (his own model's import is left as it is: Generic,
    /// arms hanging - the avatar straightens him into the T-pose the retargeting measures from); each clip is played on
    /// him and his own bones are recorded, frame by frame, into a clip of his rig (Animations/Hero, in git: the motion
    /// carried over onto our character, not the source). <see cref="HeroMocapLibrary"/> lists them for the game.
    /// </summary>
    public static class HeroMocapBaker
    {
        public const string SourceDir = "Assets/_CrimsonVeil/Art/Mocap";
        public const string OutDir = "Assets/_CrimsonVeil/Animations/Hero";
        public const string LibraryPath = OutDir + "/HeroMocapLibrary.asset";
        const string HeroPath = "Assets/_CrimsonVeil/Art/Hero/Hero.fbx";
        public const float Fps = 30f;

        /// <summary>Our bones for Unity's humanoid ones (the face, hair, cloth and sash are left to their own systems).</summary>
        public static readonly Dictionary<string, string> HumanMap = new Dictionary<string, string>
        {
            { "Hips", "Hips" }, { "Spine", "Spine" }, { "Chest", "Chest" }, { "UpperChest", "UpperChest" }, { "Neck", "Neck" }, { "Head", "Head" },
            { "LeftShoulder", "Shoulder.L" }, { "LeftUpperArm", "UpperArm.L" }, { "LeftLowerArm", "LowerArm.L" }, { "LeftHand", "Hand.L" },
            { "RightShoulder", "Shoulder.R" }, { "RightUpperArm", "UpperArm.R" }, { "RightLowerArm", "LowerArm.R" }, { "RightHand", "Hand.R" },
            { "LeftUpperLeg", "UpperLeg.L" }, { "LeftLowerLeg", "LowerLeg.L" }, { "LeftFoot", "Foot.L" }, { "LeftToes", "Toe.L" },
            { "RightUpperLeg", "UpperLeg.R" }, { "RightLowerLeg", "LowerLeg.R" }, { "RightFoot", "Foot.R" }, { "RightToes", "Toe.R" },
            { "Left Thumb Proximal", "Thumb.L.0" }, { "Left Thumb Intermediate", "Thumb.L.1" }, { "Left Thumb Distal", "Thumb.L.2" },
            { "Left Index Proximal", "Finger.L.0.0" }, { "Left Index Intermediate", "Finger.L.0.1" }, { "Left Index Distal", "Finger.L.0.2" },
            { "Left Middle Proximal", "Finger.L.1.0" }, { "Left Middle Intermediate", "Finger.L.1.1" }, { "Left Middle Distal", "Finger.L.1.2" },
            { "Left Ring Proximal", "Finger.L.2.0" }, { "Left Ring Intermediate", "Finger.L.2.1" }, { "Left Ring Distal", "Finger.L.2.2" },
            { "Left Little Proximal", "Finger.L.3.0" }, { "Left Little Intermediate", "Finger.L.3.1" }, { "Left Little Distal", "Finger.L.3.2" },
            { "Right Thumb Proximal", "Thumb.R.0" }, { "Right Thumb Intermediate", "Thumb.R.1" }, { "Right Thumb Distal", "Thumb.R.2" },
            { "Right Index Proximal", "Finger.R.0.0" }, { "Right Index Intermediate", "Finger.R.0.1" }, { "Right Index Distal", "Finger.R.0.2" },
            { "Right Middle Proximal", "Finger.R.1.0" }, { "Right Middle Intermediate", "Finger.R.1.1" }, { "Right Middle Distal", "Finger.R.1.2" },
            { "Right Ring Proximal", "Finger.R.2.0" }, { "Right Ring Intermediate", "Finger.R.2.1" }, { "Right Ring Distal", "Finger.R.2.2" },
            { "Right Little Proximal", "Finger.R.3.0" }, { "Right Little Intermediate", "Finger.R.3.1" }, { "Right Little Distal", "Finger.R.3.2" },
        };

        /// <summary>Every Mixamo source under <see cref="SourceDir"/> imported as humanoid animation (its own skeleton as avatar).</summary>
        [MenuItem("Crimson Veil/Mocap/Import sources as humanoid")]
        public static int ConfigureSources()
        {
            if (!AssetDatabase.IsValidFolder(SourceDir)) return 0;
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Model", new[] { SourceDir }))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var imp = AssetImporter.GetAtPath(path) as ModelImporter;
                if (imp == null) continue;
                bool dirty = imp.animationType != ModelImporterAnimationType.Human || imp.avatarSetup != ModelImporterAvatarSetup.CreateFromThisModel
                             || imp.materialImportMode != ModelImporterMaterialImportMode.None;
                imp.animationType = ModelImporterAnimationType.Human;
                imp.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                imp.materialImportMode = ModelImporterMaterialImportMode.None;
                imp.importAnimation = true;
                if (dirty) { imp.SaveAndReimport(); n++; }
            }
            return n;
        }

        /// <summary>
        /// A humanoid avatar for the hero (in code): his bones for the humanoid ones, the skeleton straightened into the
        /// T-pose - arms out level, forearms and hands in line, legs straight down (he is modelled with the arms hanging).
        /// </summary>
        public static Avatar BuildHeroAvatar(GameObject hero)
        {
            TPose(hero.transform);
            var bones = new List<HumanBone>();
            foreach (var kv in HumanMap)
            {
                if (CrimsonVeil.Core.RigJoint.FindDeep(hero.transform, kv.Value) == null) continue;
                bones.Add(new HumanBone { humanName = kv.Key, boneName = kv.Value, limit = new HumanLimit { useDefaultValues = true } });
            }
            var skeleton = new List<SkeletonBone>();
            foreach (var t in hero.GetComponentsInChildren<Transform>(true))
                skeleton.Add(new SkeletonBone { name = t.name, position = t.localPosition, rotation = t.localRotation, scale = t.localScale });
            var hd = new HumanDescription
            {
                human = bones.ToArray(), skeleton = skeleton.ToArray(),
                upperArmTwist = 0.5f, lowerArmTwist = 0.5f, upperLegTwist = 0.5f, lowerLegTwist = 0.5f,
                armStretch = 0.05f, legStretch = 0.05f, feetSpacing = 0f, hasTranslationDoF = false,
            };
            var avatar = AvatarBuilder.BuildHumanAvatar(hero, hd);
            avatar.name = "HeroAvatar";
            return avatar;
        }

        /// <summary>Turns the limbs of a hero at rest (facing +z, his left at -x) into the T-pose.</summary>
        public static void TPose(Transform root)
        {
            Transform F(string n) => CrimsonVeil.Core.RigJoint.FindDeep(root, n);
            void Aim(string bone, string child, Vector3 dir)
            {
                var b = F(bone); var c = F(child);
                if (b == null || c == null) return;
                var cur = c.position - b.position;
                if (cur.sqrMagnitude < 1e-8f) return;
                b.rotation = Quaternion.FromToRotation(cur.normalized, dir.normalized) * b.rotation;
            }
            foreach (var (s, x) in new[] { ("L", -1f), ("R", 1f) })
            {
                Aim("UpperArm." + s, "LowerArm." + s, new Vector3(x, 0f, 0f));
                Aim("LowerArm." + s, "Hand." + s, new Vector3(x, 0f, 0f));
                Aim("Hand." + s, "Finger." + s + ".1.0", new Vector3(x, 0f, 0f));
                Aim("UpperLeg." + s, "LowerLeg." + s, Vector3.down);
                Aim("LowerLeg." + s, "Foot." + s, Vector3.down);
            }
        }

        /// <summary>The hero's own bones a mocap clip drives (relative paths from the model's root).</summary>
        public static List<Transform> DrivenBones(Transform root)
        {
            var list = new List<Transform>();
            foreach (var name in HumanMap.Values)
            {
                var t = CrimsonVeil.Core.RigJoint.FindDeep(root, name);
                if (t != null) list.Add(t);
            }
            return list;
        }

        static string PathOf(Transform root, Transform t)
        {
            var parts = new List<string>();
            for (var x = t; x != null && x != root; x = x.parent) parts.Insert(0, x.name);
            return string.Join("/", parts);
        }

        /// <summary>
        /// Plays a humanoid clip on the hero and records his bones into a clip of his own rig (rotations of every driven
        /// bone, the hips' position; in place: the clip's root motion is not applied).
        /// </summary>
        public static AnimationClip Bake(AnimationClip source, string name, bool loop) => Bake(source, name, loop, out _);

        public static AnimationClip Bake(AnimationClip source, string name, bool loop, out float impact)
            => Bake(source, name, loop, HipsMode.Absolute, 0f, 1f, out impact, out _);

        /// <summary>Where a baked clip puts the hips (the humanoid sources lose their height over the ground: their root
        /// motion is the game's to give).</summary>
        public enum HipsMode
        {
            /// <summary>As the clip has them over his feet (standing, walking: the feet on the floor).</summary>
            Absolute,
            /// <summary>Only how they move from the clip's first frame (hanging, swinging, in the air: the game places him).</summary>
            Relative,
            /// <summary>Lowered or raised each frame so his lowest point is on the floor (landing, rolling, falling dead).</summary>
            Grounded,
        }

        /// <summary>What a bake measured of the clip (for the library and the log).</summary>
        public struct Measure
        {
            /// <summary>Metres covered per loop by its steps (twice the reach of a foot fore and aft under the hips).</summary>
            public float stride;
            /// <summary>0..1 through the clip where the left foot is furthest ahead of the right (a gait's sync point).</summary>
            public float sync;
            /// <summary>0..1 through the clip where the right hand moves fastest (a blow lands).</summary>
            public float impact;
            public float lowestHips;
        }

        /// <param name="from01">Where in the source the clip starts (0..1) - e.g. a landing from the moment of contact.</param>
        /// <param name="to01">Where it ends.</param>
        public static AnimationClip Bake(AnimationClip source, string name, bool loop, HipsMode hipsMode, float from01, float to01,
                                         out float impact, out Measure measure)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath);
            var hero = (GameObject)Object.Instantiate(asset);
            hero.name = "HeroBake";
            try
            {
                // his rest: where the hips sit and how low his lowest point is
                var hips = CrimsonVeil.Core.RigJoint.FindDeep(hero.transform, "Hips");
                var restHipsLocal = hips.localPosition;
                var lowPoints = new List<Transform>();
                foreach (var n in new[] { "Foot.L", "Foot.R", "Toe.L", "Toe.R", "Hand.L", "Hand.R", "Head", "Hips", "LowerLeg.L", "LowerLeg.R", "LowerArm.L", "LowerArm.R", "Chest" })
                {
                    var t = CrimsonVeil.Core.RigJoint.FindDeep(hero.transform, n);
                    if (t != null) lowPoints.Add(t);
                }
                float Lowest() { float y = float.MaxValue; foreach (var t in lowPoints) y = Mathf.Min(y, t.position.y); return y; }
                float restLowest = Lowest();

                // the avatar is built on a T-posed copy; the recording is made on the hero at his own rest
                var tposed = (GameObject)Object.Instantiate(asset);
                Avatar avatar;
                try { avatar = BuildHeroAvatar(tposed); }
                finally { Object.DestroyImmediate(tposed); }
                if (!avatar.isValid || !avatar.isHuman) throw new System.Exception("hero avatar invalid");

                var animator = hero.GetComponent<Animator>();
                if (animator == null) animator = hero.AddComponent<Animator>();   // (no ?? on Unity objects)
                animator.avatar = avatar;
                animator.applyRootMotion = false;
                var graph = PlayableGraph.Create("HeroMocapBake");
                graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var output = AnimationPlayableOutput.Create(graph, "out", animator);
                var play = AnimationClipPlayable.Create(graph, source);
                play.SetApplyFootIK(false);
                output.SetSourcePlayable(play);

                var bones = DrivenBones(hero.transform);
                float t0 = Mathf.Clamp01(from01) * source.length, t1 = Mathf.Clamp(to01, from01, 1f) * source.length;
                float span = Mathf.Max(1f / Fps, t1 - t0);
                int frames = Mathf.Max(2, Mathf.RoundToInt(span * Fps) + 1);
                var footL = CrimsonVeil.Core.RigJoint.FindDeep(hero.transform, "Foot.L");
                var footR = CrimsonVeil.Core.RigJoint.FindDeep(hero.transform, "Foot.R");
                float bestLead = float.MinValue, syncAt = 0f, reachMin = float.MaxValue, reachMax = float.MinValue;
                var rot = new AnimationCurve[bones.Count, 4];
                for (int b = 0; b < bones.Count; b++) for (int k = 0; k < 4; k++) rot[b, k] = new AnimationCurve();
                var hp = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
                var hand = CrimsonVeil.Core.RigJoint.FindDeep(hero.transform, "Hand.R");
                Vector3 lastHand = Vector3.zero, firstHips = Vector3.zero;
                float fastest = 0f, fastestAt = 0f, lowestHips = float.MaxValue;
                for (int f = 0; f < frames; f++)
                {
                    float t = Mathf.Min(span, f / Fps);
                    play.SetTime(t0 + t);
                    graph.Evaluate(0f);
                    if (footL != null && footR != null)
                    {
                        float lead = footL.position.z - footR.position.z;   // (the hero faces +z)
                        if (lead > bestLead) { bestLead = lead; syncAt = t; }
                        float reach = footL.position.z - hips.position.z;
                        reachMin = Mathf.Min(reachMin, reach); reachMax = Mathf.Max(reachMax, reach);
                    }
                    for (int b = 0; b < bones.Count; b++)
                    {
                        var q = bones[b].localRotation;
                        rot[b, 0].AddKey(t, q.x); rot[b, 1].AddKey(t, q.y); rot[b, 2].AddKey(t, q.z); rot[b, 3].AddKey(t, q.w);
                    }
                    // where the hips go (see HipsMode); never travelling over the ground: the game moves him
                    var local = hips.localPosition;
                    if (f == 0) firstHips = local;
                    Vector3 p;
                    if (hipsMode == HipsMode.Relative) p = restHipsLocal + (local - firstHips);
                    else
                    {
                        p = local;
                        if (hipsMode == HipsMode.Grounded && hips.parent != null)
                        {
                            float lift = restLowest - Lowest();     // world metres to raise him by
                            p = hips.parent.InverseTransformPoint(hips.position + Vector3.up * lift);
                        }
                    }
                    lowestHips = Mathf.Min(lowestHips, hips.position.y);
                    hp[0].AddKey(t, p.x); hp[1].AddKey(t, p.y); hp[2].AddKey(t, p.z);
                    if (hand != null)
                    {
                        if (f > 0)
                        {
                            float speed = (hand.position - lastHand).magnitude;
                            if (speed > fastest) { fastest = speed; fastestAt = t; }
                        }
                        lastHand = hand.position;
                    }
                }
                graph.Destroy();
                impact = span > 0f ? fastestAt / span : 0f;
                measure = new Measure
                {
                    stride = reachMax > reachMin ? 2f * (reachMax - reachMin) : 0f, sync = span > 0f ? syncAt / span : 0f,
                    impact = impact, lowestHips = lowestHips,
                };

                var clip = new AnimationClip { name = name, frameRate = Fps, legacy = false };
                string[] comp = { "localRotation.x", "localRotation.y", "localRotation.z", "localRotation.w" };
                for (int b = 0; b < bones.Count; b++)
                {
                    string path = PathOf(hero.transform, bones[b]);
                    for (int k = 0; k < 4; k++) clip.SetCurve(path, typeof(Transform), comp[k], rot[b, k]);
                }
                string hipsPath = PathOf(hero.transform, hips);
                clip.SetCurve(hipsPath, typeof(Transform), "localPosition.x", hp[0]);
                clip.SetCurve(hipsPath, typeof(Transform), "localPosition.y", hp[1]);
                clip.SetCurve(hipsPath, typeof(Transform), "localPosition.z", hp[2]);
                clip.EnsureQuaternionContinuity();
                var settings = AnimationUtility.GetAnimationClipSettings(clip);
                settings.loopTime = loop;
                AnimationUtility.SetAnimationClipSettings(clip, settings);
                return clip;
            }
            finally { Object.DestroyImmediate(hero); }
        }

        /// <summary>Imports a source's clip mirrored (left for right) or not.</summary>
        public static void SetMirror(string fbxPath, bool mirror)
        {
            var imp = AssetImporter.GetAtPath(fbxPath) as ModelImporter;
            if (imp == null) return;
            var clips = imp.clipAnimations;
            if (clips == null || clips.Length == 0) clips = imp.defaultClipAnimations;
            bool dirty = false;
            foreach (var c in clips) if (c.mirror != mirror) { c.mirror = mirror; dirty = true; }
            if (!dirty) return;
            imp.clipAnimations = clips;
            imp.SaveAndReimport();
        }

        /// <summary>The first animation clip in a source FBX.</summary>
        public static AnimationClip SourceClip(string fbxPath)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath(fbxPath))
                if (o is AnimationClip c && !c.name.StartsWith("__preview__")) return c;
            return null;
        }

        /// <summary>Bakes every entry of the library's recipe (<see cref="HeroMocapRecipe"/>) and writes the library.</summary>
        [MenuItem("Crimson Veil/Mocap/Bake hero clips")]
        public static string BakeAll()
        {
            if (!AssetDatabase.IsValidFolder(SourceDir)) return "no mocap sources (ArtSource/mocap is outside git: copy it to " + SourceDir + ")";
            ConfigureSources();
            Directory.CreateDirectory(OutDir);
            var lib = AssetDatabase.LoadAssetAtPath<HeroMocapLibrary>(LibraryPath);
            if (lib == null) { lib = ScriptableObject.CreateInstance<HeroMocapLibrary>(); AssetDatabase.CreateAsset(lib, LibraryPath); }
            var entries = new List<HeroMocapLibrary.Entry>();
            var log = new System.Text.StringBuilder();
            foreach (var r in HeroMocapRecipe.All)
            {
                SetMirror(SourceDir + "/" + r.source, r.mirror);
                var src = SourceClip(SourceDir + "/" + r.source);
                if (src == null) { log.Append("missing " + r.source + "\n"); continue; }
                var clip = Bake(src, r.key, r.loop, r.hips, r.from, r.to, out float impact, out var m);
                string outPath = OutDir + "/" + r.key + ".anim";
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath);
                if (existing != null) { EditorUtility.CopySerialized(clip, existing); clip = existing; }
                else AssetDatabase.CreateAsset(clip, outPath);
                entries.Add(new HeroMocapLibrary.Entry
                {
                    key = r.key, clip = clip, loop = r.loop, impact = r.blow ? impact : 0f,
                    stride = r.gait ? m.stride : 0f, sync = r.gait ? m.sync : 0f,
                });
                log.Append(r.key + " " + clip.length.ToString("F2") + "s" + (r.blow ? " impact " + impact.ToString("F2") : "")
                           + (r.gait ? " stride " + m.stride.ToString("F2") + " sync " + m.sync.ToString("F2") : "") + "\n");
            }
            lib.entries = entries.ToArray();
            EditorUtility.SetDirty(lib);
            AssetDatabase.SaveAssets();
            return log.ToString();
        }
    }

    /// <summary>Which source clip becomes which of the hero's clips (by key: see <see cref="HeroMocap"/>).</summary>
    public static class HeroMocapRecipe
    {
        public struct Item
        {
            public string key, source;
            public bool loop, gait, blow;
            /// <summary>Left and right swapped: the source holds in its left hand what the hero holds in his right.</summary>
            public bool mirror;
            public HeroMocapBaker.HipsMode hips;
            public float from, to;
        }

        static Item I(string key, string source, bool loop = false, HeroMocapBaker.HipsMode hips = HeroMocapBaker.HipsMode.Absolute,
                      float from = 0f, float to = 1f, bool gait = false, bool blow = false, bool mirror = false)
            => new Item { key = key, source = source, loop = loop, hips = hips, from = from, to = to, gait = gait, blow = blow, mirror = mirror };

        const HeroMocapBaker.HipsMode Rel = HeroMocapBaker.HipsMode.Relative, Gnd = HeroMocapBaker.HipsMode.Grounded;
        const string SS = "Sword and Shield Pack/", LB = "Pro Longbow Pack/";

        public static readonly Item[] All =
        {
            // standing, acting
            I(HeroMocap.Idle, "Breathing Idle.fbx", true),
            I(HeroMocap.LookAround, "Looking Around.fbx", true),
            I(HeroMocap.InjuredIdle, "Injured Idle.fbx", true),
            I(HeroMocap.Talk, "Talking Conversation.fbx", true),
            I(HeroMocap.Sad, "Sad Idle.fbx", true),
            I(HeroMocap.Angry, "Yelling.fbx"),
            I(HeroMocap.Nod, "Head Nod Yes.fbx"),
            I(HeroMocap.Point, "Pointing Forward.fbx"),
            I(HeroMocap.GetUp, "Getting Up From Back.fbx", hips: Gnd),
            // on the ground
            I(HeroMocap.Walk, "Unarmed Walk Forward.fbx", true, gait: true),
            I(HeroMocap.Run, "Run.fbx", true, gait: true),
            I(HeroMocap.Sprint, "Sprint.fbx", true, gait: true),
            I(HeroMocap.CrouchIdle, SS + "sword and shield crouch idle.fbx", true),
            I(HeroMocap.Slide, "Running Slide.fbx", hips: Gnd, from: 0.1f, to: 0.85f),
            I(HeroMocap.Dash, "Standing Dodge Forward.fbx", from: 0.25f, to: 0.75f),
            I(HeroMocap.Roll, "Sprinting Forward Roll.fbx", hips: Gnd),
            I(HeroMocap.HardLanding, "Falling To Landing.fbx", hips: Gnd, from: 0.2f),
            I(HeroMocap.LandRoll, "Falling To Roll.fbx", hips: Gnd, from: 0.06f),
            // in the air, on the lip, on the whip
            I(HeroMocap.JumpUp, "Jump In Place.fbx", hips: Rel, from: 0.36f, to: 0.45f),
            I(HeroMocap.Fall, "Falling Idle.fbx", true, Rel),
            I(HeroMocap.Flip, "Running Forward Flip.fbx", hips: Rel, from: 0.3f, to: 0.82f),
            I(HeroMocap.WallJump, "Jump From Wall.fbx", hips: Rel, from: 0.1f),
            I(HeroMocap.HangIdle, "Hanging Idle.fbx", true),
            I(HeroMocap.HangCatch, "Jumping To Hanging.fbx", hips: Rel, from: 0.18f, to: 0.6f),
            I(HeroMocap.Mantle, "Climbing.fbx", hips: Rel),
            I(HeroMocap.Swing, "Swinging.fbx", true, Rel),
            // fighting
            I(HeroMocap.SlashA, "Stable Sword Inward Slash.fbx", blow: true),
            I(HeroMocap.SlashB, "Stable Sword Outward Slash.fbx", blow: true),
            I(HeroMocap.SlashC, SS + "sword and shield slash (3).fbx", blow: true),
            I(HeroMocap.Stab, "Bayonet Stab.fbx", blow: true),
            I(HeroMocap.ShieldBash, SS + "sword and shield attack (2).fbx", blow: true),
            // the whip: one-handed torch blows (over the shoulder, overhead, low), trimmed to the blow
            I(HeroMocap.WhipA, "Standing Torch Melee Attack 01.fbx", from: 0.1f, to: 0.75f, blow: true),
            I(HeroMocap.WhipB, "Standing Torch Melee Attack 04.fbx", from: 0.1f, to: 0.7f, blow: true),
            I(HeroMocap.WhipC, "Standing Torch Melee Attack 02.fbx", from: 0.1f, to: 0.8f, blow: true),
            // the spear's combo after the bayonet thrust
            I(HeroMocap.SpearB, "Stabbing 1.fbx", from: 0.1f, to: 0.75f, blow: true),
            I(HeroMocap.SpearC, "Stabbing 3.fbx", from: 0.1f, to: 0.75f, blow: true),
            // in the air: a cut in the leap, and the overhead leap's slam for a strike under the feet
            I(HeroMocap.AirSlash, SS + "sword and shield attack (3).fbx", hips: Rel, from: 0.15f, to: 0.8f, blow: true),
            I(HeroMocap.DownStrike, SS + "sword and shield attack.fbx", hips: Rel, from: 0.3f, to: 0.75f, blow: true),
            I(HeroMocap.BlockIdle, SS + "sword and shield block idle.fbx", true),
            I(HeroMocap.Block, SS + "sword and shield block.fbx"),
            // (the archer holds the bow in his left hand, the hero in his right - the weapon hand: mirrored)
            I(HeroMocap.BowDraw, LB + "standing draw arrow.fbx", mirror: true),
            I(HeroMocap.BowAim, LB + "standing aim overdraw.fbx", true, mirror: true),
            I(HeroMocap.BowShot, LB + "standing aim recoil.fbx", mirror: true),
            I(HeroMocap.Hurt, SS + "sword and shield impact.fbx"),
            I(HeroMocap.Stagger, SS + "sword and shield impact (2).fbx", hips: Gnd),
            I(HeroMocap.Turn, "Running Turn 180.fbx"),
            I(HeroMocap.RunStop, LB + "standing run forward stop.fbx"),
            I(HeroMocap.Death, SS + "sword and shield death.fbx", hips: Gnd),
        };
    }
}
