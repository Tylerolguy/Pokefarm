using Microsoft.Xna.Framework;

namespace Pokefarm.Game;

// Builds connected dungeon layouts into a single tile map for movement/collision.
internal static class DungeonLayoutBuilder
{
    public static (List<string> LayoutRows, Point PlayerStartTile) BuildLinearLayout(IReadOnlyList<GeneratedDungeonRoom> rooms)
    {
        const int mapWidth = 64;
        const int mapHeight = 40;
        char[,] grid = CreateFilledGrid(mapWidth, mapHeight, '#');

        int cursorX = 4;
        int centerY = mapHeight / 2;
        Point playerStart = new(6, centerY);
        Point previousExit = playerStart;

        for (int index = 0; index < rooms.Count; index++)
        {
            DungeonRoomTemplate template = rooms[index].Definition.Template;
            int roomWidth = Math.Clamp(template.Size.X + 2, 10, 16);
            int roomHeight = Math.Clamp(template.Size.Y + 2, 8, 12);
            int roomTop = centerY - (roomHeight / 2);
            Rectangle roomBounds = new(cursorX, roomTop, roomWidth, roomHeight);
            CarveRoom(grid, roomBounds);
            Point entry = new(roomBounds.Left + 1, roomBounds.Top + (roomBounds.Height / 2));
            Point exit = new(roomBounds.Right - 2, roomBounds.Top + (roomBounds.Height / 2));
            CarveCorridor(grid, previousExit, entry);
            previousExit = exit;
            cursorX = roomBounds.Right + 4;
            if (cursorX >= mapWidth - 10)
            {
                break;
            }
        }

        return (ToRows(grid), playerStart);
    }

    public static (List<string> LayoutRows, Point PlayerStartTile) BuildTutorialLayout()
    {
        const int mapWidth = 64;
        const int mapHeight = 40;
        char[,] grid = CreateFilledGrid(mapWidth, mapHeight, '#');

        Rectangle room1 = new(4, 16, 10, 8);
        Rectangle room2 = new(18, 14, 16, 12);
        Rectangle room3 = new(38, 16, 10, 8);
        Rectangle room4Up = new(48, 8, 10, 8);
        Rectangle room5Down = new(46, 24, 14, 12);

        CarveRoom(grid, room1);
        CarveRoom(grid, room2);
        CarveRoom(grid, room3);
        CarveRoom(grid, room4Up);
        CarveRoom(grid, room5Down);

        CarveCorridor(grid, new Point(room1.Right - 2, room1.Top + (room1.Height / 2)), new Point(room2.Left + 1, room2.Top + (room2.Height / 2)));
        CarveCorridor(grid, new Point(room2.Right - 2, room2.Top + (room2.Height / 2)), new Point(room3.Left + 1, room3.Top + (room3.Height / 2)));
        CarveCorridor(grid, new Point(room3.Left + (room3.Width / 2), room3.Top + 1), new Point(room4Up.Left + (room4Up.Width / 2), room4Up.Bottom - 2));
        CarveCorridor(grid, new Point(room3.Left + (room3.Width / 2), room3.Bottom - 2), new Point(room5Down.Left + (room5Down.Width / 2), room5Down.Top + 1));

        Point playerStart = new(room1.Left + 2, room1.Top + (room1.Height / 2));
        return (ToRows(grid), playerStart);
    }

    private static char[,] CreateFilledGrid(int width, int height, char fill)
    {
        char[,] grid = new char[height, width];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                grid[y, x] = fill;
            }
        }

        return grid;
    }

    private static void CarveRoom(char[,] grid, Rectangle room)
    {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        for (int y = room.Top; y < room.Bottom; y++)
        {
            for (int x = room.Left; x < room.Right; x++)
            {
                if (x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1)
                {
                    continue;
                }

                bool isWall = x == room.Left || x == room.Right - 1 || y == room.Top || y == room.Bottom - 1;
                grid[y, x] = isWall ? '#' : '.';
            }
        }
    }

    private static void CarveCorridor(char[,] grid, Point start, Point end)
    {
        int x = start.X;
        int y = start.Y;
        CarveCorridorCell(grid, x, y);
        while (x != end.X)
        {
            x += Math.Sign(end.X - x);
            CarveCorridorCell(grid, x, y);
        }

        while (y != end.Y)
        {
            y += Math.Sign(end.Y - y);
            CarveCorridorCell(grid, x, y);
        }
    }

    private static void CarveCorridorCell(char[,] grid, int x, int y)
    {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        if (x <= 0 || y <= 0 || x >= width - 1 || y >= height - 1)
        {
            return;
        }

        grid[y, x] = '.';
    }

    private static List<string> ToRows(char[,] grid)
    {
        int height = grid.GetLength(0);
        int width = grid.GetLength(1);
        List<string> rows = new(height);
        for (int y = 0; y < height; y++)
        {
            char[] row = new char[width];
            for (int x = 0; x < width; x++)
            {
                row[x] = grid[y, x];
            }

            rows.Add(new string(row));
        }

        return rows;
    }
}
