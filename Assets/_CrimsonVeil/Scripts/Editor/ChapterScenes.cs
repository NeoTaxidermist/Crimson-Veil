using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrimsonVeil.EditorTools
{
    /// <summary>
    /// The game's chapters as scenes (v0.112, the user: "teď vlastně budeme mít jednu pro testovací místnost a pak
    /// druhou pro to, co máme. Budeš to muset rozdělit."): the test room, once a wing left of the ruins' start, is a
    /// scene of its own; chapter 1 (the ruins) loses it and the way to it is walled up. Run after
    /// <see cref="PrototypeSceneBuilder.Build"/> (it calls this), or from the menu.
    /// </summary>
    public static class ChapterScenes
    {
        public const string RuinsPath = "Assets/_CrimsonVeil/Scenes/Prototype_Ruins.unity";
        public const string TestRoomPath = "Assets/_CrimsonVeil/Scenes/TestRoom.unity";
        /// <summary>Where the hero starts in the test room (by its right wall).</summary>
        public static readonly Vector3 TestRoomSpawn = new Vector3(-19f, 0.05f, 0f);

        [MenuItem("Crimson Veil/Split Test Room Scene")]
        public static void Split()
        {
            if (!AssetDatabase.CopyAsset(RuinsPath, TestRoomPath))
            {
                AssetDatabase.DeleteAsset(TestRoomPath);
                AssetDatabase.CopyAsset(RuinsPath, TestRoomPath);
            }
            StripTestRoom();
            KeepOnlyTestRoom();
            var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            if (!list.Exists(s => s.path == TestRoomPath)) list.Add(new EditorBuildSettingsScene(TestRoomPath, true));
            EditorBuildSettings.scenes = list.ToArray();
            Debug.Log("[CrimsonVeil] Chapters: the test room is its own scene, the ruins walled off to the left.");
        }

        /// <summary>The ruins without the test room: its wing is gone and the passage under the wall left of the start is shut.</summary>
        static void StripTestRoom()
        {
            var scene = EditorSceneManager.OpenScene(RuinsPath, OpenSceneMode.Single);
            var level = GameObject.Find("Level");
            var room = level != null ? level.transform.Find("TestRoom") : null;
            if (room != null) Object.DestroyImmediate(room.gameObject);
            PlugTheWall(level);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>The wall left of the start stands from 4 m up; under it was the way to the test room: a copy of
        /// it closes the gap down to the floor.</summary>
        static void PlugTheWall(GameObject level)
        {
            var terrain = level != null ? level.transform.Find("Terén") : null;
            if (terrain == null) return;
            foreach (Transform t in terrain)
            {
                if (t.name != "Wall") continue;
                var col = t.GetComponentInChildren<Collider>();
                if (col == null || Mathf.Abs(col.bounds.center.x + 14f) > 1.5f) continue;
                if (terrain.Find("Wall (plug)") != null) return;
                var plug = Object.Instantiate(t.gameObject, terrain);
                plug.name = "Wall (plug)";
                plug.transform.position = t.position + Vector3.down * 9.5f;
                return;
            }
        }

        /// <summary>The test room alone: the hero, the scene's essentials, the room, a wall shutting its right side, a
        /// checkpoint (to try the altar) and two sleeping wraiths for recording blows (the clip recorder wakes them).</summary>
        static void KeepOnlyTestRoom()
        {
            var scene = EditorSceneManager.OpenScene(TestRoomPath, OpenSceneMode.Single);
            var level = GameObject.Find("Level");
            Transform wall = null, wraithA = null, wraithB = null;
            var terrain = level.transform.Find("Terén");
            foreach (Transform t in terrain)
                if (t.name == "Wall" && t.GetComponentInChildren<Collider>() is Collider c && Mathf.Abs(c.bounds.center.x + 14f) < 1.5f) wall = t;
            foreach (var e in Object.FindObjectsByType<CrimsonVeil.Combat.Enemy>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (wraithA == null) wraithA = e.transform; else if (wraithB == null) wraithB = e.transform;
            }
            Transform checkpoint = null;
            foreach (var cp in Object.FindObjectsByType<CrimsonVeil.World.Checkpoint>(FindObjectsInactive.Include, FindObjectsSortMode.None)) { checkpoint = cp.transform; break; }
            // keep: the room, the wall (and its plug), the wraiths, a checkpoint - everything else of the level goes
            var keep = new GameObject("TestRoomLevel").transform;
            keep.SetParent(level.transform, false);
            var room = level.transform.Find("TestRoom");
            if (room != null) room.SetParent(keep, true);
            if (wall != null)
            {
                wall.SetParent(keep, true);
                var plug = Object.Instantiate(wall.gameObject, keep);
                plug.name = "Wall (plug)";
                plug.transform.position = wall.position + Vector3.down * 9.5f;
            }
            if (wraithA != null) { wraithA.SetParent(keep, true); wraithA.position = new Vector3(-17f, 0f, 0f); wraithA.gameObject.SetActive(false); }
            if (wraithB != null) { wraithB.SetParent(keep, true); wraithB.position = new Vector3(-16f, 0f, 0f); wraithB.gameObject.SetActive(false); }
            if (checkpoint != null) { checkpoint.SetParent(keep, true); checkpoint.position = new Vector3(-16.5f, 0f, 0f); }
            var spawnPoint = GameObject.Find("PlayerSpawn");
            if (spawnPoint != null && spawnPoint.transform.IsChildOf(level.transform)) spawnPoint.transform.SetParent(keep, true);
            foreach (Transform t in level.transform)
                if (t != keep) Object.DestroyImmediate(t.gameObject);
            // (DestroyImmediate while iterating skips some: sweep again)
            for (int i = level.transform.childCount - 1; i >= 0; i--)
                if (level.transform.GetChild(i) != keep) Object.DestroyImmediate(level.transform.GetChild(i).gameObject);
            var spawn = GameObject.Find("PlayerSpawn");
            if (spawn != null) spawn.transform.position = TestRoomSpawn;
            var player = GameObject.Find("Player");
            if (player != null) player.transform.position = TestRoomSpawn;
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
    }
}
