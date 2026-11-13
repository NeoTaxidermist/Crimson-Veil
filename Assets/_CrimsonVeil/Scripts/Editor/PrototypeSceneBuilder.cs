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
            T(terrain, 20.5f, 0.6f, 7f, 10f, TerrainStyle.Rock, 4);          // landing     (17 .. 24)
            T(terrain, 24.7f, 9f, 1.4f, 5.8f, TerrainStyle.Ruin, 5, false); // shaft left wall (hangs: walk in underneath)
            T(terrain, 28f, -1.2f, 5.2f, 4f, TerrainStyle.Rock, 6);          // shaft floor
            T(terrain, 31.3f, 7f, 1.4f, 9f, TerrainStyle.Ruin, 7, false);   // shaft right wall
            T(terrain, 37f, 7f, 10f, 1.4f, TerrainStyle.Rock, 8);            // high road   (32 .. 42)
            T(terrain, 49f, 5f, 7f, 1.2f, TerrainStyle.Rock, 9);             // stepping ledge
            T(terrain, 69f, 0f, 22f, 12f, TerrainStyle.Rock, 10);            // tree plain  (58 .. 80)
            T(terrain, 55.5f, 2.2f, 3f, 0.8f, TerrainStyle.Rock, 11);        // little ledge down
            T(terrain, 103f, 0f, 24f, 12f, TerrainStyle.Rock, 12);           // far side    (91 .. 115)
            T(terrain, 116f, 14f, 2f, 18f, TerrainStyle.Ruin, 13, false);   // end wall

            // ---------------------------------------------------------------- gameplay
            Place("PlayerSpawn", new Vector3(-7f, 0.05f, 0f), LevelLayer.Playfield, play);
            Place("Player", new Vector3(-7f, 0.05f, 0f), LevelLayer.Playfield, null);
            foreach (var (x, y) in new[] { (-1.2f, 0f), (-0.4f, 0f), (0.5f, 0f), (62f, 0f), (62.8f, 0f), (95f, 0f), (110f, 0f) })
                Place("Urn", new Vector3(x, y, 0f), LevelLayer.Playfield, play);
            foreach (var (x, y) in new[] { (6f, 0f), (39f, 7f), (72f, 0f), (101f, 0f), (108f, 0f) })
                Place("Wraith", new Vector3(x, y, 0f), LevelLayer.Playfield, play);
            Place("Checkpoint", new Vector3(21f, 0.6f, 0f), LevelLayer.Playfield, play);
            Place("Checkpoint", new Vector3(60f, 0f, 0f), LevelLayer.Playfield, play);
            Place("Checkpoint", new Vector3(93f, 0f, 0f), LevelLayer.Playfield, play);
            Place("RopeBridge", new Vector3(80.2f, 0f, 0f), LevelLayer.Playfield, play);
            var shaftZone = Place("CameraZone", new Vector3(28f, 0f, 0f), LevelLayer.Playfield, play).GetComponent<CrimsonVeil.CameraSystem.CameraZone>();
            shaftZone.minX = 23f; shaftZone.maxX = 33f; shaftZone.offset = new Vector3(0f, 4.5f, -17f);

            // ---------------------------------------------------------------- decoration: playfield
            Deco("Gallows", new Vector3(-10f, 0f, 0f), LevelLayer.Playfield, deco);
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
            for (int i = 0; i < 46; i++)
                F.Grass(new Vector3(-16f + i * 2.9f, -2.6f + (i % 3) * 0.3f, 0f), LevelLayer.Foreground, 100 + i, 3.4f, new Vector2(0.6f, 2.1f), 40, null, 0.07f).transform.SetParent(deco, true);
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
            }
            foreach (var (x, y, h) in new[] { (6.6f, 8.6f, 3.6f), (29f, 8.2f, 3f), (51f, 8.6f, 3.4f), (95f, 8.4f, 3.2f) })
                F.Banner(new Vector3(x, y, 0f), LevelLayer.Near, 1.1f, h, (int)x).transform.SetParent(deco, true);
            F.Waterfall(new Vector3(27.5f, 14f, 0f), LevelLayer.Near, 2.4f, 16f).transform.SetParent(deco, true);

            // ---------------------------------------------------------------- decoration: mid
            for (int i = 0; i < 9; i++)
                F.Arch(new Vector3(-30f + i * 9.6f, -2f, 0f), LevelLayer.Mid, 3.75f, 5f, i == 3 ? 0.5f : 0f, i).transform.SetParent(deco, true);
            for (int i = 0; i < 6; i++)
                F.Arch(new Vector3(62f + i * 11f, -2f + (i % 2) * 1.5f, 0f), LevelLayer.Mid, 3.75f, 4.5f + (i % 3) * 0.5f, 0.3f + 0.15f * (i % 3), i + 1).transform.SetParent(deco, true);
            F.Block(deco, "MidCliff", new Vector3(52f, 4f, 26f), new Vector3(14f, 22f, 6f), LevelKitMaterials.Get().inkMid, false);
            F.Waterfall(new Vector3(44.5f, 14f, 0f), LevelLayer.Mid, 3.2f, 22f).transform.SetParent(deco, true);

            // ---------------------------------------------------------------- decoration: far & sky
            float[] farBreak = { 0f, 0.3f, 0f, 0f, 0.6f, 0f, 0.2f, 0f, 0f, 0.45f, 0f, 0.7f, 0f, 0.25f, 0f };
            for (int i = 0; i < 15; i++)
                F.Arch(new Vector3(-60f + i * 13f, -12f + (i % 3) * 1.5f, 0f), LevelLayer.Far, 3.4f, 5.8f, farBreak[i], i).transform.SetParent(deco, true);
            F.Block(deco, "FarTower", new Vector3(-22f, 10f, 69f), new Vector3(6f, 40f, 6f), LevelKitMaterials.Get().inkFar, false);
            F.Block(deco, "FarTowerTop", new Vector3(-22f, 31f, 69f), new Vector3(8f, 2f, 8f), LevelKitMaterials.Get().inkFar, false);
            F.Block(deco, "FarTower", new Vector3(86f, 8f, 75f), new Vector3(7f, 36f, 7f), LevelKitMaterials.Get().inkFar, false);
            F.Waterfall(new Vector3(14f, 16f, 0f), LevelLayer.Far, 4f, 30f).transform.SetParent(deco, true);
            F.Block(deco, "DistantCliffs", new Vector3(30f, -10f, 110f), new Vector3(500f, 24f, 4f), LevelKitMaterials.Get().inkSky, false);

            // ---------------------------------------------------------------- mist & light
            F.MistBand(new Vector3(50f, -1.6f, 1.2f), LevelLayer.Playfield, 180f, 2.6f).transform.SetParent(deco, true);
            F.MistBand(new Vector3(50f, -1f, 0f), LevelLayer.Near, 220f, 7f).transform.position += new Vector3(0f, 0f, -4f);
            F.MistBand(new Vector3(50f, 0f, 0f), LevelLayer.Mid, 300f, 12f).transform.position += new Vector3(0f, 0f, -10f);
            F.MistBand(new Vector3(50f, -2f, 0f), LevelLayer.Far, 400f, 22f).transform.position += new Vector3(0f, 0f, -20f);
            for (int i = 0; i < 6; i++)
                F.LightShaft(new Vector3(-6f + i * 21f, 12f, 0f), LevelLayer.Mid, 5f + i % 3, 34f).transform.SetParent(deco, true);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Debug.Log("[CrimsonVeil] Prototype scene built from the level kit: " + ScenePath);
        }

        static Transform Group(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
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
