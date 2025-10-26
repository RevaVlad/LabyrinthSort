namespace LabyrinthSort;

public class State
{
    private State(int roomSize)
    {
        RoomSize = roomSize;

        Rooms = new char[4][];
        for (var i = 0; i < 4; i++)
        {
            Rooms[i] = new char[roomSize];
            for (var j = 0; j < roomSize; j++) Rooms[i][j] = '.';
        }
    }

    public char[] Corridor { get; } = new string('.', 11).ToCharArray();
    public char[][] Rooms { get; }
    private int RoomSize { get; }

    public State Copy()
    {
        var newState = new State(RoomSize);
        Array.Copy(Corridor, newState.Corridor, 11);
        for (var i = 0; i < 4; i++) Array.Copy(Rooms[i], newState.Rooms[i], RoomSize);
        return newState;
    }

    public override bool Equals(object obj)
    {
        if (obj is not State other) return false;
        if (!Corridor.SequenceEqual(other.Corridor))
            return false;
        for (var i = 0; i < 4; i++)
            if (!Rooms[i].SequenceEqual(other.Rooms[i]))
                return false;
        return true;

    }

    public override int GetHashCode()
    {
        unchecked
        {
            var hash = Corridor.Aggregate(19, (current, c) => current * 41 + c.GetHashCode());
            for (var i = 0; i < 4; i++) hash = Rooms[i].Aggregate(hash, (current, c) => current * 31 + c.GetHashCode());

            return hash;
        }
    }

    public bool IsGoal()
    {
        for (var i = 0; i < 4; i++)
        {
            var targetChar = (char)('A' + i);
            for (var j = 0; j < RoomSize; j++)
                if (Rooms[i][j] != targetChar)
                    return false;
        }

        return true;
    }

    public static State Parse(List<string> lines, int roomSize)
    {
        var state = new State(roomSize);

        var corridorLine = lines[1];
        for (var i = 0; i < 11; i++) state.Corridor[i] = corridorLine[i + 1];

        for (var i = 0; i < 4; i++)
        for (var j = 0; j < roomSize; j++)
            state.Rooms[i][j] = lines[2 + j][3 + 2 * i];

        return state;
    }
}

public class Solver(int roomSize)
{
    private readonly int[] _roomEntrances = [2, 4, 6, 8];
    private readonly int[] _corridorPositions = [0, 1, 3, 5, 7, 9, 10];
    private readonly Dictionary<char, int> _costs = new()
    {
        ['A'] = 1,
        ['B'] = 10,
        ['C'] = 100,
        ['D'] = 1000
    };

    public int Solve(State initialState)
    {
        var distances = new Dictionary<State, int>();
        var pq = new PriorityQueue<State, int>();

        distances[initialState] = 0;
        pq.Enqueue(initialState, 0);

        while (pq.Count > 0)
        {
            pq.TryDequeue(out var current, out var energy);

            if (current!.IsGoal())
                return energy;

            if (energy > distances[current])
                continue;

            foreach (var (nextState, cost) in GetPossibleMoves(current))
            {
                var newEnergy = energy + cost;

                if (distances.TryGetValue(nextState, out var previousEnergy) && newEnergy >= previousEnergy) continue;
                
                distances[nextState] = newEnergy;
                pq.Enqueue(nextState, newEnergy);
            }
        }

        return -1;
    }

    private IEnumerable<(State, int)> GetPossibleMoves(State state)
    {
        foreach (var move in GetCorridorToRoomMoves(state)) 
            yield return move;

        foreach (var move in GetRoomToCorridorMoves(state)) 
            yield return move;
    }

    private IEnumerable<(State, int)> GetCorridorToRoomMoves(State state)
    {
        for (var pos = 0; pos < 11; pos++)
        {
            if (state.Corridor[pos] == '.')
                continue;

            var item = state.Corridor[pos];
            var targetRoom = item - 'A';

            if (!CanMoveToRoom(state, targetRoom, item))
                continue;

            var entrance = _roomEntrances[targetRoom];
            if (!IsPathClear(state.Corridor, pos, entrance))
                continue;

            var roomPosition = GetTargetRoomPosition(state, targetRoom);
            if (roomPosition == -1)
                continue;

            var steps = Math.Abs(pos - entrance) + roomPosition + 1;
            var cost = steps * _costs[item];

            var newState = state.Copy();
            newState.Corridor[pos] = '.';
            newState.Rooms[targetRoom][roomPosition] = item;

            yield return (newState, cost);
        }
    }

    private IEnumerable<(State, int)> GetRoomToCorridorMoves(State state)
    {
        for (var roomId = 0; roomId < 4; roomId++)
        {
            var roomPosition = GetTopItemPosition(state, roomId);
            if (roomPosition == -1)
                continue;

            var item = state.Rooms[roomId][roomPosition];

            if (roomId == item - 'A' && !HasWrongItemsBelow(state, roomId, roomPosition))
                continue;

            foreach (var targetPos in _corridorPositions)
            {
                var entrance = _roomEntrances[roomId];
                if (!IsPathClear(state.Corridor, entrance, targetPos))
                    continue;

                var steps = roomPosition + 1 + Math.Abs(entrance - targetPos);
                var cost = steps * _costs[item];

                var newState = state.Copy();
                newState.Rooms[roomId][roomPosition] = '.';
                newState.Corridor[targetPos] = item;

                yield return (newState, cost);
            }
        }
    }

    private static bool CanMoveToRoom(State state, int roomId, char item)
    {
        return roomId == item - 'A' && state.Rooms[roomId].All(roomItem => roomItem == '.' || roomItem == item);
    }

    private static bool IsPathClear(char[] corridor, int from, int to)
    {
        var start = Math.Min(from, to);
        var end = Math.Max(from, to);

        for (var i = start; i <= end; i++)
        {
            if (i != from && corridor[i] != '.')
                return false;
        }
        return true;
    }

    private int GetTargetRoomPosition(State state, int roomId)
    {
        for (var i = roomSize - 1; i >= 0; i--)
        {
            if (state.Rooms[roomId][i] == '.')
                return i;
        }
        return -1;
    }

    private int GetTopItemPosition(State state, int roomId)
    {
        for (var i = 0; i < roomSize; i++)
        {
            if (state.Rooms[roomId][i] != '.')
                return i;
        }
        return -1;
    }

    private bool HasWrongItemsBelow(State state, int roomId, int position)
    {
        for (var i = position + 1; i < roomSize; i++)
        {
            if (state.Rooms[roomId][i] != (char)('A' + roomId))
                return true;
        }
        return false;
    }
}

public class Program
{
    public static void Main()
    {
        var lines = new List<string>();
        string line;

        while ((line = Console.ReadLine()) != null && line != "") lines.Add(line);

        var result = Solve(lines);
        Console.WriteLine(result);
    }

    private static int Solve(List<string> lines)
    {
        var roomDepth = lines.Count - 3;
        var initialState = State.Parse(lines, roomDepth);
        var solver = new Solver(roomDepth);
        return solver.Solve(initialState);
    }
}