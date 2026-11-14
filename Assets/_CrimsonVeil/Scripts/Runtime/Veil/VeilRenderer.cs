using UnityEngine;

namespace CrimsonVeil.Veil
{
    /// <summary>
    /// Draws the veil as in the key art: from the wrap at the neck a broad ribbon of crimson streams back in an S,
    /// twisting so its bright face and its dark wine reverse alternate; further back it comes apart into tongues that
    /// wind into big open spirals, with fine brush strokes around them.
    /// Every strand follows the simulated centre line; tendrils branch off the main ribbon (they start *on* it,
    /// so the swirl stays connected) and every end winds into a smooth spiral curl (an Euler spiral whose
    /// curvature grows towards the tip). When the veil hardens into a weapon the tendrils fold back into the
    /// blade and the curls unroll. Colour, shading and smoky edges come from the CrimsonVeil/Veil shader.
    /// </summary>
    [DefaultExecutionOrder(60)]
    [RequireComponent(typeof(VeilController))]
    public sealed class VeilRenderer : MonoBehaviour
    {
        [SerializeField] Material material;
        [Tooltip("Catmull-Rom samples per simulation segment.")]
        [SerializeField, Range(1, 6)] int smoothing = 3;
        [SerializeField, Range(0f, 1f)] float flutterTwist = 0.35f;
        [Tooltip("Points in each end curl.")]
        [SerializeField, Range(6, 40)] int curlPoints = 34;
        [Tooltip("Global multiplier of the curl size.")]
        [SerializeField] float curlScale = 1.6f;
        [Tooltip("How far (m) the tendrils fan away from the main ribbon at their ends.")]
        [SerializeField] float tendrilSpread = 0.38f;

        public struct Strand
        {
            /// <summary>Where on the scarf the strand starts and ends (0 = neck, 1 = tip).</summary>
            public float start, end;
            public float width, side, spread, phase, alpha, seed;
            /// <summary>End curl: length (m), number of turns, winding direction.</summary>
            public float curlLength, curlTurns, curlSign;
            public Color tint;
        }

        public static readonly Strand[] Strands =
        {
            // One flame-like mass, not an octopus: the layers overlap and only fan apart towards the tail. As in
            // the scarf's animation sheet the end winds into two whorls: the body and the lower tongues roll down
            // and under, the upper tongues up and back (LooseCurl).
            // Dark wine under-layer, drifting a little behind.
            new Strand { start = 0f, end = 0.97f, width = 1.15f, side = 0.35f, spread = 0.8f, phase = 1.7f, alpha = 0.75f, seed = 0.31f,
                curlLength = 0.9f, curlTurns = 0.8f, curlSign = -1f, tint = new Color(0.72f, 0.55f, 0.55f) },
            // Main body.
            new Strand { start = 0f, end = 1f, width = 1f, side = 0f, spread = 0.25f, phase = 0f, alpha = 0.95f, seed = 0f,
                curlLength = 1f, curlTurns = 0.95f, curlSign = -1f, tint = Color.white },
            // Layers peeling off the body like tongues of a flame.
            new Strand { start = 0.35f, end = 0.92f, width = 0.75f, side = -0.8f, spread = 1f, phase = 3.9f, alpha = 0.8f, seed = 0.67f,
                curlLength = 0.8f, curlTurns = 0.75f, curlSign = 1f, tint = new Color(0.75f, 0.72f, 0.72f) },
            new Strand { start = 0.45f, end = 1f, width = 0.6f, side = 0.9f, spread = 1.1f, phase = 0.8f, alpha = 0.8f, seed = 0.12f,
                curlLength = 0.85f, curlTurns = 0.9f, curlSign = -1f, tint = new Color(1.1f, 1.02f, 1.02f) },
            new Strand { start = 0.25f, end = 0.75f, width = 0.55f, side = 0.7f, spread = 0.9f, phase = 2.4f, alpha = 0.65f, seed = 0.48f,
                curlLength = 0.6f, curlTurns = 0.6f, curlSign = -1f, tint = new Color(0.62f, 0.45f, 0.45f) },
            new Strand { start = 0.55f, end = 0.95f, width = 0.5f, side = -1.1f, spread = 1.2f, phase = 5.1f, alpha = 0.7f, seed = 0.83f,
                curlLength = 0.7f, curlTurns = 0.7f, curlSign = 1f, tint = new Color(0.9f, 0.86f, 0.86f) },
            // Bright streaks running along the flame.
            new Strand { start = 0.1f, end = 1f, width = 0.18f, side = 0.15f, spread = 1.2f, phase = 4.4f, alpha = 0.85f, seed = 0.21f,
                curlLength = 0.8f, curlTurns = 0.85f, curlSign = -1f, tint = new Color(1.35f, 1.2f, 1.2f) },
            new Strand { start = 0.3f, end = 0.95f, width = 0.14f, side = -0.35f, spread = 1.3f, phase = 1.1f, alpha = 0.8f, seed = 0.92f,
                curlLength = 0.7f, curlTurns = 0.8f, curlSign = 1f, tint = new Color(1.3f, 1.15f, 1.15f) },
            new Strand { start = 0.5f, end = 1f, width = 0.12f, side = 1.2f, spread = 1.2f, phase = 3.2f, alpha = 0.7f, seed = 0.57f,
                curlLength = 0.6f, curlTurns = 0.7f, curlSign = -1f, tint = new Color(1.2f, 1.1f, 1.1f) },
        };

