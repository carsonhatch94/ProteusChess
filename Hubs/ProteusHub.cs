namespace ProteusChess.Hubs;

using Microsoft.AspNetCore.SignalR;
using ProteusChess.Models;

public class ProteusHub(GameRoomManager roomManager) : Hub
{
    public async Task<string> CreateRoom()
    {
        var room = roomManager.CreateRoom();
        room.WhiteConnectionId = Context.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);
        return room.RoomCode;
    }

    public async Task<GameStateDto?> JoinRoom(string roomCode)
    {
        var room = roomManager.GetRoom(roomCode);
        if (room == null || room.IsFull) return null;

        room.BlackConnectionId = Context.ConnectionId;
        await Groups.AddToGroupAsync(Context.ConnectionId, room.RoomCode);

        var dto = GameStateDto.FromRoom(room);
        await Clients.Group(room.RoomCode).SendAsync("PlayerJoined", dto);
        return dto;
    }

    public async Task<GameStateDto?> MakeMove(string roomCode, int fromRow, int fromCol, int toRow, int toCol)
    {
        var room = roomManager.GetRoom(roomCode);
        if (room == null) return null;

        var color = room.GetPlayerColor(Context.ConnectionId);
        if (color == null || color != room.Game.CurrentPlayer) return null;
        if (room.Game.Phase != TurnPhase.Move) return null;
        if (!room.Game.IsValidMove(fromRow, fromCol, toRow, toCol)) return null;

        room.Game.SelectedSquare = (fromRow, fromCol);
        room.Game.OnSquareClick(toRow, toCol);

        var dto = GameStateDto.FromRoom(room);
        await Clients.Group(roomCode).SendAsync("GameUpdated", dto);
        return dto;
    }

    public async Task<GameStateDto?> Rotate(string roomCode, int row, int col, bool rotateUp)
    {
        var room = roomManager.GetRoom(roomCode);
        if (room == null) return null;

        var color = room.GetPlayerColor(Context.ConnectionId);
        if (color == null || color != room.Game.CurrentPlayer) return null;
        if (room.Game.Phase != TurnPhase.Rotate) return null;

        room.Game.SelectedSquare = (row, col);
        if (rotateUp)
            room.Game.RotateSelectedUp();
        else
            room.Game.RotateSelectedDown();

        var dto = GameStateDto.FromRoom(room);
        await Clients.Group(roomCode).SendAsync("GameUpdated", dto);
        return dto;
    }

    public async Task<GameStateDto?> RequestNewGame(string roomCode)
    {
        var room = roomManager.GetRoom(roomCode);
        if (room == null) return null;

        var color = room.GetPlayerColor(Context.ConnectionId);
        if (color == null) return null;

        room.Game.NewGame();
        var dto = GameStateDto.FromRoom(room);
        await Clients.Group(roomCode).SendAsync("GameUpdated", dto);
        return dto;
    }

    public int[][]? GetValidMoves(string roomCode, int row, int col)
    {
        var room = roomManager.GetRoom(roomCode);
        if (room == null) return null;

        var color = room.GetPlayerColor(Context.ConnectionId);
        if (color == null || color != room.Game.CurrentPlayer) return null;
        if (room.Game.Phase != TurnPhase.Move) return null;

        var piece = room.Game.Board[row, col];
        if (piece == null || piece.Owner != color.Value) return null;

        return room.Game.GetValidMoves(row, col)
            .Select(m => new[] { m.Row, m.Col })
            .ToArray();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var room = roomManager.FindRoomByConnection(Context.ConnectionId);
        if (room != null)
        {
            await Clients.Group(room.RoomCode).SendAsync("OpponentDisconnected");
            roomManager.RemoveRoom(room.RoomCode);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
