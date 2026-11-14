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
        /// <summary>Frames of play before recording starts (the scarf settles).</summary>
        public int Warmup = 0;
        public bool Done { get; private set; }

        struct Cue { public float time; public string action; }
        readonly List<Cue> cues = new List<Cue>();

        float moveX;
        bool jumpPulse, jumpHeld, dashPulse;
        VeilForm? castPulse, held;
        int frame;
        float clock;
        RenderTexture rt;
        Texture2D tex;

        /// <summary>At <paramref name="time"/> s into the clip: "move:1", "move:0", "jump", "hold-jump", "release-jump",
        /// "dash", "cast:Sword", "hold:Shield", "release".</summary>
        public void At(float time, string action) => cues.Add(new Cue { time = time, action = action });

        public float MoveX => moveX;
        public float AimY => 0f;
        public bool JumpPressed { get { bool v = jumpPulse; jumpPulse = false; return v; } }
        public bool JumpHeld => jumpHeld;
        public bool DashPressed { get { bool v = dashPulse; dashPulse = false; return v; } }
        public VeilForm? CastPressed { get { var v = castPulse; castPulse = null; return v; } }
        public bool CastHeld(VeilForm form) => held == form;

        void Start()
        {
            Time.captureFramerate = Fps;
            if (!string.IsNullOrEmpty(Folder)) Directory.CreateDirectory(Folder);
            rt = new RenderTexture(Width, Height, 24);
            tex = new Texture2D(Width, Height, TextureFormat.RGB24, false);
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
                case "cast": castPulse = (VeilForm)System.Enum.Parse(typeof(VeilForm), parts[1]); break;
                case "hold": held = (VeilForm)System.Enum.Parse(typeof(VeilForm), parts[1]); castPulse = held; break;
                case "release": held = null; break;
            }
        }

        void LateUpdate()
        {
            if (Done || Camera == null) return;
            if (Warmup > 0) { Warmup--; return; }
            var p = transform.position;
            Camera.transform.SetPositionAndRotation(p + CameraOffset, Quaternion.identity);
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
            frame++;
            clock = frame / (float)Fps;
            if (frame >= Frames)
            {
                Done = true;
                Time.captureFramerate = 0;
            }
        }

        void OnDestroy()
        {
            if (rt != null) rt.Release();
            Time.captureFramerate = 0;
        }
    }
}
