using CrimsonVeil.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using F = CrimsonVeil.EditorTools.LevelKitFactory;

namespace CrimsonVeil.EditorTools
{
    /// <summary>
    /// Rebuilds the prototype level from Level Kit pieces (the same ones the Level Editor places).
    /// Menu: Crimson Veil ▸ Build Prototype Scene.
    ///
    /// Sections (left → right):
    ///  1. Start: gallows, urns, first wraith.
    ///  2. Ledge: a block higher than a jump — grab the edge and climb.
    ///  3. Shaft: two walls — wall slide and wall jump up.
    ///  4. High road: jump down to the crimson tree.
    ///  5. Chasm: shoot the crimson rope, the plank falls as a bridge.
    /// </summary>
    public static class PrototypeSceneBuilder
    {
        const string ScenePath = LevelKitMaterials.Root + "/Scenes/Prototype_Ruins.unity";

        [MenuItem("Crimson Veil/Build Prototype Scene")]
        public static void Build()
        {
            LevelKitMaterials.Get(refresh: true);
            ConfigureModelImport();
            LevelKitCatalog.RebuildPrefabs();

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            F.ApplyRenderSettings();
            Place("LevelEssentials", Vector3.zero, LevelLayer.Playfield, null);

            var level = new GameObject("Level").transform;
            var terrain = Group(level, "Terén");
            var play = Group(level, "Hratelnost");
            var deco = Group(level, "Dekorace");

            // ---------------------------------------------------------------- terrain
            T(terrain, -14f, 14f, 2f, 18f, TerrainStyle.Ruin, 1, false);    // back wall
            T(terrain, -1.5f, 0f, 25f, 12f, TerrainStyle.Rock, 2);          // start floor  (-14 .. 11)
            T(terrain, 14.5f, 3.6f, 5f, 4.2f, TerrainStyle.Ruin, 3);        // ledge block (grab & climb)
            T(terrain, 21f, 0.6f, 8f, 10f, TerrainStyle.Rock, 4);            // landing     (17 .. 25)
            T(terrain, 25.7f, 9f, 1.4f, 6.4f, TerrainStyle.Ruin, 5, false); // shaft left wall (hangs: walk in underneath, 2 m gap)
            T(terrain, 28f, -1.2f, 3.2f, 4f, TerrainStyle.Rock, 6);          // shaft floor (3.2 m wide shaft)
            T(terrain, 30.3f, 7f, 1.4f, 9f, TerrainStyle.Ruin, 7, false);   // shaft right wall
            T(terrain, 36.5f, 7f, 11f, 1.4f, TerrainStyle.Rock, 8);          // high road   (31 .. 42)
            T(terrain, 49f, 5f, 7f, 1.2f, TerrainStyle.Rock, 9);             // stepping ledge
            T(terrain, 69f, 0f, 22f, 12f, TerrainStyle.Rock, 10);            // tree plain  (58 .. 80)
            T(terrain, 55.5f, 2.2f, 3f, 0.8f, TerrainStyle.Rock, 11);        // little ledge down
            T(terrain, 103f, 0f, 24f, 12f, TerrainStyle.Rock, 12);           // far side    (91 .. 115)
            T(terrain, 116f, 14f, 2f, 18f, TerrainStyle.Ruin, 13, false);   // end wall
            T(terrain, 98.5f, 4.6f, 4.5f, 1f, TerrainStyle.Ruin, 14, false); // overhang (a root crawler hangs under it)
            T(terrain, -11f, 6f, 4f, 1f, TerrainStyle.Ruin, 15, false);     // high perch (-13 .. -9): only the whip swing reaches it

            // ---------------------------------------------------------------- gameplay
            Place("PlayerSpawn", new Vector3(-7f, 0.05f, 0f), LevelLayer.Playfield, play);
            Place("Player", new Vector3(-7f, 0.05f, 0f), LevelLayer.Playfield, null);
            foreach (var (x, y) in new[] { (-1.2f, 0f), (-0.4f, 0f), (0.5f, 0f), (62f, 0f), (62.8f, 0f), (95f, 0f), (110f, 0f) })
                Place("Urn", new Vector3(x, y, 0f), LevelLayer.Playfield, play);
            foreach (var (x, y) in new[] { (3f, 0f), (36f, 7f), (61f, 0f), (110f, 0f) })
                Place("Wraith", new Vector3(x, y, 0f), LevelLayer.Playfield, play);
            Place("Checkpoint", new Vector3(21f, 0.6f, 0f), LevelLayer.Playfield, play);
            Place("Checkpoint", new Vector3(60f, 0f, 0f), LevelLayer.Playfield, play);
            Place("Checkpoint", new Vector3(93f, 0f, 0f), LevelLayer.Playfield, play);
            Place("RopeBridge", new Vector3(80.2f, 0f, 0f), LevelLayer.Playfield, play);
            // Whip rings: swing up to the high perch by the start (an urn waits there), and across the chasm
            // before the far side — the other way over besides cutting the bridge down.
            Place("GrapplePoint", new Vector3(-1f, 7.8f, 0f), LevelLayer.Playfield, play);
            Place("Urn", new Vector3(-11.5f, 6f, 0f), LevelLayer.Playfield, play);
            Place("GrapplePoint", new Vector3(86.5f, 7.5f, 0f), LevelLayer.Playfield, play);
            // The hero starts with the sword only; each relic teaches a weapon just before the place that needs it.
            Place("RelicWhip", new Vector3(-4f, 0f, 0f), LevelLayer.Playfield, play);     // the ring and the high perch right after
            Place("RelicSpear", new Vector3(19f, 0.6f, 0f), LevelLayer.Playfield, play);  // pogo / pierce from here on
            Place("RelicShield", new Vector3(33f, 7f, 0f), LevelLayer.Playfield, play);   // before the shadow double
            Place("RelicBow", new Vector3(63f, 0f, 0f), LevelLayer.Playfield, play);      // the bridge's rope waits at the chasm
            Place("Secret", new Vector3(-11.5f, 6.8f, 0f), LevelLayer.Playfield, play);   // the urn on the high perch

            // Bestiary: each creature where it teaches something.
            Place("ShieldMonk", new Vector3(9f, 0f, 0f), LevelLayer.Playfield, play);       // climb the ledge block to get behind him
            Place("ShadowDouble", new Vector3(40f, 7f, 0f), LevelLayer.Playfield, play);    // leap to the stepping ledge: it walks off the edge
            Place("PaperKite", new Vector3(50.5f, 5f, 0f), LevelLayer.Playfield, play);     // cut it down, glide off the ledge
            Place("HangedMan", new Vector3(68.2f, 1.4f, 0f), LevelLayer.Playfield, play);   // hangs from the crimson tree
            Place("CrowSwarm", new Vector3(70f, 3.5f, 0f), LevelLayer.Playfield, play);
            Place("LanternWisp", new Vector3(77f, 1.6f, 0f), LevelLayer.Playfield, play);   // lures over the gap before the bridge
            Place("RootCrawler", new Vector3(98.5f, 2.6f, 0f), LevelLayer.Playfield, play); // under the overhang
            Place("AshMoth", new Vector3(105f, 5f, 0f), LevelLayer.Playfield, play);
            var shaftZone = Place("CameraZone", new Vector3(28f, 0f, 0f), LevelLayer.Playfield, play).GetComponent<CrimsonVeil.CameraSystem.CameraZone>();
            shaftZone.minX = -5f; shaftZone.maxX = 5f; shaftZone.offset = new Vector3(0f, 4.5f, -17f);

            // ---------------------------------------------------------------- decoration: playfield
            Deco("Gallows", new Vector3(-7.5f, 0f, 0f), LevelLayer.Playfield, deco); // clear of the high perch above
            var tree = Deco("CrimsonTree", new Vector3(66f, 0f, 0f), LevelLayer.Playfield, deco);
            Deco("FallingLeaves", new Vector3(66f, 11f, 0f), LevelLayer.Playfield, deco);
            Deco("FallingLeaves", new Vector3(10f, 11f, 0f), LevelLayer.Playfield, deco);
            Deco("FallingLeaves", new Vector3(100f, 11f, 0f), LevelLayer.Playfield, deco);
            var playArch = F.Arch(new Vector3(74f, 0f, 0f), LevelLayer.Playfield, 4.2f, 3.6f, 0.35f, 4);
            playArch.transform.SetParent(deco, true);
            playArch.transform.position += new Vector3(0f, 0f, 1.6f);
            foreach (var x in new[] { -4f, 8f, 20f, 45f, 63f, 77f, 98f, 112f })
                F.Rubble(new Vector3(x, x > 17f && x < 24f ? 0.6f : 0f, 1.6f), LevelLayer.Playfield, (int)x + 50).transform.SetParent(deco, true);
            foreach (var x in new[] { 3f, 35f, 88f })
                F.Column(new Vector3(x, x > 30f && x < 43f ? 7f : 0f, 1.7f), LevelLayer.Playfield, 5f, true, (int)x).transform.SetParent(deco, true);

            // ---------------------------------------------------------------- decoration: foreground
            // Foreground grass grows on the ground under it (never hanging in the air over a pit) and is rooted
            // in the front lip of that ground — the same depth as the rock — so it moves with the ground at any
            // camera angle instead of sliding against it in parallax. Roots sink a little into the lip.
            Physics.SyncTransforms();
            for (int i = 0; i < 46; i++)
            {
                float gx = -16f + i * 2.9f;
                if (!GroundSpan(gx, 1.5f, out float gy, out float span)) continue;
                OnLip(F.Grass(new Vector3(gx, gy - 0.1f - (i % 3) * 0.08f, 0f), LevelLayer.Foreground, 100 + i, span, new Vector2(0.6f, 2.1f), Mathf.RoundToInt(40 * span / 3.4f), null, 0.07f), gx, deco);
                if (i % 4 == 1 && GroundSpan(gx + 1f, 1f, out float fy, out _))
                    OnLip(F.Fern(new Vector3(gx + 1f, fy - 0.1f, 0f), LevelLayer.Foreground, 400 + i, 2.2f, 11), gx + 1f, deco);
                // Thorny brambles and dry seed-heads between the grass tufts (key art density).
                string fgModel = i % 8 == 3 ? "Bramble" : "Weeds";
                float fgScale = 1.1f + 0.15f * (i % 3);
                if (i % 4 == 3 && Fits(fgModel, gx + 0.6f, fgScale, out float by))
                    OnLip(FgModel(fgModel, new Vector3(gx + 0.6f, by - 0.08f, 0f), LevelLayer.Foreground, deco, fgScale, i % 2 == 0), gx + 0.6f, deco);
            }
            // The same weeds and brambles behind the play plane, at the feet of walls and on ledges.
            for (int i = 0; i < 30; i++)
            {
                float gx = -14f + i * 4.4f + (i % 3) * 0.7f;
                string model = i % 3 == 0 ? "Bramble" : "Weeds";
                float scale = 0.8f + 0.1f * (i % 4);
                if (!Fits(model, gx, scale, out float gy)) continue;
                var m = FgModel(model, new Vector3(gx, gy - 0.05f, 0f), LevelLayer.Playfield, deco, scale, i % 2 == 1);
                m.transform.position += new Vector3(0f, 0f, 1.3f + 0.2f * (i % 3));
                OnLip(m, gx, deco); // Playfield: the anchor keeps it on the block's top, at this depth
            }
            // Ferns behind the play plane, at the foot of walls and ledges.
            foreach (var (x, y) in new[] { (-12f, 0f), (11f, 0f), (17.5f, 0.6f), (30f, -1.2f), (57f, 0f), (79f, 0f), (91.5f, 0f), (114f, 0f) })
            {
                var fern = F.Fern(new Vector3(x, y, 0f), LevelLayer.Playfield, (int)(x * 7f) + 900, 1.3f, 8);
                fern.transform.position += new Vector3(0f, 0f, 1.5f);
                fern.transform.SetParent(deco, true);
            }
            F.Block(deco, "BrokenPost", new Vector3(-4f, -1f, -3.6f), new Vector3(0.5f, 3.2f, 0.5f), LevelKitMaterials.Get().inkFG, false).transform.rotation = Quaternion.Euler(0, 0, 8f);
            F.Block(deco, "BrokenPost", new Vector3(52f, -1.2f, -3.6f), new Vector3(0.6f, 3f, 0.6f), LevelKitMaterials.Get().inkFG, false).transform.rotation = Quaternion.Euler(0, 0, -12f);

            // ---------------------------------------------------------------- decoration: near ruins
            float[] nearBreak = { 0f, 0.55f, 0.2f, 0.75f, 0f, 0.4f, 0.65f, 0.1f, 0.5f, 0.3f, 0f, 0.6f, 0.25f };
            for (int i = 0; i < 13; i++)
            {
                float x = -14f + i * 10.5f;
                F.Arch(new Vector3(x, -1f, 0f), LevelLayer.Near, 3.6f + (i % 3) * 0.6f, 4.5f + (i % 2), nearBreak[i], i).transform.SetParent(deco, true);
                F.Rubble(new Vector3(x + 3.5f, -1f, 0f), LevelLayer.Near, 200 + i, 3.5f, 1.2f).transform.SetParent(deco, true);
                if (i % 3 == 1) F.Column(new Vector3(x + 5.2f, -1f, 0f), LevelLayer.Near, 7f, i % 2 == 0, i).transform.SetParent(deco, true);
                FgModel(i % 2 == 0 ? "Bramble" : "Weeds", new Vector3(x + 1.5f, -1f, 0f), LevelLayer.Near, deco, 1.3f, i % 3 == 0);
                if (i % 4 == 2) // a chain swinging from the broken arch
                    FgModel("Chain", new Vector3(x + 2f, 3.6f, 0f), LevelLayer.Near, deco, 1f, false);
            }
            foreach (var x in new[] { -9f, 37f, 70f, 105f })
                F.Ruin("RuinWall", new Vector3(x, -1f, 0f), LevelLayer.Near).transform.SetParent(deco, true);
            foreach (var x in new[] { 15f, 88f })
                F.Ruin("RuinStairs", new Vector3(x, -1f, 0f), LevelLayer.Near).transform.SetParent(deco, true);
            foreach (var (x, y, h) in new[] { (6.6f, 8.6f, 3.6f), (29f, 8.2f, 3f), (51f, 8.6f, 3.4f), (95f, 8.4f, 3.2f) })
                F.Banner(new Vector3(x, y, 0f), LevelLayer.Near, 1.1f, h, (int)x).transform.SetParent(deco, true);
            F.Waterfall(new Vector3(27.5f, 14f, 0f), LevelLayer.Near, 2.4f, 16f).transform.SetParent(deco, true);

            // ---------------------------------------------------------------- decoration: mid
            // The two-tier aqueduct (key art) runs behind the start; a cliff with a village pours a waterfall.
            F.Ruin("Aqueduct", new Vector3(-32f, -2f, 0f), LevelLayer.Mid).transform.SetParent(deco, true);
            F.Ruin("CliffRuin", new Vector3(34f, -2f, 0f), LevelLayer.Mid).transform.SetParent(deco, true);
            F.Waterfall(new Vector3(44.6f, 21f, 0f), LevelLayer.Mid, 3.2f, 23f).transform.SetParent(deco, true);
            for (int i = 0; i < 6; i++)
                F.Arch(new Vector3(62f + i * 11f, -2f + (i % 2) * 1.5f, 0f), LevelLayer.Mid, 3.75f, 4.5f + (i % 3) * 0.5f, 0.3f + 0.15f * (i % 3), i + 1).transform.SetParent(deco, true);
            F.Ruin("Aqueduct", new Vector3(128f, -2f, 0f), LevelLayer.Mid).transform.SetParent(deco, true);
            for (int i = 0; i < 12; i++) // brambles and weeds along the mid ground
                FgModel(i % 2 == 0 ? "Weeds" : "Bramble", new Vector3(-30f + i * 13f, -2f, 0f), LevelLayer.Mid, deco, 1.6f, i % 3 == 0);

            // ---------------------------------------------------------------- decoration: far & sky
            float[] farBreak = { 0f, 0.3f, 0f, 0f, 0.6f, 0f, 0.2f, 0f, 0f, 0.45f, 0f, 0.7f, 0f, 0.25f, 0f };
            for (int i = 0; i < 15; i++)
                F.Arch(new Vector3(-60f + i * 13f, -12f + (i % 3) * 1.5f, 0f), LevelLayer.Far, 3.4f, 5.8f, farBreak[i], i).transform.SetParent(deco, true);
            foreach (var (x, y) in new[] { (-22f, -12f), (130f, -11f) })
                F.Ruin("Tower", new Vector3(x, y, 0f), LevelLayer.Far).transform.SetParent(deco, true);
            // The castle on the horizon and a second aqueduct crossing the far valley.
            var castle = F.Ruin("Castle", new Vector3(66f, -12f, 0f), LevelLayer.Far);
            castle.transform.localScale = Vector3.one * 1.7f;
            castle.transform.SetParent(deco, true);
            F.Ruin("Aqueduct", new Vector3(-10f, -12f, 0f), LevelLayer.Far).transform.SetParent(deco, true);
            F.Ruin("CliffRuin", new Vector3(118f, -14f, 0f), LevelLayer.Far).transform.SetParent(deco, true);
            foreach (var (x, y) in new[] { (8f, -2f), (58f, -2f), (98f, -2f) })
                F.Ruin("Tower", new Vector3(x, y, 0f), LevelLayer.Mid).transform.SetParent(deco, true);
            foreach (var (x, y) in new[] { (-30f, -2f), (25f, -2f), (80f, -2f), (115f, -2f) })
                F.Ruin("RuinWall", new Vector3(x, y, 0f), LevelLayer.Mid).transform.SetParent(deco, true);
            F.Waterfall(new Vector3(14f, 16f, 0f), LevelLayer.Far, 4f, 30f).transform.SetParent(deco, true);
            F.Block(deco, "DistantCliffs", new Vector3(30f, -10f, 110f), new Vector3(500f, 24f, 4f), LevelKitMaterials.Get().inkSky, false);

            // ---------------------------------------------------------------- mist & light
            F.MistBand(new Vector3(50f, -1.6f, 1.2f), LevelLayer.Playfield, 180f, 2.6f).transform.SetParent(deco, true);
            foreach (var (y, l, w, h, dz) in new[] { (-1f, LevelLayer.Near, 220f, 7f, -4f), (0f, LevelLayer.Mid, 300f, 12f, -10f), (-2f, LevelLayer.Far, 400f, 22f, -20f) })
            {
                var band = F.MistBand(new Vector3(50f, y, 0f), l, w, h);
                band.transform.position += new Vector3(0f, 0f, dz);
                band.transform.SetParent(deco, true);
            }
            for (int i = 0; i < 6; i++)
                F.LightShaft(new Vector3(-6f + i * 21f, 12f, 0f), LevelLayer.Mid, 5f + i % 3, 34f).transform.SetParent(deco, true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[CrimsonVeil] Prototype scene built from the level kit: " + ScenePath);
        }

        /// <summary>
        /// Depth of the front lip of the terrain under x (just behind its front face), or the play plane's
        /// front edge when there is none. Foreground plants rooted here stay glued to the ground in parallax.
        /// </summary>
        public static float LipZ(float x)
            => GroundAnchor.FindGround(x, 60f, out _, out var block) ? GroundAnchor.LipZ(block) : -1.9f;

        /// <summary>Roots a foreground plant in the ground lip with a <see cref="GroundAnchor"/>, so it also
        /// stays there when the level is edited later.</summary>
        static GameObject OnLip(GameObject go, float x, Transform parent)
        {
            var p = go.transform.position;
            var anchor = go.GetComponent<GroundAnchor>();
            if (anchor == null) anchor = go.AddComponent<GroundAnchor>();
            if (GroundAnchor.FindGround(x, p.y + 1.5f, out float top, out _)) anchor.sink = Mathf.Max(0f, top - p.y);
            anchor.Snap();
            go.transform.SetParent(parent, true);
            return go;
        }

        /// <summary>Half-width of a modelled plant at scale 1 (bramble canes spread ~1.5 m each way, weeds ~0.9 m).</summary>
        public static float PlantHalfWidth(string model) => model == "Bramble" ? 1.5f : 0.9f;

        /// <summary>True when the whole plant fits on level ground at x — it never hangs over a ledge's edge.</summary>
        public static bool Fits(string model, float x, float scale, out float y)
        {
            float hw = PlantHalfWidth(model) * scale;
            return GroundSpan(x, hw, out y, out float span) && span >= hw * 2.2f - 1e-3f;
        }

        /// <summary>A modelled prop (bramble, weeds, chain…) on a layer, scaled and optionally mirrored for variety.</summary>
        static GameObject FgModel(string model, Vector3 pos, LevelLayer layer, Transform parent, float scale, bool mirror)
        {
            var go = F.Ruin(model, pos, layer);
            go.transform.localScale *= scale;
            if (mirror) go.transform.rotation = Quaternion.Euler(0f, 180f, 0f); // turned round, not negatively scaled (keeps winding)
            go.transform.SetParent(parent, true);
            return go;
        }

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        /// <summary>Top of the terrain under x (topmost TerrainBlock surface), or NaN when there is none.</summary>
        public static float GroundTop(float x)
        {
            float best = float.NaN;
            foreach (var hit in Physics.RaycastAll(new Vector3(x, 60f, 0f), Vector3.down, 120f, ~0, QueryTriggerInteraction.Ignore))
                if (hit.collider.GetComponentInParent<TerrainBlock>() != null && (float.IsNaN(best) || hit.point.y > best))
                    best = hit.point.y;
            return best;
        }

        /// <summary>
        /// Finds level ground around x for a clump of half-width <paramref name="halfWidth"/> (shrinking it near
        /// edges). False when x is over a pit or a step, so decoration never hangs in the air.
        /// </summary>
        public static bool GroundSpan(float x, float halfWidth, out float y, out float width)
        {
            for (float hw = halfWidth; hw >= 0.5f; hw *= 0.5f)
            {
                float a = GroundTop(x - hw), b = GroundTop(x), c = GroundTop(x + hw);
                if (float.IsNaN(a) || float.IsNaN(b) || float.IsNaN(c)) continue;
                if (Mathf.Max(a, Mathf.Max(b, c)) - Mathf.Min(a, Mathf.Min(b, c)) > 0.3f) continue;
                y = Mathf.Min(a, Mathf.Min(b, c));
                width = hw * 2.2f;
                return true;
            }
            y = 0f; width = 0f;
            return false;
        }

        static GameObject T(Transform parent, float centerX, float topY, float width, float height, TerrainStyle style, int seed, bool grass = true)
        {
            var go = Place(height > width * 1.5f ? "Wall" : "Platform", new Vector3(centerX, topY, 0f), LevelLayer.Playfield, parent);
            var t = go.GetComponent<TerrainBlock>();
            t.width = width; t.height = height; t.style = style; t.seed = seed; t.grassOnTop = grass;
            t.hangingRoots = height < 6f;
            t.Rebuild();
            return go;
        }

        static GameObject Place(string id, Vector3 pos, LevelLayer layer, Transform parent)
            => LevelKitCatalog.Place(LevelKitCatalog.Find(id), pos, layer, parent);

        static GameObject Deco(string id, Vector3 pos, LevelLayer layer, Transform parent)
            => LevelKitCatalog.Place(LevelKitCatalog.Find(id), pos, layer, parent);

        static void ConfigureModelImport()
        {
            foreach (var path in new[] { F.HeroPath, F.WraithPath })
            {
                if (!(AssetImporter.GetAtPath(path) is ModelImporter mi)) continue;
                bool dirty = mi.animationType != ModelImporterAnimationType.Generic || mi.importAnimation || mi.materialImportMode != ModelImporterMaterialImportMode.None;
                mi.animationType = ModelImporterAnimationType.Generic;
                mi.importAnimation = false;
                mi.materialImportMode = ModelImporterMaterialImportMode.None;
                mi.importCameras = false;
                mi.importLights = false;
                if (dirty) mi.SaveAndReimport();
            }
        }
    }
}
