using System;
using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.Veil
{
    [Serializable]
    public class VeilSettings
    {
        [Header("Cloth")]
        [Min(3)] public int nodeCount = 35;
        [Tooltip("The scarf's length (m): round the neck (WrapNodes segments), then 3.2 m back from it in its animation sheet, plus the curls.")]
        public float length = 3.5f;
        [Range(1, 20)] public int constraintIterations = 14;
        [Range(0.9f, 1f)] public float damping = 0.985f;
        public float airDrag = 4.5f;
        public float gravityScale = 0.08f;
        public float depthOffset = 0.12f;
        public float depthWobble = 0.08f;
        [Tooltip("How far (m) in front of the hero a hardened weapon is held.")]
        public float weaponDepth = 0.3f;

        [Header("Depth (3D cloth)")]
        [Tooltip("Pull (1/s²) of the loose cloth back towards its depth just behind the hero: it may swing round the body and out of the picture plane, but its side view stays the scarf's.")]
        public float depthStiffness = 30f;
        [Tooltip("Damping (1/s) of the loose cloth's motion in depth.")]
        public float depthDamping = 3f;
        [Tooltip("A wandering push (m/s²) in depth, growing towards the tail: the cloth lives in three dimensions.")]
        public float depthFlutter = 3f;
        [Tooltip("Furthest (m) the loose cloth may stray in depth from its place behind the hero.")]
        public float maxDepth = 0.7f;
        [Tooltip("Radius (m) of the scarf's wrap round the neck.")]
        public float neckRadius = 0.085f;
        [Tooltip("How many segments of the scarf are wound round the neck (from the throat, round the side towards the camera, to the back).")]
        [Min(0)] public int wrapNodes = 3;
        [Tooltip("How far (m) the cloth stays off the body it falls against.")]
        public float bodySkin = 0.03f;

        [Header("Vortex shedding (Karman street)")]
        public float strouhal = 0.21f;
        public float bodyDiameter = 0.45f;
        public float idleShedFrequency = 0.8f;
        public float circulationGain = 1.6f;
        public float idleCirculation = 0.9f;
        public float coreRadius = 0.2f;
        public float viscosity = 0.03f;
        public float vortexLifetime = 1.8f;
        [Min(1)] public int maxVortices = 40;
        public float lateralShedOffset = 0.3f;
        [Tooltip("Body speed (m/s) above which shedding gets no stronger, so falls and dashes don't throw the scarf around.")]
        public float maxShedSpeed = 8f;

        [Header("Life (a scarf with a will of its own, not a cape)")]
        [Tooltip("The run wind is felt at most this strongly (m/s), so the scarf trails without stretching into a sheet.")]
        public float maxRelativeWind = 1.2f;
        [Tooltip("Serpentine undulation travelling from the neck to the tip (acceleration, m/s²).")]
        public float undulation = 7f;
        public float undulationWaves = 1.6f;
        public float undulationSpeed = 3.2f;
        [Tooltip("Extra undulation per m/s of the hero's speed.")]
        public float undulationPerSpeed = 2.2f;
        [Tooltip("Upward drift of the tail, like smoke rising.")]
        public float buoyancy = 1.4f;
        [Tooltip("The tip slowly curls into a loop.")]
        public float tipCurl = 5f;
        [Tooltip("Magical breeze (m/s) that keeps the scarf floating behind the hero even at rest (key art).")]
        public float drift = 1.4f;
        [Tooltip("Upward part of the breeze (m/s).")]
        public float driftLift = 0.1f;
        [Tooltip("Weightless cloth: its vertical speed relative to the hero is limited to this (m/s), so jumps don't fling it up and landings don't slam it down. 0 = off.")]
        public float maxRelativeFall = 2.5f;

        [Header("Rest shape (the scarf's animation sheet, VeilRestShape)")]
        [Tooltip("Pull (1/s²) of the loose cloth towards the sheet's S-shaped centre line behind the hero.")]
        public float restShapeStiffness = 120f;
        [Tooltip("Damping (1/s) of the cloth's motion relative to the hero while it holds the sheet's shape.")]
        public float restShapeDamping = 7f;

        [Header("Hardening (weapon forms)")]
        public float hardenRate = 45f;

        [Header("Simulation")]
        public float maxSubstep = 1f / 120f;
        public float teleportDistance = 5f;
    }

    public struct Vortex
    {
        public Vector2 position;
        public float circulation;
        public float age;
        public float lifetime;
        public float coreRadius0;
        public float viscosity;

        /// <summary>Fades in quickly (no popping) and out linearly over its lifetime.</summary>
        public float Fade => Mathf.Clamp01(age / 0.08f) * (1f - Mathf.Clamp01(age / lifetime));
        public float Strength => circulation * Fade;
        /// <summary>Lamb-Oseen core grows by viscous diffusion: rc² = rc0² + 4νt.</summary>
        public float CoreRadius => Mathf.Sqrt(coreRadius0 * coreRadius0 + 4f * viscosity * age);
    }

    /// <summary>
    /// The magic scarf: a Verlet ribbon pushed around by a 2D Kármán vortex street that the
    /// hero's body sheds as it moves through the air. It is cloth in three dimensions: wound round the neck, it swings
    /// round the body and out of the picture plane and falls against the body (capsules round its bones), pulled back
    /// towards its place just behind the hero. Weapon forms "harden" the ribbon by
    /// pulling nodes towards a target shape while the vortices still make it ripple.
    /// Pure C#, no MonoBehaviour, fully deterministic and testable.
    /// </summary>
    public sealed class VeilSimulation
    {
        static readonly Vector3 Gravity = new Vector3(0f, -9.81f, 0f);

        public VeilSettings Settings { get; }
        public int Count { get; }
        public Vector3[] Positions { get; }
        public Vector3[] Targets { get; }
        /// <summary>Rest length of segment i → i+1 for the hardened shape.</summary>
        public float[] TargetRest { get; }
        public float FlowRestLength { get; }

        public float Hardness { get; set; }
        /// <summary>
        /// Optional per-node hardness (0..1). When <see cref="UseNodeHardness"/> is set, hardening can
        /// travel along the ribbon as a wave instead of snapping all at once.
        /// </summary>
        public float[] NodeHardness { get; private set; }
        public bool UseNodeHardness { get; set; }
        /// <summary>Optional path the cloth is led along (world space) and how firmly (0..1) per node: a weapon melting back.</summary>
        public Vector3[] GuideTargets { get; private set; }
        public float[] GuideWeight { get; private set; }
        public Vector2 Wind { get; set; }

        /// <summary>0 = cloth behind the hero, 1 = weapon in front of the hero, by node hardness.</summary>
        public static float WeaponDepthBlend(float hardness) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.25f, 0.7f, hardness));

        float H(int i) => UseNodeHardness ? Mathf.Clamp01(NodeHardness[i]) : Mathf.Clamp01(Hardness);
        /// <summary>Slow magical drift of the air around the hero (e.g. "behind and slightly up").</summary>
        public Vector2 AmbientFlow { get; set; }
        public Vector3 AnchorVelocity { get; private set; }
        /// <summary>Extra acceleration (m/s²) on the loose cloth, growing towards the tail (e.g. a weapon melting back).</summary>
        public Vector2 Breath { get; set; }
        /// <summary>Which way (-1/+1 in x) the magical breeze blows; 0 = no breeze. Set to minus the hero's facing.</summary>
        public float DriftDirection { get; set; }
        /// <summary>
        /// A whirl of air round a point (the somersault): the loose cloth is carried round <see cref="SwirlCenter"/>
        /// at <see cref="SwirlSpeed"/> (rad/s, + = anticlockwise) and drawn onto a ring of <see cref="SwirlRadius"/>,
        /// so it winds round the body. <see cref="SwirlWeight"/> 0..1 fades it in and out.
        /// </summary>
        public Vector2 SwirlCenter { get; set; }
        public float SwirlSpeed { get; set; }
        public float SwirlRadius { get; set; } = 0.75f;
        public float SwirlWeight { get; set; }

        /// <summary>
        /// How firmly (0..1) the loose cloth is kept behind his chest line, <see cref="FrontReach"/> m in front of the
        /// neck the way <see cref="FrontFacing"/> (+1/-1) he faces: once he faces one way the cloth swinging out in front
        /// of him is stopped there and drapes back, as cloth meeting his chest would.
        /// </summary>
        public float FrontHold { get; set; }
        public float FrontFacing { get; set; } = 1f;
        public const float FrontReach = 0.15f;
        /// <summary>The span of the veil (fractions) held in the fist, and how firmly (0..1): it sits exactly on the
        /// weapon's shape from the moment of the cast, and a little behind the palm so the fist is drawn over it.</summary>
        public float HeldFrom { get; set; }
        public float HeldTo { get; set; }
        public float HeldWeight { get; set; }
        /// <summary>The loose scarf's place for node <paramref name="i"/> (the world's x and y): the line it is laid
        /// along from the neck this step and led to - level behind him, tilted up in a fall, round a wall.</summary>
        public Vector2 PlaceOf(int i) => shapeAt[i];
        /// <summary>0..1 how firmly the rest of the weapon, beyond <see cref="HeldTo"/>, is held on its line as well
        /// (the whip: thrown, fallen slack, lashed round a ring - its line is laid for it, the cloth's springs and the
        /// scarf's own shape have no say in it).</summary>
        public float HeldBeyond { get; set; }

        /// <summary>The set weapon's turn through depth (radians, + far end behind the hero) about
        /// <see cref="WeaponPivot"/>: a blow sweeps through the depth of the scene (VeilWeaponPose.SweepTilt).</summary>
        public float WeaponTilt { get; set; }
        /// <summary>Where the weapon is held (the palm), the point it turns about through depth.</summary>
        public Vector3 WeaponPivot { get; set; }

        /// <summary>
        /// Depth (z) of the palm the weapon is held in, or NaN for none: the weapon stands in the hand's depth. Set at a
        /// fixed 0.3 m before the neck, it was up to 0.36 m off the real palm - seen from the side it looked held, from
        /// three-quarters the sword started at the waist and the spear ran through the body.
        /// </summary>
        public float WeaponPlane { get; set; } = float.NaN;

        /// <summary>The depth the weapon is held at for a palm at <paramref name="palmZ"/> by a neck at <paramref name="neckZ"/>:
        /// just behind the fingers, but never more than <see cref="WeaponBehindNeck"/> behind the body's plane (the torso
        /// would hide it from the game's side view).</summary>
        public static float WeaponPlaneFor(float palmZ, float neckZ) => Mathf.Min(palmZ + 0.015f, neckZ + WeaponBehindNeck);

        /// <summary>How far (m) behind the neck's plane a weapon may follow a hand drawn back behind the body.</summary>
        public const float WeaponBehindNeck = 0.05f;

        /// <summary>Depth (m) off the weapon's plane of a point <paramref name="p"/> of it turned by
        /// <paramref name="tilt"/> about <paramref name="pivot"/>: its distance from the hand in the picture, into depth.</summary>
        public static float SweepDepth(Vector3 p, Vector3 pivot, float tilt)
        {
            if (tilt == 0f) return 0f;
            float dx = p.x - pivot.x, dy = p.y - pivot.y;
            return Mathf.Sqrt(dx * dx + dy * dy) * Mathf.Sin(tilt);
        }
        /// <summary>How far (m) behind the weapon's plane the held part lies, so the fist in front of it covers it.</summary>
        public const float HeldTuck = 0.08f;

        /// <summary>The sharpest bend (degrees) the loose cloth makes at a node before it is straightened.</summary>
        public const float MaxKink = 60f;

        /// <summary>The sharpest bend (degrees) the loose cloth makes at a node, held by the bending constraint.</summary>
        public const float MaxBend = 35f;

        /// <summary>Holds node <paramref name="i"/> at least <paramref name="least"/> m from the one after next, in the
        /// picture plane (the bending constraint): the free ends of the pair take it, as they are free.</summary>
        void HoldBend(int i, float least)
        {
            float wa = Free(i) * (1f - H(i)), wb = Free(i + 2) * (1f - H(i + 2));
            float sum = wa + wb;
            if (sum < 1e-4f) return;
            float dx = Positions[i + 2].x - Positions[i].x, dy = Positions[i + 2].y - Positions[i].y;
            float len = Mathf.Sqrt(dx * dx + dy * dy);
            if (len >= least || len < 1e-6f) return;
            float push = (least - len) / len;
            Positions[i].x -= dx * push * (wa / sum);
            Positions[i].y -= dy * push * (wa / sum);
            Positions[i + 2].x += dx * push * (wb / sum);
            Positions[i + 2].y += dy * push * (wb / sum);
        }

        /// <summary>The least distance (m) between a node and the one after next, for segments <paramref name="segment"/>
        /// m long: two segments bent by MaxBend at the node between them span that much.</summary>
        public static float SkipLength(float segment) => 2f * segment * Mathf.Cos(0.5f * MaxBend * Mathf.Deg2Rad);

        /// <summary>How far (0..1) the bend at <paramref name="b"/> between <paramref name="a"/> and <paramref name="c"/> goes
        /// past MaxKink (0 within it, 1 folded right back on itself).</summary>
        public static float KinkExcess(Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 u = b - a, v = c - b;
            float lu = u.magnitude, lv = v.magnitude;
            if (lu < 1e-6f || lv < 1e-6f) return 0f;
            float cos = Vector3.Dot(u, v) / (lu * lv);
            float limit = Mathf.Cos(MaxKink * Mathf.Deg2Rad);
            return cos >= limit ? 0f : (limit - cos) / (limit + 1f);
        }
        /// <summary>
        /// Which way the scarf trails round the body while it is wound in the somersault (+1 anticlockwise,
        /// -1 clockwise; 0 = against <see cref="SwirlSpeed"/>). The body turns one way, the cloth lags the other.
        /// </summary>
        public float WrapTrail { get; set; }
        /// <summary>How far (m) the wrapped scarf's spiral widens from the neck to its end.</summary>
        public const float WrapWiden = 0.25f;
        /// <summary>How fast (1/s) the loose cloth is laid onto the wrap while it is wound.</summary>
        public const float WrapRate = 38f;

        /// <summary>
        /// Where node at <paramref name="along"/> metres from the neck lies when the scarf is wound round the body:
        /// a spiral round <paramref name="centre"/> that starts at the neck and winds on, trailing the turn, widening
        /// by <see cref="WrapWiden"/> to its end.
        /// </summary>
        /// <summary>0..1: how much harder the scarf is pulled back into its shape <paramref name="since"/> s after the
        /// somersault let go (1 at once, gone after 0.5 s; 0 if it hasn't been wound).</summary>
        public static float UnwindBoost(float since) => since >= 0.5f ? 0f : 1f - Mathf.SmoothStep(0f, 1f, since / 0.5f);

        /// <summary>0..1 how rigidly a node of hardness <paramref name="hardness"/> is held on the weapon's shape: not at
        /// all while it is cloth, fully once set.</summary>
        public static float RigidHold(float hardness) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.77f, 0.9f, hardness)); // (the whip, set at 0.75, stays supple)

        /// <summary>
        /// One piece of the scarf's line as it winds into the somersault's ring: the sheet's piece <paramref name="sheet"/>
        /// turned towards the ring's piece <paramref name="ring"/> by <paramref name="wound"/> (0..1) of the angle
        /// between them, its length likewise. <paramref name="turn"/> carries that angle on from the piece before, so
        /// the whole line winds the same way round. The scarf curls up into the ring and out of it like a fern: one
        /// line of its own length all the way. (Each point drawn part of the way from its place in the sheet to its
        /// place on the ring, the line between the two shapes was neither: it shrank, looped and knotted.)
        /// </summary>
        public static Vector2 WoundStep(Vector2 sheet, Vector2 ring, float wound, ref float turn)
        {
            float ls = sheet.magnitude, lr = ring.magnitude;
            if (ls < 1e-9f || lr < 1e-9f) return Vector2.Lerp(sheet, ring, wound);
            float from = Mathf.Atan2(sheet.y, sheet.x);
            float raw = Mathf.Atan2(ring.y, ring.x) - from;
            turn += Mathf.DeltaAngle(turn * Mathf.Rad2Deg, raw * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            float angle = from + turn * wound;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Mathf.Lerp(ls, lr, wound);
        }

        float windTurn;     // radians from the sheet's first piece to the ring's, followed from step to step
        bool winding;

        /// <summary>
        /// Lays out the scarf's place for this step, piece by piece from the neck <paramref name="a"/>: the sheet
        /// streaming on the breeze (<see cref="SheetPiece"/>), wound into the somersault's ring (<see cref="WoundStep"/>),
        /// turned aside along walls (<see cref="LayPiece"/>). One line of the scarf's own length whatever he does:
        /// the cloth is held and led to it (<c>shapeAt</c>).
        /// </summary>
        void LayShape(Vector3 a)
        {
            var s = Settings;
            float length = s.length * LengthScale;
            float trail = WrapTrail != 0f ? WrapTrail : -Mathf.Sign(SwirlSpeed);
            float wound = trail != 0f ? SwirlWeight : 0f;
            Vector2 ringBefore = wound > 0.001f ? WrapPoint(SwirlCenter, a, 0f, length, trail) : Vector2.zero;
            float spineAlong = 0f, spineHeight = VeilRestShape.Height(0f, time);
            float turn = 0f;
            bool first = true;
            int kept = flowNewest;
            FlowAt(0f, ref kept, out nodeAngle[0], out nodeSide[0]);
            // 1) Each piece's own direction and length.
            for (int i = 1; i < Count; i++)
            {
                float f = (float)i / (Count - 1);
                // what the neck did a moment ago: its direction runs down the scarf (TurnTravel to the tip)
                FlowAt(FoldReach(f) * TurnTravel, ref kept, out nodeAngle[i], out nodeSide[i]);
                // (measured from the end of the wrap round the neck: the sheet is the part that streams off it; its S
                // takes up length, so it reaches back RestReach of its length)
                float dAlong = Mathf.Max(0f, f * s.length - WrapLength) * LengthScale;
                float sheetHeight = VeilRestShape.Height(dAlong, time);
                float run = dAlong * RestReach - spineAlong, rise = sheetHeight - spineHeight;
                // (the part still streaming the old way as he turns round lies over the fold: its S is calmed, or
                // its waves hung down across the part already turned and the scarf read as a figure of eight)
                Vector2 step = SheetPiece(run, rise, nodeAngle[i], nodeSide[i] * FoldCalm(nodeSide[i] * nodeSide[0]));
                sheetLen[i] = step.magnitude;
                sheetLift[i] = sheetLen[i] > 1e-9f ? Mathf.Atan2(rise, run) : 0f;
                spineAlong = dAlong * RestReach;
                spineHeight = sheetHeight;
                if (wound > 0.001f)
                {
                    Vector2 ring = WrapPoint(SwirlCenter, a, dAlong, length, trail);
                    Vector2 piece = ring - ringBefore;
                    ringBefore = ring;
                    if (first && step.sqrMagnitude > 1e-12f && piece.sqrMagnitude > 1e-12f)
                    {
                        // Which way round the first piece turns: the nearest as the winding starts and while it is
                        // fully wound (there it makes no difference); in between it is followed from step to step,
                        // so the line never changes its mind halfway and flips over.
                        float raw = Mathf.Atan2(piece.y, piece.x) - Mathf.Atan2(step.y, step.x);
                        if (!winding || wound >= 0.999f) windTurn = Mathf.DeltaAngle(0f, raw * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                        else windTurn += Mathf.DeltaAngle(windTurn * Mathf.Rad2Deg, raw * Mathf.Rad2Deg) * Mathf.Deg2Rad;
                        winding = true;
                        turn = windTurn;
                        first = false;
                    }
                    step = WoundStep(step, piece, wound, ref turn);
                }
                pieceLen[i] = step.magnitude;
                pieceAngle[i] = pieceLen[i] > 1e-9f ? Mathf.Atan2(step.y, step.x) : nodeAngle[i];
            }
            if (wound <= 0.001f) winding = false;

            // 2) Laid on from the neck, turned aside where a wall is in the way.
            shapeAt[0] = a;
            Vector2 spine = Vector2.zero;
            laidTurn = 0f;
            laidAny = false;
            laidFirstNow = 0f;
            pieceTurn[0] = 0f;
            for (int i = 1; i < Count; i++)
            {
                spine = LayPiece(a, spine, new Vector2(Mathf.Cos(pieceAngle[i]), Mathf.Sin(pieceAngle[i])) * pieceLen[i], i);
                pieceTurn[i] = pieceLen[i] > 1e-9f ? laidTurn : pieceTurn[i - 1];
                shapeAt[i] = new Vector3(a.x + spine.x, a.y + spine.y, a.z);
            }

            // 3) Rounded: where the line was turned aside all at once - a corner - the pieces before it take up the
            // turn a little each, so it comes to the wall in a curve and runs along it. (A corner in its place is a
            // fold in the band: its edges crossed there and the tongues beside it splayed.)
            if (laidRound)
            {
                for (int i = Count - 2; i >= 1; i--)
                    pieceTurn[i] = Mathf.Clamp(pieceTurn[i], pieceTurn[i + 1] - LaidEase, pieceTurn[i + 1] + LaidEase);
                spine = Vector2.zero;
                laidTurn = 0f;
                for (int i = 1; i < Count; i++)
                {
                    float angle = pieceAngle[i] + pieceTurn[i];
                    spine = LayPiece(a, spine, new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * pieceLen[i]);
                    shapeAt[i] = new Vector3(a.x + spine.x, a.y + spine.y, a.z);
                }
            }
            laidFirst = laidFirstNow;
            shapeFree[0] = 1f;
            for (int i = 1; i < Count; i++) shapeFree[i] = ShapeOutsideWalls(DepthInObstacles(shapeAt[i]));
        }

        float[] sheetLen, sheetLift;               // per piece: the sheet's own length and its rise (radians) in the S
        float[] pieceLen, pieceAngle, pieceTurn;   // per piece: its length, its own direction, how far it was turned aside

        /// <summary>
        /// The change of tilt (radians) at which the sheet, streaming towards <paramref name="side"/> tilted by
        /// <paramref name="tilt"/>, lies clear of every wall, ledge and ceiling from the neck <paramref name="a"/> to its
        /// tip. It starts from the change it has (<paramref name="now"/>): clear there, it comes back a step towards
        /// <paramref name="hint"/> (its own tilt, or the rise a wall behind him calls for) if that is clear too; in the
        /// way there, it takes the nearest tilt that is clear, within <see cref="TiltSteps"/> steps - or stays.
        ///
        /// The scarf streams where there is room: up a wall behind him, dipping under a block that hangs over its
        /// path, lifting over a step. (Laid straight on, it was only turned aside at the obstacle itself: under the
        /// corner of a block its S was pressed into a Z. And it keeps to the tilt it has while that is clear: picked
        /// afresh each moment, the tilt leapt between up the wall and under it with every wave of its S.)
        /// </summary>
        public float FreeTilt(Vector3 a, float side, float tilt, float hint, float now)
        {
            if (Obstacles.Count == 0) return hint;
            int all = Count - 1;
            int clearNow = ClearPieces(a, side, SheetTotal(tilt, now));
            if (clearNow >= all)
            {
                float back = Mathf.MoveTowards(now, hint, TiltSearch);
                return back != now && ClearPieces(a, side, SheetTotal(tilt, back)) >= all ? back : now;
            }
            // In the way where it is. A wall right behind him: the rise that calls for, if that is clear.
            if (hint != now && ClearPieces(a, side, SheetTotal(tilt, hint)) >= all) return hint;
            for (int k = 1; k <= 2 * TiltSteps; k++)
            {
                // a step up from where it is, a step down, two up... (only so far: it does not leap from under a
                // ceiling to up the wall beside it)
                float c = now + ((k & 1) == 1 ? 1f : -1f) * ((k + 1) / 2) * TiltSearch;
                if (tilt + c > MaxFreeTilt || tilt + c < -MaxDip) continue;
                if (ClearPieces(a, side, tilt + c) >= all) return c;
            }
            // No tilt near clears it all: it stays (the line is turned aside along what is in its way, LayPiece) -
            // or rises a step towards what a wall behind him calls for.
            return hint > now ? Mathf.Min(hint, now + TiltSearch) : now;
        }

        /// <summary>The sheet's tilt (radians): <paramref name="fall"/>, its tilt into a fall, changed by
        /// <paramref name="free"/> to clear the walls - never past the steepest it rises or the farthest it dips.</summary>
        public static float SheetTotal(float fall, float free)
            => Mathf.Clamp(fall + free, Mathf.Min(fall, -MaxDip), Mathf.Max(fall, MaxFreeTilt));

        /// <summary>How many pieces of the sheet, streaming towards <paramref name="side"/> at <paramref name="tilt"/>,
        /// lie clear of the obstacles (with <see cref="TiltClear"/> to spare) before the first that does not.</summary>
        int ClearPieces(Vector3 a, float side, float tilt)
        {
            float direction = FlowGoal(side, tilt), turn = side < 0f ? -1f : 1f;
            float x = a.x, y = a.y;
            for (int i = 1; i < Count; i++)
            {
                if (sheetLen[i] <= 1e-9f) continue;
                float angle = direction + sheetLift[i] * turn;
                x += Mathf.Cos(angle) * sheetLen[i];
                y += Mathf.Sin(angle) * sheetLen[i];
                if (InObstacle(x, y, WallSkin + TiltClear)) return i - 1;
            }
            return Count - 1;
        }

        /// <summary>The step (radians) and the number of steps either way of the search for a free tilt, the room (m) the
        /// sheet is given past a wall, and the farthest (radians) it dips below level.</summary>
        public const float TiltSearch = 0.06f, TiltClear = 0.08f, MaxDip = 0.5f;
        /// <summary>The steepest (radians) the sheet stands to clear a wall: all but upright, as he falls close past a
        /// face (<see cref="MaxWallTilt"/> is where it starts looking with his back to a wall).</summary>
        public const float MaxFreeTilt = 1.5f;
        const int TiltSteps = 10;

        /// <summary>The sheet's tilt (radians, + = the tail up) towards <paramref name="side"/>: into a fall, and changed
        /// by what clears the walls there (<see cref="FreeTilt"/>).</summary>
        float SheetTilt(float side)
        {
            return SheetTotal(Mathf.Max(TrailTilt, 0f), side < 0f ? wallTiltMinus : wallTiltPlus);
        }

        public static Vector2 WrapPoint(Vector2 centre, Vector2 neck, float along, float length, float trail)
        {
            Vector2 n0 = neck - centre;
            float r0 = Mathf.Max(n0.magnitude, 0.3f);
            float r = r0 + WrapWiden * Mathf.Clamp01(along / Mathf.Max(length, 1e-3f));
            float angle = Mathf.Atan2(n0.y, n0.x) + Mathf.Sign(trail) * along / (0.5f * (r0 + r));
            return centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * r;
        }
        /// <summary>
        /// 0..1: how firmly the loose cloth keeps the shape of its animation sheet (VeilRestShape): streaming back
        /// level in an S behind the hero (towards <see cref="DriftDirection"/>). Full at rest, eased off when the hero
        /// runs or somersaults, so the cloth's own physics shows through.
        /// </summary>
        public float RestShapeWeight { get; set; }

        /// <summary>Horizontal reach of the sheet's S per metre of scarf (its waves take up the rest).</summary>
        public const float RestReach = 0.88f;

        /// <summary>
        /// Turning round, the breeze does not flip the whole scarf at once (it crumpled round the head): the turn runs
        /// down it from the neck, the tail last, as a fold runs down a gymnast's ribbon. Seconds for it to reach the tip.
        /// It goes over the top, at the scarf's full length (squeezed across through the neck instead, it bunched into a
        /// ball round his head - worst crouched, with the floor right under it).
        /// (At 0.2 s with each point taking 0.4 s the whole scarf swung over at once like the hand of a clock, its tip at
        /// over 30 m/s: the cloth could not follow and broke up. Now a third of it is in the fold at a time - a round
        /// U of about 0.35 m radius - and the tail beyond it only slides along itself, at under 15 m/s.)
        /// </summary>
        public const float TurnTravel = 0.42f;
        /// <summary>Seconds one point of the scarf takes to swing over.</summary>
        public const float TurnBlend = 0.18f;

        /// <summary>The breeze's heading (radians from +x, pi = towards -x) for a breeze towards <paramref name="dir"/>.</summary>
        public static float HeadingOf(float dir) => dir < 0f ? Mathf.PI : 0f;

        /// <summary>
        /// How far through its run (0..1) the fold is as it reaches the point <paramref name="f"/> of the scarf: a little
        /// quicker by the neck, slower towards the tip. (The tail's pieces hang down steeply in the sheet's S and have
        /// the farthest to turn; at an even pace the tip cracked like a whip, at 30 m/s.)
        /// </summary>
        public static float FoldReach(float f) => Mathf.Pow(Mathf.Clamp01(f), FoldEase);

        public const float FoldEase = 1.4f;

        /// <summary>
        /// The direction (radians, followed round without wrapping) a turn from <paramref name="from"/> ends at, for a
        /// sheet that is to stream towards <paramref name="goal"/>: the nearer way round. Where the two ways are
        /// within <see cref="RouteTie"/> of each other (the sheet lies level: straight across) it goes over the top
        /// (<paramref name="way"/> +1: passing straight up) or underneath (-1); <paramref name="forced"/> takes that
        /// way however far it is.
        ///
        /// (The way used to be picked by how he moved - underneath as he leapt up - and kept whatever came after:
        /// falling a moment later with the sheet tilted up on both sides, the fold still hung below him and the scarf
        /// closed into a ring over his head. The nearer way never turns it by more than half a circle.)
        /// </summary>
        public static float TurnRoute(float from, float goal, int way, bool forced = false)
        {
            float near = from + Mathf.DeltaAngle(from * Mathf.Rad2Deg, goal * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            if (Mathf.Abs(near - from) < 1e-4f) return near;
            float far = near + (near > from ? -2f : 2f) * Mathf.PI;
            if (!forced && Mathf.Abs(near - from) < Mathf.PI - RouteTie) return near;
            bool nearOver = Mathf.Sin(0.5f * (from + near)) > 0f;
            return nearOver == (way > 0) ? near : far;
        }

        /// <summary>Radians: two ways round a turn closer to each other than this count as equal (see <see cref="TurnRoute"/>).</summary>
        public const float RouteTie = 0.35f;

        /// <summary>The direction (radians) the sheet streams off the neck towards <paramref name="side"/> (-1/+1 in x),
        /// tilted by <paramref name="tilt"/> (+ = the tail up, on either side).</summary>
        public static float FlowGoal(float side, float tilt) => side < 0f ? Mathf.PI - tilt : tilt;

        /// <summary>
        /// How firmly (0..1) the cloth is led along the sheet's shape <paramref name="clock"/> s into a turn: at once as
        /// it starts, all through it, and let go over a moment after (<see cref="GuideSettle"/>). The fold running down
        /// the scarf is a shape laid out point by point (see <see cref="SheetPiece"/>): led along it, the cloth goes over
        /// as one smooth curve. Left to springs and the air it lagged behind its place, each point by a different
        /// amount, and folded up sharply.
        /// </summary>
        public static float TurnGuide(float clock)
        {
            float end = TurnTravel + TurnBlend;
            if (clock < 0f || clock >= end + GuideSettle) return 0f;
            return Mathf.SmoothStep(0f, 1f, clock / GuideIn) * (1f - Mathf.SmoothStep(0f, 1f, (clock - end) / GuideSettle));
        }

        /// <summary>Seconds the lead comes in over as a turn starts and is let go over after it; how closely (1/s) the
        /// cloth is drawn to its place while led.</summary>
        public const float GuideIn = 0.05f, GuideSettle = 0.35f, GuideRate = 90f;

        /// <summary>How firmly (0..1) the cloth is being led through a turn right now (see <see cref="TurnGuide"/>).</summary>
        public float TurnLead => DriftDirection != 0f ? TurnGuide(flowClock) * (1f - SwirlWeight) : 0f;

        /// <summary>
        /// Lays one piece of the sheet's line on from <paramref name="from"/> (from the neck <paramref name="a"/>): along
        /// <paramref name="step"/>, or - where that leads into a wall, a ledge or a ceiling - turned aside by the least
        /// angle that clears it, at the same length: along its face. The pieces after it keep to that side (turning
        /// each to its own nearest side, the line zigzagged up and down a wall with every wave of its S) and come back
        /// to their own direction in a curve. So the sheet's shape runs up a wall and over the edge of a ledge as one
        /// line. (Laid
        /// straight through, each point in the block was moved out of it on its own: as he fell past a ledge the
        /// line broke at its edge - one part down the face, the next half a metre in over the top - and the cloth
        /// folded into a hairpin there.)
        /// </summary>
        public Vector2 LayPiece(Vector3 a, Vector2 from, Vector2 step, int node = -1)
        {
            float len = step.magnitude;
            if (len < 1e-9f) return from + step;
            if (Obstacles.Count == 0) { laidTurn = 0f; return from + step; }
            float angle = Mathf.Atan2(step.y, step.x);
            // the piece before was turned aside: this one comes back to its own direction a little at a time
            float ease = Mathf.MoveTowards(laidTurn, 0f, LaidEase);
            if (PieceClear(a, from, angle + ease, len))
            {
                laidTurn = ease;
                if (node >= 0) laidSide[node] = 0f;
                return from + new Vector2(Mathf.Cos(angle + ease), Mathf.Sin(angle + ease)) * len;
            }
            laidRound = true;
            // Which side it keeps to: the side the line is turned to already, else the side this piece took last step.
            // (or, for the first piece in the way, the side the line was turned to last step: crawling out of a tunnel
            // the scarf stays under its ceiling - it does not leap up the face over the mouth)
            float keep = Mathf.Abs(laidTurn) > 1e-4f ? Mathf.Sign(laidTurn) : node >= 0 ? laidSide[node] : 0f;
            if (keep == 0f && node >= 0 && !laidAny) keep = laidFirst;
            if (node >= 0 && !laidAny) laidFirstNow = 0f;
            float best = 0f, bestCost = float.MaxValue;
            for (int side = -1; side <= 1; side += 2)
                for (int k = 1; k <= LaidSteps; k++)
                {
                    float t = ease + side * k * LaidSearch;
                    if (!PieceClear(a, from, angle + t, len)) continue;
                    float blocked = t - side * LaidSearch;
                    for (int b = 0; b < 5; b++)
                    {
                        float mid = 0.5f * (blocked + t);
                        if (PieceClear(a, from, angle + mid, len)) t = mid; else blocked = mid;
                    }
                    // (the least turn; rather to the side the line is on; rather up a wall than down it - the cloth
                    // streams up like a flame: turned down a face, its tail hooked back under itself)
                    float cost = Mathf.Abs(t - ease) - (side == keep ? LaidKeep : 0f) - LaidUp * Mathf.Sin(angle + t);
                    if (cost < bestCost) { bestCost = cost; best = t; }
                    break;
                }
            if (bestCost == float.MaxValue) return from + step;   // (walled in all round)
            laidTurn = best;
            if (node >= 0)
            {
                laidSide[node] = Mathf.Sign(best);
                if (!laidAny) { laidAny = true; laidFirstNow = Mathf.Sign(best); }
            }
            return from + new Vector2(Mathf.Cos(angle + best), Mathf.Sin(angle + best)) * len;
        }

        /// <summary>Whether a piece <paramref name="len"/> m long laid on from <paramref name="from"/> (from the neck
        /// <paramref name="a"/>) at <paramref name="angle"/> ends clear of every obstacle.</summary>
        bool PieceClear(Vector3 a, Vector2 from, float angle, float len)
            => !InObstacle(a.x + from.x + Mathf.Cos(angle) * len, a.y + from.y + Mathf.Sin(angle) * len);

        /// <summary>Radians a piece comes back towards its own direction after the piece before was turned aside (so
        /// the line leaves a wall in a curve); the step (radians) and the number of steps of the search for a clear
        /// direction; how much farther (radians) a piece turns to stay on the side the line is on already; and how
        /// much farther to go up a wall rather than down it.</summary>
        public const float LaidEase = 0.3f, LaidSearch = 0.1309f, LaidKeep = 1.2f, LaidUp = 0.6f;
        const int LaidSteps = 24;

        float laidTurn;     // radians the last piece of the line being laid was turned aside
        bool laidAny;       // the line being laid has been turned aside already
        float laidFirst, laidFirstNow;   // the side (-1/+1) the line was first turned to last step / this step (0: not turned)
        float[] laidSide;   // per node: the side (-1/+1) its piece was turned to last step (0: not turned)

        bool laidRound;     // this step the sheet's line had to be laid round an obstacle
        float wallLead;     // 0..1, eased: the cloth is led along the sheet's shape past a wall, a ledge or a ceiling

        /// <summary>How closely (1/s) the cloth is drawn to its place in the sheet's shape where that is laid along a
        /// wall, up a ledge or under a ceiling, and how fast (1/s) that comes in and goes. Left to itself there the
        /// cloth was blown into the wall, went limp beyond the touch and piled up against it in a crumpled heap.</summary>
        public const float WallLeadRate = 20f, WallLeadEase = 4f;

        /// <summary>Whether the point lies in an obstacle or within <see cref="WallSkin"/> of one.</summary>
        bool InObstacle(float x, float y, float skin = WallSkin)
        {
            for (int o = 0; o < Obstacles.Count; o++)
            {
                Rect r = Obstacles[o];
                if (x > r.xMin - skin && x < r.xMax + skin && y > r.yMin - skin && y < r.yMax + skin) return true;
            }
            return false;
        }

        /// <summary>How closely (1/s) the cloth is drawn to its place in the sheet's shape as he falls or leaps (see
        /// <see cref="FallDrag"/>): led, it streams up behind him as one line and slides off a ledge after him. Left
        /// to the pull of his neck it dropped onto the ledge, was dragged over its edge and slung out in front of him.</summary>
        public const float FallLeadRate = 25f;

        /// <summary>The direction (radians) the sheet streams at <paramref name="f"/> along the scarf right now: what the
        /// neck's was a moment ago (see <see cref="TurnTravel"/>).</summary>
        public float HeadingAt(float f)
        {
            float x = Mathf.Clamp01(f) * (Count - 1);
            int i0 = Mathf.Min((int)x, Count - 2);
            return Mathf.Lerp(nodeAngle[i0], nodeAngle[i0 + 1], x - i0);
        }

        /// <summary>The side (-1..1 in x) the sheet streams to at <paramref name="f"/> along the scarf right now (see <see cref="TurnTravel"/>).</summary>
        public float DriftAt(float f)
        {
            if (DriftDirection == 0f) return 0f;
            float x = Mathf.Clamp01(f) * (Count - 1);
            int i0 = Mathf.Min((int)x, Count - 2);
            return Mathf.Lerp(nodeSide[i0], nodeSide[i0 + 1], x - i0);
        }

        // The direction the sheet streams off the neck (radians from +x, followed round without wrapping) and the side
        // it streams to (-1..1), as they were over the last moments. They run down the scarf from the neck as on a
        // ribbon: each point takes up what the neck did a moment before (FlowAt). So whatever the neck does - a turn,
        // a fall, a leap, a turn in mid-air and a fall right after - the scarf is the trace of one motion: a curve.
        // (A turn used to be worked out point by point, each from where it stood to where it was to end, and the
        // tilt into a fall was laid on the whole scarf at once on top: one upon the other they wound it into a ring.)
        const int FlowKept = 192;
        const float FlowGrain = 0.003f;
        float[] flowTime, flowAngleAt, flowSideAt;
        int flowNewest;
        float flowAngle, flowSide, flowGoal, flowFrom, sideFrom, flowClock = 99f, flowBlend = TurnBlend;
        float flowStir;             // 0..1, eased: the direction differs along the scarf (a wave is running down it)
        float[] nodeAngle, nodeSide;   // per node: the sheet's direction and side there this step

        /// <summary>How much of its S a piece keeps as the scarf turns round: all of it on the side the neck streams to
        /// (<paramref name="sides"/> = its side times the neck's, 1), <see cref="CalmS"/> of it on the other side - the
        /// part that has yet to come over.</summary>
        public static float FoldCalm(float sides) => Mathf.Lerp(CalmS, 1f, Mathf.SmoothStep(0f, 1f, 0.5f + sides));
        public const float CalmS = 0.35f;

        /// <summary>The sheet's direction and side at the neck <paramref name="ago"/> s ago. <paramref name="kept"/>
        /// walks back through what is kept: start it at <c>flowNewest</c> and ask for ever earlier moments.</summary>
        void FlowAt(float ago, ref int kept, out float angle, out float side)
        {
            float t = time - ago;
            for (int n = 1; n < FlowKept && flowTime[kept] > t; n++)
            {
                int before = (kept + FlowKept - 1) % FlowKept;
                if (flowTime[before] > flowTime[kept]) break;   // (the oldest kept)
                kept = before;
            }
            int after = kept == flowNewest ? kept : (kept + 1) % FlowKept;
            float span = flowTime[after] - flowTime[kept];
            float w = span > 1e-6f ? Mathf.Clamp01((t - flowTime[kept]) / span) : 0f;
            angle = Mathf.Lerp(flowAngleAt[kept], flowAngleAt[after], w);
            side = Mathf.Lerp(flowSideAt[kept], flowSideAt[after], w);
        }

        /// <summary>The whole scarf streams the way the neck does now (no wave running down it).</summary>
        void FillFlow()
        {
            for (int k = 0; k < FlowKept; k++)
            {
                flowTime[k] = time;
                flowAngleAt[k] = flowAngle;
                flowSideAt[k] = flowSide;
            }
            for (int i = 0; i < Count; i++)
            {
                nodeAngle[i] = flowAngle;
                nodeSide[i] = flowSide;
            }
            flowStir = 0f;
        }

        /// <summary>
        /// Moves the sheet's direction at the neck on by <paramref name="h"/> s and keeps it. Turning round, it swings
        /// over to the other side in <see cref="TurnBlend"/> s by the nearer way round (<see cref="TurnRoute"/>);
        /// otherwise it follows the tilt into a fall or up a wall.
        /// </summary>
        void StepFlow(float h, Vector3 a)
        {
            float side = DriftDirection;
            if (side != 0f)
            {
                float goal = FlowGoal(side, SheetTilt(side));
                if (side != driftTo)
                {
                    if (driftTo == 0f)
                    {
                        flowAngle = flowGoal = flowFrom = goal;
                        flowSide = sideFrom = side;
                        flowClock = 99f;
                        FillFlow();
                    }
                    else
                    {
                        // Over the top - or underneath where a ceiling is too low over him for the fold to pass and
                        // there is more room below. Under a low ceiling the fold is made tighter as well.
                        float reach = Settings.length * LengthScale * RestReach;
                        float over = RoomOver(a, 1f, reach), below = RoomOver(a, -1f, reach);
                        bool under = over < FoldRoomLeast && below > over;
                        flowFrom = flowAngle;
                        sideFrom = flowSide;
                        flowGoal = TurnRoute(flowFrom, goal, under ? -1 : 1, under);
                        flowBlend = TurnBlend * Mathf.Lerp(FoldTight, 1f, Mathf.InverseLerp(FoldRoomLeast, FoldRoom, under ? below : over));
                        flowClock = 0f;
                    }
                }
                else flowGoal += Mathf.DeltaAngle(flowGoal * Mathf.Rad2Deg, goal * Mathf.Rad2Deg) * Mathf.Deg2Rad;
            }
            driftTo = side;
            flowClock += h;
            float u = Mathf.SmoothStep(0f, 1f, flowClock / Mathf.Max(0.01f, flowBlend));
            flowAngle = Mathf.Lerp(flowFrom, flowGoal, u);
            flowSide = Mathf.Lerp(sideFrom, side, u);
            if (u >= 1f) { flowFrom = flowGoal; sideFrom = side; }
            if (time - flowTime[flowNewest] >= FlowGrain) flowNewest = (flowNewest + 1) % FlowKept;
            flowTime[flowNewest] = time;
            flowAngleAt[flowNewest] = flowAngle;
            flowSideAt[flowNewest] = flowSide;
        }

        /// <summary>
        /// One piece of the sheet's centre line, laid on from the last: at rest it runs <paramref name="run"/> m back and
        /// <paramref name="rise"/> m up (its S and its lift off the neck); here it is laid along
        /// <paramref name="direction"/> (radians), its rise turned to <paramref name="side"/> (-1..1: mirrored on
        /// the other side, flat as it stands up mid-turn). The piece keeps its length.
        ///
        /// The whole shape is laid out piece by piece like this, so it is always one line of the scarf's own length
        /// whose direction changes only gently. (Each point placed on a straight ray from the neck at its own heading,
        /// it was a spiral three times the scarf's length in a turn; its S added as a height on top of a centre line,
        /// the fold running down it was sheared and folded over on itself. Held or led to either, the cloth broke up.)
        /// </summary>
        public static Vector2 SheetPiece(float run, float rise, float direction, float side)
        {
            float length = Mathf.Sqrt(run * run + rise * rise);
            if (length < 1e-9f) return Vector2.zero;
            float angle = direction + Mathf.Atan2(rise, run) * side;
            return new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * length;
        }

        /// <summary>
        /// Radians the sheet's S is tilted out of the level (+ = the tail up): it streams against the hero's motion, up
        /// behind him as he falls, instead of keeping level and catching on the ledge he dropped off.
        /// </summary>
        public float TrailTilt { get; set; }

        /// <summary>
        /// Radians the sheet's S is tilted to clear what stands in its way (see <see cref="FreeTilt"/>): it rises when
        /// a wall stands behind him closer than the scarf is long - it streams up the wall like a flame instead of
        /// being blown into it and crumpling into a heap behind his head - and dips under a block hanging over its
        /// path. Eased in and out.
        /// </summary>
        public float WallTilt => DriftDirection < 0f ? wallTiltMinus : wallTiltPlus;

        // The tilt on either side (towards +x and -x), each eased on its own: only the part of the sheet heading for a
        // wall rises up it. (One tilt for the whole sheet: turning round with a wall now behind him, the tail still
        // streaming the free way was stood up as well, and the scarf swung over as an upright column.)
        float wallTiltPlus, wallTiltMinus;
        float freeTiltPlus, freeTiltMinus;   // (what each is eased towards: see FreeTilt)

        /// <summary>The steepest the scarf rises against a wall, and how fast (rad/s) it rises and settles.</summary>
        public const float MaxWallTilt = 1.3f, WallTiltRate = 3f;

        /// <summary>
        /// The tilt that fits a sheet <paramref name="reach"/> m long into <paramref name="room"/> m of free space before a
        /// wall: none with room to spare, steeper the closer the wall.
        /// </summary>
        public static float WallTiltFor(float room, float reach)
        {
            if (reach <= 1e-4f || room >= reach) return 0f;
            return Mathf.Min(MaxWallTilt, Mathf.Acos(Mathf.Clamp01(Mathf.Max(0f, room) / reach)));
        }

        /// <summary>Metres of free height over the neck in which the fold of a turn passes over the top at its full
        /// size; the least it can be squeezed into (<see cref="FoldTight"/> of its size: any lower and it goes
        /// underneath, if there is more room there).</summary>
        public const float FoldRoom = 1.0f, FoldRoomLeast = 0.45f, FoldTight = 0.55f;

        /// <summary>Free height (m) over the neck <paramref name="a"/> (<paramref name="dir"/> +1) or under it (-1), within
        /// <paramref name="span"/> m to either side. (For the fold of a turn: the scarf's own reach - the fold lifts
        /// the tail on its way over, and a block hanging over the tail is in its way as much as one over his head.)</summary>
        public float RoomOver(Vector3 a, float dir, float span = 0.5f)
        {
            float room = 10f;
            for (int o = 0; o < Obstacles.Count; o++)
            {
                Rect r = Obstacles[o];
                if (r.xMax < a.x - span || r.xMin > a.x + span) continue;
                float d = dir > 0f ? r.yMin - a.y : a.y - r.yMax;
                if (d < -0.05f) continue;   // on the other side of him, or he stands in it
                room = Mathf.Min(room, d);
            }
            return room;
        }

        /// <summary>Free space (m) from <paramref name="a"/> towards <paramref name="dir"/> (-1/+1 in x) before a wall at his height.</summary>
        public float RoomBehind(Vector3 a, float dir, float maxRoom)
        {
            float room = maxRoom;
            for (int o = 0; o < Obstacles.Count; o++)
            {
                Rect r = Obstacles[o];
                // a wall: it stands across his neck's height (a floor under him or a ceiling over him is no wall)
                if (a.y < r.yMin + 0.2f || a.y > r.yMax - 0.2f) continue;
                float d = dir > 0f ? r.xMin - a.x : a.x - r.xMax;
                if (d < -0.05f) continue;   // behind his back the other way, or he stands in it
                room = Mathf.Min(room, d);
            }
            return room;
        }

        float driftTo;
        Vector3[] shapeAt;          // per node: its place in the sheet's shape this step
        float[] shapeFree;          // per node: how far that place is clear of walls (0 inside one)

        /// <summary>How much of his fall the sheet's shape drags the cloth down with, tilted by <paramref name="tilt"/> into the fall.</summary>
        public static float FallDrag(float tilt) => 1f - Mathf.SmoothStep(0f, 1f, tilt / 0.6f);

        /// <summary>1/s: how hard cloth swinging out ahead of him is braked in a fall.</summary>
        public const float ForwardBrake = 30f;

        /// <summary>Metres within which the sheet's shape lets go as its place sinks into a wall.</summary>
        public const float ShapeWallFade = 0.25f;

        /// <summary>How much of the sheet's shape holds where its place is <paramref name="depth"/> m inside a wall (1 outside).</summary>
        public static float ShapeOutsideWalls(float depth) => 1f - Mathf.SmoothStep(0f, 1f, depth / ShapeWallFade);

        /// <summary>How deep (m) <paramref name="p"/> lies inside the obstacles (0 outside them).</summary>
        public float DepthInObstacles(Vector3 p)
        {
            float deepest = 0f;
            for (int o = 0; o < Obstacles.Count; o++)
            {
                Rect r = Obstacles[o];
                if (!r.Contains(new Vector2(p.x, p.y))) continue;
                float d = Mathf.Min(Mathf.Min(p.x - r.xMin, r.xMax - p.x), Mathf.Min(p.y - r.yMin, r.yMax - p.y));
                deepest = Mathf.Max(deepest, d);
            }
            return deepest;
        }

        /// <summary>Node index after which the ribbon is not held together (-1 = whole): node i and i + 1 move apart freely.</summary>
        public int DetachedAfter { get; set; } = -1;

        /// <summary>Air velocity of the swirl at <paramref name="p"/> relative to the moving centre.</summary>
        public static Vector2 SwirlVelocity(Vector2 p, Vector2 center, float speed, float radius)
        {
            Vector2 d = p - center;
            float r = d.magnitude;
            if (r < 1e-4f) return Vector2.zero;
            Vector2 dir = d / r;
            var tangent = new Vector2(-dir.y, dir.x);
            // Round the ring at its speed (not faster further out, which would fling the tail off) and pulled onto it.
            return tangent * (speed * radius) - dir * ((r - radius) * 22f);
        }
        public IReadOnlyList<Vortex> Vortices => vortices;
        public int ShedCount { get; private set; }

        /// <summary>A part of the hero's body the cloth falls against: a capsule from <see cref="a"/> to <see cref="b"/>.</summary>
        public struct BodyCapsule
        {
            public Vector3 a, b;
            public float radius;
            public BodyCapsule(Vector3 a, Vector3 b, float radius) { this.a = a; this.b = b; this.radius = radius; }
        }

        /// <summary>The hero's body as capsules (filled each frame by the owner): the loose cloth stays outside them.</summary>
        public readonly List<BodyCapsule> Body = new List<BodyCapsule>();

        /// <summary>Up along the neck (the wrap winds round it).</summary>
        public Vector3 NeckUp { get; set; } = Vector3.up;
        /// <summary>Where the throat faces (the hero's facing): the wrap starts there.</summary>
        public Vector3 NeckForward { get; set; } = Vector3.right;
        /// <summary>Which way is towards the camera (the wrap goes round that side of the neck).</summary>
        public Vector3 TowardsCamera { get; set; } = Vector3.back;

        /// <summary>
        /// Where node <paramref name="i"/> (1..WrapNodes) of the scarf lies wound round the neck: on a ring of
        /// <paramref name="radius"/> about the neck at <paramref name="centre"/>, from the throat (node 1) round the side
        /// towards the camera, a segment's length on from the one before.
        /// </summary>
        public static Vector3 WrapRingPoint(Vector3 centre, Vector3 up, Vector3 forward, Vector3 towardsCamera, float radius, float segment, int i)
        {
            Vector3 f = Vector3.ProjectOnPlane(forward, up);
            if (f.sqrMagnitude < 1e-6f) f = Vector3.ProjectOnPlane(Vector3.right, up);
            f.Normalize();
            Vector3 side = Vector3.ProjectOnPlane(towardsCamera, up);
            side = Vector3.ProjectOnPlane(side, f);
            if (side.sqrMagnitude < 1e-6f) side = Vector3.Cross(up, f);
            side.Normalize();
            float angle = (i - 1) * segment / Mathf.Max(0.01f, radius);
            return centre + (f * Mathf.Cos(angle) + side * Mathf.Sin(angle)) * radius;
        }

        /// <summary>0..1 how firmly node <paramref name="i"/> is held on the neck's wrap (the loose cloth's part of it).</summary>
        float WrapHold(int i) => i >= 1 && i <= Settings.wrapNodes ? (1f - H(i)) * (1f - GuideWeight[i]) : 0f;   // (a weapon melting back is led off it)

        /// <summary>The node lies wound round the neck (1..WrapNodes, while cloth).</summary>
        public bool IsWrapped(int i) => WrapHold(i) > 0.5f;

        /// <summary>How free node <paramref name="i"/> is to be moved by the constraints (0: held on the neck or the anchor).</summary>
        float Free(int i) => i == 0 ? 0f : 1f - WrapHold(i);

        readonly Vector3[] previous;
        readonly List<Vortex> vortices = new List<Vortex>();
        float shedPhase;
        int nextSign = 1;
        float time;
        float sinceWrap = 10f;
        Vector3 lastAnchor;

        public VeilSimulation(VeilSettings settings, Vector3 anchor)
        {
            Settings = settings ?? new VeilSettings();
            Count = Mathf.Max(3, Settings.nodeCount);
            Positions = new Vector3[Count];
            previous = new Vector3[Count];
            Targets = new Vector3[Count];
            TargetRest = new float[Count - 1];
            NodeHardness = new float[Count];
            GuideTargets = new Vector3[Count];
            GuideWeight = new float[Count];
            wallContact = new float[Count];
            flowTime = new float[FlowKept];
            flowAngleAt = new float[FlowKept];
            flowSideAt = new float[FlowKept];
            nodeAngle = new float[Count];
            nodeSide = new float[Count];
            shapeAt = new Vector3[Count];
            shapeFree = new float[Count];
            laidSide = new float[Count];
            sheetLen = new float[Count];
            sheetLift = new float[Count];
            pieceLen = new float[Count];
            pieceAngle = new float[Count];
            pieceTurn = new float[Count];
            FlowRestLength = Settings.length / (Count - 1);
            Reset(anchor);
        }

        public void Reset(Vector3 anchor)
        {
            var dir = new Vector3(-0.3f, -0.95f, 0f).normalized;
            for (int i = 0; i < Count; i++)
            {
                Positions[i] = previous[i] = Targets[i] = anchor + dir * (FlowRestLength * i);
                if (i < Count - 1) TargetRest[i] = FlowRestLength;
            }
            vortices.Clear();
            if (wallContact != null) Array.Clear(wallContact, 0, wallContact.Length);
            wallTiltPlus = wallTiltMinus = freeTiltPlus = freeTiltMinus = 0f;
            shedPhase = 0f;
            lastAnchor = anchor;
            AnchorVelocity = Vector3.zero;
            driftTo = DriftDirection;
            if (flowTime != null)
            {
                flowAngle = flowGoal = flowFrom = HeadingOf(DriftDirection);
                flowSide = sideFrom = DriftDirection;
                flowClock = 99f;
                FillFlow();
            }
        }

        /// <summary>The scarf's length (m) wound round the neck.</summary>
        public float WrapLength => Mathf.Min(Settings.wrapNodes, Count - 2) * FlowRestLength;

        /// <summary>Recompute <see cref="TargetRest"/> from the current <see cref="Targets"/>.</summary>
        public void RecomputeTargetRest()
        {
            for (int i = 0; i < Count - 1; i++)
                TargetRest[i] = Vector3.Distance(Targets[i], Targets[i + 1]);
        }

        public void Step(float dt, Vector3 anchor)
        {
            if (dt <= 0f || float.IsNaN(dt) || float.IsInfinity(dt)) return;
            dt = Mathf.Min(dt, 0.1f);

            if ((anchor - lastAnchor).magnitude > Settings.teleportDistance)
                Reset(anchor);

            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / Mathf.Max(0.001f, Settings.maxSubstep)));
            float h = dt / steps;
            Vector3 from = lastAnchor;
            Vector3 anchorVel = (anchor - from) / dt;
            AnchorVelocity = anchorVel;

            for (int k = 1; k <= steps; k++)
            {
                Vector3 a = Vector3.Lerp(from, anchor, (float)k / steps);
                SubStep(h, a, anchorVel, a - anchor);
            }

            lastAnchor = anchor;
        }

        void SubStep(float h, Vector3 a, Vector3 anchorVel, Vector3 targetOffset)
        {
            var s = Settings;
            time += h;
            {
                float reach = Settings.length * LengthScale * RestReach;
                bool walls = DriftDirection != 0f;
                // (a wall right behind him gives the first guess; the search finds what really clears it)
                float fall = Mathf.Max(TrailTilt, 0f);
                freeTiltPlus = walls ? FreeTilt(a, 1f, fall, Mathf.Max(0f, WallTiltFor(RoomBehind(a, 1f, reach), reach) - fall), freeTiltPlus) : 0f;
                freeTiltMinus = walls ? FreeTilt(a, -1f, fall, Mathf.Max(0f, WallTiltFor(RoomBehind(a, -1f, reach), reach) - fall), freeTiltMinus) : 0f;
                wallTiltPlus = Mathf.MoveTowards(wallTiltPlus, freeTiltPlus, h * WallTiltRate);
                wallTiltMinus = Mathf.MoveTowards(wallTiltMinus, freeTiltMinus, h * WallTiltRate);
            }
            StepFlow(h, a);
            Vector2 air = Wind + AmbientFlow;

            // 1) Advect and age vortices (they live in the air, not on the body).
            for (int i = vortices.Count - 1; i >= 0; i--)
            {
                var v = vortices[i];
                v.age += h;
                v.position += air * h;
                if (v.age >= v.lifetime) vortices.RemoveAt(i);
                else vortices[i] = v;
            }

            // 2) Shed alternating vortices at the Strouhal frequency f = St·U/D (+ idle magic).
            Vector2 rel = air - new Vector2(anchorVel.x, anchorVel.y);
            float speed = Mathf.Min(rel.magnitude, s.maxShedSpeed); // a fall must not whip up a storm
            float freq = s.idleShedFrequency + s.strouhal * speed / Mathf.Max(0.05f, s.bodyDiameter);
            shedPhase += freq * h;
            while (shedPhase >= 1f)
            {
                shedPhase -= 1f;
                Shed(a, rel, speed);
            }

            // 2b) Hardened parts are carried rigidly by the body, so a running hero never outruns the weapon.
            Vector3 carry = anchorVel * h;
            for (int i = 1; i < Count; i++)
            {
                float hi = H(i);
                if (hi <= 0.001f) continue;
                Positions[i] += carry * hi;
                previous[i] += carry * hi;
            }

            // 3) Verlet integration with drag towards the local air velocity.
            Vector3 g = Gravity * s.gravityScale;
            // Just after the somersault the scarf unwinds at once: for a moment it is pulled back into its shape
            // much harder and may move up and down freely (it lies wound round the legs as the hero lands).
            sinceWrap = SwirlWeight > 0.01f ? 0f : sinceWrap + h;
            float unwind = UnwindBoost(sinceWrap);
            var bodyVel = new Vector2(anchorVel.x, anchorVel.y);
            float bodySpeed = bodyVel.magnitude;
            float wavePhase = s.undulationSpeed * time;
            float turnLead = TurnLead;
            // The scarf's place this step: one line laid from the neck (the sheet, the somersault's ring, round walls).
            laidRound = false;
            LayShape(a);
            // How closely the cloth is led along it: through a turn, a fall or a leap, the somersault and the moment
            // after it, and along walls.
            bool streams = DriftDirection != 0f;
            float fallLead = streams ? 1f - FallDrag(Mathf.Abs(TrailTilt)) : 0f;
            wallLead = Mathf.MoveTowards(wallLead, streams && (laidRound || Mathf.Abs(WallTilt) > 0.05f) ? 1f : 0f, h * WallLeadEase);
            // (and while a wave runs down it: the neck's direction changed and the tail has yet to take it up)
            float lowest = nodeAngle[0], highest = nodeAngle[0];
            for (int i = 1; i < Count; i++)
            {
                lowest = Mathf.Min(lowest, nodeAngle[i]);
                highest = Mathf.Max(highest, nodeAngle[i]);
            }
            float stir = streams ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.1f, 0.5f, highest - lowest)) : 0f;
            flowStir = Mathf.MoveTowards(flowStir, stir, h / (stir > flowStir ? GuideIn : GuideSettle));
            float leadRate = Mathf.Max(streams ? GuideRate * TurnGuide(flowClock) : 0f, FallLeadRate * Mathf.Max(fallLead, flowStir));
            leadRate = Mathf.Max(leadRate, WallLeadRate * wallLead);
            leadRate = Mathf.Max(leadRate, WrapRate * Mathf.Max(SwirlWeight, streams ? unwind : 0f));
            for (int i = 1; i < Count; i++)
            {
                wallContact[i] = Mathf.Max(0f, wallContact[i] - h * 2.5f);
                Vector3 p = Positions[i];
                Vector3 vel = (p - previous[i]) / h;
                float f = (float)i / (Count - 1);
                // (not while it streams up behind him in a fall: a touch of the ledge he dropped off let the tail go
                // limp and it whipped round in front of him)
                // (it used to go limp beyond a point touching a wall, to drape down it: it piled up there in a heap.
                // Now the sheet's shape itself is laid along the wall - LayPiece - and the cloth is led along it.)
                float loose = 1f - H(i);

                // Feel the run wind only up to a point: the scarf trails, it doesn't become a taut sheet.
                Vector2 airV = SampleAirVelocity(p);
                Vector2 relAir = airV - bodyVel;
                if (relAir.sqrMagnitude > s.maxRelativeWind * s.maxRelativeWind) relAir = relAir.normalized * s.maxRelativeWind;
                airV = Vector2.Lerp(airV, bodyVel + relAir, loose);
                // The veil floats on its own breeze: it streams behind the hero even standing still.
                float headingHere = nodeAngle[i];
                Vector2 breeze = new Vector2(Mathf.Cos(headingHere), Mathf.Sin(headingHere));
                if (DriftDirection != 0f)
                    airV += new Vector2(breeze.x * s.drift, s.driftLift + breeze.y * s.drift) * (loose * Mathf.SmoothStep(0f, 1f, f * 4f));
                if (SwirlWeight > 0f)
                    airV = Vector2.Lerp(airV, bodyVel + SwirlVelocity(p, SwirlCenter, SwirlSpeed, SwirlRadius), loose * SwirlWeight);
                // In the whirl the cloth is gripped by the air far harder: it winds round within the turn.
                float drag = s.airDrag * (1f + 5f * SwirlWeight * loose);
                Vector3 acc = new Vector3((airV.x - vel.x) * drag, (airV.y - vel.y) * drag, 0f) + g;

                float shaped = RestShapeWeight * loose * (DriftDirection != 0f ? 1f : 0f);
                if (shaped > 0.001f)
                {
                    // Held to its place in the line (LayShape). Where that is inside a wall the hold lets go.
                    var target = new Vector3(shapeAt[i].x, shapeAt[i].y, p.z);
                    float falling = 1f - FallDrag(TrailTilt);
                    shaped *= shapeFree[i];
                    Vector3 pull = (target - p) * (s.restShapeStiffness * (1f + 3f * unwind));
                    Vector3 still = new Vector3(anchorVel.x - vel.x, anchorVel.y - vel.y, 0f) * (s.restShapeDamping * (1f + 1.5f * unwind));
                    // Falling, the cloth is not dragged down at his speed (it was pressed onto the ledge he dropped
                    // off and crumpled there): it lags above him and is drawn after him by the neck.
                    still.y *= FallDrag(TrailTilt);
                    acc += new Vector3(pull.x + still.x, pull.y + still.y, 0f) * shaped;

                    // Falling, cloth swinging out ahead of him is braked: slung round the corner of the ledge he
                    // stepped off, the tail flew a metre out in front before the shape brought it back.
                    if (falling > 0.001f && DriftDirection != 0f)
                    {
                        float back = Mathf.Sign(DriftDirection);
                        float forward = -(vel.x - anchorVel.x) * back;
                        if (forward > 0f) acc.x += forward * back * (ForwardBrake * falling * loose);
                    }
                }

                if (loose > 0.001f)
                {
                    // Its own life: a serpentine wave running down the scarf (stronger when moving),
                    // a tail that rises like smoke and a tip that curls over (quieter while it holds the sheet's shape).
                    float own = 1f - 0.9f * shaped;
                    Vector3 tan = Positions[Mathf.Min(i + 1, Count - 1)] - Positions[i - 1];
                    tan.z = 0f;
                    if (tan.sqrMagnitude > 1e-8f)
                    {
                        tan.Normalize();
                        var normal = new Vector3(-tan.y, tan.x, 0f);
                        float amp = (s.undulation + s.undulationPerSpeed * bodySpeed) * f;
                        float wave = Mathf.Sin(s.undulationWaves * Mathf.PI * 2f * f - wavePhase);
                        acc += normal * (amp * wave + s.tipCurl * Mathf.SmoothStep(0f, 1f, (f - 0.7f) / 0.3f)) * loose * own;
                    }
                    acc.y += s.buoyancy * f * f * loose * own;
                    acc += new Vector3(Breath.x, Breath.y, 0f) * (f * loose);
                }
                // Depth: the loose cloth is pulled back towards its place just behind the hero (with a little wander
                // of its own), so it can swing round the body and out of the picture plane yet keeps the scarf's side view.
                if (loose > 0.001f)
                {
                    float zRest = a.z + s.depthOffset + Mathf.Sin(time * 2.3f + i * 0.55f) * s.depthWobble * f;
                    acc.z += ((zRest - p.z) * s.depthStiffness - vel.z * s.depthDamping
                              + Mathf.Sin(time * 1.7f + i * 0.9f) * Mathf.Cos(time * 0.6f - i * 0.3f) * s.depthFlutter * f) * loose;
                }
                Vector3 next = p + (p - previous[i]) * s.damping + acc * (h * h);
                // The veil is weightless: it rides along with the hero's jumps and falls instead of carrying
                // their momentum — no flinging up over the head at the apex, no slapping onto the floor on landing.
                if (s.maxRelativeFall > 0f && loose > 0.001f)
                {
                    Vector3 relV = (next - p) / h - anchorVel;
                    // A breath (weapon melting back) may lift it faster than the normal limit.
                    float limit = relV.y > 0f ? s.maxRelativeFall + Mathf.Max(0f, Breath.y) * 0.25f : s.maxRelativeFall;
                    limit += (SwirlWeight + unwind) * 40f; // winding round the body (and off it) moves up and down freely
                    // (and so does cloth that is led: held to 2.5 m/s it could not keep up with its place as he
                    // dropped off a ledge - it sank onto the ledge's top and was dragged over the edge in a heap)
                    limit += Mathf.Clamp01(leadRate / FallLeadRate) * 40f;
                    if (Mathf.Abs(relV.y) > limit)
                    {
                        relV.y = Mathf.Lerp(relV.y, Mathf.Sign(relV.y) * limit, loose);
                        next = p + (anchorVel + relV) * h;
                    }
                }
                previous[i] = p;
                Positions[i] = next;
            }

            // 4) Root is pinned to the anchor.
            Positions[0] = a;
            previous[0] = a - anchorVel * h;

            // 5) Hardening: pull towards the weapon shape and bleed velocity.
            for (int i = 1; i < Count; i++)
            {
                float hi = H(i);
                if (hi <= 0.001f) continue;
                float f = 1f - Mathf.Exp(-s.hardenRate * hi * h);
                Positions[i] = Vector3.Lerp(Positions[i], Targets[i] + targetOffset, f);
                previous[i] = Vector3.Lerp(previous[i], Positions[i], f * 0.5f);
            }

            // 5a) Round the neck: the first segments of the cloth are wound round it (from the throat round the side
            // towards the camera, to the back), held there; the rest streams off the wrap.
            LayWrap(a);

            // 5b) The cloth is led along its line: each loose node is drawn to its place in it, which carries it at
            // that place's own speed - through the fold of a turn (TurnGuide), a fall or a leap (FallLeadRate), along
            // walls, and into the somersault's ring and out of it (the line winds round the body as the body turns).
            if (leadRate > 0.001f)
                for (int i = Mathf.Max(1, s.wrapNodes); i < Count; i++)
                {
                    float lead = leadRate * (1f - H(i)) * Free(i) * shapeFree[i];
                    if (lead <= 0.001f) continue;
                    Vector3 to = shapeAt[i];
                    to.z = Positions[i].z;
                    Positions[i] = Vector3.Lerp(Positions[i], to, 1f - Mathf.Exp(-lead * h));
                }

            // 6) Distance constraints (rest length morphs from cloth to weapon shape, node by node).
            for (int it = 0; it < s.constraintIterations; it++)
            {
                for (int i = 0; i < Count - 1; i++)
                {
                    if (i == DetachedAfter) continue; // a weapon that floats free of the hand (the shield's ring)
                    float rest = Mathf.Lerp(FlowRestLength * LengthScale, TargetRest[i], 0.5f * (H(i) + H(i + 1)));
                    // (in three dimensions: the cloth swings round the body and out of the picture plane; set weapons
                    // keep their depth in step 7, so their shapes still measure in the plane)
                    Vector3 d = Positions[i + 1] - Positions[i];
                    float hard = 0.5f * (WeaponDepthBlend(H(i)) + WeaponDepthBlend(H(i + 1)));
                    d.z *= 1f - hard;
                    float len = d.magnitude;
                    if (len < 1e-6f) continue;
                    // the anchor and the wrap round the neck are held: the free end of a segment takes it all
                    float wa = Free(i), wb = Free(i + 1);
                    float sum = wa + wb;
                    if (sum < 1e-4f) continue;
                    Vector3 corr = d * ((len - rest) / len);
                    Positions[i] += corr * (wa / sum);
                    Positions[i + 1] -= corr * (wb / sum);
                }
                // The chest line (FrontHold): kept by the cloth itself, which drapes back from it in one piece. (Pressed
                // back in the drawing instead, point by point, a band reaching out in front was squashed into a sliver
                // whose edges crossed - a Z-shaped notch.)
                // (not while it is led through a turn: the tail still to come passes over his head from in front)
                float front = FrontHold * (1f - turnLead);
                if (front > 0f)
                    for (int i = Mathf.Max(1, s.wrapNodes); i < Count; i++)
                    {
                        float loose = (1f - H(i)) * Free(i);
                        if (loose <= 0f) continue;
                        float ahead = (Positions[i].x - a.x) * FrontFacing - FrontReach;
                        if (ahead <= 0f) continue;
                        Positions[i].x -= FrontFacing * ahead * front * loose;
                        // (and it stops there rather than bouncing off: its speed outwards is spent)
                        if ((Positions[i].x - previous[i].x) * FrontFacing > 0f) previous[i].x = Positions[i].x;
                    }
                // Bending: as in rope and hair simulation, each node is held at least SkipLength from the one after next
                // (in the picture, as it is drawn), which keeps the bend at the node between them within MaxBend. Bent
                // further, the drawn curve looped between the nodes and the band showed steps and shards.
                // (swept down the scarf and back up it: one way only, a node at the far end was left bent)
                float least = SkipLength(FlowRestLength * LengthScale);
                int first = Mathf.Max(1, s.wrapNodes);   // (past the wrap round the neck: wound tight)
                for (int i = first; i < Count - 2; i++) HoldBend(i, least);
                for (int i = Count - 3; i >= first; i--) HoldBend(i, least);
                // Cloth doesn't fold sharply: where the loose cloth bends at a node by more than MaxKink, the node is
                // drawn towards the middle of its neighbours (gentle curves - its S - are left alone). Without it,
                // turning round crumpled it node by node into a zigzag, drawn as broken pieces with gaps between them.
                for (int i = 2; i < Count - 1; i++)
                {
                    float loose = (1f - H(i)) * Free(i) * Free(i - 1);
                    if (loose <= 0f) continue;
                    float kink = KinkExcess(Positions[i - 1], Positions[i], Positions[i + 1]);
                    if (kink <= 0f) continue;
                    Positions[i] += ((Positions[i - 1] + Positions[i + 1]) * 0.5f - Positions[i]) * (0.8f * kink * loose);
                }
            }

            // 6a) Follow the leader (as in hair simulation, Mueller et al. 2012): each loose segment is set to its exact
            // length along its own direction, from the neck outwards. A few passes of the constraints above could not hold
            // it in a fast turn: the cloth was squeezed to half its length, bunched up and folded sharply between nodes.
            // (Its speed is kept: the node is carried along, not flung.)
            {
                float rest = FlowRestLength * LengthScale;
                for (int i = Mathf.Max(1, s.wrapNodes) + 1; i < Count; i++)
                {
                    if (i - 1 == DetachedAfter) break;
                    float loose = (1f - H(i)) * (1f - H(i - 1)) * Free(i);
                    if (loose < 0.999f) continue;
                    Vector3 d = Positions[i] - Positions[i - 1];
                    float len = d.magnitude;
                    if (len < 1e-6f) continue;
                    Vector3 shift = Positions[i - 1] + d * (rest / len) - Positions[i];
                    Positions[i] += shift;
                    previous[i] += shift;
                }
                // (and the bend limit once more: set to length along its own direction, a curling tail wound tighter)
                float least = SkipLength(rest);
                int first = Mathf.Max(1, s.wrapNodes);
                for (int i = first; i < Count - 2; i++) HoldBend(i, least);
            }

            // 6b) A set weapon is a solid thing in the hand: once a node has hardened it sits exactly on the weapon's
            // shape (the blade goes with the fist through the fastest swing instead of trailing behind it); only
            // the half-set cloth in between stays soft. Its previous position moves along, so no speed is invented.
            for (int i = 1; i < Count; i++)
            {
                float rigid = Mathf.Max(RigidHold(H(i)), Held(i));
                if (rigid <= 0f) continue;
                Vector3 want = Targets[i] + targetOffset;
                want.z = Positions[i].z;
                Vector3 shift = (want - Positions[i]) * rigid;
                Positions[i] += shift;
                previous[i] += shift;
            }

            // 6c) A weapon melting back into cloth is led along a path (VeilTransform.MeltProgress) instead of
            // collapsing on its own; the previous position moves along, so it neither springs nor bunches up.
            for (int i = 1; i < Count; i++)
            {
                float guide = GuideWeight[i];
                if (guide <= 0f) continue;
                Vector3 want = GuideTargets[i];
                want.z = Positions[i].z;
                Vector3 shift = (want - Positions[i]) * guide;
                Positions[i] += shift;
                previous[i] += shift;
            }

            // 6d) The loose cloth can't pass through walls: it drapes against them (see CollideWithObstacles), nor
            // through the hero: it falls against the body and slides round it (see CollideWithBody).
            CollideWithBody();
            CollideWithObstacles();
            LayWrap(a);   // (the wrap held where it is after all the rest)

            // 7) Depth: the loose cloth moves in it freely (its spring above keeps it behind the hero, within
            // maxDepth); a weapon in the hand is held in front of the body, so the torso never hides it.
            for (int i = 1; i < Count; i++)
            {
                if (IsWrapped(i)) continue;
                float t = (float)i / (Count - 1);
                float weapon = WeaponDepthBlend(H(i));
                float z = Positions[i].z;
                float zRest = a.z + s.depthOffset;
                z = Mathf.Clamp(z, zRest - s.maxDepth, zRest + s.maxDepth);
                float plane = float.IsNaN(WeaponPlane) ? a.z - s.weaponDepth : WeaponPlane;
                z = Mathf.Lerp(z, plane + SweepDepth(Positions[i], WeaponPivot, WeaponTilt), weapon);
                float heldHere = Held(i);
                if (heldHere > 0f) z = Mathf.Lerp(z, plane + (float.IsNaN(WeaponPlane) ? HeldTuck : 0f), heldHere);
                // A weapon melting back passes over the body: drawn in front, so the torso doesn't swallow it.
                z = Mathf.Lerp(z, a.z - s.weaponDepth, GuideWeight[i]);
                float held = Mathf.Max(weapon, Mathf.Max(GuideWeight[i], heldHere));
                previous[i].z = Mathf.Lerp(previous[i].z + (z - Positions[i].z), z, held);   // (held: no speed in depth)
                Positions[i].z = z;
            }
        }

        /// <summary>
        /// Solid level geometry near the scarf, as rectangles in the picture plane (min.xy, max.xy). Filled each
        /// frame by the owner (VeilController gathers the walls and floors around the hero).
        /// </summary>
        public readonly List<Rect> Obstacles = new List<Rect>();

        /// <summary>
        /// How long the loose scarf is, as a share of its full length (its strength: casting and a torn scarf
        /// shorten it, it grows back). The cloth itself is shorter - its curls stay at its end - rather than its tail
        /// fading out, which read as a scarf cut off.
        /// </summary>
        public float LengthScale = 1f;

        /// <summary>
        /// The top of the floor under (x, y): the highest obstacle top below the point, or one it has sunk into by
        /// up to <paramref name="sunk"/> m. -infinity when there is none.
        /// </summary>
        public float FloorUnder(float x, float y, float sunk = 0.6f)
        {
            float best = float.NegativeInfinity;
            for (int o = 0; o < Obstacles.Count; o++)
            {
                var r = Obstacles[o];
                if (x < r.xMin || x > r.xMax || r.yMax > y + sunk) continue;
                if (r.yMax > best) best = r.yMax;
            }
            return best;
        }
        /// <summary>Lays the loose cloth's first segments round the neck (held there, no speed of their own).</summary>
        void LayWrap(Vector3 a)
        {
            var s = Settings;
            int n = Mathf.Min(s.wrapNodes, Count - 2);
            for (int i = 1; i <= n; i++)
            {
                float hold = WrapHold(i);
                if (hold <= 0f) continue;
                Vector3 ring = WrapRingPoint(a, NeckUp, NeckForward, TowardsCamera, s.neckRadius, FlowRestLength, i);
                Vector3 shift = (ring - Positions[i]) * hold;
                Positions[i] += shift;
                previous[i] += shift;
            }
        }

        /// <summary>
        /// Keeps the loose cloth out of the hero's body (<see cref="Body"/>): a node inside a capsule is put on its
        /// surface (plus <see cref="VeilSettings.bodySkin"/>) straight out from its axis; it loses its speed into the
        /// body and keeps most of its slide round it. The wrap round the neck and set weapons are left alone.
        /// </summary>
        void CollideWithBody()
        {
            if (Body.Count == 0) return;
            float skin = Settings.bodySkin;
            for (int i = 1; i < Count; i++)
            {
                float loose = (1f - Mathf.Max(Mathf.Max(RigidHold(H(i)), GuideWeight[i]), Held(i))) * Free(i);
                if (loose <= 0.01f) continue;
                for (int c = 0; c < Body.Count; c++)
                {
                    var cap = Body[c];
                    Vector3 p = Positions[i];
                    Vector3 axis = ClosestOnSegment(cap.a, cap.b, p);
                    Vector3 off = p - axis;
                    float dist = off.magnitude;
                    float reach = cap.radius + skin;
                    if (dist >= reach) continue;
                    Vector3 n = dist > 1e-5f ? off / dist : TowardsCamera.normalized;
                    Vector3 push = n * (reach - dist) * loose;
                    Positions[i] += push;
                    Vector3 v = Positions[i] - previous[i];
                    float vn = Vector3.Dot(v, n);
                    Vector3 slide = (v - n * vn) * 0.9f + n * Mathf.Max(0f, vn);
                    previous[i] = Positions[i] - Vector3.Lerp(v, slide, loose);
                }
            }
        }

        /// <summary>The point of the segment a-b nearest to p.</summary>
        public static Vector3 ClosestOnSegment(Vector3 a, Vector3 b, Vector3 p)
        {
            Vector3 ab = b - a;
            float l2 = ab.sqrMagnitude;
            if (l2 < 1e-10f) return a;
            return a + ab * Mathf.Clamp01(Vector3.Dot(p - a, ab) / l2);
        }

        float[] wallContact;
        /// <summary>0..1 how recently node <paramref name="i"/> was stopped by a wall.</summary>
        public float WallContact(int i) => wallContact[i];

        /// <summary>How far off a wall the cloth stays (m), so its width doesn't sink into the surface.</summary>
        public const float WallSkin = 0.06f;

        /// <summary>
        /// Keeps the loose cloth out of the obstacles. Walked from the neck to the tip, a node that ended up in a
        /// wall is put back where its segment entered it (the side the scarf came from), not at the nearest face —
        /// a node deep in a thick pillar would otherwise pop out on its far side, as if the scarf ran through it.
        /// It loses its speed into the wall and slides along it with a little friction. Set weapons are left alone
        /// (they are held in the hand).
        /// </summary>
        /// <summary>0..1 how firmly node <paramref name="i"/> is held on the weapon's line (the span HeldFrom..HeldTo).</summary>
        float Held(int i)
        {
            if (HeldWeight <= 0f) return 0f;
            float f = (float)i / (Count - 1);
            if (f < HeldFrom) return 0f;
            return f <= HeldTo ? HeldWeight : HeldWeight * HeldBeyond;
        }

        void CollideWithObstacles()
        {
            if (Obstacles.Count == 0) return;
            for (int i = 1; i < Count; i++)
            {
                // (what is held on a weapon's line is placed by it: the whip lies on the floor where its own line lays
                // it - pushed out of the floor's skin here as well, the lash was bunched up to half its length)
                float loose = 1f - Mathf.Max(Mathf.Max(RigidHold(H(i)), GuideWeight[i]), Held(i));
                if (loose <= 0.01f) continue;
                Vector2 p = Positions[i];
                Vector2 from = Positions[i - 1];
                for (int o = 0; o < Obstacles.Count; o++)
                {
                    Rect r = Obstacles[o];
                    Rect grown = new Rect(r.xMin - WallSkin, r.yMin - WallSkin, r.width + 2f * WallSkin, r.height + 2f * WallSkin);
                    if (!grown.Contains(p)) continue;
                    Vector2 outP;
                    if (RoundedCorner(grown, p, CornerRadius, out Vector2 round, out bool inCorner) && inCorner) outP = round;
                    else if (inCorner) continue;   // in the rounded-off corner: free
                    else outP = grown.Contains(from) ? NearestOutside(grown, p) : EntryPoint(grown, from, p);
                    Vector2 push = outP - p;
                    Vector3 shift = new Vector3(push.x, push.y, 0f) * loose;
                    Positions[i] += shift;
                    wallContact[i] = 1f;
                    // velocity: drop the part into the wall, keep most of the slide along it
                    Vector2 v = (Vector2)(Positions[i] - previous[i]);
                    Vector2 n = push.sqrMagnitude > 1e-10f ? push.normalized : Vector2.zero;
                    float vn = Vector2.Dot(v, n);
                    Vector2 slide = (v - n * vn) * 0.85f + n * Mathf.Max(0f, vn);
                    Vector2 newPrev = (Vector2)Positions[i] - Vector2.Lerp(v, slide, loose);
                    previous[i] = new Vector3(newPrev.x, newPrev.y, previous[i].z);
                    p = Positions[i];
                }
            }
        }

        /// <summary>
        /// The corners of walls and ledges are rounded off this much for the loose cloth: dragged round the square edge
        /// of a ledge he dropped off, the broad band bent through a right angle and crumpled there; round a curve it slides.
        /// </summary>
        public const float CornerRadius = 0.25f;

        /// <summary>
        /// Is <paramref name="p"/> (inside <paramref name="r"/>) in one of its corners, rounded by <paramref name="radius"/>
        /// (<paramref name="inCorner"/>), and if so inside the rounding (returns true, <paramref name="outP"/> the nearest
        /// point on the curve) or in the cut-off corner, outside it (false)?
        /// </summary>
        public static bool RoundedCorner(Rect r, Vector2 p, float radius, out Vector2 outP, out bool inCorner)
        {
            outP = p;
            radius = Mathf.Min(radius, 0.5f * Mathf.Min(r.width, r.height));   // (two-argument Min: the params overload allocates)
            float cx = Mathf.Clamp(p.x, r.xMin + radius, r.xMax - radius), cy = Mathf.Clamp(p.y, r.yMin + radius, r.yMax - radius);
            inCorner = cx != p.x && cy != p.y;
            if (!inCorner) return false;
            Vector2 d = p - new Vector2(cx, cy);
            float len = d.magnitude;
            if (len >= radius) return false;
            outP = new Vector2(cx, cy) + (len > 1e-6f ? d / len : new Vector2(0f, 1f)) * (radius + 1e-3f);
            return true;
        }

        /// <summary>Where the segment from <paramref name="from"/> (outside) to <paramref name="to"/> (inside) enters the rectangle.</summary>
        public static Vector2 EntryPoint(Rect r, Vector2 from, Vector2 to)
        {
            Vector2 d = to - from;
            float tEnter = 0f;
            // slab test: the latest entry over both axes
            for (int axis = 0; axis < 2; axis++)
            {
                float o = from[axis], dir = d[axis];
                float min = axis == 0 ? r.xMin : r.yMin, max = axis == 0 ? r.xMax : r.yMax;
                if (Mathf.Abs(dir) < 1e-8f) continue;
                float t0 = (min - o) / dir, t1 = (max - o) / dir;
                tEnter = Mathf.Max(tEnter, Mathf.Min(t0, t1));
            }
            tEnter = Mathf.Clamp01(tEnter);
            Vector2 e = from + d * tEnter;
            // just outside the face it crossed
            Vector2 c = r.center;
            float dx = Mathf.Min(Mathf.Abs(e.x - r.xMin), Mathf.Abs(e.x - r.xMax));
            float dy = Mathf.Min(Mathf.Abs(e.y - r.yMin), Mathf.Abs(e.y - r.yMax));
            if (dx <= dy) e.x = e.x < c.x ? r.xMin - 1e-3f : r.xMax + 1e-3f;
            else e.y = e.y < c.y ? r.yMin - 1e-3f : r.yMax + 1e-3f;
            return e;
        }

        /// <summary>The closest point just outside the rectangle.</summary>
        /// <summary>
        /// <paramref name="p"/> put out of every obstacle (grown by <paramref name="margin"/>) the shortest way, in the
        /// picture plane: for what is drawn or simulated on top of the cloth (its strands, its whorls), which would
        /// otherwise pass into a wall or a low ceiling and be cut off by it.
        /// </summary>
        public Vector3 OutOfObstacles(Vector3 p, float margin)
        {
            for (int o = 0; o < Obstacles.Count; o++)
            {
                Rect r = Obstacles[o];
                Rect grown = new Rect(r.xMin - margin, r.yMin - margin, r.width + 2f * margin, r.height + 2f * margin);
                Vector2 q = new Vector2(p.x, p.y);
                if (!grown.Contains(q)) continue;
                // (its corners rounded as for the cloth, CornerRadius: else the band drawn round the curve was cut square)
                if (RoundedCorner(grown, q, CornerRadius, out Vector2 round, out bool inCorner)) q = round;
                else if (inCorner) continue;
                else q = NearestOutside(grown, q);
                p.x = q.x; p.y = q.y;
            }
            return p;
        }

        public static Vector2 NearestOutside(Rect r, Vector2 p)
        {
            float l = p.x - r.xMin, ri = r.xMax - p.x, b = p.y - r.yMin, t = r.yMax - p.y;
            float m = Mathf.Min(Mathf.Min(l, ri), Mathf.Min(b, t));
            if (m == l) return new Vector2(r.xMin - 1e-3f, p.y);
            if (m == ri) return new Vector2(r.xMax + 1e-3f, p.y);
            if (m == b) return new Vector2(p.x, r.yMin - 1e-3f);
            return new Vector2(p.x, r.yMax + 1e-3f);
        }

        void Shed(Vector3 anchor, Vector2 rel, float speed)
        {
            var s = Settings;
            Vector2 dir = rel.sqrMagnitude > 1e-6f ? rel.normalized : Vector2.down;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            float d = s.bodyDiameter;

            var vortex = new Vortex
            {
                position = new Vector2(anchor.x, anchor.y) + dir * (d * 0.5f) + perp * (s.lateralShedOffset * nextSign),
                circulation = nextSign * (s.circulationGain * speed * d + s.idleCirculation * d),
                age = 0f,
                lifetime = s.vortexLifetime,
                coreRadius0 = s.coreRadius,
                viscosity = s.viscosity,
            };

            if (vortices.Count >= s.maxVortices) vortices.RemoveAt(0);
            vortices.Add(vortex);
            ShedCount++;
            nextSign = -nextSign;
        }

        /// <summary>Air velocity at a world point: wind + ambient drift + all vortices.</summary>
        public Vector2 SampleAirVelocity(Vector3 p)
        {
            Vector2 v = Wind + AmbientFlow;
            var q = new Vector2(p.x, p.y);
            for (int i = 0; i < vortices.Count; i++)
            {
                var vx = vortices[i];
                v += InducedVelocity(q, vx.position, vx.Strength, vx.CoreRadius);
            }
            return v;
        }

        /// <summary>
        /// Lamb-Oseen vortex: u_θ = Γ/(2πr)·(1 − e^(−r²/rc²)). Finite at the core, 1/r far away.
        /// Positive Γ rotates counter-clockwise.
        /// </summary>
        public static Vector2 InducedVelocity(Vector2 point, Vector2 center, float circulation, float coreRadius)
        {
            Vector2 r = point - center;
            float r2 = r.sqrMagnitude;
            if (r2 < 1e-8f) return Vector2.zero;
            float rc2 = Mathf.Max(1e-6f, coreRadius * coreRadius);
            float k = circulation / (2f * Mathf.PI * r2) * (1f - Mathf.Exp(-r2 / rc2));
            return new Vector2(-r.y, r.x) * k;
        }
    }
}
