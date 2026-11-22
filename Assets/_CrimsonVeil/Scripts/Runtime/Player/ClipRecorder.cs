using System.Collections.Generic;
using System.IO;
using CrimsonVeil.Veil;
using UnityEngine;

namespace CrimsonVeil.Player
{
    /// <summary>
    /// Records short clips of the game for review (the scarf, the weapons, the somersault): plays the hero by a timed
    /// script of inputs at a fixed frame rate (Time.captureFramerate, so the clip is smooth however slowly the editor
    /// renders), keeps a camera on the hero and writes every frame as a PNG into <see cref="Folder"/>. Like
    /// <see cref="AutopilotInput"/> it is an input source: assign it to <see cref="PlayerController.InputSource"/>.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    public sealed class ClipRecorder : MonoBehaviour, IPlayerInput
    {
        public string Folder;
        public int Frames = 48;
        public int Fps = 24;
        public int Width = 960, Height = 540;
        public Camera Camera;
        /// <summary>Camera position relative to the hero.</summary>
        public Vector3 CameraOffset = new Vector3(-1.4f, 1.1f, -16f);
        public float FieldOfView = 12f;
        /// <summary>Turn the camera round the hero (degrees round the vertical; 0 = the game's side view), to see the
        /// body in depth (a twist, an arm swung out) from three-quarters.</summary>
        public float OrbitYaw = 0f;
        /// <summary>Tilt the camera down over the hero (degrees; 0 = level, 90 = straight from above).</summary>
        public float OrbitPitch = 0f;
        /// <summary>Frames of play before recording starts (the scarf settles).</summary>
        public int Warmup = 0;
        public bool Done { get; private set; }
        /// <summary>Optional measurement taken every recorded frame and written to probe.txt beside the frames.</summary>
        public System.Func<string> Probe;
        readonly System.Text.StringBuilder probeLog = new System.Text.StringBuilder();

        struct Cue { public float time; public string action; }
        readonly List<Cue> cues = new List<Cue>();

        float moveX, aimY;
        bool jumpPulse, jumpHeld, dashPulse;
        VeilForm? castPulse, held;
        bool stand;
        int frame;
        float clock;
        RenderTexture rt;
        Texture2D tex;

        /// <summary>At <paramref name="time"/> s into the clip: "move:1", "move:0", "jump", "hold-jump", "release-jump",
        /// "dash", "aim:1" (up) / "aim:-1" / "aim:0", "cast:Sword", "hold:Shield", "release", "stand:1" / "stand:0" (Ctrl).</summary>
        public void At(float time, string action) => cues.Add(new Cue { time = time, action = action });

        public float MoveX => moveX;
        public float AimY => aimY;
        public bool JumpPressed { get { bool v = jumpPulse; jumpPulse = false; return v; } }
        public bool JumpHeld => jumpHeld;
        public bool DashPressed { get { bool v = dashPulse; dashPulse = false; return v; } }
        public bool StandHeld => stand;
        public VeilForm? CastPressed { get { var v = castPulse; castPulse = null; return v; } }
        public bool CastHeld(VeilForm form) => held == form;

        void Start()
        {
            Time.captureFramerate = Fps;
            if (!string.IsNullOrEmpty(Folder)) Directory.CreateDirectory(Folder);
            rt = new RenderTexture(Width, Height, 24);
            tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
            StartCoroutine(CaptureLoop());
        }

        void Update()
        {
            if (Done) return;
            for (int i = cues.Count - 1; i >= 0; i--)
            {
                if (cues[i].time > clock) continue;
                Apply(cues[i].action);
                cues.RemoveAt(i);
            }
        }

        /// <summary>Applies one input action now (see <see cref="At"/>).</summary>
        public void Apply(string a)
        {
            var parts = a.Split(':');
            switch (parts[0])
            {
                case "move": moveX = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture); break;
                case "jump": jumpPulse = true; jumpHeld = true; break;
                case "hold-jump": jumpHeld = true; break;
                case "release-jump": jumpHeld = false; break;
                case "dash": dashPulse = true; break;
                case "aim": aimY = float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture); break;
                case "cast": castPulse = (VeilForm)System.Enum.Parse(typeof(VeilForm), parts[1]); break;
                case "hold": held = (VeilForm)System.Enum.Parse(typeof(VeilForm), parts[1]); castPulse = held; break;
                case "release": held = null; break;
                case "stand": stand = parts[1] == "1"; break;
            }
        }

        // Frames are captured at the end of the frame, after Unity has skinned the meshes. A Camera.Render() in
        // LateUpdate would draw the body with the previous frame's skinning while the veil (built in LateUpdate)
        // is already current, so the weapon would seem to leave the fist by a whole frame of motion.
        System.Collections.IEnumerator CaptureLoop()
        {
            var end = new WaitForEndOfFrame();
            while (!Done)
            {
                yield return end;
                Capture();
            }
        }

        void Capture()
        {
            if (Done || Camera == null) return;
            if (Warmup > 0) { Warmup--; return; }
            var p = transform.position;
            var orbit = Quaternion.Euler(OrbitPitch, OrbitYaw, 0f);
            var aim = p + new Vector3(CameraOffset.x, CameraOffset.y, 0f);
            Camera.transform.SetPositionAndRotation(aim + orbit * new Vector3(0f, 0f, CameraOffset.z), orbit);
            Camera.fieldOfView = FieldOfView;
            var keep = Camera.targetTexture;
            Camera.targetTexture = rt;
            Camera.Render();
            Camera.targetTexture = keep;
            var active = RenderTexture.active;
            RenderTexture.active = rt;
            tex.ReadPixels(new Rect(0, 0, Width, Height), 0, 0);
            tex.Apply();
            RenderTexture.active = active;
            File.WriteAllBytes(Path.Combine(Folder, $"f{frame:000}.png"), tex.EncodeToPNG());
            if (Probe != null) probeLog.AppendLine($"{frame:000} {Probe()}");
            frame++;
            clock = frame / (float)Fps;
            if (frame >= Frames)
            {
                Done = true;
                Time.captureFramerate = 0;
                if (Probe != null) File.WriteAllText(Path.Combine(Folder, "probe.txt"), probeLog.ToString());
            }
        }

        void OnDestroy()
        {
            if (rt != null) rt.Release();
            Time.captureFramerate = 0;
        }
    }
}
