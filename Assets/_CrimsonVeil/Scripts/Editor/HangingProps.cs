using System.Collections.Generic;
using CrimsonVeil.World;
using UnityEngine;
using F = CrimsonVeil.EditorTools.LevelKitFactory;

namespace CrimsonVeil.EditorTools
{
    /// <summary>
    /// Chains, cages and banners in the backdrop must hang from something: placed by eye against a tower in a
    /// further layer they hung from thin air as soon as the camera moved (the layers slide past each other). Here
    /// each one looks straight up for stone in its own layer (an arch, a wall, a tower of the same depth) and a chain
    /// is run up to it; a banner may also hang on a wall right behind it. One with nothing to hang from is removed.
    /// </summary>
    public static class HangingProps
    {
        /// <summary>Set pieces that are stone a thing can hang from (the deco's own children, by name).</summary>
        public static readonly HashSet<string> Solid = new HashSet<string>
        {
            "Arch", "BrokenArch", "Column", "BrokenColumn", "RuinWall", "Tower", "RuinStairs", "Aqueduct", "Castle",
            "CliffRuin", "Statue", "Spires", "BrokenPost", "Rubble"
        };

        /// <summary>Things that hang (the deco's own children, by name).</summary>
        public static readonly HashSet<string> Hanging = new HashSet<string> { "Chain", "Cage", "Banner" };

        /// <summary>The name of the group a hanging thing and the chain it hangs on are gathered in.</summary>
        public const string GroupName = "Hanging";

        /// <summary>How far up (m) a chain may run to reach the stone it hangs from.</summary>
        public const float MaxReach = 14f;
        /// <summary>A banner closer than this (m) in front of a wall hangs on it.</summary>
        public const float WallBehind = 1.2f;

        /// <summary>
        /// Lengths of the chain links stacked to run <paramref name="distance"/> m up, from a model
        /// <paramref name="modelLength"/> m long: as few as cover it, each squeezed evenly (never stretched) to fit.
        /// </summary>
        public static float[] ChainRun(float distance, float modelLength)
        {
            if (distance <= 1e-3f || modelLength <= 1e-3f) return new float[0];
            int n = Mathf.CeilToInt(distance / modelLength - 1e-4f);
            var lengths = new float[n];
            for (int i = 0; i < n; i++) lengths[i] = distance / n;
            return lengths;
        }

        /// <returns>(kept, removed)</returns>
        public static Vector2Int Anchor(Transform deco)
        {
            var colliders = new List<Collider>();
            var hanging = new List<Transform>();
            foreach (Transform child in deco)
            {
                if (Hanging.Contains(child.name)) { hanging.Add(child); continue; }
                if (!Solid.Contains(child.name)) continue;
                foreach (var mf in child.GetComponentsInChildren<MeshFilter>())
                {
                    if (mf.sharedMesh == null) continue;
                    var mc = mf.gameObject.AddComponent<MeshCollider>();
                    mc.sharedMesh = mf.sharedMesh;
                    colliders.Add(mc);
                }
            }
            Physics.SyncTransforms();
            // (a thing whose top is already in the stone finds the stone's top face from inside)
            bool backfaces = Physics.queriesHitBackfaces;
            Physics.queriesHitBackfaces = true;

            int kept = 0, removed = 0;
            foreach (var h in hanging)
            {
                if (Bounds(h, out var b) && TryPlace(h, b, out var spot))
                {
                    if (spot.layer != LayerOf(h)) F.ApplyLayer(h.gameObject, spot.layer);
                    h.position = spot.pivot;
                    var group = Group(deco, h);
                    if (spot.reach > 0.2f) RunChain(group, h, spot.top, spot.reach);
                    kept++;
                }
                else { Object.DestroyImmediate(h.gameObject); removed++; }
            }

            Physics.queriesHitBackfaces = backfaces;
            foreach (var c in colliders) Object.DestroyImmediate(c);
            Physics.SyncTransforms();
            return new Vector2Int(kept, removed);
        }

        struct Spot { public LevelLayer layer; public Vector3 pivot, top; public float reach; }

        static LevelLayer LayerOf(Transform h)
        {
            var tag = h.GetComponent<DepthLayer>();
            return tag != null ? tag.layer : LevelLayer.Near;
        }

