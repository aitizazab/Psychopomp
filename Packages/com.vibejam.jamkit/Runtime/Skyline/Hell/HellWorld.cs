using System;
using System.Numerics;

namespace JamKit.Skyline
{
    /// <summary>
    /// Where everything sits in the one Hell scene (pure, so the editor builder and the run director agree): all 18 rooms
    /// are built once, each in its own slot (a row per floor, a column per room type, far apart), and the run visits the 4
    /// the deck deals. Rooms are joined by drops: a room's exit corridor ends in a hole; falling into it drops you into the
    /// next room's entry vestibule from above. After a floor's fourth room the hole drops you down the floor's shaft.
    /// </summary>
    public static class HellWorld
    {
        public const float SlotSpacing = 320f, FloorSpacing = 420f;
        /// <summary>How far above a room's entry the player arrives from a drop.</summary>
        public const float DropInHeight = 4f;
        /// <summary>The shaft: its half width, its top (where you arrive) and its bottom floor.</summary>
        public const float ShaftHalf = 5f, ShaftTop = 40f, ShaftBottom = -10f;

        public static Vector3 SlotOffset(int floor, HellRoomKind kind) =>
            new Vector3(Array.IndexOf(HellRoomLayouts.Kinds, kind) * SlotSpacing, 0f, floor * FloorSpacing);

        /// <summary>The room of <paramref name="kind"/> on <paramref name="floor"/> in world space.</summary>
        public static HellRoomDef Room(int floor, HellRoomKind kind) =>
            new HellRoomTransform { offset = SlotOffset(floor, kind) }.Room(HellRoomVariants.Build(kind, floor));

        /// <summary>The middle of floor <paramref name="floor"/>'s shaft at ground level (past the last room column).</summary>
        public static Vector3 ShaftCentre(int floor) => new Vector3(HellRoomLayouts.Kinds.Length * SlotSpacing, 0f, floor * FloorSpacing);

        public static Vector3 ShaftArrival(int floor) => ShaftCentre(floor) + new Vector3(0f, ShaftTop - 2f, 0f);

        /// <summary>Feet in this box: the bottom of the shaft (the floor is cleared).</summary>
        public static CoverBox ShaftBottomZone(int floor)
        {
            var c = ShaftCentre(floor);
            return new CoverBox
            {
                min = new Vector3(c.X - ShaftHalf, ShaftBottom - 1f, c.Z - ShaftHalf),
                max = new Vector3(c.X + ShaftHalf, ShaftBottom + 3f, c.Z + ShaftHalf),
            };
        }
    }
}
