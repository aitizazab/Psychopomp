using System;
using JamKit.Skyline;
using UnityEngine;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// A burn on an enemy (docs/HELL-ABILITIES.md): damage over time through the bot's own <see cref="SkylineBot.TakeHit"/>
    /// (a body hit with no gun), embers while it lasts. Burn sources call <see cref="Apply"/>: lava and the ooze's lava pools
    /// (rooms / enemies branches), Combustion's explosions. A bot wrecked while burning raises <see cref="KilledWhileBurning"/>
    /// (Combustion listens). Added to the bot on first burn; never destroyed.
    /// </summary>
    public sealed class HellBurn : MonoBehaviour
    {
        /// <summary>Defaults for a lava touch: 3 s at 8 damage a second.</summary>
        public const float LavaSeconds = 3f, LavaDps = 8f;

        /// <summary>A bot wrecked while burning, and where its centre was.</summary>
        public static event Action<SkylineBot, Vector3> KilledWhileBurning;

        readonly BurnStatus _burn = new BurnStatus();
        SkylineBot _bot;
        float _ember;

        public bool Burning => _burn.Burning;

        /// <summary>Sets <paramref name="bot"/> burning for <paramref name="seconds"/> at <paramref name="dps"/> (a longer or
        /// hotter burn refreshes it).</summary>
        public static void Apply(SkylineBot bot, float seconds = LavaSeconds, float dps = LavaDps)
        {
            if (bot == null || !bot.Alive) return;
            if (!bot.TryGetComponent<HellBurn>(out var b)) { b = bot.gameObject.AddComponent<HellBurn>(); b._bot = bot; }
            b._burn.Apply(seconds, dps);
        }

        public static bool IsBurning(SkylineBot bot) => bot != null && bot.TryGetComponent<HellBurn>(out var b) && b.Burning;

        void Awake() { if (_bot == null) _bot = GetComponent<SkylineBot>(); }

        void Update()
        {
            if (_bot == null || !_burn.Burning) return;
            if (!_bot.Alive) { _burn.Clear(); return; }
            float due = _burn.Tick(Time.deltaTime);
            _ember -= Time.deltaTime;
            if (_ember <= 0f)
            {
                _ember = 0.12f;
                HellShadowFire.Burning(_bot.Center);   // a burning enemy lights the dark round it (docs/HELL-SHADOW.md)
                SkylineSpark.Burst(_bot.Center + UnityEngine.Random.insideUnitSphere * _bot.Radius * 0.6f, Vector3.up, 2, 1.6f);
            }
            if (due > 0f)
            {
                var c = _bot.Center;
                _bot.TakeHit(new SkylineHitInfo
                {
                    gun = null, damage = due, zone = HitZone.Body, plate = -1, point = c, normal = Vector3.up, direction = Vector3.down,
                    fromWrit = false, knockback = 0f, time = Time.time,
                });
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => KilledWhileBurning = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Hook()
        {
            // after SkylineBot's own reset (SubsystemRegistration) has cleared its events
            SkylineBot.Wrecked += bot =>
            {
                if (bot == null || !bot.TryGetComponent<HellBurn>(out var b) || !b._burn.Burning) return;
                b._burn.Clear();
                KilledWhileBurning?.Invoke(bot, bot.transform.position + Vector3.up * 0.5f);
            };
        }
    }
}
