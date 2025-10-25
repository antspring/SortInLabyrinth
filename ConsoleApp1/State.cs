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