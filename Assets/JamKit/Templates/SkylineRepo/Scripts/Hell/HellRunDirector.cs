using System.Collections.Generic;
using JamKit.Skyline;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace JamKit.SkylineRepo
{
    /// <summary>
    /// The descent (docs/HELL-ROOMS.md): deals the run's deck (<see cref="HellDeck"/>, from <see cref="seed"/>; 0 = a new seed
    /// each run), drops the player into each dealt room in turn, restarts the room on a death, moves the player from a room's
    /// drop hole into the next room's vestibule (from above), and after a floor's fourth room down the floor's shaft; at its
    /// bottom raises <see cref="HellRunEvents.FloorCleared"/> and waits for <see cref="HellRunEvents.ContinueAfterPick"/> (the
    /// skill pick). The last floor's shaft ends the run (Enter descends again). The 18 rooms are built in the scene once
    /// (<see cref="HellWorld"/>); only the current and the previous room are active.
    /// </summary>
    public sealed class HellRunDirector : MonoBehaviour
    {
        public SkylinePlayer player;
        /// <summary>The run's seed (0: a fresh one every run). <c>-hellSeed N</c> on the command line overrides it.</summary>
        public int seed;

        public HellRun Run { get; private set; }
        public HellRoom Current { get; private set; }

        readonly Dictionary<long, HellRoom> _rooms = new Dictionary<long, HellRoom>();
        readonly HellHudLine _hud = new HellHudLine();
        HellRoom _previous;
        float _pickWait;

        static long Key(int floor, HellRoomKind kind) => floor * 100L + (int)kind;

        void Start()
        {
            if (player == null) player = FindFirstObjectByType<SkylinePlayer>();
            foreach (var r in FindObjectsByType<HellRoom>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                _rooms[Key(r.floor, r.kind)] = r;
                r.gameObject.SetActive(false);
            }
            ulong s = (ulong)(seed != 0 ? seed : 0);
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i + 1 < args.Length; i++)
                if (args[i] == "-hellSeed" && ulong.TryParse(args[i + 1], out var parsed)) s = parsed;
            if (s == 0) s = Rng.TimeSeed();
            Run = new HellRun(s);
            Run.RoomEntered += OnRoomEntered;
            Run.FloorCleared += f => Debug.Log("HELL_RUN floor " + f + " cleared");
            Run.Finished += OnFinished;
            if (player != null) player.Wrecked += OnWrecked;
            Debug.Log("HELL_RUN start seed " + s + " deck " + DeckText());
            Run.Start();
            HellRunEvents.RaiseRunStarted();
        }

        void OnDestroy()
        {
            if (player != null) player.Wrecked -= OnWrecked;
        }

        string DeckText()
        {
            var sb = new System.Text.StringBuilder();
            for (int f = 0; f < Run.Deck.Length; f++)
            {
                sb.Append(f == 0 ? "" : " | ");
                for (int i = 0; i < Run.Deck[f].Length; i++) sb.Append(i == 0 ? "" : ", ").Append(Run.Deck[f][i]);
            }
            return sb.ToString();
        }

        void OnRoomEntered(int floor, int index)
        {
            var kind = Run.CurrentKind;
            if (!_rooms.TryGetValue(Key(floor, kind), out var room))
            {
                Debug.LogError("HELL_RUN no room built for floor " + floor + " " + kind + ": rebuild the Hell scene");
                return;
            }
            if (_previous != null && _previous != Current) _previous.gameObject.SetActive(false);
            if (Current != null) Current.End();
            _previous = Current;
            Current = room;
            room.gameObject.SetActive(true);
            room.Begin(player);
            var d = room.Def;
            player.Movement.Teleport(HellRoom.U(d.entry) + Vector3.up * HellWorld.DropInHeight, d.entryYaw, Vector3.zero);
            player.SetAim(d.entryYaw, 0f);
            if (index == 0) _hud.Banner(HellRoomVariants.Floors[floor].name.ToUpperInvariant(), 3f);
            Debug.Log("HELL_RUN room " + (floor + 1) + "." + (index + 1) + " " + kind);
            HellRunEvents.RaiseRoomEntered(floor, index, kind.ToString());
        }

        void OnWrecked()
        {
            if (Run == null || player == null) return;
            if (Run.Phase == HellRunPhase.InRoom && Current != null)
            {
                Run.Died();
                Current.Restart();
                player.Restart(HellRoom.U(Current.Def.entry), Current.Def.entryYaw);
                _hud.Banner("AGAIN", 1.5f);
                Debug.Log("HELL_RUN died in " + Current.kind + " (deaths " + Run.Deaths + ")");
                HellRunEvents.RaiseRoomRestarted();
            }
            else player.Restart(HellRoom.U(HellWorld.ShaftCentre(Run.Floor)) + Vector3.up * (HellWorld.ShaftBottom + 0.1f), 0f);
        }

        void OnFinished()
        {
            if (Current != null) Current.End();
            _hud.Banner("THE BOTTOM OF HELL", -1f);
            _hud.SetLine("Deaths " + Run.Deaths + "   -   Enter: descend again");
            Debug.Log("HELL_RUN finished, deaths " + Run.Deaths);
            HellRunEvents.RaiseRunFinished();
        }

        void Update()
        {
            if (!_hud.Built && SkylineHud.Instance != null && SkylineHud.Instance.Safe != null) _hud.Build(SkylineHud.Instance.Safe);
            _hud.Tick();
            if (Run == null || player == null) return;
            var feet = player.transform.position;
            switch (Run.Phase)
            {
                case HellRunPhase.InRoom:
                    if (Current != null)
                    {
                        float c = Current.Clock;
                        _hud.SetLine(Current.Objective.Line + "     " + (int)(c / 60f) + ":" + ((int)c % 60).ToString("00"));
                        if (Current.InDrop(feet) && Run.ReachedDrop())
                        {
                            if (Run.Phase == HellRunPhase.Dropping) Run.Landed();
                            else EnterShaft();
                        }
                    }
                    break;
                case HellRunPhase.Shaft:
                    if (HellRoomLint.Contains(HellWorld.ShaftBottomZone(Run.Floor), HellRoom.N(feet)))
                    {
                        Run.ShaftBottom();
                        if (Run.Phase == HellRunPhase.AwaitingPick)
                        {
                            _hud.SetLine("");
                            _pickWait = 0f;
                            if (!HellRunEvents.RaiseFloorCleared(Run.Floor)) Run.Continue();
                        }
                    }
                    break;
                case HellRunPhase.AwaitingPick:
                    _pickWait += Time.unscaledDeltaTime;
                    if (!HellRunEvents.WaitingForPick) Run.Continue();
                    break;
                case HellRunPhase.Finished:
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    if (kb != null && kb.enterKey.wasPressedThisFrame) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
                    break;
            }
        }

        void EnterShaft()
        {
            if (Current != null) Current.End();
            player.Movement.Teleport(HellRoom.U(HellWorld.ShaftArrival(Run.Floor)), 0f, Vector3.zero);
            _hud.SetLine("DOWN");
            Debug.Log("HELL_RUN shaft of floor " + Run.Floor);
        }
    }
}