        VeilController veil;
        GameObject meshObject;
        Mesh mesh;
        Vector3[] centre, normalAt;
        float[] hardAt, widthAt, alphaAt, shadeAt;
        Vector3[] vertices;
        Color[] colors;
        Vector2[] uv2;
        Vector3[] curl;
        int samples, perStrand;
        Vector3 lastAnchor;
        float speedSmoothed;

        /// <summary>
        /// Width (m) of the loose scarf <paramref name="d"/> metres from the neck, measured on its animation sheet
        /// (ArtSource/ref/scarf_anim_sheet.png, the hero 170 px = 1.9 m): slim where it leaves the wrap at the neck
        /// (~0.16 m), ~0.32 m a metre back, ~0.38 m along the middle, ~0.3 m where it winds into its whorls.
        /// </summary>
        public static float ScarfWidth(float d)
        {
            if (d < 0f) d = 0f;
            if (d < 1f) return Mathf.Lerp(0.16f, 0.32f, Mathf.SmoothStep(0f, 1f, d));
            if (d < 2f) return Mathf.Lerp(0.32f, 0.38f, Mathf.SmoothStep(0f, 1f, d - 1f));
            return Mathf.Lerp(0.38f, 0.3f, Mathf.SmoothStep(0f, 1f, (d - 2f) / 1.2f));
        }

        /// <summary>
        /// The ribbon's twist (radians) at veil fraction <paramref name="t"/>. As in the key art the band leaves the
        /// neck full and bright, turns over about halfway (pinching where it is edge-on) so its rear half shows the
        /// dark wine reverse, and turns back to the bright face where it comes apart into the curls; the folds
        /// drift a little as it flies.
        /// </summary>
        public static float Twist(float t, float time)
            => Mathf.PI * (-0.1f + 0.6f * t + 1.6f * t * t + 0.06f * Mathf.Sin(2.2f * Mathf.PI * t - 0.9f * time));

        /// <summary>Width factor of the twisted ribbon: a little narrower (0.72) where it turns (the sheet: no hard pinch).</summary>
        public static float TwistWidth(float twist) => Mathf.Lerp(0.72f, 1f, Mathf.Abs(Mathf.Cos(twist)));

        /// <summary>Shade of the ribbon's face: 1 on the bright face, BackShade on the dark wine reverse.</summary>
        public static float TwistShade(float twist)
            => Mathf.Lerp(BackShade, 1f, Mathf.SmoothStep(0f, 1f, (Mathf.Cos(twist) + 0.25f) / 0.5f));

        /// <summary>
        /// Signed turns of a loose end's spiral. In the key art every end rolls down and under (the main mass into a
        /// vortex below the band, the upper tongue into a hook) - the opposite sense to the weapon's flame tongues -
        /// and winds in about LooseCurlTurns times as far as before, into a tight brush-stroke spiral.
        /// </summary>
        public static float LooseCurl(Strand st) => -st.curlSign * st.curlTurns * LooseCurlTurns;

        public const float LooseCurlTurns = 1.35f;

        /// <summary>The reverse of the scarf, relative to its face (key art: deep wine under bright crimson).</summary>
        public const float BackShade = 0.6f;

        /// <summary>
        /// Opacity of strand <paramref name="s"/> of the loose scarf at veil fraction <paramref name="f"/>: near the
        /// neck it is one band of cloth (the body and its darker underside); the other layers - the tongues of smoke
        /// of the key art - only come out where the cloth starts to come apart.
        /// </summary>
        public static float LooseLayerAlpha(int s, float f) => s <= 1 ? 1f : Mathf.SmoothStep(0f, 1f, (f - ClothUntil) / 0.18f);

