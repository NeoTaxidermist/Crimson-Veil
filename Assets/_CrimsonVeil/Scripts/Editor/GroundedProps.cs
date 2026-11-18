using System.Collections.Generic;
using CrimsonVeil.World;
using UnityEngine;
using F = CrimsonVeil.EditorTools.LevelKitFactory;

namespace CrimsonVeil.EditorTools
{
    /// <summary>
    /// Arches, columns, walls and statues in the backdrop must stand on something. At ground level each layer's
    /// floor is lost in its mist band; but set up high (behind the tower climb) by eye against a tower in a further
    /// layer, they stood on thin air as soon as the camera moved (the layers slide past each other). Here each one
    /// standing above its layer's floor must have stone of its own layer under both feet; else it is set on the
    /// nearest stone top that carries it - in its own layer, or a deeper one (the top of a tower behind) - or removed.
    /// </summary>
    public static class GroundedProps
    {
        /// <summary>Set pieces that stand on their feet (the deco's own children, by name).</summary>
        public static readonly HashSet<string> Standing = new HashSet<string>
        {
            "Arch", "BrokenArch", "Column", "BrokenColumn", "RuinWall", "RuinStairs", "Statue", "DeadTree"
        };

        /// <summary>A foot this close (m) above stone stands on it.</summary>
        public const float FootGap = 0.6f;
        /// <summary>Feet this little above the layer's floor are in its mist: they stand on the ground.</summary>
        public const float FloorMist = 3.5f;
        /// <summary>The two feet of a thing set on a top must be this level (m).</summary>
        public const float Level = 0.4f;

        /// <summary>The ground level of a backdrop layer (its floor is lost in the layer's mist band).</summary>
        public static float Floor(LevelLayer layer)
        {
            switch (layer)
            {
                case LevelLayer.Near: return -1f;
                case LevelLayer.Mid: return -2f;
                case LevelLayer.Far: return -12f;
                default: return float.NegativeInfinity;
            }
        }

        /// <summary>Where the feet of a standing thing are (x), from its bounds: in a little from either side.</summary>
        public static void Feet(Bounds b, out float left, out float right)
        {
            left = b.min.x + b.size.x * 0.12f;
            right = b.max.x - b.size.x * 0.12f;
        }

        /// <summary>Is stone of the layer at depth <paramref name="z"/> right under the foot at (x, y)?</summary>
        public static bool OnStone(float x, float y, float z, Transform self)
        {
            foreach (var h in Physics.RaycastAll(new Vector3(x, y + 0.3f, z), Vector3.down, 0.3f + FootGap))
            {
                if (self != null && h.collider.transform.IsChildOf(self)) continue;
                if (Mathf.Abs(h.point.z - z) > 6f) continue;
                return true;
            }
            return false;
        }

        /// <returns>(moved, removed)</returns>
        public static Vector2Int Ground(Transform deco)
        {
            var colliders = new List<Collider>();
            var standing = new List<Transform>();
            foreach (Transform child in deco)
            {
                if (!HangingProps.Solid.Contains(child.name) && !Standing.Contains(child.name)) continue;
                if (Standing.Contains(child.name)) standing.Add(child);
                foreach (var mf in child.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    colliders.Add(mc);
                }
            }
            Physics.SyncTransforms();

            int moved = 0, removed = 0;
            foreach (var s in standing)
            {
                var layer = LayerOf(s);
                if (layer == LevelLayer.Playfield || !Bounds(s, out var b)) continue;
                if (Stands(s, b, layer)) continue;
                if (TryRest(s, b, out var spot))
                {
                    if (spot.layer != layer) F.ApplyLayer(s.gameObject, spot.layer);
                    s.position = spot.pivot;
                    Physics.SyncTransforms();
                    moved++;
                }
                else
                {
                    foreach (var c in s.GetComponentsInChildren<Collider>()) colliders.Remove(c);
                    Object.DestroyImmediate(s.gameObject);
                    Physics.SyncTransforms();
                    removed++;
                }
            }

            foreach (var c in colliders) if (c != null) Object.DestroyImmediate(c);
            Physics.SyncTransforms();
            return new Vector2Int(moved, removed);
        }

