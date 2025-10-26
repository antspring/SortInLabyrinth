using System;
using System.Collections.Generic;
using System.Linq;

namespace ConsoleApp1;

public class State(string hallway, Dictionary<char, string> rooms)
{
    public string Hallway { get; } = hallway;
    public Dictionary<char, string> Rooms { get; } = rooms;

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Hallway.GetHashCode();
            foreach (var kv in Rooms)
                hash = (hash * 31) ^ (kv.Key.GetHashCode() ^ kv.Value.GetHashCode());
            return hash;
        }
    }

    public override bool Equals(object obj)
    {
        if (obj is not State s)
            return false;

        if (Hallway != s.Hallway)
            return false;

        foreach (var key in Rooms.Keys)
            if (!s.Rooms.ContainsKey(key) || s.Rooms[key] != Rooms[key])
                return false;

        return true;
    }
}

class Program
{
    static readonly Dictionary<char, int> RoomX = new()
    {
        { 'A', 2 },
        { 'B', 4 },
        { 'C', 6 },
        { 'D', 8 }
    };

    static readonly HashSet<int> Blocked = new() { 2, 4, 6, 8 };

    static readonly Dictionary<char, int> Cost = new()
    {
        { 'A', 1 },
        { 'B', 10 },
        { 'C', 100 },
        { 'D', 1000 }
    };

    public static IEnumerable<(State next, int moveCost)> GenerateMoves(State s)
    {
        foreach (var move in MovesFromRooms(s))
            yield return move;
        foreach (var move in MovesToRooms(s))
            yield return move;
    }

    public static IEnumerable<(State, int)> MovesFromRooms(State s)
    {
        var hallway = s.Hallway.ToCharArray();
        foreach (var (roomType, content) in s.Rooms)
        {
            var roomX = RoomX[roomType];

            if (content.All(c => c == '.' || c == roomType))
                continue;

            var depth = -1;
            var amphipod = '.';
            for (var i = 0; i < content.Length; i++)
            {
                if (content[i] != '.')
                {
                    amphipod = content[i];
                    depth = i;
                    break;
                }
            }

            if (depth == -1) continue;

            var stepsOut = depth + 1;

            for (var pos = roomX - 1; pos >= 0; pos--)
            {
                if (hallway[pos] != '.') break;
                if (Blocked.Contains(pos)) continue;
                yield return MakeMove(s, roomType, depth, pos, amphipod, stepsOut, roomX);
            }

            for (var pos = roomX + 1; pos < hallway.Length; pos++)
            {
                if (hallway[pos] != '.') break;
                if (Blocked.Contains(pos)) continue;
                yield return MakeMove(s, roomType, depth, pos, amphipod, stepsOut, roomX);
            }
        }
    }

    public static (State, int) MakeMove(State s, char roomType, int depth, int pos, char amphipod,
        int stepsOut, int roomX)
    {
        var newHallway = s.Hallway.ToCharArray();
        var newRooms = s.Rooms.ToDictionary(kv => kv.Key, kv => kv.Value.ToCharArray());

        newHallway[pos] = amphipod;
        newRooms[roomType][depth] = '.';

        var newHallStr = new string(newHallway);
        var newRoomsDict = newRooms.ToDictionary(kv => kv.Key, kv => new string(kv.Value));

        var dist = Math.Abs(pos - roomX) + stepsOut;
        var cost = dist * Cost[amphipod];
        return (new State(newHallStr, newRoomsDict), cost);
    }

    public static IEnumerable<(State, int)> MovesToRooms(State s)
    {
        var hallway = s.Hallway.ToCharArray();
        foreach (var pos in Enumerable.Range(0, hallway.Length))
        {
            var ch = hallway[pos];
            if (ch == '.') continue;
            var targetX = RoomX[ch];

            var step = pos < targetX ? 1 : -1;
            var blocked = false;
            for (var i = pos + step; i != targetX + step; i += step)
            {
                if (hallway[i] != '.')
                {
                    blocked = true;
                    break;
                }
            }

            if (blocked) continue;

            var room = s.Rooms[ch];
            if (room.Any(c => c != '.' && c != ch))
                continue;

            var newHallway = s.Hallway.ToCharArray();
            var newRooms = s.Rooms.ToDictionary(kv => kv.Key, kv => kv.Value.ToCharArray());

            var depth = -1;
            for (var i = room.Length - 1; i >= 0; i--)
                if (room[i] == '.')
                {
                    depth = i;
                    break;
                }

            if (depth == -1) continue;

            newHallway[pos] = '.';
            newRooms[ch][depth] = ch;

            var newHallStr = new string(newHallway);
            var newRoomsDict = newRooms.ToDictionary(kv => kv.Key, kv => new string(kv.Value));

            var dist = Math.Abs(pos - targetX) + depth + 1;
            var cost = dist * Cost[ch];
            yield return (new State(newHallStr, newRoomsDict), cost);
        }
    }

    public static int Solve(State start, State goal)
    {
        var pq = new PriorityQueue<State, int>();
        var best = new Dictionary<State, int> { [start] = 0 };

        pq.Enqueue(start, 0);

        while (pq.Count > 0)
        {
            pq.TryDequeue(out var state, out var cost);
            if (state.Equals(goal)) return cost;
            if (cost > best[state]) continue;

            foreach (var (next, moveCost) in GenerateMoves(state))
            {
                var newCost = cost + moveCost;
                if (!best.TryGetValue(next, out var old) || newCost < old)
                {
                    best[next] = newCost;
                    pq.Enqueue(next, newCost);
                }
            }
        }

        return int.MaxValue;
    }

    public static (State start, State goal) ParseInput(List<string> lines)
    {
        var hallway = lines[1].Trim('#').Trim();
        char[] order = { 'A', 'B', 'C', 'D' };

        var roomLines = new List<string>();
        for (var i = 2; i < lines.Count; i++)
        {
            if (lines[i].Contains('#') && lines[i].Contains('A') || lines[i].Contains('B') || lines[i].Contains('C') ||
                lines[i].Contains('D'))
                roomLines.Add(lines[i]);
        }

        var depth = roomLines.Count;
        var rooms = new Dictionary<char, string>();

        for (var i = 0; i < 4; i++)
        {
            var col = 3 + i * 2;
            var content = string.Concat(roomLines.Select(line => line[col]));
            rooms[order[i]] = content;
        }

        var goalRooms = order.ToDictionary(c => c, c => new string(c, depth));

        var start = new State(hallway, rooms);
        var goal = new State(hallway, goalRooms);

        return (start, goal);
    }


    public static void Main()
    {
        var lines = new List<string>();
        string line;

        while ((line = Console.ReadLine()) != null)
        {
            lines.Add(line);
        }

        var (start, goal) = ParseInput(lines);
        var result = Solve(start, goal);

        Console.WriteLine($"Минимальная энергия: {result}");
    }
}