        /// <summary>Veil fraction up to which the loose scarf is solid cloth (~0.8 m of 2.7 m, the concept's part).</summary>
        public const float ClothUntil = 0.3f;

        /// <summary>How far (in band widths) the loose scarf hangs below the line of the neck.</summary>
        public const float HangBelowNeck = 0.3f;

        public int SampleCount => samples;
        public int PointsPerStrand => perStrand;

        void Awake() => veil = GetComponent<VeilController>();

        void Start()
        {
            int n = veil.Simulation.Count;
            samples = (n - 1) * smoothing + 1;
            perStrand = samples + curlPoints;
            centre = new Vector3[samples];
            normalAt = new Vector3[samples];
            hardAt = new float[samples];
            widthAt = new float[samples];
            alphaAt = new float[samples];
            shadeAt = new float[samples];
            curl = new Vector3[curlPoints + 1];

            int per = perStrand * 2;
            vertices = new Vector3[per * Strands.Length];
            colors = new Color[vertices.Length];
            uv2 = new Vector2[vertices.Length];
            var uvs = new Vector2[vertices.Length];
            var tris = new int[(perStrand - 1) * 6 * Strands.Length];
            int k = 0;
            for (int s = 0; s < Strands.Length; s++)
            {
                int baseV = s * per;
                for (int i = 0; i < perStrand; i++)
                {
                    // Keep the curl mostly out of the shader's tail erosion: it ends at 0.9.
                    float t = 0.9f * i / (perStrand - 1);
                    uvs[baseV + i * 2] = new Vector2(t, 0f);
                    uvs[baseV + i * 2 + 1] = new Vector2(t, 1f);
                    if (i == perStrand - 1) continue;
                    int v = baseV + i * 2;
                    tris[k++] = v; tris[k++] = v + 2; tris[k++] = v + 1;
                    tris[k++] = v + 1; tris[k++] = v + 2; tris[k++] = v + 3;
                }
            }

            mesh = new Mesh { name = "VeilCloth" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.triangles = tris;

            meshObject = new GameObject("VeilCloth");
            meshObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var mr = meshObject.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        void OnDestroy()
        {
            if (meshObject != null) Destroy(meshObject);
            if (mesh != null) Destroy(mesh);
        }

        /// <summary>Catmull-Rom point between p1 and p2.</summary>
        public static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
        {
            float t2 = t * t, t3 = t2 * t;
            return 0.5f * (2f * p1 + (-p0 + p2) * t + (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 + (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
        }

        /// <summary>
        /// Smooth end curl (Euler spiral): starts at <paramref name="start"/> heading along <paramref name="heading"/>
        /// (radians, XY plane) and turns by <paramref name="turns"/> full revolutions, the curvature growing
        /// with the distance, so the stroke winds tighter towards its tip. Writes count + 1 points.
        /// </summary>
        public static void BuildCurl(Vector3 start, float heading, float sign, float length, float turns, int count, Vector3[] into)
        {
            into[0] = start;
            float total = turns * Mathf.PI * 2f * sign;
            float ds = length / count;
            Vector3 p = start;
            for (int i = 1; i <= count; i++)
            {
                float u = (i - 0.5f) / count;
                float a = heading + total * u * u;
                p += new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * ds;
                into[i] = p;
            }
        }

        /// <summary>Sideways offset of a tendril from the main ribbon: zero where it branches, so it is attached.</summary>
        public static float BranchOffset(float u, float side, float width) => side * width * Mathf.SmoothStep(0f, 1f, u);

        void LateUpdate()
        {
            var sim = veil.Simulation;
            if (sim == null || mesh == null) return;

            var p = sim.Positions;
            int n = sim.Count;
            float formHardness = Mathf.Max(0.01f, VeilFormLibrary.Hardness(veil.ShapeForm));
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);

            // Speed of the hero: curls stretch open a little at a run and wind up again at rest.
            float speed = (p[0] - lastAnchor).magnitude / dt;
            lastAnchor = p[0];
            if (speed > 40f) speed = 0f; // teleport
            speedSmoothed = Mathf.Lerp(speedSmoothed, speed, 1f - Mathf.Exp(-3f * dt));
            float unroll = Mathf.Lerp(1f, 0.62f, Mathf.Clamp01(speedSmoothed / 8f));

            // Smooth centre line + per-sample hardness.
            for (int seg = 0; seg < n - 1; seg++)
            {
                Vector3 p0 = p[Mathf.Max(seg - 1, 0)], p1 = p[seg], p2 = p[seg + 1], p3 = p[Mathf.Min(seg + 2, n - 1)];
                float h1 = sim.UseNodeHardness ? sim.NodeHardness[seg] : sim.Hardness;
                float h2 = sim.UseNodeHardness ? sim.NodeHardness[seg + 1] : sim.Hardness;
                for (int k = 0; k < smoothing; k++)
                {
                    float u = (float)k / smoothing;
                    int idx = seg * smoothing + k;
                    centre[idx] = CatmullRom(p0, p1, p2, p3, u);
                    hardAt[idx] = Mathf.Clamp01(Mathf.Lerp(h1, h2, u) / formHardness);
                }
            }
            centre[samples - 1] = p[n - 1];
            hardAt[samples - 1] = Mathf.Clamp01((sim.UseNodeHardness ? sim.NodeHardness[n - 1] : sim.Hardness) / formHardness);

            // Wound round the body in a somersault, the cloth draws in to a slim band (no crumpled blob).
            float flowW = VeilFormLibrary.Width(VeilForm.Flow) * (1f - 0.4f * sim.SwirlWeight);
            float formW = VeilFormLibrary.Width(veil.ShapeForm);
            float hf = veil.HandFraction;
            float time = Time.time;
            float vit = Mathf.Clamp01(veil.Vitality);
            // Length of the loose scarf: the lesser of the hero's life and the scarf's strength.
            float reach = Mathf.Min(vit, Mathf.Lerp(0.3f, 1f, veil.Charge.Value));
            float facing = veil.Facing;
            bool bow = veil.ShapeForm == VeilForm.Bow;
            bool shield = veil.ShapeForm == VeilForm.Shield;
            float shieldCut = Mathf.Ceil(hf * (n - 1)) / (n - 1) + 0.002f;
            Vector3 shieldCentre = p[0] + new Vector3(VeilFormLibrary.ShieldCenter.x * facing, VeilFormLibrary.ShieldCenter.y, 0f);
            float gripFraction = Mathf.Max(hf, VeilFormLibrary.GripFraction(veil.ShapeForm));

            // Per-sample frame, width and alpha of the scarf body.
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / (samples - 1);
                Vector3 tan = centre[Mathf.Min(i + 1, samples - 1)] - centre[Mathf.Max(i - 1, 0)];
                tan.z = 0f;
                if (tan.sqrMagnitude < 1e-8f) tan = Vector3.down;
                tan.Normalize();
                normalAt[i] = new Vector3(-tan.y, tan.x, 0f);
                float hard = Mathf.SmoothStep(0f, 1f, hardAt[i]);
                hardAt[i] = hard;

                // The concept's scarf: a slim band at the neck that opens out behind the shoulder, then comes apart
                // into the key art's tongues of smoke (ScarfWidth, in metres); it ripples gently as it flies.
                float flowProfile = ScarfWidth(t * sim.Settings.length) / VeilFormLibrary.Width(VeilForm.Flow) * (1f + 0.07f * Mathf.Sin(t * 7f - time * 2.4f));
                float twist = Mathf.Abs(Mathf.Cos(time * 1.1f + t * 5f));
                flowProfile *= Mathf.Lerp(1f, twist * 0.65f + 0.35f, flutterTwist * t);

                float weaponProfile = t < hf ? 0.4f : TipProfile(veil.ShapeForm, t);
                weaponProfile *= 1f + 0.08f * Mathf.Sin(t * 14f - time * 5f);
                // The bow's string is part of the same ribbon: a thin glowing thread between the limb tips.
                if (bow) weaponProfile *= Mathf.Lerp(1f, 0.32f, VeilFormLibrary.BowStringWeight(t));
                // Keeps its volume until nearly set, so the transformation reads as the scarf winding in, not thinning out.
                // the loose ribbon twists: it pinches where it turns edge-on and shows its dark reverse
                float tw = Twist(t, time);
                float looseHere = 1f - hard;
                flowProfile *= Mathf.Lerp(1f, TwistWidth(tw), looseHere);
                shadeAt[i] = Mathf.Lerp(1f, TwistShade(tw), looseHere);
                widthAt[i] = Mathf.Lerp(flowW * flowProfile, formW * weaponProfile, hard * hard * hard) * Mathf.Lerp(0.45f, 1f, vit);

                // The loose scarf ends where its strength runs out: casting shortens it, it grows back.
                float fray = 1f - Mathf.SmoothStep(0f, 1f, (t - Mathf.Lerp(0.3f, 1.05f, reach)) / 0.25f);
                alphaAt[i] = Mathf.Lerp(fray, 1f, hard) * Mathf.Lerp(0.6f, 1f, vit) * Mathf.SmoothStep(0f, 1f, t * 12f + 0.3f);
                // A weapon is the whole veil transformed: the band from the neck to the hand melts away as it
                // sets (and comes back as the weapon softens), so the weapon isn't tied to the neck.
                alphaAt[i] *= 1f - NeckBandFade(t, hf, hard);
                // The shield's ring floats free of the hand: nothing may be drawn across the gap to it.
                if (shield && t < shieldCut) alphaAt[i] *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.8f, hard));
            }

            int per = perStrand * 2;
            // How far the veil is into its weapon form (0 cloth .. 1 weapon).
            float weaponness = Mathf.Clamp01(veil.Hardness / formHardness);
            // A coiled whip is just its loops: no braids or flame tongues tangling it into a ball of wool.
            float coiled = veil.ShapeForm == VeilForm.Whip ? (1f - veil.WhipOpen) * weaponness : 0f;
            Vector3 up = Vector3.up, back = new Vector3(-facing, 0f, 0f);
            for (int s = 0; s < Strands.Length; s++)
            {
                var st = Strands[s];
                bool main = s == 1;
                bool branch = st.start > 0f;
                int baseV = s * per;
                Vector3 lastC = centre[0], lastTan = Vector3.right;
                float lastW = 0f, lastA = 0f, lastHard = 0f, lastFront = 0f, lastShade = 1f;
                // As a weapon, each strand ends somewhere along the blade, where its flame tongue peels off.
                // (On a bow every strand runs the whole way round, so both limbs are equally full.)
                float end = StrandEnd(st, main, bow || shield, weaponness, gripFraction); // (every layer burns all round the shield)
                // Keep a pointed weapon's tip clean: the braided strands' tongues flare before the point.
                if (!main && Pointed(veil.ShapeForm)) end = Mathf.Min(end, Mathf.Lerp(1f, 0.82f, weaponness));
                // On a weapon the braided strands start at the hand, so the part behind it (a spear's butt) is
                // just the slim shaft, not every layer of the scarf bundled together.
                float start = branch && !bow ? Mathf.Lerp(st.start, Mathf.Max(st.start, gripFraction), weaponness) : st.start;

                // 1) Body: follows the scarf from `start` to `end`.
                for (int i = 0; i < samples; i++)
                {
                    float u = (float)i / (samples - 1);
                    float f = Mathf.Lerp(start, end, u);
                    float fi = f * (samples - 1);
                    int i0 = Mathf.Min(Mathf.FloorToInt(fi), samples - 2);
                    float fr = fi - i0;
                    Vector3 c = Vector3.Lerp(centre[i0], centre[i0 + 1], fr);
                    Vector3 nrm = Vector3.Lerp(normalAt[i0], normalAt[i0 + 1], fr).normalized;
                    float w = Mathf.Lerp(widthAt[i0], widthAt[i0 + 1], fr);
                    float a = Mathf.Lerp(alphaAt[i0], alphaAt[i0 + 1], fr);
                    float hard = Mathf.Lerp(hardAt[i0], hardAt[i0 + 1], fr);
                    float loose = 1f - hard;

                    // Strands drift apart towards the tail and fold back into one when hardened.
                    float wave = Mathf.Sin(f * Mathf.PI * 2f - time * 0.9f + st.phase);
                    float off;
                    if (branch)
                        off = (BranchOffset(u, st.side, tendrilSpread * Mathf.Lerp(0.5f, 1f, vit)) + wave * 0.14f * st.spread * u) * loose;
                    else
                        off = (st.side + wave * 0.35f * st.spread * f) * w * loose * Mathf.SmoothStep(0f, 1f, f * 3f);
                    // Mirror-true: a positive side is below the band whichever way it streams (the normal points
                    // down for a scarf streaming left, up for one streaming right); hanging straight down, the
                    // layers close up on the band.
                    off *= Mathf.Lerp(1f, Mathf.Clamp(-nrm.y * 4f, -1f, 1f), loose);
                    // Hang from the neck rather than centre on it: the band and its layers spread mostly
                    // downwards, so the scarf streams level behind the hero instead of rising over the head.
                    // The hardening front (half-set cloth) flares up and swirls hardest: the ignition wave.
                    float front = 4f * hard * loose;
                    // As a weapon the strands don't vanish into the blade: they braid around it like
                    // twisting flames (a helix running up the blade), wilder while it ignites.
                    if (!main)
                        off += WeaponBraid(f, time, st.phase, w, hard) * (1f + 1.5f * front) * (1f - coiled);

                    Vector3 ov = nrm * off;
                    // The band itself is kept from rising over the head; the tongues further back fan as in the key
                    // art - the upper one climbs into its hook, the lower ones stay close under the band.
                    if (ov.y > 0f) ov.y *= branch ? Mathf.Lerp(0.85f, 0.5f, hard) : 0.5f;
                    else if (branch) ov.y *= Mathf.Lerp(0.55f, 1f, hard);
                    c += ov + new Vector3(0f, -w * HangBelowNeck * loose * Mathf.SmoothStep(0f, 1f, f * 4f), (s - 1) * 0.012f);

                    // The shield's ring burns: tongues of flame lick outwards off it and its body flickers.
                    float lick = 0f, burn = 1f, set = 0f;
                    if (shield && f >= hf)
                    {
                        set = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.4f, 0.9f, hard));
                        lick = ShieldFlameLick(f, time, st.phase, st.seed) * set;
                        burn = Mathf.Lerp(1f, ShieldFlicker(f, time, st.phase), set);
                        Vector3 outward = c - shieldCentre;
                        outward.z = 0f;
                        if (outward.sqrMagnitude > 1e-6f)
                            c += outward.normalized * (lick * (main ? 0.16f : 0.6f) + (main ? 0f : 0.05f * set));
                    }

                    // Tendrils emerge from the ribbon: thin where they split, then full.
                    float sw = w * Mathf.Lerp(st.width, main ? 1f : 0.3f, hard) * burn;
                    if (!main) sw *= (1f + 1.6f * set) * (1f - 0.7f * lick); // bold tongues, each narrowing to its tip
                    if (branch) sw *= Mathf.Lerp(Mathf.Lerp(0.38f, 0.25f, hard), Mathf.Lerp(0.95f, 0.51f, hard), Mathf.SmoothStep(0f, 1f, u))
                                      * Mathf.Lerp(1f - 0.15f * Mathf.SmoothStep(0.6f, 1f, u), 1f, hard); // a broad stroke into its whorl
                    float alpha = a * Mathf.Lerp(st.alpha, main ? 1f : 0.7f, hard);
                    // Loose, it is one band of cloth near the neck (the concept); the tongues of smoke come out
                    // further back (the key art), and all of them as it hardens into a weapon.
                    alpha *= Mathf.Lerp(LooseLayerAlpha(s, f), 1f, hard);
                    if (branch) alpha *= Mathf.SmoothStep(0f, 1f, u * 5f) * Mathf.Lerp(1f, 0.12f, coiled);

                    float shade = Mathf.Lerp(shadeAt[i0], shadeAt[i0 + 1], fr);
                    WriteVertex(baseV + i * 2, c, nrm, sw, alpha, st, hard, vit, front, shade);
                    if (i > 0) lastTan = c - lastC;
                    lastC = c; lastW = sw; lastA = alpha; lastHard = hard; lastFront = front; lastShade = shade;
                }

                // 2) End curl: loose cloth winds into a spiral; on a weapon the end becomes a flame tongue
                // that licks upwards and back off the blade.
                float looseEnd = 1f - lastHard;
                float breathe = 1f + 0.18f * Mathf.Sin(time * (0.5f + 0.4f * st.seed) + st.phase);
                float flicker = 1f + 0.25f * Mathf.Sin(time * (3.1f + 2f * st.seed) + st.phase * 2f);
                // Signed turns, blended so the change is smooth: loose, the ends roll down and under into the key
                // art's spirals (LooseCurl: against the weapon tongues' sense, and tighter); as a weapon a short
                // tongue flicks off.
                float turns = Mathf.Lerp(st.curlSign * 0.2f * flicker, LooseCurl(st) * breathe * unroll, looseEnd);
                // A pointed weapon ends in its point: the core gets no flame tongue curling off the tip.
                float tongue = main && Pointed(veil.ShapeForm) ? 0f : FlameTongueLength(main) * (1f - coiled);
                float length = Mathf.Lerp(tongue * flicker, st.curlLength * curlScale, looseEnd) * Mathf.Lerp(0.5f, 1f, reach);
                lastTan.z = 0f;
                Vector3 dir = lastTan.sqrMagnitude > 1e-10f ? lastTan.normalized : Vector3.down;
                // Flames rise: a weapon's tongues bend up and back, away from the direction of the blow.
                Vector3 flameDir = (dir * 0.35f + up * 0.9f + back * 0.35f).normalized;
                dir = Vector3.Slerp(dir, flameDir, lastHard).normalized;
                float heading = Mathf.Atan2(dir.y, dir.x);
                BuildCurl(lastC, heading, facing, length, turns, curlPoints, curl);
                for (int k = 1; k <= curlPoints; k++)
                {
                    float u = (float)k / curlPoints;
                    Vector3 d = curl[Mathf.Min(k + 1, curlPoints)] - curl[k - 1];
                    d.z = 0f;
                    Vector3 nrm = d.sqrMagnitude > 1e-10f ? new Vector3(-d.y, d.x, 0f).normalized : Vector3.up;
                    // A brush stroke: broad as it winds round its whorl (the sheet), thinning to a fine end at the centre.
                    float sw = lastW * Mathf.Lerp(1f, Mathf.Lerp(0.06f, 0.03f, lastHard), Mathf.Pow(u, Mathf.Lerp(1.05f, 0.6f, lastHard)));
                    float alpha = lastA * Mathf.Lerp(1f, 0.55f, u);
                    WriteVertex(baseV + (samples - 1 + k) * 2, curl[k], nrm, sw, alpha, st, lastHard, vit, lastFront, lastShade);
                }
            }

