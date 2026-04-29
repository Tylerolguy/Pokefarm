using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

// Static helper for dungeon Generator logic shared across the game loop.
internal static class DungeonGenerator
{
    // Builds generate from current inputs for downstream gameplay logic.
    public static GeneratedDungeon Generate(DungeonDefinition definition)
    {
        if (definition.IsPredetermined && string.Equals(definition.PredeterminedLayoutId, "tutorial", StringComparison.OrdinalIgnoreCase))
        {
            List<GeneratedDungeonRoom> tutorialRooms =
            [
                new GeneratedDungeonRoom(1, new DungeonRoomDefinition("Tutorial Room 1", DungeonRoomType.Reward, CreatePlaceholderTemplate())),
                new GeneratedDungeonRoom(2, new DungeonRoomDefinition("Tutorial Room 2", DungeonRoomType.Reward, CreatePlaceholderTemplate())),
                new GeneratedDungeonRoom(3, new DungeonRoomDefinition("Tutorial Junction", DungeonRoomType.Puzzle, CreatePlaceholderTemplate())),
                new GeneratedDungeonRoom(4, new DungeonRoomDefinition("Upper Path", DungeonRoomType.Trap, CreatePlaceholderTemplate())),
                new GeneratedDungeonRoom(5, new DungeonRoomDefinition("Lower Path", DungeonRoomType.Enemy, CreatePlaceholderTemplate()))
            ];
            (List<string> layoutRows, Point playerStartTile) = DungeonLayoutBuilder.BuildTutorialLayout();
            return new GeneratedDungeon(definition.Name, tutorialRooms, layoutRows, playerStartTile);
        }

        if (definition.RoomPool.Count == 0)
        {
            return new GeneratedDungeon(definition.Name, [], [new string('#', 8)], Point.Zero);
        }

        List<DungeonRoomDefinition> validRooms = [];
        foreach (DungeonRoomDefinition room in definition.RoomPool)
        {
            if (DungeonRoomTemplateValidator.IsValid(room.Template, out _))
            {
                validRooms.Add(room);
            }
        }

        if (validRooms.Count == 0)
        {
            return new GeneratedDungeon(definition.Name, [], [new string('#', 8)], Point.Zero);
        }

        int minRooms = Math.Max(1, definition.MinRoomCount);
        int maxRooms = Math.Max(minRooms, definition.MaxRoomCount);
        int roomCount = Random.Shared.Next(minRooms, maxRooms + 1);

        List<GeneratedDungeonRoom> rooms = [];
        for (int index = 0; index < roomCount; index++)
        {
            int depth = index + 1;
            List<DungeonRoomDefinition> eligibleRooms = validRooms
                .Where(room => depth >= room.MinDepth && depth <= room.MaxDepth)
                .ToList();
            if (eligibleRooms.Count == 0)
            {
                eligibleRooms = validRooms;
            }

            DungeonRoomDefinition room = PickWeightedRoom(eligibleRooms);
            rooms.Add(new GeneratedDungeonRoom(index + 1, room));
        }

        (List<string> generatedLayoutRows, Point generatedStart) = DungeonLayoutBuilder.BuildLinearLayout(rooms);
        return new GeneratedDungeon(definition.Name, rooms, generatedLayoutRows, generatedStart);
    }

    private static DungeonRoomTemplate CreatePlaceholderTemplate()
    {
        return new DungeonRoomTemplate(
            new Point(8, 6),
            ["########", "#......#", "#......#", "#......#", "#......#", "########"],
            [],
            [],
            []);
    }

    // Handles pick Weighted Room for this gameplay subsystem.
    private static DungeonRoomDefinition PickWeightedRoom(IReadOnlyList<DungeonRoomDefinition> rooms)
    {
        int totalWeight = 0;
        foreach (DungeonRoomDefinition room in rooms)
        {
            totalWeight += Math.Max(1, room.Weight);
        }

        int roll = Random.Shared.Next(1, totalWeight + 1);
        int runningWeight = 0;
        foreach (DungeonRoomDefinition room in rooms)
        {
            runningWeight += Math.Max(1, room.Weight);
            if (roll <= runningWeight)
            {
                return room;
            }
        }

        return rooms[^1];
    }
}
