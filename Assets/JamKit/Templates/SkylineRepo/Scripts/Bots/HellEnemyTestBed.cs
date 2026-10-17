using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.InputSystem;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// A drop-in check of the Hell enemies (docs/HELL-ENEMIES.md, "See them in Unity"): add it to any scene with a
    /// <see cref="SkylinePlayer"/> on flat ground (Add Component, "JamKit/Hell/Enemy Test Bed") and press Play. At start it spawns,
    /// in front of the player, a fire imp (14 m out, hovering 3 m up), a scythe demon (22 m out) and a lava ooze (9 m out), each
    /// awake in its own group, and a beam lane across the way 6 m ahead. Keys: <b>T</b> spawns the set again, <b>R</b> uses the
    /// real <see cref="HellRewind"/> on the player for free (no cooldown; added to the player if the abilities aren't on), so
    /// the player rewinds too (docs/HELL-INTEGRATION.md). Logs <c>HELL_TESTBED ...</c>.
    /// </summary>
    [AddComponentMenu("JamKit/Hell/Enemy Test Bed")]
    public sealed class HellEnemyTestBed : MonoBehaviour
    {
        public bool imp = true, demon = true, ooze = true, beamLane = true, debugRewind = true;
        public float impDistance = 14f, demonDistance = 22f, oozeDistance = 9f;

        SkylinePlayer _player;
        int _sets;

        void Start()
        {
            SkylineBot.HellRoster = true;
            HellFx.Prewarm();
            SkylineBolt.Prewarm();
            _player = Object.FindAnyObjectByType<SkylinePlayer>();
            if (_player == null)
            {
                Debug.LogWarning("HELL_TESTBED no SkylinePlayer in the scene");
                enabled = false;
                return;
            }
            if (debugRewind && _player.GetComponent<HellRewind>() == null) _player.gameObject.AddComponent<HellRewind>();
            // the imp's patrol and search stay inside SkylineBot.SearchArea (the Skyline hall by default): here, 60 m round the player
            Vector3 at = _player.transform.position;
            SkylineBot.SearchArea = Rect.MinMaxRect(at.x - 60f, at.z - 60f, at.x + 60f, at.z + 60f);
            SpawnSet();
            if (beamLane)
            {
                Vector3 p = _player.transform.position, f = Flat(_player.transform.forward), r = new Vector3(f.z, 0f, -f.x);
                Vector3 c = p + f * 6f + Vector3.up * 1f;
                HellBeamLane.Create(c - r * 7f, c + r * 7f, 3.5f, 1.5f, transform, _player);
                Debug.Log("HELL_TESTBED beam lane across 6 m ahead, every 3.5 s");
            }
        }

        static Vector3 Flat(Vector3 v)
        {
            v.y = 0f;
            return v.sqrMagnitude > 1e-6f ? v.normalized : Vector3.forward;
        }

        void SpawnSet()
        {
            _sets++;
            Vector3 p = _player.transform.position, f = Flat(_player.transform.forward), r = new Vector3(f.z, 0f, -f.x);
            float yaw = Mathf.Atan2(-f.x, -f.z) * Mathf.Rad2Deg;   // facing the player
            int g = 9000 + _sets * 10;
            ulong seed = (ulong)(1000 + _sets);
            if (imp) Wake(SkylineBot.Spawn(SkylineEnemies.DroneId, p + f * impDistance - r * 4f + Vector3.up * 4.25f, yaw, seed, _player, null, transform, g, Vector3.zero), "fire imp");
            if (demon) Wake(SkylineBot.Spawn(SkylineEnemies.SpotlightId, p + f * demonDistance, yaw, seed + 1, _player, null, transform, g + 1, Vector3.zero), "scythe demon");
            if (ooze) Wake(SkylineBot.Spawn(SkylineEnemies.FloorCareId, p + f * oozeDistance + r * 3f, yaw, seed + 2, _player, null, transform, g + 2, Vector3.zero), "lava ooze");
        }

        static void Wake(SkylineBot b, string what)
        {
            if (b == null) { Debug.LogWarning("HELL_TESTBED could not spawn " + what); return; }
            SkylineBot.Wake(b.group, b);
            Debug.Log("HELL_TESTBED spawned " + what + " (" + b.Id + ", " + b.Def.displayName + ") at " + b.transform.position);
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            if (kb.tKey.wasPressedThisFrame) SpawnSet();
            if (debugRewind && kb.rKey.wasPressedThisFrame)
            {
                var rw = _player != null ? _player.GetComponent<HellRewind>() : null;
                bool done = rw != null && rw.TryRewind(free: true);
                Debug.Log("HELL_TESTBED rewind " + (done ? "used" : "not ready (nothing recorded)") + "; live bots now " + SkylineBot.Live.Count);
            }
        }
    }
}
