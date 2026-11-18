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
    ///  6.-9. The tower, the ramparts, the well and the climb out; 10.-14. the belfry, the ridge, the chasm and the
    ///  garden (see Extension).
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
            T(terrain, 98.5f, 4.6f, 4.5f, 1f, TerrainStyle.Ruin, 14, false); // overhang (a root crawler hangs under it)
            T(terrain, -11f, 6f, 4f, 1f, TerrainStyle.Ruin, 15, false);     // high perch (-13 .. -9): only the whip swing reaches it
            // The way on (the tower, the ramparts, the well, the climb out): see Extension.
            foreach (var b in Extension) T(terrain, b.cx, b.top, b.width, b.height, b.style, b.seed, b.grass);

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

            // ---- 6. The tower: ledges to and fro up its inside, a wall-jump shaft, a ring; out at the top onto the ramparts.
            Place("Checkpoint", new Vector3(119f, 0f, 0f), LevelLayer.Playfield, play);
            Place("RootCrawler", new Vector3(119.5f, 5f, 0f), LevelLayer.Playfield, play);    // under ledge B, over the way in
            Place("Urn", new Vector3(120f, 8f, 0f), LevelLayer.Playfield, play);
            Place("Wraith", new Vector3(128.5f, 12f, 0f), LevelLayer.Playfield, play);
            Place("Urn", new Vector3(130f, 12f, 0f), LevelLayer.Playfield, play);
            Place("GrapplePoint", new Vector3(125f, 21f, 0f), LevelLayer.Playfield, play);    // a swing up to the top ledge, besides the shaft
            Place("AshMoth", new Vector3(125f, 18f, 0f), LevelLayer.Playfield, play);
            Place("Checkpoint", new Vector3(128f, 25f, 0f), LevelLayer.Playfield, play);
            Zone(play, 124f, -9f, 9f, new Vector3(0f, 3.5f, -21f));
            // ---- 7. The ramparts, high over the valley: gaps, a kite, crows, a monk; a ring over the widest gap.
            Place("ShieldMonk", new Vector3(138f, 25f, 0f), LevelLayer.Playfield, play);
            Place("PaperKite", new Vector3(143f, 28f, 0f), LevelLayer.Playfield, play);
            Place("Urn", new Vector3(147.5f, 26.5f, 0f), LevelLayer.Playfield, play);
            Place("Checkpoint", new Vector3(158f, 24f, 0f), LevelLayer.Playfield, play);
            Place("CrowSwarm", new Vector3(160f, 27.5f, 0f), LevelLayer.Playfield, play);
            Place("GrapplePoint", new Vector3(166f, 30.5f, 0f), LevelLayer.Playfield, play);
            Place("Wraith", new Vector3(173f, 25f, 0f), LevelLayer.Playfield, play);
            // ---- 8. The well: down from ledge to ledge into the deep, a crawler under the last one, a wisp at the bottom.
            Place("RootCrawler", new Vector3(181f, 1.5f, 0f), LevelLayer.Playfield, play);
            Place("Checkpoint", new Vector3(190f, -4f, 0f), LevelLayer.Playfield, play);
            Place("Urn", new Vector3(184f, -4f, 0f), LevelLayer.Playfield, play);
            Place("LanternWisp", new Vector3(191f, -1.5f, 0f), LevelLayer.Playfield, play);
            Place("Wraith", new Vector3(193f, -4f, 0f), LevelLayer.Playfield, play);
            Zone(play, 186f, -11f, 11f, new Vector3(0f, 2.5f, -21f));
            // ---- 9. Out of the well up the steps to the last plateau, the end wall.
            Place("Checkpoint", new Vector3(204f, 7f, 0f), LevelLayer.Playfield, play);
            Place("ShieldMonk", new Vector3(214.5f, 7f, 0f), LevelLayer.Playfield, play);   // waiting past the tunnel
            Place("Wraith", new Vector3(217f, 7f, 0f), LevelLayer.Playfield, play);
            Place("Urn", new Vector3(220f, 7f, 0f), LevelLayer.Playfield, play);
            // ---- 10. The belfry: up ledge by ledge, a shaft by its right wall, a ring; out onto its roof at 38 m.
            Place("Checkpoint", new Vector3(226f, 7f, 0f), LevelLayer.Playfield, play);
            Place("RootCrawler", new Vector3(226f, 12.5f, 0f), LevelLayer.Playfield, play);   // under ledge 2, over the way in
            Place("Wraith", new Vector3(236f, 19f, 0f), LevelLayer.Playfield, play);
            Place("Urn", new Vector3(224.5f, 23f, 0f), LevelLayer.Playfield, play);
            Place("GrapplePoint", new Vector3(230f, 33f, 0f), LevelLayer.Playfield, play);    // a swing up past the shaft
            Place("AshMoth", new Vector3(229f, 30f, 0f), LevelLayer.Playfield, play);
            Place("Urn", new Vector3(226f, 38f, 0f), LevelLayer.Playfield, play);             // the lookout over the pillar
            Place("Secret", new Vector3(224.5f, 38.8f, 0f), LevelLayer.Playfield, play);
            Zone(play, 231f, -9f, 9f, new Vector3(0f, 3.5f, -22f));
            // ---- 11. The ridge: floating stones high over the valley.
            Place("Checkpoint", new Vector3(246f, 38f, 0f), LevelLayer.Playfield, play);
            Place("PaperKite", new Vector3(266f, 48f, 0f), LevelLayer.Playfield, play);
            Place("GrapplePoint", new Vector3(274f, 49f, 0f), LevelLayer.Playfield, play);
            Place("CrowSwarm", new Vector3(274f, 47f, 0f), LevelLayer.Playfield, play);
            Place("ShieldMonk", new Vector3(279.5f, 44f, 0f), LevelLayer.Playfield, play);
            // ---- 12. The chasm: down from ledge to ledge to its floor.
            Place("LanternWisp", new Vector3(291f, 18f, 0f), LevelLayer.Playfield, play);
            Place("RootCrawler", new Vector3(288f, 5.5f, 0f), LevelLayer.Playfield, play);    // under ledge 5
            Place("GrapplePoint", new Vector3(300f, -4f, 0f), LevelLayer.Playfield, play);
            Place("Checkpoint", new Vector3(298f, -14f, 0f), LevelLayer.Playfield, play);
            Place("Wraith", new Vector3(292f, -14f, 0f), LevelLayer.Playfield, play);
            Place("Urn", new Vector3(284f, -14f, 0f), LevelLayer.Playfield, play);
            Zone(play, 296f, -15f, 15f, new Vector3(0f, 2f, -24f));
            // ---- 13./14. Up the shaft out of the chasm into the garden; the end.
            Place("Checkpoint", new Vector3(316f, 2f, 0f), LevelLayer.Playfield, play);
            Place("ShieldMonk", new Vector3(329f, 2f, 0f), LevelLayer.Playfield, play);     // past the tunnel
            Place("Wraith", new Vector3(334f, 2f, 0f), LevelLayer.Playfield, play);
            Place("Urn", new Vector3(336f, 2f, 0f), LevelLayer.Playfield, play);

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
            for (int i = 0; i < 124; i++)
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
            for (int i = 0; i < 81; i++)
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
            foreach (var (x, y) in new[] { (-12f, 0f), (11f, 0f), (17.5f, 0.6f), (30f, -1.2f), (57f, 0f), (79f, 0f), (91.5f, 0f), (114f, 0f),
                                           (132f, 0f), (134f, 25f), (176.5f, -4f), (194.5f, -4f), (203.5f, 7f), (221f, 7f) })
            {
                var fern = F.Fern(new Vector3(x, y, 0f), LevelLayer.Playfield, (int)(x * 7f) + 900, 1.3f, 8);
                fern.transform.position += new Vector3(0f, 0f, 1.5f);
                fern.transform.SetParent(deco, true);
            }
            F.Block(deco, "BrokenPost", new Vector3(-4f, -1f, -3.6f), new Vector3(0.5f, 3.2f, 0.5f), LevelKitMaterials.Get().inkFG, false).transform.rotation = Quaternion.Euler(0, 0, 8f);
            F.Block(deco, "BrokenPost", new Vector3(52f, -1.2f, -3.6f), new Vector3(0.6f, 3f, 0.6f), LevelKitMaterials.Get().inkFG, false).transform.rotation = Quaternion.Euler(0, 0, -12f);

            // ---------------------------------------------------------------- decoration: near ruins
            float[] nearBreak = { 0f, 0.55f, 0.2f, 0.75f, 0f, 0.4f, 0.65f, 0.1f, 0.5f, 0.3f, 0f, 0.6f, 0.25f };
            for (int i = 0; i < 34; i++)
            {
                float x = -14f + i * 10.5f;
                F.Arch(new Vector3(x, -1f, 0f), LevelLayer.Near, 3.6f + (i % 3) * 0.6f, 4.5f + (i % 2), nearBreak[i % nearBreak.Length], i).transform.SetParent(deco, true);
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
            for (int i = 0; i < 25; i++)
                F.Arch(new Vector3(62f + i * 11f, -2f + (i % 2) * 1.5f, 0f), LevelLayer.Mid, 3.75f, 4.5f + (i % 3) * 0.5f, 0.3f + 0.15f * (i % 3), i + 1).transform.SetParent(deco, true);
            F.Ruin("Aqueduct", new Vector3(128f, -2f, 0f), LevelLayer.Mid).transform.SetParent(deco, true);
            for (int i = 0; i < 29; i++) // brambles and weeds along the mid ground
                FgModel(i % 2 == 0 ? "Weeds" : "Bramble", new Vector3(-30f + i * 13f, -2f, 0f), LevelLayer.Mid, deco, 1.6f, i % 3 == 0);

            // ---------------------------------------------------------------- decoration: far & sky
            float[] farBreak = { 0f, 0.3f, 0f, 0f, 0.6f, 0f, 0.2f, 0f, 0f, 0.45f, 0f, 0.7f, 0f, 0.25f, 0f };
            for (int i = 0; i < 32; i++)
                F.Arch(new Vector3(-60f + i * 13f, -12f + (i % 3) * 1.5f, 0f), LevelLayer.Far, 3.4f, 5.8f, farBreak[i % farBreak.Length], i).transform.SetParent(deco, true);
            foreach (var (x, y) in new[] { (-22f, -12f), (130f, -11f) })
                F.Ruin("Tower", new Vector3(x, y, 0f), LevelLayer.Far).transform.SetParent(deco, true);
            // The castle on the horizon and a second aqueduct crossing the far valley.
            var castle = F.Ruin("Castle", new Vector3(66f, -12f, 0f), LevelLayer.Far);
            castle.transform.localScale = Vector3.one * 1.7f;
            castle.transform.SetParent(deco, true);
            F.Ruin("Aqueduct", new Vector3(-10f, -12f, 0f), LevelLayer.Far).transform.SetParent(deco, true);
            F.Ruin("CliffRuin", new Vector3(118f, -14f, 0f), LevelLayer.Far).transform.SetParent(deco, true);
            // A gothic skyline on the horizon, dead trees and statues through the depths, cages hanging from the arches.
            F.Ruin("Spires", new Vector3(25f, -16f, 0f), LevelLayer.Sky).transform.SetParent(deco, true);
            F.Ruin("Spires", new Vector3(105f, -15f, 0f), LevelLayer.Far).transform.SetParent(deco, true);
            foreach (var (x, y, l) in new[] { (-24f, -2f, LevelLayer.Mid), (49f, -2f, LevelLayer.Mid), (93f, -2f, LevelLayer.Mid), (6f, -12f, LevelLayer.Far), (72f, -12f, LevelLayer.Far), (32f, -1f, LevelLayer.Near) })
                F.Ruin("DeadTree", new Vector3(x, y, 0f), l).transform.SetParent(deco, true);
            foreach (var (x, y, l) in new[] { (11f, -1f, LevelLayer.Near), (82f, -1f, LevelLayer.Near), (-8f, -2f, LevelLayer.Mid), (66f, -2f, LevelLayer.Mid) })
                F.Ruin("Statue", new Vector3(x, y, 0f), l).transform.SetParent(deco, true);
            foreach (var (x, y) in new[] { (-11.5f, 9.6f), (40f, 9.3f), (98f, 9.4f) })
                F.Ruin("Cage", new Vector3(x, y, 0f), LevelLayer.Near).transform.SetParent(deco, true);
            foreach (var (x, y) in new[] { (8f, -2f), (58f, -2f), (98f, -2f) })
                F.Ruin("Tower", new Vector3(x, y, 0f), LevelLayer.Mid).transform.SetParent(deco, true);
            foreach (var (x, y) in new[] { (-30f, -2f), (25f, -2f), (80f, -2f), (115f, -2f) })
                F.Ruin("RuinWall", new Vector3(x, y, 0f), LevelLayer.Mid).transform.SetParent(deco, true);
            F.Waterfall(new Vector3(14f, 16f, 0f), LevelLayer.Far, 4f, 30f).transform.SetParent(deco, true);
            F.Block(deco, "DistantCliffs", new Vector3(30f, -10f, 110f), new Vector3(500f, 24f, 4f), LevelKitMaterials.Get().inkSky, false);

            // ---------------------------------------------------------------- mist & light
            F.MistBand(new Vector3(50f, -1.6f, 1.2f), LevelLayer.Playfield, 180f, 2.6f).transform.SetParent(deco, true);
            foreach (var (y, l, w, h, dz) in new[] { (-1f, LevelLayer.Near, 440f, 7f, -4f), (0f, LevelLayer.Mid, 560f, 12f, -10f), (-2f, LevelLayer.Far, 700f, 22f, -20f) })
            {
                var band = F.MistBand(new Vector3(160f, y, 0f), l, w, h);
                band.transform.position += new Vector3(0f, 0f, dz);
                band.transform.SetParent(deco, true);
            }
            for (int i = 0; i < 17; i++)
                F.LightShaft(new Vector3(-6f + i * 21f, 12f, 0f), LevelLayer.Mid, 5f + i % 3, 34f).transform.SetParent(deco, true);

            // ---------------------------------------------------------------- decoration: the tower, the ramparts and the well
            // Up there the ground-level backdrop is below the view: ruins stand up behind the climb.
            foreach (var (x, y, sc) in new[] { (126f, -2f, 2.4f), (152f, -2f, 2.0f), (172f, -2f, 2.2f), (200f, -2f, 1.8f) })
            {
                var tw = F.Ruin("Tower", new Vector3(x, y, 0f), LevelLayer.Mid);
                tw.transform.localScale *= sc;
                tw.transform.SetParent(deco, true);
            }
            foreach (var (x, y) in new[] { (139f, 24f), (151f, 25.5f), (161f, 23f), (173f, 24f) })
                F.Arch(new Vector3(x, y, 0f), LevelLayer.Near, 3.4f, 4.5f, 0.35f, (int)x).transform.SetParent(deco, true);
            foreach (var (x, y, h) in new[] { (122f, 31f, 4f), (130f, 29f, 3.2f), (148f, 33f, 3.4f), (210f, 15f, 3.6f) })
                F.Banner(new Vector3(x, y, 0f), LevelLayer.Near, 1.1f, h, (int)x).transform.SetParent(deco, true);
            foreach (var (x, y) in new[] { (119f, 30f), (128.5f, 33f), (183f, 22f), (192f, 20f) })
                FgModel("Chain", new Vector3(x, y, 0f), LevelLayer.Near, deco, 1.2f, false);
            foreach (var (x, y) in new[] { (124f, 34f), (186f, 24f) })
                F.Ruin("Cage", new Vector3(x, y, 0f), LevelLayer.Near).transform.SetParent(deco, true);
            F.Ruin("CliffRuin", new Vector3(200f, -2f, 0f), LevelLayer.Mid).transform.SetParent(deco, true);
            F.Ruin("Tower", new Vector3(212f, -11f, 0f), LevelLayer.Far).transform.SetParent(deco, true);
            F.Waterfall(new Vector3(163f, 40f, 0f), LevelLayer.Mid, 3f, 44f).transform.SetParent(deco, true);
            foreach (var (x, y, sc) in new[] { (232f, -2f, 2.8f), (262f, -2f, 2.6f), (292f, -2f, 2.2f), (326f, -2f, 1.8f) })
            {
                var tw = F.Ruin("Tower", new Vector3(x, y, 0f), LevelLayer.Mid);
                tw.transform.localScale *= sc;
                tw.transform.SetParent(deco, true);
            }
            foreach (var (x, y) in new[] { (247f, 37f), (262f, 44f), (279f, 43f), (320f, 1f), (333f, 1f) })
                F.Arch(new Vector3(x, y, 0f), LevelLayer.Near, 3.4f, 4.5f, 0.35f, (int)x).transform.SetParent(deco, true);
            foreach (var (x, y, h) in new[] { (226f, 46f, 4f), (236f, 44f, 3.4f), (252f, 47f, 3.4f), (300f, 12f, 4f), (328f, 11f, 3.6f) })
                F.Banner(new Vector3(x, y, 0f), LevelLayer.Near, 1.1f, h, (int)x).transform.SetParent(deco, true);
            foreach (var (x, y) in new[] { (229f, 50f), (237f, 47f), (290f, 42f), (298f, 32f), (303f, 10f) })
                FgModel("Chain", new Vector3(x, y, 0f), LevelLayer.Near, deco, 1.2f, false);
            foreach (var (x, y) in new[] { (231f, 52f), (294f, 36f) })
                F.Ruin("Cage", new Vector3(x, y, 0f), LevelLayer.Near).transform.SetParent(deco, true);
            F.Waterfall(new Vector3(285f, 52f, 0f), LevelLayer.Mid, 3f, 66f).transform.SetParent(deco, true);
            F.Ruin("Aqueduct", new Vector3(330f, -2f, 0f), LevelLayer.Mid).transform.SetParent(deco, true);
            F.Ruin("Castle", new Vector3(300f, -12f, 0f), LevelLayer.Far).transform.SetParent(deco, true);
            foreach (var x in new[] { 125f, 137f, 159f, 188f, 212f, 246f, 279f, 296f, 325f })
                F.Rubble(new Vector3(x, GroundTop(x), 1.6f), LevelLayer.Playfield, (int)x + 50).transform.SetParent(deco, true);

            // Arches, columns and walls stand on stone in their own layer (or on a tower top behind; or go).
            var grounded = GroundedProps.Ground(deco);
            Debug.Log("[CrimsonVeil] Standing props set on stone: " + grounded.x + ", removed (nothing to stand on): " + grounded.y);
            // Chains, cages and banners hang from stone in their own layer (or go).
            var hung = HangingProps.Anchor(deco);
            Debug.Log("[CrimsonVeil] Hanging props anchored: " + hung.x + ", removed (nothing to hang from): " + hung.y);

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

        static void Zone(Transform parent, float x, float minX, float maxX, Vector3 offset)
        {
            var z = Place("CameraZone", new Vector3(x, 0f, 0f), LevelLayer.Playfield, parent).GetComponent<CrimsonVeil.CameraSystem.CameraZone>();
            z.minX = minX; z.maxX = maxX; z.offset = offset;
        }

        /// <summary>A block of the level (centre x and top of the walkable edge, size), as T() builds it.</summary>
        public struct BlockDef
        {
            public string name;
            public float cx, top, width, height;
            public TerrainStyle style;
            public int seed;
            public bool grass;
            public float Left => cx - width * 0.5f;
            public float Right => cx + width * 0.5f;
            public float Bottom => top - height;
            public BlockDef(string name, float cx, float top, float width, float height, TerrainStyle style, int seed, bool grass = true)
            { this.name = name; this.cx = cx; this.top = top; this.width = width; this.height = height; this.style = style; this.seed = seed; this.grass = grass; }
        }

        /// <summary>
        /// The level beyond the far side (x 115 on), much of it vertical:
        /// 6. the tower: ledges to and fro up its inside (4 m apart), a wall-jump shaft by its left wall (3.2 m wide,
        ///    entered under a hanging pillar) and a whip ring, out at 25 m onto
        /// 7. the ramparts: high platforms with gaps (the widest, 6 m, with a ring over it), then
        /// 8. the well: down from ledge to ledge to its floor at -4 m, and
        /// 9. up the steps out of it to the last plateau and the end wall.
        /// </summary>
        public static readonly BlockDef[] Extension =
        {
            new BlockDef("tower floor", 124f, 0f, 18f, 12f, TerrainStyle.Rock, 16),               // 115 .. 133
            new BlockDef("tower left wall", 116.3f, 30f, 1.4f, 27f, TerrainStyle.Ruin, 17, false), // hangs: walk in under it (3 m)
            new BlockDef("tower ledge A", 128f, 4f, 6f, 4f, TerrainStyle.Ruin, 18),               // 125 .. 131, a step up from the floor
            new BlockDef("tower ledge B", 119.75f, 8f, 5.5f, 1.2f, TerrainStyle.Ruin, 19),        // 117 .. 122.5
            new BlockDef("tower ledge C", 128f, 12f, 6f, 1.2f, TerrainStyle.Ruin, 20),
            new BlockDef("tower ledge D", 119.75f, 16f, 5.5f, 1.2f, TerrainStyle.Ruin, 21),
            new BlockDef("tower pillar", 120.8f, 25f, 1.2f, 7f, TerrainStyle.Ruin, 22, false),     // 120.2 .. 121.4, hangs 2 m over D
            new BlockDef("tower top", 126.2f, 25f, 9.6f, 1.4f, TerrainStyle.Ruin, 23),            // 121.4 .. 131
            new BlockDef("tower right wall", 132f, 25f, 2f, 25f, TerrainStyle.Ruin, 24, false),    // 131 .. 133
            new BlockDef("rampart 1", 137f, 25f, 8f, 3f, TerrainStyle.Rock, 25),                  // 133 .. 141
            new BlockDef("rampart 2", 148f, 26.5f, 6f, 1.4f, TerrainStyle.Ruin, 26),              // 145 .. 151
            new BlockDef("rampart 3", 159f, 24f, 8f, 3f, TerrainStyle.Rock, 27),                  // 155 .. 163
            new BlockDef("rampart 4", 172.5f, 25f, 7f, 3f, TerrainStyle.Rock, 28),                // 169 .. 176
            new BlockDef("well left wall", 175.3f, 22f, 1.4f, 26f, TerrainStyle.Ruin, 29, false),  // under rampart 4, down to the floor
            new BlockDef("well ledge 1", 180f, 17f, 3f, 1.2f, TerrainStyle.Ruin, 30),
            new BlockDef("well ledge 2", 188f, 10f, 3.5f, 1.2f, TerrainStyle.Ruin, 31),
            new BlockDef("well ledge 3", 181f, 4f, 4f, 1.2f, TerrainStyle.Ruin, 32),
            new BlockDef("well floor", 188f, -4f, 24f, 10f, TerrainStyle.Rock, 33),               // 176 .. 200
            new BlockDef("step 1", 197f, 0f, 4f, 4f, TerrainStyle.Rock, 34),                      // 195 .. 199
            new BlockDef("step 2", 201f, 3.5f, 4f, 7.5f, TerrainStyle.Rock, 35),                  // 199 .. 203
            new BlockDef("plateau", 212.5f, 7f, 19f, 12f, TerrainStyle.Rock, 36),                 // 203 .. 222
            new BlockDef("plateau tunnel", 208f, 15f, 5f, 6.8f, TerrainStyle.Ruin, 65),          // 205.5 .. 210.5, 1.2 m over the plateau: crawl or slide
            // 10. the belfry: a step, ledges to and fro 4 m apart, a wall-jump shaft by its right wall, out at 38 m
            new BlockDef("belfry floor", 230f, 7f, 16f, 12f, TerrainStyle.Rock, 37),            // 222 .. 238
            new BlockDef("belfry left wall", 222.7f, 44f, 1.4f, 34f, TerrainStyle.Ruin, 38, false), // hangs: walk in under it (3 m)
            new BlockDef("belfry ledge 1", 235f, 11f, 6f, 4f, TerrainStyle.Ruin, 39),            // 232 .. 238, a step up from the floor
            new BlockDef("belfry ledge 2", 226.2f, 15f, 5.6f, 1.2f, TerrainStyle.Ruin, 40),      // 223.4 .. 229
            new BlockDef("belfry ledge 3", 235f, 19f, 6f, 1.2f, TerrainStyle.Ruin, 41),
            new BlockDef("belfry ledge 4", 226.2f, 23f, 5.6f, 1.2f, TerrainStyle.Ruin, 42),
            new BlockDef("belfry ledge 5", 235f, 27f, 6f, 1.2f, TerrainStyle.Ruin, 43),
            new BlockDef("belfry pillar", 234.2f, 38f, 1.2f, 9f, TerrainStyle.Ruin, 44, false),   // 233.6 .. 234.8, hangs 2 m over ledge 5
            new BlockDef("belfry lookout", 228.5f, 38f, 10.2f, 1.4f, TerrainStyle.Ruin, 45),     // 223.4 .. 233.6, over the pillar
            new BlockDef("belfry right wall", 239f, 38f, 2f, 43f, TerrainStyle.Ruin, 46, false),  // 238 .. 240, the shaft's outer wall, down past the floor
            // 11. the ridge high over the valley: up and along floating stones
            new BlockDef("ridge roof", 246f, 38f, 12f, 3f, TerrainStyle.Rock, 47),               // 240 .. 252
            new BlockDef("ridge stone 1", 255.5f, 42f, 3f, 1.2f, TerrainStyle.Ruin, 48),         // 254 .. 257
            new BlockDef("ridge stone 2", 261.5f, 45f, 5f, 1.4f, TerrainStyle.Ruin, 49),         // 259 .. 264
            new BlockDef("ridge stone 3", 270f, 44f, 4f, 1.4f, TerrainStyle.Ruin, 50),           // 268 .. 272
            new BlockDef("ridge end", 279f, 44f, 6f, 3f, TerrainStyle.Rock, 51),                 // 276 .. 282
            // 12. the chasm: down ledge by ledge, left and right, to its floor at -14 m
            new BlockDef("chasm left wall", 281.3f, 41f, 1.4f, 57f, TerrainStyle.Ruin, 52, false), // under the ridge's end, down to the floor
            new BlockDef("chasm ledge 1", 286f, 36f, 3f, 1.2f, TerrainStyle.Ruin, 53),
            new BlockDef("chasm ledge 2", 293f, 29f, 3.5f, 1.2f, TerrainStyle.Ruin, 54),
            new BlockDef("chasm ledge 3", 287f, 22f, 4f, 1.2f, TerrainStyle.Ruin, 55),
            new BlockDef("chasm ledge 4", 295f, 15f, 4f, 1.2f, TerrainStyle.Ruin, 56),
            new BlockDef("chasm ledge 5", 288f, 8f, 4f, 1.2f, TerrainStyle.Ruin, 57),
            new BlockDef("chasm ledge 6", 296f, 1f, 4f, 1.2f, TerrainStyle.Ruin, 58),
            new BlockDef("chasm ledge 7", 289f, -6f, 4f, 1.2f, TerrainStyle.Ruin, 59),
            new BlockDef("chasm floor", 296f, -14f, 28f, 10f, TerrainStyle.Rock, 60),            // 282 .. 310
            // 13. out of the chasm: a 16 m wall-jump shaft by its right wall
            new BlockDef("chasm pillar", 306.2f, 2f, 1.2f, 14f, TerrainStyle.Ruin, 61, false),   // 305.6 .. 306.8, hangs 2 m over the floor
            new BlockDef("chasm right wall", 311f, 2f, 2f, 18f, TerrainStyle.Ruin, 62, false),    // 310 .. 312
            // 14. the garden at the end, and the end wall
            new BlockDef("garden", 325f, 2f, 26f, 14f, TerrainStyle.Rock, 63),                   // 312 .. 338
            new BlockDef("garden tunnel", 322f, 9.2f, 6f, 6.1f, TerrainStyle.Ruin, 66),          // 319 .. 325, 1.1 m over the ground: a long crawl, or a slide
            new BlockDef("end wall", 339f, 22f, 2f, 30f, TerrainStyle.Ruin, 64, false),
        };

        /// <summary>How the main way goes through <see cref="Extension"/>: (from, to, how).</summary>
        public static readonly (string from, string to, string how)[] ExtensionRoute =
        {
            ("tower floor", "tower ledge A", "climb"), ("tower ledge A", "tower ledge B", "climb"), ("tower ledge B", "tower ledge C", "climb"),
            ("tower ledge C", "tower ledge D", "climb"), ("tower ledge D", "tower top", "shaft"), ("tower top", "rampart 1", "walk"),
            ("rampart 1", "rampart 2", "jump"), ("rampart 2", "rampart 3", "drop"), ("rampart 3", "rampart 4", "double"),
            ("rampart 4", "well ledge 1", "drop"), ("well ledge 1", "well ledge 2", "drop"), ("well ledge 2", "well ledge 3", "drop"),
            ("well ledge 3", "well floor", "drop"), ("well floor", "step 1", "climb"), ("step 1", "step 2", "climb"), ("step 2", "plateau", "climb"),
            ("plateau", "belfry floor", "walk"), ("belfry floor", "belfry ledge 1", "climb"), ("belfry ledge 1", "belfry ledge 2", "climb"),
            ("belfry ledge 2", "belfry ledge 3", "climb"), ("belfry ledge 3", "belfry ledge 4", "climb"), ("belfry ledge 4", "belfry ledge 5", "climb"),
            ("belfry ledge 5", "belfry right wall", "shaft"), ("belfry right wall", "ridge roof", "walk"),
            ("ridge roof", "ridge stone 1", "climb"), ("ridge stone 1", "ridge stone 2", "climb"), ("ridge stone 2", "ridge stone 3", "jump"),
            ("ridge stone 3", "ridge end", "jump"), ("ridge end", "chasm ledge 1", "drop"), ("chasm ledge 1", "chasm ledge 2", "drop"),
            ("chasm ledge 2", "chasm ledge 3", "drop"), ("chasm ledge 3", "chasm ledge 4", "drop"), ("chasm ledge 4", "chasm ledge 5", "drop"),
            ("chasm ledge 5", "chasm ledge 6", "drop"), ("chasm ledge 6", "chasm ledge 7", "drop"), ("chasm ledge 7", "chasm floor", "drop"),
            ("chasm floor", "chasm right wall", "shaft"), ("chasm right wall", "garden", "walk"),
        };

        /// <summary>Low passages on the way: (the block overhead, the ground under it). Crouch (down) to crawl through,
        /// or slide (down + dash).</summary>
        public static readonly (string roof, string ground)[] ExtensionTunnels =
        {
            ("plateau tunnel", "plateau"),
            ("garden tunnel", "garden"),
        };

        /// <summary>The wall-jump shafts of the way: (the ledge it starts from, the hanging pillar walked in under, the
        /// wall across from it, where it comes out).</summary>
        public static readonly (string ledge, string pillar, string wall, string exit)[] ExtensionShafts =
        {
            ("tower ledge D", "tower pillar", "tower left wall", "tower top"),
            ("belfry ledge 5", "belfry pillar", "belfry right wall", "belfry right wall"),
            ("chasm floor", "chasm pillar", "chasm right wall", "chasm right wall"),
        };

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
