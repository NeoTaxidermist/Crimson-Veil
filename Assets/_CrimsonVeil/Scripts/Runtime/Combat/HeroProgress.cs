using System;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>
    /// What the hero gathers through a chapter (v0.110, the user: "z nepřátel by mělo něco po zabití vypadnout... k
    /// upgradům energie a maximálního zdraví? Nebo k healování?"):
    /// <list type="bullet">
    /// <item><b>Crimson</b> - drops that fall from every foe he kills (<see cref="CrimsonDrop"/>), spent at a checkpoint's
    /// altar on the three upgrades below. Dying leaves what he carries where he fell (<see cref="CrimsonCache"/>).</item>
    /// <item><b>Mend</b> - a knot of crimson the scarf ties from blows that land (<see cref="MendPerHit"/> each); a full
    /// knot lets him bind a wound (<see cref="PlayerHealth"/>, held <c>R</c>). Life no longer grows back by itself.
    /// (Not from the scarf's own length: that grows back by itself, and healing from it would be the autoheal again.)</item>
    /// <item><b>Shards</b> - found in secret places; every <see cref="ShardsPerHeart"/> make one more life.</item>
    /// <item><b>Upgrades</b> (each up to <see cref="MaxLevel"/>): a longer scarf (every weapon costs less of it), a
    /// stronger binding (heals more), a keener veil (blows hurt more).</item>
    /// </list>
    /// Kept for the play session (the game has no saved progress yet).
    /// </summary>
    public static class HeroProgress
    {
        public enum Upgrade { Scarf, Bandage, Blade }

        public const int MaxLevel = 3, ShardsPerHeart = 4;
        /// <summary>A full mend knot (one wound bound) takes this many blows that land.</summary>
        public const float MendPerHit = 0.25f;
        /// <summary>Dying leaves the crimson he carries where he fell (to be fetched back); a second death first loses it.</summary>
        public static bool DeathLeavesCrimson = true;

        public static int Crimson { get; private set; }
        public static int Shards { get; private set; }
        /// <summary>The mend knot, 0..1: full, he can bind a wound.</summary>
        public static float Mend { get; private set; }
        static readonly int[] levels = new int[3];

        /// <summary>Something changed (the HUD and the hero read it again).</summary>
        public static event Action Changed;

        public static int Level(Upgrade u) => levels[(int)u];

        /// <summary>
        /// What each level of an upgrade costs, in crimson (v0.114.7: 40, 90, 150; it was 20, 45, 80). The first
        /// chapter gives some 147 crimson to one who slays all in it, and lets each upgrade reach level 1: the three
        /// cost 120 of it, so a thorough chapter buys them all with a little to spare for a death, a hasty one not.
        /// </summary>
        public static readonly int[] Prices = { 40, 90, 150 };

        /// <summary>What the next level of an upgrade costs; -1 at the top - the top of this chapter
        /// (<see cref="ChapterCap"/>) or of all.</summary>
        public static int Cost(Upgrade u)
        {
            int l = Level(u);
            return l >= Cap ? -1 : Prices[Mathf.Clamp(l, 0, Prices.Length - 1)];
        }

        /// <summary>The highest level an upgrade may reach in the chapter being played (v0.111: so crimson gathered
        /// early cannot buy what belongs to later chapters). 0: no cap of its own (the game's top).</summary>
        public static int ChapterCap { get; set; }
        /// <summary>The level an upgrade can reach now.</summary>
        public static int Cap => ChapterCap > 0 ? Mathf.Min(ChapterCap, MaxLevel) : MaxLevel;
        /// <summary>The next level is held back by the chapter, not by the game's top.</summary>
        public static bool CappedByChapter(Upgrade u) => Level(u) >= Cap && Level(u) < MaxLevel;

        /// <summary>The chapter a scene is (its upgrades go up to that level): 0 for a scene that is no chapter.</summary>
        public static int ChapterOf(string scene) => scene == "Prototype_Ruins" ? 1 : 0;

        // ---- the chapter's start, to go back to ---------------------------------------------------------------

        struct Snapshot { public int crimson, shards; public float mend; public int[] levels; }
        static Snapshot chapterStart;
        static bool haveStart;
        static string chapterScene;

        /// <summary>The state as he entered the chapter is remembered (a fresh scene of a chapter, not a restart of it).</summary>
        public static void RememberChapterStart(string scene)
        {
            chapterScene = scene;
            chapterStart = new Snapshot { crimson = Crimson, shards = Shards, mend = Mend, levels = (int[])levels.Clone() };
            haveStart = true;
        }

        /// <summary>
        /// Back to the chapter's start (v0.111, the user: restart the chapter when it went wrong, the foes with it,
        /// but no endless grinding): what he gathered since is taken back - crimson, upgrades, shards, the knot - so a
        /// restart never makes him stronger than he came in. True when there was a start to go back to.
        /// </summary>
        public static bool RestoreChapterStart()
        {
            if (!haveStart) return false;
            Crimson = chapterStart.crimson; Shards = chapterStart.shards; Mend = chapterStart.mend;
            for (int i = 0; i < levels.Length; i++) levels[i] = chapterStart.levels[i];
            Changed?.Invoke();
            return true;
        }

        /// <summary>The scene the remembered start belongs to (null: none yet).</summary>
        public static string ChapterScene => haveStart ? chapterScene : null;

        /// <summary>The next load of the chapter is a restart: it keeps the remembered start instead of taking a new one.</summary>
        public static bool Restarting { get; set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void HookScenes()
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }

        static void OnSceneLoaded(UnityEngine.SceneManagement.Scene s, UnityEngine.SceneManagement.LoadSceneMode mode)
        {
            if (mode != UnityEngine.SceneManagement.LoadSceneMode.Single) return;
            int chapter = ChapterOf(s.name);
            if (chapter <= 0) return;
            ChapterCap = chapter;
            // entering a chapter remembers how he came in; restarting it keeps what was remembered
            if (Restarting && haveStart && chapterScene == s.name) { Restarting = false; return; }
            Restarting = false;
            RememberChapterStart(s.name);
        }

        public static string Name(Upgrade u)
            => u == Upgrade.Scarf ? "Delší šála" : u == Upgrade.Bandage ? "Pevnější obvaz" : "Ostřejší závoj";

        public static string What(Upgrade u)
            => u == Upgrade.Scarf ? "zbraně a štít berou o pětinu méně šály"
             : u == Upgrade.Bandage ? "obvaz zahojí o půl života víc"
             : "údery zraňují o 15 % víc";

        /// <summary>Buys the next level if he has the crimson; true when bought.</summary>
        public static bool Buy(Upgrade u)
        {
            int c = Cost(u);
            if (c < 0 || Crimson < c) return false;
            Crimson -= c;
            levels[(int)u]++;
            Changed?.Invoke();
            return true;
        }

        /// <summary>How much longer the scarf is (every cost and drain is divided by it).</summary>
        public static float ScarfCapacity => 1f + 0.25f * Level(Upgrade.Scarf);
        /// <summary>Lives a bound wound gives back.</summary>
        public static float HealAmount => 1f + 0.5f * Level(Upgrade.Bandage);
        /// <summary>How much harder the veil's blows land.</summary>
        public static float DamageScale => 1f + 0.15f * Level(Upgrade.Blade);
        /// <summary>Lives added by the shards found.</summary>
        public static int ExtraLives => Shards / ShardsPerHeart;

        public static void AddCrimson(int n)
        {
            if (n <= 0) return;
            Crimson += n;
            Changed?.Invoke();
        }

        /// <summary>Takes all he carries (dying); returns how much it was.</summary>
        public static int TakeAllCrimson()
        {
            int n = Crimson;
            Crimson = 0;
            if (n > 0) Changed?.Invoke();
            return n;
        }

        /// <summary>A shard found; true when it completed a life.</summary>
        public static bool AddShard()
        {
            Shards++;
            Changed?.Invoke();
            return Shards % ShardsPerHeart == 0;
        }

        /// <summary>A blow of his landed: the knot is tied a little further.</summary>
        public static void BlowLanded()
        {
            if (Mend >= 1f) return;
            Mend = Mathf.Min(1f, Mend + MendPerHit);
            Changed?.Invoke();
        }

        /// <summary>Unties the full knot to bind a wound; false if it is not full.</summary>
        public static bool SpendMend()
        {
            if (Mend < 1f - 1e-4f) return false;
            Mend = 0f;
            Changed?.Invoke();
            return true;
        }

        /// <summary>A checkpoint ties the knot whole again.</summary>
        public static void FillMend()
        {
            if (Mend >= 1f) return;
            Mend = 1f;
            Changed?.Invoke();
        }

        /// <summary>A fresh start (a new chapter, tests).</summary>
        public static void Reset()
        {
            Crimson = 0; Shards = 0; Mend = 0f;
            for (int i = 0; i < levels.Length; i++) levels[i] = 0;
            ChapterCap = 0; haveStart = false; Restarting = false;
            Changed?.Invoke();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void OnLoad() { Changed = null; Reset(); }
    }
}
