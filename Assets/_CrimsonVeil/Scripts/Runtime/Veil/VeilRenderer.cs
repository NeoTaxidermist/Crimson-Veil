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

        /// <summary>The band itself among <see cref="Strands"/> (the scarf's centre line); the rest are simulated (VeilStrands).</summary>
        public const int MainStrand = 1;

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
        float[] hardAt, widthAt, alphaAt, twistAt;
        Vector3[] vertices, normals;
        Color[] colors;
        Vector2[] uv2;
        Vector3[] curl;
        // per strand sample: the face's normal (bright side) and how deep the cloth bows across (in half widths)
        Vector3[] faceAt;
        float[] cupAt;
        // a simulated strand's course along the drawn samples and its direction there
        Vector3[] simPt, simRun;
        // each end's whorl: where it heads and how far it winds, eased in time (taken afresh from the strand's last
        // segment every frame, a whorl swung to and fro with every flicker of the strand's tip)
        float[] curlHeading, curlTurnsNow;
        bool curlsStarted;
        int samples, perStrand, centreBase;
        // The cloth is drawn back to front in pieces (PaintOrder): a strand winding round in front of the band covers
        // it, one behind is covered (one mesh otherwise paints in its own order, and depth-tested layers of the
        // painted cloth cut each other into jagged seams).
        int[] trisAll, trisSorted, chunkTri, chunkTris, chunkV, chunkVs, order;
        float[] chunkKey;
        Camera viewer;

        /// <summary>Segments of a strand in one piece of the painter's order.</summary>
        public const int ChunkSegments = 4;
        /// <summary>How much nearer (m) each later strand of the drawing counts in the order: layers lying together
        /// keep their painted order instead of flickering.</summary>
        public const float LayerBias = 0.004f;

        Vector3 lastAnchor;
        float speedSmoothed;
        float sinceSwirl = 10f;
        float settleNow;

        /// <summary>
        /// Width (m) of the loose scarf <paramref name="d"/> metres from the neck, measured on its animation sheet
        /// (ArtSource/ref/scarf_anim_sheet.png, the hero 170 px = 1.9 m): slim where it leaves the wrap at the neck
        /// (~0.16 m), ~0.32 m a metre back, ~0.38 m along the middle, opening to ~0.48 m into its whorls.
        /// </summary>
        public static float ScarfWidth(float d)
        {
            if (d < 0f) d = 0f;
            if (d < 1f) return Mathf.Lerp(0.16f, 0.32f, Mathf.SmoothStep(0f, 1f, d));
            if (d < 2f) return Mathf.Lerp(0.32f, 0.38f, Mathf.SmoothStep(0f, 1f, d - 1f));
            return Mathf.Lerp(0.38f, 0.32f, Mathf.SmoothStep(0f, 1f, (d - 2f) / 1.2f));   // (the key art: the band narrows as its tongues take over)
        }

        /// <summary>
        /// The ribbon's twist (radians) at veil fraction <paramref name="t"/>. As in the key art the band shows its bright
        /// face all the way: it leaves the neck flat, then sways round its length in waves running back along it
        /// (TwistSway of a turn at most), narrowing where it leans away - a flame licking, never turned over into a
        /// dark knot.
        /// </summary>
        public static float Twist(float t, float time)
            => TwistSway * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.05f, 0.5f, t)) * Mathf.Sin(2f * Mathf.PI * 0.9f * t - 1.3f * time);

        /// <summary>How far (rad) the loose ribbon sways round its length at most (0.3 pi: it narrows to three fifths).</summary>
        public const float TwistSway = 0.3f * Mathf.PI;

        /// <summary>
        /// The direction across the ribbon, its in-picture normal <paramref name="nrm"/> turned by
        /// <paramref name="twist"/> round the ribbon's course into depth: flat to the camera at 0, edge-on at a
        /// quarter turn (it is seen |cos| as wide), the other way round at a half.
        /// </summary>
        public static Vector3 RibbonSide(Vector3 nrm, float twist)
        {
            float c = Mathf.Cos(twist), s = Mathf.Sin(twist);
            return new Vector3(nrm.x * c, nrm.y * c, s);
        }

        /// <summary>The normal of the ribbon's bright face (towards the camera at twist 0, away from it at a half turn:
        /// there the dark wine reverse shows - the CrimsonVeil/Veil shader shades by it).</summary>
        public static Vector3 RibbonFace(Vector3 nrm, float twist)
        {
            float c = Mathf.Cos(twist), s = Mathf.Sin(twist);
            return new Vector3(nrm.x * s, nrm.y * s, -c);
        }

        /// <summary>How deep (in half widths) loose cloth bows out across its middle: no flat paper strip, and
        /// edge-on it still shows that much of itself.</summary>
        public const float CupDepth = 0.1f;

        /// <summary>
        /// The ribbon's cross-section between its edges <paramref name="left"/> and <paramref name="right"/>: its middle
        /// bows out along the face normal <paramref name="face"/> by <paramref name="cup"/> half widths, and the normals at
        /// the edges lean out with the slope of the bow (the light runs across the cloth as across a real fold).
        /// </summary>
        public static void CupSection(Vector3 left, Vector3 right, Vector3 face, float cup, out Vector3 centre, out Vector3 nLeft, out Vector3 nRight)
        {
            Vector3 half = (left - right) * 0.5f;
            float hw = half.magnitude;
            centre = right + half + face * (cup * hw);
            if (hw < 1e-6f || cup <= 0f) { nLeft = nRight = face; return; }
            Vector3 lean = half * (2f * cup / hw);
            nLeft = (face + lean).normalized;
            nRight = (face - lean).normalized;
        }

        /// <summary>
        /// Signed turns of a loose end's whorl of smoke (+ rolls down, - up, for a scarf streaming either way: BuildCurl
        /// turns with the facing). As in the new key art the scarf ends in an S of whorls: the band and the tongues
        /// above it curl up and back over, the tongues below it roll down and under.
        /// </summary>
        public static float LooseCurl(Strand st) => (st.side > 0.05f ? 1f : -1f) * st.curlTurns * LooseCurlTurns;

        public const float LooseCurlTurns = 0.9f;

        /// <summary>
        /// Opacity of strand <paramref name="s"/> of the loose scarf at veil fraction <paramref name="f"/>: near the
        /// neck it is one band of cloth (the body and its darker underside); the other layers - the tongues of smoke
        /// of the key art - only come out where the cloth starts to come apart.
        /// </summary>
        public static float LooseLayerAlpha(int s, float f) => s <= 1 ? 1f : Mathf.SmoothStep(0f, 1f, (f - ClothUntil) / 0.18f);

        /// <summary>Veil fraction up to which the loose scarf is solid cloth (~0.8 m of 2.7 m, the concept's part).</summary>
        public const float ClothUntil = 0.3f;

        /// <summary>How far (in band widths) the loose scarf hangs below the line of the neck.</summary>
        public const float HangBelowNeck = 0.1f;

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
            twistAt = new float[samples];
            curl = new Vector3[curlPoints + 1];
            simPt = new Vector3[samples];
            simRun = new Vector3[samples];
            curlHeading = new float[Strands.Length];
            curlTurnsNow = new float[Strands.Length];

            // Every strand is a ribbon of three rows: its two edges (pairs, as ClampFolds wants them) and, after all
            // the pairs, the row down its middle, where the cloth bows out (CupSection).
            int per = perStrand * 2;
            centreBase = per * Strands.Length;
            vertices = new Vector3[centreBase + perStrand * Strands.Length];
            normals = new Vector3[vertices.Length];
            colors = new Color[vertices.Length];
            uv2 = new Vector2[vertices.Length];
            faceAt = new Vector3[perStrand * Strands.Length];
            cupAt = new float[faceAt.Length];
            var uvs = new Vector2[vertices.Length];
            var tris = new int[(perStrand - 1) * 12 * Strands.Length];
            int k = 0;
            for (int s = 0; s < Strands.Length; s++)
            {
                int baseV = s * per, baseC = centreBase + s * perStrand;
                for (int i = 0; i < perStrand; i++)
                {
                    // Keep the curl mostly out of the shader's tail erosion: it ends at 0.9.
                    float t = 0.9f * i / (perStrand - 1);
                    uvs[baseV + i * 2] = new Vector2(t, 0f);
                    uvs[baseV + i * 2 + 1] = new Vector2(t, 1f);
                    uvs[baseC + i] = new Vector2(t, 0.5f);
                    if (i == perStrand - 1) continue;
                    int l = baseV + i * 2, r = l + 1, c = baseC + i;
                    tris[k++] = l; tris[k++] = l + 2; tris[k++] = c;
                    tris[k++] = c; tris[k++] = l + 2; tris[k++] = c + 1;
                    tris[k++] = c; tris[k++] = c + 1; tris[k++] = r;
                    tris[k++] = r; tris[k++] = c + 1; tris[k++] = r + 2;
                }
            }

            // pieces of ChunkSegments segments along every strand
            int perChunks = (perStrand - 1 + ChunkSegments - 1) / ChunkSegments;
            int chunks = perChunks * Strands.Length;
            chunkTri = new int[chunks]; chunkTris = new int[chunks]; chunkV = new int[chunks]; chunkVs = new int[chunks];
            chunkKey = new float[chunks]; order = new int[chunks];
            for (int s = 0, ch = 0; s < Strands.Length; s++)
                for (int c = 0; c < perChunks; c++, ch++)
                {
                    int first = c * ChunkSegments, segs = Mathf.Min(ChunkSegments, perStrand - 1 - first);
                    chunkTri[ch] = (s * (perStrand - 1) + first) * 12;
                    chunkTris[ch] = segs * 12;
                    chunkV[ch] = centreBase + s * perStrand + first;
                    chunkVs[ch] = segs + 1;
                    order[ch] = ch;
                }
            trisAll = tris;
            trisSorted = new int[tris.Length];

            mesh = new Mesh { name = "VeilCloth" };
            mesh.MarkDynamic();
            mesh.vertices = vertices;
            mesh.normals = normals;
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
            using var marker = CrimsonVeil.Core.Perf.Begin(CrimsonVeil.Core.Perf.Id.VeilDraw);

            var p = sim.Positions;
            int n = sim.Count;
            var strands = veil.Strands;
            float formHardness = Mathf.Max(0.01f, VeilFormLibrary.Hardness(veil.ShapeForm));
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);

            // Speed of the hero: curls stretch open a little at a run and wind up again at rest.
            // After a somersault the cloth is still wound round in front: it swings back behind on its own (the
            // rest shape pulls it) instead of being folded flat against the chest into a crumpled ball.
            sinceSwirl = sim.SwirlWeight > 0.01f ? 0f : sinceSwirl + dt;
            float speed = (p[0] - lastAnchor).magnitude / dt;
            lastAnchor = p[0];
            if (speed > 40f) speed = 0f; // teleport
            speedSmoothed = Mathf.Lerp(speedSmoothed, speed, 1f - Mathf.Exp(-3f * dt));
            float unroll = Mathf.Lerp(1f, 0.62f, Mathf.Clamp01(speedSmoothed / 8f));
            // Against a wall the end hangs down it: no spirals curling round in the pile of cloth (they made a ball of it).
            float walled = 0f;
            for (int i = n / 2; i < n; i++) walled = Mathf.Max(walled, sim.WallContact(i));
            unroll *= 1f - 0.8f * walled;
            // Streaming up over him in a fall the whorls open out into straight tongues: wound up they curled off to
            // the side of the upright scarf, half of them forward over his head.
            unroll *= UnrollInTilt(sim.TrailTilt);
            // Falling past a ledge, what of the drawn band strays into it is left there, hidden behind its face as if
            // the cloth trailed past behind the edge: pushed out, each strand was laid flat on the top in steps.
            // (The cloth itself is still kept out of it by the simulation.)
            float keepOut = KeepOutInTilt(sim.TrailTilt);

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
            float reach = veil.LooseReach;
            float facing = veil.Facing;
            // the loose cloth turns over with the hero as he turns round (his facing eases from one side to the other):
            // flipped in a single frame, its twist and its whorls snapped over and the cloth broke up in the turn
            // (the turn runs down the scarf from the neck, VeilSimulation.TurnTravel: the tail and its whorls turn last)
            float turnFacing = Mathf.Clamp(-sim.DriftAt(1f), -1f, 1f);
            bool bow = veil.ShapeForm == VeilForm.Bow;
            // Pulled out into a rope (the whip round a ring) the scarf runs several times its length: it must still
            // read as the braided weapon, not a stretched-out red line.
            bool rope = veil.Latched != null;
            // a whip is no stiff blade: like the loose scarf it lies on the floor rather than passing into it
            var form = veil.ShapeForm;
            float arc = 0f;
            for (int i = 1; i < samples; i++) arc += Vector3.Distance(centre[i - 1], centre[i]);
            float braidScale = rope ? RopeBraidScale(arc, sim.Settings.length) * RopeBraidTwist : 1f;
            bool shield = veil.ShapeForm == VeilForm.Shield;
            float shieldCut = Mathf.Ceil(hf * (n - 1)) / (n - 1) + 0.002f;
            Vector2 sc = veil.ShieldCentre;   // (up round the crouched body)
            Vector3 shieldCentre = p[0] + new Vector3(sc.x * facing, sc.y, 0f);
            float gripFraction = Mathf.Max(hf, VeilFormLibrary.GripFraction(veil.ShapeForm));
            // A sword's flame tongues run along its blade, from the crossguard: the hilt stays a clean hilt.
            if (veil.ShapeForm == VeilForm.Sword)
            {
                VeilFormLibrary.SwordHilt(out _, out _, out float guardAt);
                gripFraction = Mathf.Max(gripFraction, guardAt);
            }

            bool whipForm = veil.ShapeForm == VeilForm.Whip;
            VeilFormLibrary.WhipHandleSpan(out float whipButt, out _, out float whipStockEnd);
            float whipHandleShown = whipForm && veil.Form == VeilForm.Whip && veil.Hardness > 0.5f * formHardness ? 1f : 0f;

            // How far the weapon has set where it leaves the hand (the neck band only melts away after that).
            float handHard = Mathf.SmoothStep(0f, 1f, hardAt[Mathf.Clamp(Mathf.RoundToInt(Mathf.Min(1f, hf + 0.03f) * (samples - 1)), 0, samples - 1)]);
            // ... and once the weapon has taken its shape (the shield's ring is thrown round first): until then the
            // band is what joins the forming weapon to the neck. Releasing, the band comes back with the hardness.
            float bandShaped = veil.Form == VeilForm.Flow ? 1f : NeckBandShaped(veil.ShapeMorph);

            float wrapEnd = (float)Mathf.Min(sim.Settings.wrapNodes, n - 2) / (n - 1);
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
                // the rope keeps the lash's width all the way to the ring (a whip tapering over 7 m read as a thread)
                if (rope && t >= hf) weaponProfile = Mathf.Max(weaponProfile, RopeWidth);
                weaponProfile *= 1f + 0.08f * Mathf.Sin(t * 14f - time * 5f);
                // The bow's string is part of the same ribbon: a thin glowing thread between the limb tips.
                if (bow) weaponProfile *= Mathf.Lerp(1f, 0.32f, VeilFormLibrary.BowStringWeight(t));
                // Keeps its volume until nearly set, so the transformation reads as the scarf winding in, not thinning out.
                // The loose ribbon twists for real (RibbonSide): it turns edge-on and over, showing its dark reverse.
                // (mirror-true: the other way round when the hero faces the other way)
                twistAt[i] = Twist(t, time) * (1f - hard) * Mathf.Clamp(-sim.DriftAt(t), -1f, 1f);
                widthAt[i] = Mathf.Lerp(flowW * flowProfile, formW * weaponProfile, hard * hard * hard) * Mathf.Lerp(0.45f, 1f, vit);
                // slipping out from behind the collar it is gathered narrow, and opens out as it flies
                widthAt[i] *= Mathf.Lerp(1f, NeckGather(t, wrapEnd), 1f - hard);

                // (the loose scarf's length follows its strength in the cloth itself - VeilSimulation.LengthScale - so
                // its end keeps its curls; fading the tail out instead looked like a scarf cut off)
                alphaAt[i] = Mathf.Lerp(0.6f, 1f, vit) * Mathf.SmoothStep(0f, 1f, t * 12f + 0.3f);
                // The wrap round the neck isn't drawn: seen from the side it doubled back on itself into a blob stuck to
                // the neck (the hero's collar is his own); the loose scarf streams from the back of the neck.
                alphaAt[i] *= Mathf.Lerp(1f, FromTheNeck(t, wrapEnd), 1f - hard);
                // A weapon is the whole veil transformed: the band from the neck to the hand melts away as it
                // sets (and comes back as the weapon softens), so the weapon isn't tied to the neck.
                // It goes once the weapon has set *at the hand*: the hardening starts at the neck, and fading the band
                // by its own hardness cut the scarf off at the neck on the first frame of a cast.
                float bandSet = Mathf.Min(Mathf.Min(hard, handHard), bandShaped);
                alphaAt[i] *= 1f - NeckBandFade(t, hf, bandSet);
                // The shield's ring floats free of the hand: nothing may be drawn across the gap to it.
                if (shield && t < shieldCut) alphaAt[i] *= 1f - Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.8f, bandSet));
                // The whip's handle is a solid stock in the fist (VeilController.UpdateWhipHandle): no ink over it.
                if (whipForm && t >= whipButt - 0.01f && t <= whipStockEnd + 0.004f) alphaAt[i] *= 1f - whipHandleShown;
            }

            // A flame's ripple runs back along the loose cloth (from the neck, growing towards the tail).
            for (int i = 0; i < samples; i++)
                centre[i] += normalAt[i] * (FlameRipple((float)i / (samples - 1), time) * (1f - hardAt[i]));

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
                float lastW = 0f, lastA = 0f, lastHard = 0f, lastFront = 0f;
                // As a weapon, each strand ends somewhere along the blade, where its flame tongue peels off.
                // (On a bow every strand runs the whole way round, so both limbs are equally full.)
                // (every layer burns all round the shield, and runs the whole rope: braided all the way to the ring)
                float end = StrandEnd(st, main, bow || shield || rope, weaponness, gripFraction);
                // Keep a pointed weapon's tip clean: the braided strands' tongues flare before the point.
                if (!main && Pointed(veil.ShapeForm)) end = Mathf.Min(end, Mathf.Lerp(1f, 0.82f, weaponness));
                // On a weapon the braided strands start at the hand, so the part behind it (a spear's butt) is
                // just the slim shaft, not every layer of the scarf bundled together.
                float start = branch && !bow ? Mathf.Lerp(st.start, Mathf.Max(st.start, gripFraction), weaponness) : st.start;

                // A simulated strand's course (VeilStrands) at the drawn samples, once: where it runs and which way.
                int si = s < MainStrand ? s : s - 1;
                bool simulated = strands != null && !main && weaponness < 0.999f;
                // A tongue runs only part of the scarf on as many samples as the band: every other one is enough, the
                // ones between are laid half way (FillBetween).
                int step = branch ? 2 : 1;
                float defStart = 0f, defEnd = 1f;
                if (simulated)
                {
                    var def = strands.Defs[si];
                    defStart = def.start; defEnd = def.end;
                    for (int i = 0; i < samples; i++)
                    {
                        if (Skipped(i, step, samples)) continue;
                        simPt[i] = strands.At(si, Mathf.InverseLerp(defStart, defEnd, Mathf.Lerp(start, end, (float)i / (samples - 1))));
                    }
                    for (int i = 0; i < samples; i++)
                    {
                        if (Skipped(i, step, samples)) continue;
                        Vector3 run = simPt[Mathf.Min(i + step, samples - 1)] - simPt[Mathf.Max(i - step, 0)];
                        run.z = 0f;
                        simRun[i] = run;
                    }
                }
                float strandTwist = branch ? st.phase * 0.25f * turnFacing : 0f;
                int baseF = s * perStrand;
                float lastTwist = 0f, lastCup = 0f;

                // 1) Body: follows the scarf from `start` to `end`.
                for (int i = 0; i < samples; i++)
                {
                    if (Skipped(i, step, samples)) continue;
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
                    // A real helix: round the blade in depth too, in front of it and behind it in turn (PaintOrder).
                    float braidZ = 0f;
                    if (!main)
                    {
                        float braidGain = (1f + 1.5f * front) * (1f - coiled) * (rope ? RopeBraidSwell : 1f);
                        off += WeaponBraid(f * braidScale, time, st.phase, w, hard) * braidGain;
                        braidZ = WeaponBraidDepth(f * braidScale, time, st.phase, w, hard) * braidGain;
                    }

                    Vector3 ov = nrm * off;
                    // The band itself is kept from rising over the head; the tongues further back fan as in the key
                    // art - the upper one climbs into its hook, the lower ones stay close under the band.
                    if (ov.y > 0f) ov.y *= branch ? Mathf.Lerp(0.55f, 0.5f, hard) : 0.5f;
                    else if (branch) ov.y *= Mathf.Lerp(0.55f, 1f, hard);
                    // (the dark under-layer lies behind the band: the cloth is drawn with depth now)
                    c += ov + new Vector3(0f, -w * HangBelowNeck * loose * Mathf.SmoothStep(0f, 1f, f * 4f), (s == 0 ? 0f : (s - 1) * 0.012f) * loose + braidZ);
                    // Loose, a strand is cloth of its own (VeilStrands): it runs where its simulation has it - round
                    // the band, over and under the others - from where it comes off the band; set into a weapon it
                    // is the braid drawn above.
                    if (simulated)
                    {
                        float simW = loose * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(defStart, defStart + 0.04f, f));
                        if (simW > 0f)
                        {
                            c = Vector3.Lerp(c, simPt[i], simW);
                            // its width across its own course, not the band's (across the band's, a strand turning
                            // away from it folded into spikes)
                            Vector3 run = simRun[i];
                            if (run.sqrMagnitude > 1e-8f)
                            {
                                var own = new Vector3(-run.y, run.x, 0f).normalized;
                                if (own.x * nrm.x + own.y * nrm.y < 0f) own = -own;
                                nrm = Vector3.Lerp(nrm, own, simW);
                                float nl = nrm.magnitude;
                                nrm = nl > 1e-6f ? nrm / nl : own;
                            }
                        }
                    }

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
                            c += outward.normalized * (lick * (main ? 0.05f : 0.3f) + (main ? 0f : 0.04f * set));
                        // ... and it circles the body in three dimensions: tilted, its forward half comes round in
                        // front of the hero and its rear half passes behind him (its outline in the picture unchanged).
                        c.z += ShieldRingDepth(c.x - shieldCentre.x, facing, set);
                    }

                    // Tendrils emerge from the ribbon: thin where they split, then full.
                    float sw = w * Mathf.Lerp(st.width, main ? 1f : 0.3f, hard) * burn;
                    if (!main) sw *= (1f + 1.6f * set) * (1f - 0.3f * lick); // bold tongues, broad brush strokes
                    if (branch) sw *= Mathf.Lerp(TongueWidth(u), Mathf.Lerp(0.25f, 0.51f, Mathf.SmoothStep(0f, 1f, u)), hard);
                    // a tongue grows out of the band from nothing (no square-cut start where it branches off)
                    if (branch) sw *= Mathf.Lerp(Mathf.SmoothStep(0f, 1f, u / 0.22f), 1f, hard);
                    // the key art's cloth is solid ink, not a translucent wash (layers seen through each other looked as
                    // if they passed through each other)
                    float alpha = a * Mathf.Lerp(Mathf.Lerp(st.alpha, SolidCloth, OpaqueLoose), main ? 1f : 0.7f, hard);
                    // Loose, it is one band of cloth near the neck (the concept); the tongues of smoke come out
                    // further back (the key art), and all of them as it hardens into a weapon.
                    alpha *= Mathf.Lerp(LooseLayerAlpha(s, f), 1f, hard);
                    if (branch) alpha *= Mathf.SmoothStep(0f, 1f, u * 5f) * Mathf.Lerp(1f, 0.12f, coiled);
                    if (!main) alpha = Mathf.Lerp(Mathf.Min(1f, alpha * 1.2f), alpha, hard); // fuller tongues (the sheet)

                    // a tongue of flame ripples on its own, out of step with the band
                    if (branch) c += nrm * (FlameRipple(f, time + st.phase * 0.4f) * TongueRipple * loose);
                    float twist = Mathf.Lerp(twistAt[i0], twistAt[i0 + 1], fr) + strandTwist * loose;
                    // set into a weapon the cloth is solid: a ridged blade, a round shaft, lash and limbs (the light runs
                    // round them)
                    float cup = CupDepth * loose + WeaponCup(form, f, main) * hard;
                    c = AboveFloor(sim, c, sw, FloorHold(form, loose) * keepOut);
                    if (loose > 0.01f) c = Vector3.Lerp(c, sim.OutOfObstacles(c, sw * 0.5f), loose * keepOut);   // no tongue cut off by a wall or ceiling
                    // (the dark under-layer lies behind the band's face, turning with it: kept behind it in world depth,
                    // the band's edges swung through it as it twisted and a dark and light 'centipede' showed)
                    WriteVertex(baseV + i * 2, baseF + i, c, nrm, sw, alpha, st, hard, vit, front, twist, cup, s == 0 ? UnderLayerDepth * loose : 0f);
                    if (i > 0) lastTan = c - lastC;
                    lastC = c; lastW = sw; lastA = alpha; lastHard = hard; lastFront = front; lastTwist = twist; lastCup = cup;
                }

                if (step > 1) FillBetween(baseV, baseF);
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
                float length = Mathf.Lerp(tongue * flicker, st.curlLength * curlScale * (1f - 0.6f * walled), looseEnd) * Mathf.Lerp(0.5f, 1f, reach);
                lastTan.z = 0f;
                Vector3 dir = lastTan.sqrMagnitude > 1e-10f ? lastTan.normalized : Vector3.down;
                // Flames rise: a weapon's tongues bend up and back, away from the direction of the blow.
                Vector3 flameDir = (dir * 0.35f + up * 0.9f + back * 0.35f).normalized;
                dir = Vector3.Slerp(dir, flameDir, lastHard).normalized;
                float heading = Mathf.Atan2(dir.y, dir.x);
                if (!curlsStarted) { curlHeading[s] = heading; curlTurnsNow[s] = turns; }
                curlHeading[s] = EaseAngle(curlHeading[s], heading, CurlEase, dt);
                curlTurnsNow[s] = Mathf.Lerp(curlTurnsNow[s], turns, 1f - Mathf.Exp(-CurlEase * dt));
                // (a weapon's tongue follows its blade at once: the ease is for loose cloth)
                heading = Mathf.LerpAngle(curlHeading[s] * Mathf.Rad2Deg, heading * Mathf.Rad2Deg, lastHard) * Mathf.Deg2Rad;
                curlHeading[s] = heading;
                turns = Mathf.Lerp(curlTurnsNow[s], turns, lastHard);
                BuildCurl(lastC, heading, Mathf.Lerp(turnFacing, facing, lastHard), length, turns, curlPoints, curl);
                for (int k = 1; k <= curlPoints; k++)
                {
                    float u = (float)k / curlPoints;
                    Vector3 d = curl[Mathf.Min(k + 1, curlPoints)] - curl[k - 1];
                    d.z = 0f;
                    Vector3 nrm = d.sqrMagnitude > 1e-10f ? new Vector3(-d.y, d.x, 0f).normalized : Vector3.up;
                    // Loose, a curl of smoke: it narrows to a thin line within its first third and winds on as that line
                    // into an open whorl, fading (the key art; a broad stroke wound into a whorl made a ball of wool).
                    float sw = lastW * Mathf.Lerp(1f, Mathf.Lerp(TipWidth, 0.03f, lastHard), Mathf.Pow(u, Mathf.Lerp(SmokeTaper, 0.6f, lastHard)));
                    float alpha = lastA * Mathf.Lerp(1f, Mathf.Lerp(TipAlpha, 0.55f, lastHard), u);
                    curl[k] = AboveFloor(sim, curl[k], sw, FloorHold(form, looseEnd) * keepOut);
                    if (looseEnd > 0.01f) curl[k] = Vector3.Lerp(curl[k], sim.OutOfObstacles(curl[k], sw * 0.5f), looseEnd * keepOut);
                    WriteVertex(baseV + (samples - 1 + k) * 2, baseF + samples - 1 + k, curl[k], nrm, sw, alpha, st, lastHard, vit, lastFront, lastTwist, lastCup * (1f - u), s == 0 ? UnderLayerDepth * looseEnd : 0f);
                }
                ClampFolds(vertices, uv2, baseV, perStrand);

                // 3) The middle row: the cloth bows out between its edges, and its normals follow the bow.
                int baseC = centreBase + baseF;
                for (int i = 0; i < perStrand; i++)
                {
                    int v = baseV + i * 2, cv = baseC + i;
                    CupSection(vertices[v], vertices[v + 1], faceAt[baseF + i], cupAt[baseF + i], out vertices[cv], out normals[v], out normals[v + 1]);
                    normals[cv] = faceAt[baseF + i];
                    colors[cv] = colors[v];
                    uv2[cv] = uv2[v];
                }
            }

            curlsStarted = true;

            // Loose cloth stays behind the hero: when he stops, the swinging band and its layers must not sweep
            // out in front of the body. Anything past the chest line is softly folded back (not cut).
            // Only once the hero faces one way — while turning round the scarf swings freely across.
            // (Not while it is wound round the body in a somersault: there it must go round the front too.)
            // (nor while it streams up over him in a fall, VeilSimulation.TrailTilt: cut at the chest line it stood as a flat board)
            float settleWanted = Mathf.InverseLerp(0.85f, 1f, Mathf.Abs(veil.FacingBlend)) * (1f - sim.SwirlWeight)
                                 * SettleAfterSwirl(sinceSwirl) * SettleInTilt(sim.TrailTilt);
            // eased in (out at once): it came in within a frame as he finished turning round, and the cloth still
            // swinging round in front of him jumped behind him
            settleNow = settleWanted < settleNow ? settleWanted : Mathf.MoveTowards(settleNow, settleWanted, dt * SettleRate);
            float settle = settleNow;
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

            if (viewer == null || !viewer.isActiveAndEnabled) viewer = Camera.main;
            if (viewer != null)
            {
                var vt = viewer.transform;
                PaintOrder(vertices, chunkV, chunkVs, chunkKey, order, vt.position, vt.forward, Strands.Length > 0 ? chunkKey.Length / Strands.Length : 1);
                for (int o = 0, k = 0; o < order.Length; o++)
                {
                    int ch = order[o];
                    System.Array.Copy(trisAll, chunkTri[ch], trisSorted, k, chunkTris[ch]);
                    k += chunkTris[ch];
                }
            }
            else System.Array.Copy(trisAll, trisSorted, trisAll.Length);

            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.SetTriangles(trisSorted, 0, false);
            mesh.colors = colors;
            mesh.SetUVs(1, uv2);
            mesh.RecalculateBounds();
        }

        /// <summary>
        /// How much of the neck → hand band is hidden (0..1) at veil fraction <paramref name="t"/>: nothing on loose
        /// cloth, all of it once that part has set into the weapon form; the weapon itself (past the hand) stays.
        /// </summary>
        /// <summary>How far the neck band may melt away given the weapon's morph (0..1): not before it is mostly formed.</summary>
        public static float NeckBandShaped(float morph) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.9f, morph));

        public static float NeckBandFade(float t, float handFraction, float hard)
        {
            float beforeHand = Mathf.SmoothStep(0f, 1f, (handFraction - t) / 0.05f);
            return beforeHand * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 0.95f, hard));
        }

        /// <summary>How firmly the cloth may be folded back off the chest <paramref name="since"/> s after a somersault's
        /// whirl let go: not at first (it swings back by itself instead of being crumpled), fully after ~0.9 s.</summary>
        public static float SettleAfterSwirl(float since) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.2f, 0.9f, since));

        /// <summary>How far (m) in front of the neck the loose scarf may reach (about the chest line).</summary>
        public const float FrontLimit = 0.15f;

        /// <summary>Soft fold for cloth past the front limit: grows at first, then saturates at 0.1 m.</summary>
        public static float FoldBack(float over) => over <= 0f ? over : 0.1f * (1f - Mathf.Exp(-over / 0.1f));

        /// <summary>Sideways offset of a strand braided around a weapon: a travelling helix, zero for loose cloth.</summary>
        /// <summary>The braid's depth (m) off the weapon: the other half of the helix of <see cref="WeaponBraid"/> - behind
        /// the blade (positive) as it comes back across it, in front of it on the way out.</summary>
        public static float WeaponBraidDepth(float f, float time, float phase, float width, float hard)
            => Mathf.Cos(f * 16f - time * 3.2f + phase) * width * 0.24f * hard;

        /// <summary>
        /// The weapon's cross-section, in half widths of bow (<see cref="CupSection"/>): a sword's blade is ridged down
        /// its middle and its grip is round, a spear has a round shaft and a ridged leaf-head, a whip's lash, a bow's
        /// limbs and the braided strands are round cords, the shield's ring a thick band.
        /// </summary>
        public static float WeaponCup(VeilForm form, float t, bool core)
        {
            if (!core) return RoundCup;
            switch (form)
            {
                case VeilForm.Sword:
                    VeilFormLibrary.SwordHilt(out _, out _, out float guard);
                    return t < guard ? RoundCup : BladeRidge;
                case VeilForm.Spear: return t < 0.86f ? RoundCup : BladeRidge;
                case VeilForm.Whip:
                case VeilForm.Bow: return RoundCup;
                case VeilForm.Shield: return 0.6f;
                default: return 0f;
            }
        }

        /// <summary>A round cord (shaft, lash, limb) and a blade's ridge, in half widths of bow.</summary>
        public const float RoundCup = 0.9f, BladeRidge = 0.5f;

        /// <summary>How far the shield's ring is tilted round the hero: depth (m) per metre across the picture.</summary>
        public const float ShieldTilt = 0.45f;

        /// <summary>Depth (m) of a point of the shield's ring <paramref name="across"/> m in front of (+, the way the hero
        /// faces) or behind its centre: the front half towards the camera, the rear half away, once it has set.</summary>
        public static float ShieldRingDepth(float across, float facing, float set) => -across * facing * ShieldTilt * set;

        public static float WeaponBraid(float f, float time, float phase, float width, float hard)
            => Mathf.Sin(f * 16f - time * 3.2f + phase) * width * 0.24f * hard;

        /// <summary>How much faster the braid twists along a rope pulled out to <paramref name="arcLength"/> (m) from
        /// a scarf of <paramref name="restLength"/>: its turns keep their length in metres instead of being drawn out
        /// (never slower than on the weapon itself).</summary>
        public static float RopeBraidScale(float arcLength, float restLength)
            => restLength > 1e-3f ? Mathf.Max(1f, arcLength / restLength) : 1f;

        /// <summary>The rope, in the whip's width: a little fuller than the lash, so it reads as a cord of the scarf.</summary>
        public const float RopeWidth = 0.9f;
        /// <summary>The rope's braid twists twice as tight as a weapon's and its strands swell out further round it:
        /// thin as it is, the crimson and ink strands winding round each other show along its whole length.</summary>
        public const float RopeBraidTwist = 2f, RopeBraidSwell = 1.8f;

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
            // broad, rounded tongues (a brush stroke licking out), not needle spikes
            float s = Mathf.Sin(f * 2f * Mathf.PI * 6f + phase * 2.3f - time * 8f);
            float spike = Mathf.Pow(Mathf.Max(0f, s), 2f);
            float gust = 0.55f + 0.45f * Mathf.Sin(f * 31f - time * 6.5f + seed * 17f);
            return Mathf.Clamp01(spike * gust * 1.2f);
        }

        /// <summary>Flickering thickness of the burning ring (about 0.65..1.3).</summary>
        public static float ShieldFlicker(float f, float time, float phase)
            => 0.98f + 0.2f * Mathf.Sin(f * 23f - time * 11f + phase) + 0.13f * Mathf.Sin(f * 11f + time * 6f + phase * 1.7f);

        /// <summary>How firmly a stroke of this form is kept on the floor: a whip always (it is no stiff blade), else as loose as it is.</summary>
        public static float FloorHold(VeilForm form, float loose) => form == VeilForm.Whip ? 1f : loose;

        /// <summary>
        /// Loose cloth drawn into the floor (a curl rolling down, the band's width round a node lying low) is laid on
        /// it instead: the point is lifted to half its stroke's width over the floor under it. A weapon is left alone.
        /// A point that went into the block through its side (the scarf round the corner of a ledge he dropped off)
        /// is not lifted onto the top: it is nearer the side, and lifted, pieces of the band snapped up to 0.6 m
        /// onto the ledge and lay there cut flat.
        /// </summary>
        public static Vector3 AboveFloor(VeilSimulation sim, Vector3 p, float width, float loose)
        {
            if (loose <= 0.01f) return p;
            float floor = sim.FloorUnder(p.x, p.y);
            if (float.IsNegativeInfinity(floor)) return p;
            if (p.y < floor)
            {
                Vector3 shortest = sim.OutOfObstacles(p, 0f);
                if (Mathf.Abs(shortest.x - p.x) > Mathf.Abs(shortest.y - p.y)) return p;
            }
            float lowest = floor + 0.5f * width + VeilSimulation.WallSkin * 0.5f;
            if (p.y < lowest) p.y = Mathf.Lerp(p.y, lowest, loose);
            return p;
        }

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
                    // The hilt: a knob of a pommel, a slim grip through the fist, a broad crossguard; then the blade,
                    // whose last 16% sweeps into a point.
                    VeilFormLibrary.SwordHilt(out float pommel, out float fist, out float guard);
                    float span = Mathf.Max(1e-4f, guard - pommel);
                    if (t < guard + span * 0.35f)
                    {
                        float knob = SwordPommelWidth * Mathf.Exp(-Mathf.Pow((t - pommel) / (span * 0.12f), 2f));
                        float cross = SwordGuardWidth * Mathf.Exp(-Mathf.Pow((t - guard) / (span * 0.1f), 2f));
                        float grip = t < guard ? SwordGripWidth : Mathf.Lerp(SwordGripWidth, 1f, Mathf.SmoothStep(0f, 1f, (t - guard) / (span * 0.35f)));
                        return Mathf.Max(grip, Mathf.Max(knob, cross));
                    }
                    float x = Mathf.Clamp01((t - 0.84f) / 0.16f);
                    return Mathf.Pow(1f - x, 0.7f) * (1f + 0.15f * Mathf.Sin(x * Mathf.PI));
                }
                case VeilForm.Whip:
                {
                    // The handle is a solid stock drawn on its own (VeilController.UpdateWhipHandle), not ink; the ink
                    // is the lash, leaving the stock's collar and tapering to the tip.
                    VeilFormLibrary.WhipHandleSpan(out _, out _, out float stockEnd);
                    if (t <= stockEnd) return WhipLashWidth;
                    float x = Mathf.Clamp01((t - stockEnd) / Mathf.Max(1e-3f, 1f - stockEnd));
                    return Mathf.Lerp(WhipLashWidth, 0.15f, Mathf.SmoothStep(0f, 1f, x));
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

        /// <summary>The sword's hilt, in blade widths: the grip in the fist, the pommel's knob, the crossguard.</summary>
        public const float SwordGripWidth = 0.32f, SwordPommelWidth = 0.6f, SwordGuardWidth = 2.6f;

        /// <summary>The whip, in its width: the stock of the handle, the knob at its butt, the collar, the lash.</summary>
        public const float WhipLashWidth = 0.62f;

        /// <summary>The spearhead's widest point, in shaft widths.</summary>
        public const float SpearHeadWidth = 3.2f;

        /// <summary>
        /// Where a strand ends (veil fraction) for weapon-ness 0..1. The core ribbon always runs the whole weapon to
        /// its tip, as does every strand of a bow; the other strands' flame tongues peel off along the part of the
        /// weapon in front of the hand (<paramref name="gripFraction"/>).
        /// </summary>
        public static float StrandEnd(Strand st, bool main, bool bow, float weaponness, float gripFraction)
            => Mathf.Lerp(LooseEnd(st), main || bow ? 1f : WeaponStrandEnd(st, gripFraction), weaponness);

        /// <summary>Where a layer of the loose scarf ends: every layer runs on into the whorls at the end (the sheet
        /// has curls only there - a layer ending mid-band curled up into a stray scrap on top of it).</summary>
        public static float LooseEnd(Strand st) => Mathf.Max(st.end, LooseLayersEnd);

        public const float LooseLayersEnd = 0.93f;

        /// <summary>Length (m) of the flame tongue at a weapon strand's end.</summary>
        public static float FlameTongueLength(bool main) => main ? 0.22f : 0.34f;

        /// <summary>
        /// A broad stroke must not fold over itself where its centre line bends tighter than its half width (a sharp
        /// turn into a curl drew a stray triangle): at every point the half width is limited to 0.9 of the local
        /// radius of the bend - on the loose cloth (hardness in <paramref name="data"/>.x below 0.5). Works on the strand's
        /// vertex pairs in place (left = v, right = v + 1).
        /// </summary>
        public static void ClampFolds(Vector3[] verts, Vector2[] data, int baseV, int count)
        {
            for (int k = 1; k < count - 1; k++)
            {
                int v = baseV + k * 2;
                if (data != null && data[v].x > 0.5f) continue; // a weapon keeps its own shape
                Vector3 c0 = (verts[v - 2] + verts[v - 1]) * 0.5f, c1 = (verts[v] + verts[v + 1]) * 0.5f, c2 = (verts[v + 2] + verts[v + 3]) * 0.5f;
                float ax = c1.x - c0.x, ay = c1.y - c0.y, bx = c2.x - c1.x, by = c2.y - c1.y;
                float la = Mathf.Sqrt(ax * ax + ay * ay), lb = Mathf.Sqrt(bx * bx + by * by);
                if (la < 1e-5f || lb < 1e-5f) continue;
                float turn = Mathf.Atan2(Mathf.Abs(ax * by - ay * bx), ax * bx + ay * by);
                if (turn < 1e-3f) continue;
                float radius = 0.5f * (la + lb) / turn;
                Vector3 half = (verts[v] - verts[v + 1]) * 0.5f;
                float hl = half.magnitude, limit = 0.9f * radius;
                if (hl <= limit) continue;
                half *= limit / hl;
                verts[v] = c1 + half;
                verts[v + 1] = c1 - half;
            }
        }

        /// <summary>
        /// The painter's order of the cloth's pieces: each piece (<paramref name="firstV"/>, <paramref name="countV"/> of
        /// its middle row in <paramref name="verts"/>) is as far from the viewer as its middle along the view (a later
        /// strand a little nearer, LayerBias); <paramref name="order"/> comes out farthest first. Sorted in place by
        /// insertion: from one frame to the next it is nearly sorted already.
        /// </summary>
        public static void PaintOrder(Vector3[] verts, int[] firstV, int[] countV, float[] key, int[] order, Vector3 eye, Vector3 forward, int chunksPerStrand)
        {
            for (int ch = 0; ch < key.Length; ch++)
            {
                float sum = 0f;
                int a = firstV[ch], n = countV[ch];
                for (int v = a; v < a + n; v++)
                {
                    var d = verts[v];
                    sum += (d.x - eye.x) * forward.x + (d.y - eye.y) * forward.y + (d.z - eye.z) * forward.z;
                }
                key[ch] = sum / Mathf.Max(1, n) - LayerBias * (ch / Mathf.Max(1, chunksPerStrand));
            }
            for (int i = 1; i < order.Length; i++)
            {
                int o = order[i];
                float k = key[o];
                int j = i - 1;
                while (j >= 0 && key[order[j]] < k) { order[j + 1] = order[j]; j--; }
                order[j + 1] = o;
            }
        }

        /// <summary>Whether sample <paramref name="i"/> is left out when every <paramref name="step"/>-th is drawn (the
        /// last one never is).</summary>
        public static bool Skipped(int i, int step, int count) => step > 1 && i % step != 0 && i != count - 1;

        /// <summary>Lays the samples left out of a tongue's body (every other one) half way between their neighbours.</summary>
        void FillBetween(int baseV, int baseF)
        {
            for (int i = 1; i < samples - 1; i += 2)
            {
                int v = baseV + i * 2, f = baseF + i;
                vertices[v] = (vertices[v - 2] + vertices[v + 2]) * 0.5f;
                vertices[v + 1] = (vertices[v - 1] + vertices[v + 3]) * 0.5f;
                var face = faceAt[f - 1] + faceAt[f + 1];
                float fl = face.magnitude;
                faceAt[f] = fl > 1e-6f ? face / fl : faceAt[f - 1];
                cupAt[f] = (cupAt[f - 1] + cupAt[f + 1]) * 0.5f;
                var col = (colors[v - 2] + colors[v + 2]) * 0.5f;
                colors[v] = colors[v + 1] = col;
                uv2[v] = uv2[v + 1] = (uv2[v - 2] + uv2[v + 2]) * 0.5f;
            }
        }

        /// <summary>
        /// The ripple (m, across the cloth) running back along the loose scarf at fraction <paramref name="t"/>: a wave of
        /// about 2.2 m that travels from the neck to the tail, nothing at the neck, its full height towards the end -
        /// the cloth licks like a flame.
        /// </summary>
        public static float FlameRipple(float t, float time)
            => RippleHeight * Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.08f, 0.85f, t)) * Mathf.Sin(2f * Mathf.PI * RippleWaves * t - RippleSpeed * time);

        public const float RippleHeight = 0.2f, RippleWaves = 0.9f, RippleSpeed = 2.6f;
        /// <summary>How much more a tongue ripples than the band.</summary>
        public const float TongueRipple = 1f;

        /// <summary>
        /// Width of a loose tongue along its length (0 where it leaves the band .. 1 at its tip), in its strand's width:
        /// it swells out of the band, is full through its first half and narrows towards its tip - a tongue of flame,
        /// not a brush stroke broadening into a whorl.
        /// </summary>
        public static float TongueWidth(float u)
            => Mathf.Lerp(0.45f, 1f, Mathf.SmoothStep(0f, 1f, u / 0.35f)) * Mathf.Lerp(1f, 0.65f, Mathf.SmoothStep(0f, 1f, (u - 0.55f) / 0.45f));

        /// <summary>A loose end's tip: its width (of the stroke's) and opacity where it ends - a hairline fading out.</summary>
        public const float TipWidth = 0.04f, TipAlpha = 0.3f;
        /// <summary>How fast a loose end narrows into its line of smoke (the power of its taper: under 1, fast).</summary>
        public const float SmokeTaper = 0.4f;

        /// <summary>How much of the loose scarf is drawn at fraction <paramref name="t"/>: none of the wrap round the neck
        /// (up to <paramref name="wrapEnd"/>), then it grows out of the back of the neck.</summary>
        public static float FromTheNeck(float t, float wrapEnd)
            => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(wrapEnd - NeckTuck, wrapEnd - NeckTuck + NeckEmerge, t));

        /// <summary>Over what fraction of the scarf it grows out of the back of the neck, and how far before the wrap's
        /// end it starts (tucked under the collar: no gap between the collar and the scarf).</summary>
        public const float NeckEmerge = 0.05f, NeckTuck = 0.03f;

        /// <summary>Width factor of the loose scarf at <paramref name="t"/> as it slips out from behind the collar: gathered
        /// to NeckGatherWidth there (no thick slab stuck to the neck), opening out over the next ~0.4 m.</summary>
        public static float NeckGather(float t, float wrapEnd)
            => Mathf.Lerp(NeckGatherWidth, 1f, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(wrapEnd - NeckTuck, wrapEnd + 0.12f, t)));

        public const float NeckGatherWidth = 0.3f;

        /// <summary>How much of its layer's own tint the loose cloth shows (a weapon all of it).</summary>
        public const float LooseTintContrast = 0.35f;

        /// <summary>How firmly the drawn loose cloth is put out of walls while the scarf is tilted by <paramref name="tilt"/> (radians) into a fall.</summary>
        public static float KeepOutInTilt(float tilt) => 1f - Mathf.SmoothStep(0f, 1f, Mathf.Abs(tilt) / 0.6f);

        /// <summary>How wound up the end whorls stay while the scarf is tilted by <paramref name="tilt"/> (radians) into a fall.</summary>
        public static float UnrollInTilt(float tilt) => 1f - 0.8f * Mathf.SmoothStep(0f, 1f, Mathf.Abs(tilt) / 0.9f);

        /// <summary>How much of the fold behind the chest is kept while the scarf is tilted by <paramref name="tilt"/> (radians).</summary>
        public static float SettleInTilt(float tilt) => 1f - Mathf.SmoothStep(0f, 1f, Mathf.Abs(tilt) / 0.5f);

        /// <summary>Eases angle <paramref name="from"/> (radians) towards <paramref name="to"/> at <paramref name="rate"/>
        /// per second, the short way round.</summary>
        public static float EaseAngle(float from, float to, float rate, float dt)
            => from + Mathf.DeltaAngle(from * Mathf.Rad2Deg, to * Mathf.Rad2Deg) * Mathf.Deg2Rad * (1f - Mathf.Exp(-rate * dt));

        /// <summary>How quickly (1/s) a loose end's whorl turns to where its strand heads.</summary>
        public const float CurlEase = 5f;

        /// <summary>How fast (per second) the fold back behind the chest comes in once he faces one way.</summary>
        public const float SettleRate = 1.5f;

        /// <summary>How far (m) behind the band its dark under-layer lies.</summary>
        public const float UnderLayerDepth = 0.1f;

        /// <summary>How opaque every layer of the loose cloth is (the key art: solid ink), and how far its own opacity
        /// gives way to that.</summary>
        public const float SolidCloth = 0.97f, OpaqueLoose = 1f;

        void WriteVertex(int v, int f, Vector3 c, Vector3 nrm, float width, float alpha, Strand st, float hard, float vit, float glow, float twist, float cup, float behind)
        {
            float cs = Mathf.Cos(twist), sn = Mathf.Sin(twist), hw = width * 0.5f;
            // behind the face of the cloth (its face looks towards the camera at no twist)
            c -= new Vector3(nrm.x * sn, nrm.y * sn, -cs) * behind;
            var side = new Vector3(nrm.x * cs * hw, nrm.y * cs * hw, sn * hw);   // RibbonSide * half width
            vertices[v] = c + side;
            vertices[v + 1] = c - side;
            faceAt[f] = new Vector3(nrm.x * sn, nrm.y * sn, -cs);                 // RibbonFace
            cupAt[f] = cup;
            // loose, the layers are one cloth: their tints only hint (stacked at full contrast they read as bands)
            var col = Color.Lerp(Color.white, st.tint, Mathf.Lerp(LooseTintContrast, 1f, hard)) * (1f + 0.9f * glow);
            col.a = alpha;
            col.r *= Mathf.Lerp(0.6f, 1f, vit);
            colors[v] = colors[v + 1] = col;
            uv2[v] = uv2[v + 1] = new Vector2(hard, st.seed);
        }
    }
}
