using System;
using UnityEngine;

namespace CrimsonVeil.Combat
{
    public enum EnemyState
    {
        Patrol,
        Chase,
        Windup,
        Strike,
        Recover,
        Hurt,
        Dead,
    }

    [Serializable]
    public class EnemyBrainSettings
    {
        public float detectRange = 7f;
        public float loseRange = 11f;
        public float verticalSight = 3f;
        public float attackRange = 1.9f;
        public float patrolSpeed = 1.4f;
        public float chaseSpeed = 3.6f;
        public float patrolHalfWidth = 3f;
        [Tooltip("Telegraph before the strike — readable, never instant.")]
        public float windupTime = 0.5f;
        public float strikeTime = 0.2f;
        public float strikeLungeSpeed = 7f;
        public float recoverTime = 0.65f;
        public float hurtTime = 0.35f;
    }

    public struct EnemySenses
    {
        public float selfX;
        public float playerX;
        public float playerDeltaY;
        public bool playerAlive;
        /// <summary>False when a wall or ledge blocks movement in the facing direction.</summary>
        public bool pathClear;
    }

    public struct EnemyIntent
    {
        public float velocityX;
        public int facing;
        public bool strikeActive;
    }

    /// <summary>
    /// Readable melee enemy: patrol → chase → wind-up (telegraph) → strike (lunge) → recover.
    /// Hits interrupt the wind-up, so aggressive play is rewarded.
    /// </summary>
    public sealed class EnemyBrain
    {
        public EnemyBrainSettings Settings { get; }
        public EnemyState State { get; private set; } = EnemyState.Patrol;
        public float StateTime { get; private set; }
        public int Facing { get; private set; } = -1;
        public float PatrolOrigin { get; set; }
        public int StrikeCount { get; private set; }

        public event Action<EnemyState> StateChanged;

        public EnemyBrain(EnemyBrainSettings settings, float patrolOrigin)
        {
            Settings = settings ?? new EnemyBrainSettings();
            PatrolOrigin = patrolOrigin;
        }

        public EnemyIntent Tick(float dt, EnemySenses s)
        {
            var c = Settings;
            StateTime += dt;
            var intent = new EnemyIntent { facing = Facing };

            float dx = s.playerX - s.selfX;
            float dist = Mathf.Abs(dx);
            bool sees = s.playerAlive && Mathf.Abs(s.playerDeltaY) < c.verticalSight;

            switch (State)
            {
                case EnemyState.Patrol:
                    if (sees && dist < c.detectRange) { Face(dx); Enter(EnemyState.Chase); break; }
                    float offset = s.selfX - PatrolOrigin;
                    if ((offset > c.patrolHalfWidth && Facing > 0) || (offset < -c.patrolHalfWidth && Facing < 0) || !s.pathClear)
                        Facing = -Facing;
                    intent.velocityX = Facing * c.patrolSpeed;
                    break;

                case EnemyState.Chase:
                    if (!sees || dist > c.loseRange) { PatrolOrigin = s.selfX; Enter(EnemyState.Patrol); break; }
                    Face(dx);
                    if (dist < c.attackRange) { Enter(EnemyState.Windup); break; }
                    intent.velocityX = s.pathClear ? Facing * c.chaseSpeed : 0f;
                    break;

                case EnemyState.Windup:
                    if (StateTime >= c.windupTime) { StrikeCount++; Enter(EnemyState.Strike); }
                    break;

                case EnemyState.Strike:
                    intent.strikeActive = true;
                    intent.velocityX = s.pathClear ? Facing * c.strikeLungeSpeed : 0f;
                    if (StateTime >= c.strikeTime) Enter(EnemyState.Recover);
                    break;

                case EnemyState.Recover:
                    if (StateTime >= c.recoverTime) Enter(sees ? EnemyState.Chase : EnemyState.Patrol);
                    break;

                case EnemyState.Hurt:
                    if (StateTime >= c.hurtTime) Enter(sees ? EnemyState.Chase : EnemyState.Patrol);
                    break;
            }

            intent.facing = Facing;
            return intent;
        }

        public void OnHurt(bool dead)
        {
            if (State == EnemyState.Dead) return;
            Enter(dead ? EnemyState.Dead : EnemyState.Hurt);
        }

        /// <summary>Blocked by a shield: knocked out of the strike into a long recovery.</summary>
        public void OnStaggered()
        {
            if (State == EnemyState.Dead) return;
            Enter(EnemyState.Recover);
            StateTime = -0.4f;
        }

        void Face(float dx)
        {
            if (Mathf.Abs(dx) > 0.05f) Facing = dx > 0f ? 1 : -1;
        }

        void Enter(EnemyState next)
        {
            State = next;
            StateTime = 0f;
            StateChanged?.Invoke(next);
        }
    }
}
