using System.Collections.Generic;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    /// <summary>A creature that can fight the hero, and says whether it is fighting him now.</summary>
    public interface IHostile
    {
        /// <summary>
        /// It knows the hero is there and is after him: chasing, winding up, striking, recovering from a blow, swooping,
        /// hunting. A creature on patrol, asleep or out of reach is not.
        /// </summary>
        bool EngagedWithHero { get; }
        bool IsAlive { get; }
        /// <summary>Where it is (its body's centre, for the line of sight).</summary>
        Vector3 HostilePosition { get; }
        Transform transform { get; }
    }

    /// <summary>
    /// Every living hostile in the level, and the exchanges of blows between them and the hero: a blow of his landing
    /// on one, or one of theirs reaching him. The hero's sense of a fight (<see cref="CombatState"/>) is built from these.
    /// </summary>
    public static class Hostiles
    {
        static readonly List<IHostile> all = new List<IHostile>();
        public static IReadOnlyList<IHostile> All => all;

        public static void Register(IHostile h) { if (h != null && !all.Contains(h)) all.Add(h); }
        public static void Unregister(IHostile h) => all.Remove(h);

        /// <summary>Counts every exchange of blows between the hero and a hostile (watch it for changes).</summary>
        public static int Exchanges { get; private set; }

        /// <summary>A blow passed between the hero and a hostile.</summary>
        public static void NoteExchange() => Exchanges++;

        /// <summary>A hit the hero took: an exchange when it came from a hostile (not a spike or a fall).</summary>
        public static void NoteHitFrom(GameObject source)
        {
            if (source != null && source.GetComponentInParent<IHostile>() != null) NoteExchange();
        }
    }

    /// <summary>
    /// Whether the hero is in a fight - engine independent. A fight is a hostile after him and in sight within
    /// <see cref="EngageRadius"/>, or a blow exchanged with one a moment ago. It ends <see cref="EndAfter"/> s after
    /// neither holds, at once when no living hostile is left anywhere near (all fallen, or he has got away).
    /// </summary>
    public sealed class CombatState
    {
        /// <summary>How far (m) a hostile after the hero makes a fight (if it can see him).</summary>
        public const float EngageRadius = 12f;
        /// <summary>Beyond this (m) no living hostile is near: the fight is over at once.</summary>
        public const float NearRadius = 18f;
        /// <summary>Seconds an exchange of blows keeps the fight going, even with no one in sight.</summary>
        public const float ExchangeMemory = 2.5f;
        /// <summary>Seconds the fight lasts once no hostile in sight is after him (no flicker at its edge).</summary>
        public const float EndAfter = 2f;

        float sinceEngaged = 99f, sinceExchange = 99f;

        public bool InCombat => sinceEngaged < EndAfter || sinceExchange < ExchangeMemory;

        /// <param name="engagedInSight">A hostile within <see cref="EngageRadius"/> is after him and can see him.</param>
        /// <param name="anyNear">Some living hostile is within <see cref="NearRadius"/>.</param>
        public void Tick(float dt, bool engagedInSight, bool anyNear)
        {
            if (!anyNear) { sinceEngaged = sinceExchange = 99f; return; }
            sinceEngaged = engagedInSight ? 0f : sinceEngaged + dt;
            sinceExchange += dt;
        }

        /// <summary>A blow passed between him and a hostile.</summary>
        public void Exchange() => sinceExchange = 0f;

        public void Reset() => sinceEngaged = sinceExchange = 99f;
    }
}
