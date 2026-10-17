using System;
using System.Collections.Generic;

namespace JamKit.Skyline
{
    /// <summary>The run's deck (event/HELL-DESIGN.md): each floor deals 4 of its 6 rooms, in a seeded random order.</summary>
    public static class HellDeck
    {
        public const int RoomsPerFloor = 4;
        // Its own PCG stream, so nothing else seeded from the run's seed shifts the deal.
        const ulong DeckStream = 0x6A09E667F3BCC909UL;

        /// <summary>For each of the <see cref="HellRoomVariants.FloorCount"/> floors, the 4 room kinds dealt from the 6, in order.</summary>
        public static HellRoomKind[][] Deal(ulong seed)
        {
            var rng = new Rng(seed, DeckStream);
            var floors = new HellRoomKind[HellRoomVariants.FloorCount][];
            for (int f = 0; f < floors.Length; f++)
            {
                var kinds = (HellRoomKind[])HellRoomLayouts.Kinds.Clone();
                for (int i = kinds.Length - 1; i > 0; i--)
                {
                    int j = rng.Range(0, i + 1);
                    var t = kinds[i]; kinds[i] = kinds[j]; kinds[j] = t;
                }
                floors[f] = new HellRoomKind[RoomsPerFloor];
                Array.Copy(kinds, floors[f], RoomsPerFloor);
            }
            return floors;
        }
    }

    public enum HellRunPhase
    {
        /// <summary>Before the start (the title, or the first drop).</summary>
        NotStarted,
        /// <summary>In a room: the objective is live (or done and the exit open) until the player's feet reach the drop.</summary>
        InRoom,
        /// <summary>Falling down a room's drop hole toward the next room.</summary>
        Dropping,
        /// <summary>Falling down the floor's shaft (after the 4th room).</summary>
        Shaft,
        /// <summary>The floor is cleared and the skill pick is open: waiting for <see cref="HellRun.Continue"/>.</summary>
        AwaitingPick,
        /// <summary>The last floor's shaft was reached: the run is won.</summary>
        Finished,
    }

    /// <summary>
    /// The run's state machine (pure): 3 floors of 4 dealt rooms; a room ends at its drop hole (to the next room) or, for the
    /// fourth, at the floor's shaft; the shaft raises <see cref="FloorCleared"/> and waits for <see cref="Continue"/> (the skill
    /// pick) before the next floor's first room. The last floor's shaft finishes the run. A death restarts the current room
    /// (<see cref="Died"/> counts it; the run keeps going). Out-of-order calls change nothing and return false.
    /// </summary>
    public sealed class HellRun
    {
        public readonly HellRoomKind[][] Deck;
        public HellRunPhase Phase { get; private set; } = HellRunPhase.NotStarted;
        /// <summary>0-based floor (0 the Ember Gate) and room (0..3) indices.</summary>
        public int Floor { get; private set; }
        public int Room { get; private set; }
        public int Deaths { get; private set; }
        public int RoomsCleared { get; private set; }
        public HellRoomKind CurrentKind => Deck[Floor][Room];
        public bool LastRoomOfFloor => Room == HellDeck.RoomsPerFloor - 1;
        public bool LastFloor => Floor == HellRoomVariants.FloorCount - 1;

        /// <summary>The new room the player is now in (floor, room).</summary>
        public event Action<int, int> RoomEntered;
        /// <summary>The floor (0-based) whose shaft was reached; the skill pick opens. Not raised for the last floor.</summary>
        public event Action<int> FloorCleared;
        public event Action Finished;

        public HellRun(ulong seed) { Deck = HellDeck.Deal(seed); }
        public HellRun(HellRoomKind[][] deck) { Deck = deck; }

        public bool Start()
        {
            if (Phase != HellRunPhase.NotStarted) return false;
            Phase = HellRunPhase.InRoom;
            Floor = 0; Room = 0;
            RoomEntered?.Invoke(Floor, Room);
            return true;
        }

        /// <summary>The player's feet reached the room's drop hole (the exit corridor's end).</summary>
        public bool ReachedDrop()
        {
            if (Phase != HellRunPhase.InRoom) return false;
            RoomsCleared++;
            Phase = LastRoomOfFloor ? HellRunPhase.Shaft : HellRunPhase.Dropping;
            return true;
        }

        /// <summary>The drop delivered the player into the next room.</summary>
        public bool Landed()
        {
            if (Phase != HellRunPhase.Dropping) return false;
            Room++;
            Phase = HellRunPhase.InRoom;
            RoomEntered?.Invoke(Floor, Room);
            return true;
        }

        /// <summary>The bottom of the floor's shaft: the floor is cleared (or the run won on the last floor).</summary>
        public bool ShaftBottom()
        {
            if (Phase != HellRunPhase.Shaft) return false;
            if (LastFloor)
            {
                Phase = HellRunPhase.Finished;
                Finished?.Invoke();
                return true;
            }
            Phase = HellRunPhase.AwaitingPick;
            FloorCleared?.Invoke(Floor);
            return true;
        }

        /// <summary>The skill pick closed: the next floor's first room.</summary>
        public bool Continue()
        {
            if (Phase != HellRunPhase.AwaitingPick) return false;
            Floor++;
            Room = 0;
            Phase = HellRunPhase.InRoom;
            RoomEntered?.Invoke(Floor, Room);
            return true;
        }

        /// <summary>A death: the room restarts (the caller resets it). Only counts in a room.</summary>
        public bool Died()
        {
            if (Phase != HellRunPhase.InRoom) return false;
            Deaths++;
            return true;
        }
    }
}