        /// <summary>On its layer's floor (in the mist), or both feet on stone of its layer.</summary>
        public static bool Stands(Transform s, Bounds b, LevelLayer layer)
        {
            if (b.min.y <= Floor(layer) + FloorMist) return true;
            Feet(b, out float l, out float r);
            float z = s.position.z;
            return OnStone(l, b.min.y, z, s) && OnStone(r, b.min.y, z, s);
        }

        struct Spot { public LevelLayer layer; public Vector3 pivot; }

        static LevelLayer LayerOf(Transform t)
        {
            var tag = t.GetComponent<DepthLayer>();
            return tag != null ? tag.layer : LevelLayer.Playfield;
        }

        /// <summary>
        /// The stone top that carries the thing on both feet, level, with room for it above - the one closest to where
        /// it was (sideways and in height), its own layer before a deeper one.
        /// </summary>
        static bool TryRest(Transform s, Bounds b, out Spot spot)
        {
            var own = LayerOf(s);
            var layers = new List<LevelLayer> { own };
            foreach (var l in new[] { LevelLayer.Near, LevelLayer.Mid, LevelLayer.Far })
                if (DepthLayer.Z(l) > DepthLayer.Z(own)) layers.Add(l);
            Feet(b, out float footL, out float footR);
            spot = default;
            float best = float.MaxValue;
            for (int li = 0; li < layers.Count; li++)
            {
                var layer = layers[li];
                float k = F.LayerScale(layer) / F.LayerScale(own);
                float z = layer == own ? s.position.z : DepthLayer.Z(layer);
                float lOff = (footL - s.position.x) * k, rOff = (footR - s.position.x) * k;
                float bottomOff = (b.min.y - s.position.y) * k;
                var half = new Vector3(b.extents.x * k * 0.85f, b.extents.y * k * 0.9f, Mathf.Max(0.1f, b.extents.z * k * 0.5f));
                float cxOff = (b.center.x - s.position.x) * k;
                for (int i = 0; i <= 30; i++)
                {
                    float dx = (i % 2 == 0 ? 1f : -1f) * ((i + 1) / 2) * 0.6f;
                    float px = s.position.x + dx;
                    if (!Top(px + lOff, z, s, out float yl) || !Top(px + rOff, z, s, out float yr)) continue;
                    if (Mathf.Abs(yl - yr) > Level) continue;
                    float rest = Mathf.Max(yl, yr);
                    if (rest <= Floor(layer) + FloorMist) continue;   // (down at the floor: it might as well stand there)
                    // room above the top for the whole thing
                    var centre = new Vector3(px + cxOff, rest + 0.15f + half.y, z);
                    if (Blocked(centre, half, s)) continue;
                    float cost = Mathf.Abs(rest - b.min.y) + 2f * Mathf.Abs(dx) + 8f * li;
                    if (cost >= best) continue;
                    best = cost;
                    spot = new Spot { layer = layer, pivot = new Vector3(px, rest - bottomOff, z) };
                }
            }
            return best < float.MaxValue;
        }

        /// <summary>The highest stone top under x in the layer at depth z (looked for from high above everything).</summary>
        static bool Top(float x, float z, Transform self, out float y)
        {
            y = float.NegativeInfinity;
            foreach (var h in Physics.RaycastAll(new Vector3(x, 150f, z), Vector3.down, 200f))
            {
                if (h.collider.transform.IsChildOf(self)) continue;
                if (Mathf.Abs(h.point.z - z) > 6f || h.normal.y < 0.7f) continue;
                if (h.point.y > y) y = h.point.y;
            }
            return !float.IsNegativeInfinity(y);
        }

        static bool Blocked(Vector3 centre, Vector3 half, Transform self)
        {
            foreach (var c in Physics.OverlapBox(centre, half))
                if (!c.transform.IsChildOf(self)) return true;
            return false;
        }

        public static bool Bounds(Transform t, out Bounds b)
        {
            b = default;
            var rs = t.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return false;
            b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return true;
        }
    }
}