            // Loose cloth stays behind the hero: when he stops, the swinging band and its layers must not sweep
            // out in front of the body. Anything past the chest line is softly folded back (not cut).
            // Only once the hero faces one way — while turning round the scarf swings freely across.
            // (Not while it is wound round the body in a somersault: there it must go round the front too.)
            float settle = Mathf.InverseLerp(0.85f, 1f, Mathf.Abs(veil.FacingBlend)) * (1f - sim.SwirlWeight);
            if (settle > 0f)
            {
                float ax = p[0].x;
                for (int v = 0; v < vertices.Length; v++)
                {
                    float looseV = 1f - Mathf.Clamp01(uv2[v].x / 0.6f);
                    if (looseV <= 0f) continue;
                    float ahead = (vertices[v].x - ax) * facing;
                    if (ahead <= FrontLimit) continue;
                    float folded = FrontLimit + FoldBack(ahead - FrontLimit);
                    vertices[v].x = ax + facing * Mathf.Lerp(ahead, folded, looseV * settle);
                }
            }

            mesh.vertices = vertices;
            mesh.colors = colors;
            mesh.SetUVs(1, uv2);
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// How much of the neck → hand band is hidden (0..1) at veil fraction <paramref name="t"/>: nothing on loose
        /// cloth, all of it once that part has set into the weapon form; the weapon itself (past the hand) stays.
        /// </summary>
        public static float NeckBandFade(float t, float handFraction, float hard)
        {
            float beforeHand = Mathf.SmoothStep(0f, 1f, (handFraction - t) / 0.05f);
            return beforeHand * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.95f, hard));
        }

        /// <summary>How far (m) in front of the neck the loose scarf may reach (about the chest line).</summary>
        public const float FrontLimit = 0.15f;

        /// <summary>Soft fold for cloth past the front limit: grows at first, then saturates at 0.1 m.</summary>
        public static float FoldBack(float over) => over <= 0f ? over : 0.1f * (1f - Mathf.Exp(-over / 0.1f));

        /// <summary>Sideways offset of a strand braided around a weapon: a travelling helix, zero for loose cloth.</summary>
        public static float WeaponBraid(float f, float time, float phase, float width, float hard)
            => Mathf.Sin(f * 16f - time * 3.2f + phase) * width * 0.24f * hard;

        /// <summary>Where along the veil a strand ends once it is a weapon: spread along the blade, so flame
        /// tongues peel off all along it rather than only at the tip.</summary>
        public static float WeaponStrandEnd(Strand st, float handFraction)
            => Mathf.Lerp(Mathf.Min(handFraction + 0.12f, 0.95f), 0.99f, 0.15f + 0.85f * Mathf.Repeat(st.seed * 1.7f + st.phase * 0.13f, 1f));

        /// <summary>
        /// 0..1 how far a tongue of flame licks out of the shield's ring at veil fraction <paramref name="f"/>:
        /// sharp, irregular spikes that run along the ring and flicker (each strand its own set).
        /// </summary>
        public static float ShieldFlameLick(float f, float time, float phase, float seed)
        {
            float s = Mathf.Sin(f * 2f * Mathf.PI * 9f + phase * 2.3f - time * 10f);
            float spike = Mathf.Pow(Mathf.Max(0f, s), 5f);
            float gust = 0.55f + 0.45f * Mathf.Sin(f * 31f - time * 6.5f + seed * 17f);
            return Mathf.Clamp01(spike * gust * 1.2f);
        }

        /// <summary>Flickering thickness of the burning ring (about 0.65..1.3).</summary>
        public static float ShieldFlicker(float f, float time, float phase)
            => 0.98f + 0.2f * Mathf.Sin(f * 47f - time * 13f + phase) + 0.13f * Mathf.Sin(f * 19f + time * 7f + phase * 1.7f);

        /// <summary>Weapons that end in a point (no flame curl at the tip).</summary>
        public static bool Pointed(VeilForm form) => form == VeilForm.Sword || form == VeilForm.Spear;

        /// <summary>
        /// Width multiplier of the weapon part at veil fraction <paramref name="t"/> (1 = the weapon's width):
        /// a sword's blade narrows to a sharp point, a spear's slim shaft ends in a leaf-shaped head,
        /// a bow keeps its width; the others thin out towards a smouldering tip.
        /// </summary>
        public static float TipProfile(VeilForm form, float t)
        {
            switch (form)
            {
                case VeilForm.Bow:
                    return 1f;
                case VeilForm.Shield:
                    // A firm ring: full width almost to its end, rounded off only at the very tip.
                    return 1f - Mathf.SmoothStep(0f, 1f, (t - 0.96f) / 0.04f) * 0.6f;
                case VeilForm.Sword:
                {
                    // Last 16% of the veil: the edges sweep into a point.
                    float x = Mathf.Clamp01((t - 0.84f) / 0.16f);
                    return Mathf.Pow(1f - x, 0.7f) * (1f + 0.15f * Mathf.Sin(x * Mathf.PI));
                }
                case VeilForm.Spear:
                {
                    // Last 14%: a leaf blade — widens past the socket, then tapers to the point.
                    float x = Mathf.Clamp01((t - 0.86f) / 0.14f);
                    if (x <= 0f) return 1f;
                    if (x < 0.35f) return Mathf.Lerp(0.8f, SpearHeadWidth, Mathf.SmoothStep(0f, 1f, x / 0.35f));
                    return SpearHeadWidth * Mathf.Pow(1f - (x - 0.35f) / 0.65f, 1.1f);
                }
                default:
                    return 1f - Mathf.SmoothStep(0f, 1f, (t - 0.86f) / 0.14f) * 0.85f;
            }
        }

        /// <summary>The spearhead's widest point, in shaft widths.</summary>
        public const float SpearHeadWidth = 3.2f;

        /// <summary>
        /// Where a strand ends (veil fraction) for weapon-ness 0..1. The core ribbon always runs the whole weapon to
        /// its tip, as does every strand of a bow; the other strands' flame tongues peel off along the part of the
        /// weapon in front of the hand (<paramref name="gripFraction"/>).
        /// </summary>
        public static float StrandEnd(Strand st, bool main, bool bow, float weaponness, float gripFraction)
            => Mathf.Lerp(st.end, main || bow ? 1f : WeaponStrandEnd(st, gripFraction), weaponness);

        /// <summary>Length (m) of the flame tongue at a weapon strand's end.</summary>
        public static float FlameTongueLength(bool main) => main ? 0.22f : 0.34f;

        void WriteVertex(int v, Vector3 c, Vector3 nrm, float width, float alpha, Strand st, float hard, float vit, float glow = 0f, float shade = 1f)
        {
            vertices[v] = c + nrm * (width * 0.5f);
            vertices[v + 1] = c - nrm * (width * 0.5f);
            var col = st.tint * (1f + 0.9f * glow) * shade;
            col.a = alpha;
            col.r *= Mathf.Lerp(0.6f, 1f, vit);
            colors[v] = colors[v + 1] = col;
            uv2[v] = uv2[v + 1] = new Vector2(hard, st.seed);
        }
    }
}