        /// <summary>
        /// Where the thing can hang: where it is; else a little to the side; else on a deeper layer (a tower behind it),
        /// hung just in front of that layer's wall - under stone it can be chained to (a banner on the wall itself).
        /// The thing itself must not be stuck in stone there.
        /// </summary>
        static bool TryPlace(Transform h, Bounds b, out Spot spot)
        {
            var own = LayerOf(h);
            var layers = new List<LevelLayer> { own };
            foreach (var l in new[] { LevelLayer.Near, LevelLayer.Mid, LevelLayer.Far })
                if (DepthLayer.Z(l) > DepthLayer.Z(own)) layers.Add(l);
            foreach (var layer in layers)
            {
                float k = F.LayerScale(layer) / F.LayerScale(own);
                float topOff = (b.max.y - h.position.y) * k, bottomOff = (b.min.y - h.position.y) * k;
                float cxOff = (b.center.x - h.position.x) * k, halfDepth = b.extents.z * k;
                float zLayer = layer == own ? h.position.z : DepthLayer.Z(layer);
                for (int i = 0; i <= 16; i++)
                {
                    float dx = (i % 2 == 0 ? 1f : -1f) * ((i + 1) / 2) * 0.6f;
                    float px = h.position.x + dx, cx = px + cxOff;
                    float topY = h.position.y + topOff;
                    // a wall of this layer right behind (seen from in front)?
                    float z = zLayer;
                    bool wall = false;
                    if (FaceOfLayer(new Vector3(cx, topY - 0.3f, zLayer - 20f), layer, out var face))
                    {
                        float inFront = face.point.z - 0.12f - halfDepth;
                        if (layer != own || inFront < z) z = Mathf.Min(z, inFront);
                        wall = face.point.z - z <= WallBehind + halfDepth;
                        // (a banner hangs flat on the wall all the way down, not half over a window)
                        float halfW = b.extents.x * k * 0.85f;
                        for (int j = 0; wall && j < 12; j++)
                        {
                            float y = Mathf.Lerp(topY - 0.3f, h.position.y + bottomOff + 0.2f, (j / 3) / 3f);
                            float x = cx + (j % 3 - 1) * halfW;
                            wall = FaceOfLayer(new Vector3(x, y, zLayer - 20f), layer, out var under)
                                   && Mathf.Abs(under.point.z - face.point.z) < 0.4f;
                        }
                    }
                    var bottom = new Vector3(cx, h.position.y + bottomOff + 0.02f, z);
                    if (Physics.Raycast(bottom, Vector3.up, topY - bottom.y)) continue;   // stuck in stone
                    var top = new Vector3(cx, topY, z);
                    if (h.name == "Banner" && wall)
                    {
                        spot = new Spot { layer = layer, pivot = new Vector3(px, h.position.y, z), top = top, reach = 0f };
                        return true;
                    }
                    if (Physics.Raycast(top - new Vector3(0f, 0.03f, 0f), Vector3.up, out var hit, MaxReach))
                    {
                        spot = new Spot { layer = layer, pivot = new Vector3(px, h.position.y, z), top = top, reach = hit.distance + 0.18f };
                        return true;
                    }
                }
            }
            spot = default;
            return false;
        }

        /// <summary>The nearest stone face of <paramref name=layer/> looking into the scene from <paramref name="from"/>.</summary>
        static bool FaceOfLayer(Vector3 from, LevelLayer layer, out RaycastHit face)
        {
            face = default;
            float best = float.MaxValue;
            foreach (var hit in Physics.RaycastAll(from, Vector3.forward, 40f))
            {
                if (Mathf.Abs(hit.point.z - DepthLayer.Z(layer)) >= 8f || hit.normal.z > -0.3f || hit.distance >= best) continue;
                best = hit.distance; face = hit;
            }
            return best < float.MaxValue;
        }

        static Transform Group(Transform deco, Transform h)
        {
            var g = new GameObject(GroupName).transform;
            g.SetParent(deco, false);
            g.position = h.position;
            h.SetParent(g, true);
            return g;
        }

        /// <summary>Chain links stacked from <paramref name="from"/> straight up <paramref name="distance"/> m.</summary>
        static void RunChain(Transform group, Transform h, Vector3 from, float distance)
        {
            // A chain's own length is the model's (its pivot is its top); cages and banners get a fresh chain over them.
            var tag = h.GetComponent<DepthLayer>();
            var layer = tag != null ? tag.layer : LevelLayer.Near;
            float scale = h.name == "Chain" ? h.localScale.x : F.LayerScale(layer);
            var probe = F.Ruin("Chain", Vector3.zero, layer);
            probe.transform.localScale = Vector3.one * scale;
            float model = Bounds(probe.transform, out var pb) ? pb.size.y : 3.8f;
            Object.DestroyImmediate(probe);

            float y = from.y;
            foreach (float len in ChainRun(distance, model))
            {
                var link = F.Ruin("Chain", Vector3.zero, layer);
                link.transform.SetParent(group, true);
                link.transform.position = new Vector3(from.x, y + len, from.z);
                link.transform.localScale = new Vector3(scale, scale * len / model, scale);
                y += len;
            }
        }

        static bool Bounds(Transform t, out Bounds b)
        {
            b = default;
            bool any = false;
            foreach (var r in t.GetComponentsInChildren<Renderer>())
            {
                if (!any) { b = r.bounds; any = true; }
                else b.Encapsulate(r.bounds);
            }
            return any;
        }
    }
}
