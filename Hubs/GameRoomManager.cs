namespace ProteusChess.Hubs;

using System.Collections.Concurrent;

public class GameRoomManager
{
    private readonly ConcurrentDictionary<string, GameRoom> _rooms = new();

    public GameRoom CreateRoom()
    {
        var room = new GameRoom();
        _rooms[room.RoomCode] = room;
        return room;
    }

    public GameRoom? GetRoom(string roomCode)
    {
        _rooms.TryGetValue(roomCode.ToUpperInvariant(), out var room);
        return room;
    }

    public void RemoveRoom(string roomCode)
    {
        _rooms.TryRemove(roomCode.ToUpperInvariant(), out _);
    }

    public GameRoom? FindRoomByConnection(string connectionId)
    {
        return _rooms.Values.FirstOrDefault(r =>
            r.WhiteConnectionId == connectionId || r.BlackConnectionId == connectionId);
    }
}
