using System;
using System.IO;
using CrimsonVeil.Combat;
using CrimsonVeil.Player;
using CrimsonVeil.World;
using UnityEngine;

namespace CrimsonVeil.UI
{
    /// <summary>
    /// Measures a playtest: the time into each section of the level, the deaths (and falls) in each, the whole run.
    /// The table is written as it goes (so a crash or a stopped Play keeps it) to Playtests/ in the project folder
    /// (in a build: the player's data folder), one file per run. Created with the HUD.
    /// </summary>
    public sealed class PlaytestLog : MonoBehaviour
    {
        /// <summary>Off in automated tests (they teleport the hero about; their runs are no playtests).</summary>
        public static bool WriteFiles = true;

        /// <summary>Where the run ends: the level's end wall.</summary>
        public float endX = 336f;

        public PlaytestModel Model { get; private set; }
        public string FilePath { get; private set; }

        PlayerController player;
        PlayerHealth health;
        float startTime, nextSave;
        bool hooked, hookedOnce, hasLastX;
        float lastX, lastDeathTime = -100f;

        /// <summary>The hero was moved about (not by a respawn): not a real playtest, not saved.</summary>
        public bool Invalid { get; private set; }

        public static PlaytestLog Create(GameObject host) => host.GetComponent<PlaytestLog>() ?? host.AddComponent<PlaytestLog>();

        /// <summary>The folder the runs are written to.</summary>
        public static string Folder =>
            Application.isEditor ? Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Playtests"))
                                 : Path.Combine(Application.persistentDataPath, "Playtests");

        void Start()
        {
            startTime = Time.time;
            Model = new PlaytestModel(PlaytestModel.PrototypeSections, endX);
            Model.Entered += _ => Save();
            FilePath = Path.Combine(Folder, "playtest_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt");
        }

        void Hook()
        {
            player = FindFirstObjectByType<PlayerController>();
            if (player == null) return;
            if (hookedOnce)
            {
                // a new hero (the level loaded again): the old run is kept on disk, a new one starts
                Save();
                startTime = Time.time;
                Model = new PlaytestModel(PlaytestModel.PrototypeSections, endX);
                Model.Entered += _ => Save();
                FilePath = Path.Combine(Folder, "playtest_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".txt");
                Invalid = false;
            }
            hasLastX = false;
            hookedOnce = true;
            health = player.GetComponent<PlayerHealth>();
            if (health != null) health.Died += OnDied;
            player.Fell += OnDied;
            hooked = true;
        }

        void OnDied()
        {
            if (Model == null || player == null) return;
            lastDeathTime = Time.time;
            Model.Died(player.transform.position.x);
            Save();
        }

        void Update()
        {
            if (hooked && player == null) hooked = false;   // the hero was destroyed (a new scene, a test): find the next one
            if (!hooked) Hook();
            if (!hooked || Model == null) return;
            float x = player.transform.position.x;
            // a jump of many metres in a frame that isn't a respawn after a death: the hero was moved (a test, a debug
            // teleport) - this run is no playtest and isn't saved
            if (hasLastX && Mathf.Abs(x - lastX) > 12f && Time.time - lastDeathTime > 4f) Invalid = true;
            lastX = x; hasLastX = true;
            Model.Tick(Time.time - startTime, x);
            if (Time.time >= nextSave) { nextSave = Time.time + 30f; Save(); }
        }

        /// <summary>Writes the table so far.</summary>
        public void Save()
        {
            if (!WriteFiles || Model == null || string.IsNullOrEmpty(FilePath)) return;
            if (Model.Elapsed < 10f && !Model.Finished) return;   // a Play pressed and stopped again is no playtest
            if (Invalid) return;                                   // the hero was teleported about
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
                File.WriteAllText(FilePath, Model.Report("Crimson Veil - playtest " + Path.GetFileNameWithoutExtension(FilePath).Replace("playtest_", "")));
            }
            catch (Exception e) { Debug.LogWarning("[Playtest] could not write " + FilePath + ": " + e.Message); }
        }

        void OnDestroy()
        {
            if (hooked)
            {
                if (health != null) health.Died -= OnDied;
                if (player != null) player.Fell -= OnDied;
            }
            Save();
        }

        void OnApplicationQuit() => Save();
    }
}
