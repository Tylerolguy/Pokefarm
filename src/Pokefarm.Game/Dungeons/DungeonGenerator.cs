namespace Pokefarm.Game;

internal static class DungeonGenerator
{
    public static GeneratedDungeon Generate(DungeonDefinition definition)
    {
        if (definition.RoomPool.Count == 0)
        {
            return new GeneratedDungeon(definition.Name, []);
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
            return new GeneratedDungeon(definition.Name, []);
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

        return new GeneratedDungeon(definition.Name, rooms);
    }

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
