namespace TicTacToe.Tests.Domain;

using System;
using System.Linq;
using TicTacToe.Domain.Aggregates;
using TicTacToe.Domain.Events;
using TicTacToe.Domain.Exceptions;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public class GameEventEmissionTests
{
    [Fact]
    public void MakeMove_WhenPlayerXWinsRow_RaisesSingleGameCompletedEvent()
    {
        // Arrange: X wins on row 0 (cells 0, 1, 2)
        var game = Game.Create(GameMode.TwoPlayer);

        // Act
        game.MakeMove(Player.X, new CellIndex(0)); // X
        game.MakeMove(Player.O, new CellIndex(3)); // O
        game.MakeMove(Player.X, new CellIndex(1)); // X
        game.MakeMove(Player.O, new CellIndex(4)); // O
        game.MakeMove(Player.X, new CellIndex(2)); // X wins!

        // Assert
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Single(game.DomainEvents);

        var domainEvent = game.DomainEvents.First() as GameCompletedEvent;
        Assert.NotNull(domainEvent);
        Assert.Equal(game.Id, domainEvent.GameId);
        Assert.Equal(GameStatus.Won, domainEvent.Result);
        Assert.Equal(Player.X, domainEvent.Winner);
        Assert.Equal(new[] { 0, 1, 2 }, domainEvent.WinningCells);
        Assert.Equal(5, domainEvent.MoveCount);
        Assert.NotEqual(Guid.Empty, domainEvent.EventId);
        Assert.True(domainEvent.OccurredOn <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void MakeMove_WhenPlayerOWinsCol_RaisesSingleGameCompletedEvent()
    {
        // Arrange: O wins on col 1 (cells 1, 4, 7)
        var game = Game.Create(GameMode.TwoPlayer);

        // Act
        game.MakeMove(Player.X, new CellIndex(0)); // X
        game.MakeMove(Player.O, new CellIndex(1)); // O
        game.MakeMove(Player.X, new CellIndex(2)); // X
        game.MakeMove(Player.O, new CellIndex(4)); // O
        game.MakeMove(Player.X, new CellIndex(3)); // X
        game.MakeMove(Player.O, new CellIndex(7)); // O wins!

        // Assert
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Single(game.DomainEvents);

        var domainEvent = game.DomainEvents.First() as GameCompletedEvent;
        Assert.NotNull(domainEvent);
        Assert.Equal(game.Id, domainEvent.GameId);
        Assert.Equal(GameStatus.Won, domainEvent.Result);
        Assert.Equal(Player.O, domainEvent.Winner);
        Assert.Equal(new[] { 1, 4, 7 }, domainEvent.WinningCells);
        Assert.Equal(6, domainEvent.MoveCount);
    }

    [Fact]
    public void MakeMove_WhenDraw_RaisesSingleGameCompletedEventWithNullWinner()
    {
        // Arrange: classic draw sequence
        // Board layout:
        // X O X
        // X X O
        // O X O
        var game = Game.Create(GameMode.TwoPlayer);

        // Act
        game.MakeMove(Player.X, new CellIndex(0)); // X (0)
        game.MakeMove(Player.O, new CellIndex(1)); // O (1)
        game.MakeMove(Player.X, new CellIndex(2)); // X (2)
        game.MakeMove(Player.O, new CellIndex(5)); // O (5)
        game.MakeMove(Player.X, new CellIndex(3)); // X (3)
        game.MakeMove(Player.O, new CellIndex(6)); // O (6)
        game.MakeMove(Player.X, new CellIndex(4)); // X (4)
        game.MakeMove(Player.O, new CellIndex(8)); // O (8)
        game.MakeMove(Player.X, new CellIndex(7)); // X (7) - Draw!

        // Assert
        Assert.Equal(GameStatus.Draw, game.Status);
        Assert.Single(game.DomainEvents);

        var domainEvent = game.DomainEvents.First() as GameCompletedEvent;
        Assert.NotNull(domainEvent);
        Assert.Equal(game.Id, domainEvent.GameId);
        Assert.Equal(GameStatus.Draw, domainEvent.Result);
        Assert.Null(domainEvent.Winner);
        Assert.Empty(domainEvent.WinningCells);
        Assert.Equal(9, domainEvent.MoveCount);
    }

    [Fact]
    public void MakeMove_WhenNonTerminal_RaisesZeroDomainEvents()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);

        // Act
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(1));

        // Assert
        Assert.Equal(GameStatus.InProgress, game.Status);
        Assert.Empty(game.DomainEvents);
    }

    [Fact]
    public void MakeMove_WhenMoveRejected_RaisesZeroDomainEvents()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));

        // Act & Assert
        Assert.Throws<CellOccupiedException>(() => game.MakeMove(Player.O, new CellIndex(0)));
        Assert.Throws<InvalidTurnException>(() => game.MakeMove(Player.X, new CellIndex(1)));
        Assert.Empty(game.DomainEvents);
    }

    [Fact]
    public void MakeMove_AfterGameCompleted_ThrowsExceptionAndRaisesZeroNewEvents()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // X wins!

        Assert.Single(game.DomainEvents);

        // Act & Assert
        Assert.Throws<GameAlreadyCompletedException>(() => game.MakeMove(Player.O, new CellIndex(5)));
        Assert.Single(game.DomainEvents); // still only 1
    }

    [Fact]
    public void ClearDomainEvents_RemovesAllQueuedEvents()
    {
        // Arrange
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0));
        game.MakeMove(Player.O, new CellIndex(3));
        game.MakeMove(Player.X, new CellIndex(1));
        game.MakeMove(Player.O, new CellIndex(4));
        game.MakeMove(Player.X, new CellIndex(2)); // Win

        Assert.Single(game.DomainEvents);

        // Act
        game.ClearDomainEvents();

        // Assert
        Assert.Empty(game.DomainEvents);
    }

    [Fact]
    public void MakeMove_FinalMoveCompletesWinningLine_TakesPrecedenceOverDraw_RaisesWonEvent()
    {
        // Arrange: 9th move completes winning line and fills board simultaneously
        // Board layout:
        // X X O
        // O X X
        // X O O - wait, let's construct an exact line where move 9 wins for X
        // Move sequence:
        // X: 0, O: 2
        // X: 4, O: 1
        // X: 3, O: 5
        // X: 7, O: 6
        // X: 8 (completes diagonal 0, 4, 8 and fills board!)
        var game = Game.Create(GameMode.TwoPlayer);
        game.MakeMove(Player.X, new CellIndex(0)); // X (0)
        game.MakeMove(Player.O, new CellIndex(2)); // O (2)
        game.MakeMove(Player.X, new CellIndex(4)); // X (4)
        game.MakeMove(Player.O, new CellIndex(1)); // O (1)
        game.MakeMove(Player.X, new CellIndex(3)); // X (3)
        game.MakeMove(Player.O, new CellIndex(5)); // O (5)
        game.MakeMove(Player.X, new CellIndex(7)); // X (7)
        game.MakeMove(Player.O, new CellIndex(6)); // O (6)
        // 8 moves made, board has 1 cell left: 8
        // Move 9: Player X plays 8. Completes diagonal 0, 4, 8!
        game.MakeMove(Player.X, new CellIndex(8));

        // Assert
        Assert.Equal(GameStatus.Won, game.Status);
        Assert.Equal(Player.X, game.Winner);
        Assert.Single(game.DomainEvents);

        var domainEvent = Assert.IsType<GameCompletedEvent>(game.DomainEvents.First());
        Assert.Equal(GameStatus.Won, domainEvent.Result);
        Assert.Equal(Player.X, domainEvent.Winner);
        Assert.Equal(new[] { 0, 4, 8 }, domainEvent.WinningCells);
        Assert.Equal(9, domainEvent.MoveCount);
    }
}
