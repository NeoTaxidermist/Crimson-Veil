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

        /// <param name="impact">0..1 through the clip where the right hand moves fastest (a blow lands).</param>
        public static AnimationClip Bake(AnimationClip source, string name, bool loop, out float impact)
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(HeroPath);
            var hero = (GameObject)Object.Instantiate(asset);
            hero.name = "HeroBake";
            try
            {
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
                int frames = Mathf.Max(2, Mathf.RoundToInt(source.length * Fps) + 1);
                var rot = new AnimationCurve[bones.Count, 4];
                for (int b = 0; b < bones.Count; b++) for (int k = 0; k < 4; k++) rot[b, k] = new AnimationCurve();
                var hips = CrimsonVeil.Core.RigJoint.FindDeep(hero.transform, "Hips");
                var hp = new[] { new AnimationCurve(), new AnimationCurve(), new AnimationCurve() };
                var hand = CrimsonVeil.Core.RigJoint.FindDeep(hero.transform, "Hand.R");
                Vector3 lastHand = Vector3.zero;
                float fastest = 0f, fastestAt = 0f;
                for (int f = 0; f < frames; f++)
                {
                    float t = Mathf.Min(source.length, f / Fps);
                    play.SetTime(t);
                    graph.Evaluate(0f);
                    for (int b = 0; b < bones.Count; b++)
                    {
                        var q = bones[b].localRotation;
                        rot[b, 0].AddKey(t, q.x); rot[b, 1].AddKey(t, q.y); rot[b, 2].AddKey(t, q.z); rot[b, 3].AddKey(t, q.w);
                    }
                    var p = hips.localPosition;
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
                impact = source.length > 0f ? fastestAt / source.length : 0f;

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
                var src = SourceClip(SourceDir + "/" + r.source);
                if (src == null) { log.Append("missing " + r.source + "\n"); continue; }
                var clip = Bake(src, r.key, r.loop, out float impact);
                bool blow = r.key == HeroMocap.SlashA || r.key == HeroMocap.SlashB || r.key == HeroMocap.Stab;
                string outPath = OutDir + "/" + r.key + ".anim";
                var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(outPath);
                if (existing != null) { EditorUtility.CopySerialized(clip, existing); clip = existing; }
                else AssetDatabase.CreateAsset(clip, outPath);
                entries.Add(new HeroMocapLibrary.Entry { key = r.key, clip = clip, loop = r.loop, impact = blow ? impact : 0f });
                log.Append(r.key + " " + clip.length.ToString("F2") + "s" + (blow ? " impact " + impact.ToString("F2") : "") + "\n");
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
        public struct Item { public string key, source; public bool loop; public Item(string k, string s, bool l) { key = k; source = s; loop = l; } }

        public static readonly Item[] All =
        {
            new Item(HeroMocap.Idle, "Breathing Idle.fbx", true),
            new Item(HeroMocap.LookAround, "Looking Around.fbx", true),
            new Item(HeroMocap.HangIdle, "Hanging Idle.fbx", true),
            new Item(HeroMocap.Mantle, "Climbing.fbx", false),
            new Item(HeroMocap.HardLanding, "Hard Landing.fbx", false),
            new Item(HeroMocap.Roll, "Sprinting Forward Roll.fbx", false),
            new Item(HeroMocap.SlashA, "Stable Sword Inward Slash.fbx", false),
            new Item(HeroMocap.SlashB, "Stable Sword Outward Slash.fbx", false),
            new Item(HeroMocap.Stab, "Bayonet Stab.fbx", false),
            new Item(HeroMocap.Talk, "Talking Conversation.fbx", true),
            new Item(HeroMocap.Sad, "Sad Idle.fbx", true),
            new Item(HeroMocap.Angry, "Yelling.fbx", false),
            new Item(HeroMocap.Nod, "Head Nod Yes.fbx", false),
            new Item(HeroMocap.Point, "Pointing Forward.fbx", false),
            new Item(HeroMocap.GetUp, "Getting Up From Back.fbx", false),
        };
    }
}
