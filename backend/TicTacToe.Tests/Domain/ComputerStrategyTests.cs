namespace TicTacToe.Tests.Domain;

using TicTacToe.Domain.Entities;
using TicTacToe.Domain.Services;
using TicTacToe.Domain.ValueObjects;
using Xunit;

public sealed class ComputerStrategyTests
{
    private readonly BasicComputerMoveStrategy _strategy = new();

    #region Priority 1: O Winning Move

    [Fact]
    public void SelectMove_WhenImmediateWinAvailableForO_SelectsWinningCell()
    {
        // O | O | . (0, 1 occupied by O; 2 is winning cell)
        // X | X | .
        // . | . | .
        var board = new Board(new Player?[]
        {
            Player.O, Player.O, null,
            Player.X, Player.X, null,
            null, null, null
        });

        var move = _strategy.SelectMove(board);

        Assert.Equal(2, move.Value);
    }

    [Fact]
    public void SelectMove_WhenImmediateColumnWinAvailableForO_SelectsWinningCell()
    {
        // O | X | .
        // O | X | .
        // . | . | . (cell 6 wins col 0 for O)
        var board = new Board(new Player?[]
        {
            Player.O, Player.X, null,
            Player.O, Player.X, null,
            null, null, null
        });

        var move = _strategy.SelectMove(board);

        Assert.Equal(6, move.Value);
    }

    [Fact]
    public void SelectMove_WhenImmediateDiagonalWinAvailableForO_SelectsWinningCell()
    {
        // O | X | .
        // X | O | .
        // . | . | . (cell 8 wins main diagonal for O)
        var board = new Board(new Player?[]
        {
            Player.O, Player.X, null,
            Player.X, Player.O, null,
            null, null, null
        });

        var move = _strategy.SelectMove(board);

        Assert.Equal(8, move.Value);
    }

    [Fact]
    public void SelectMove_WhenMultipleWinsAvailableForO_SelectsLowestIndexDeterministically()
    {
        // Row 0: O, O, null -> cell 2 wins
        // Col 0: O, null, O -> cell 3 wins
        // Multiple wins available: cells 2 and 3. Should pick cell 2 (lowest index).
        // Let's set up:
        // O | O | . (cell 2 wins row 0)
        // . | X | X (cell 3 wins col 0 if cell 6 is O)
        // O | . | .
        var board = new Board(new Player?[]
        {
            Player.O, Player.O, null,
            null,     Player.X, Player.X,
            Player.O, null,     null
        });

        var move = _strategy.SelectMove(board);

        Assert.Equal(2, move.Value);
    }

    [Fact]
    public void SelectMove_WhenBothWinForOAndBlockForXAvailable_PrioritizesWinOverBlock()
    {
        // O | O | . (cell 2 wins for O)
        // X | X | . (cell 5 wins for X, needs blocking)
        // . | . | .
        var board = new Board(new Player?[]
        {
            Player.O, Player.O, null,
            Player.X, Player.X, null,
            null, null, null
        });

        var move = _strategy.SelectMove(board);

        // Must win (cell 2), not block (cell 5)
        Assert.Equal(2, move.Value);
    }

    #endregion

    #region Priority 2: Block Winning Move for X

    [Fact]
    public void SelectMove_WhenHumanThreatensRowWin_BlocksThreat()
    {
        // X | X | . (cell 2 is threat)
        // O | . | .
        // . | . | .
        var board = new Board(new Player?[]
        {
            Player.X, Player.X, null,
            Player.O, null,     null,
            null,     null,     null
        });

        var move = _strategy.SelectMove(board);

        Assert.Equal(2, move.Value);
    }

    [Fact]
    public void SelectMove_WhenHumanThreatensColWin_BlocksThreat()
    {
        // X | O | .
        // X | . | .
        // . | . | . (cell 6 is threat)
        var board = new Board(new Player?[]
        {
            Player.X, Player.O, null,
            Player.X, null,     null,
            null,     null,     null
        });

        var move = _strategy.SelectMove(board);

        Assert.Equal(6, move.Value);
    }

    [Fact]
    public void SelectMove_WhenHumanThreatensDiagonalWin_BlocksThreat()
    {
        // X | . | .
        // O | X | .
        // . | . | . (cell 8 is threat)
        var board = new Board(new Player?[]
        {
            Player.X, null,     null,
            Player.O, Player.X, null,
            null,     null,     null
        });

        var move = _strategy.SelectMove(board);

        Assert.Equal(8, move.Value);
    }

    [Fact]
    public void SelectMove_WhenMultipleThreatsExist_BlocksDeterministicallyByLowestIndex()
    {
        // Fork position: X threatens row 0 (cell 2) and col 0 (cell 6)
        // X | X | . (threat at 2)
        // X | O | .
        // . | . | . (threat at 6)
        var board = new Board(new Player?[]
        {
            Player.X, Player.X, null,
            Player.X, Player.O, null,
            null,     null,     null
        });

        var move = _strategy.SelectMove(board);

        // Lowest index between 2 and 6 is 2
        Assert.Equal(2, move.Value);
    }

    #endregion

    #region Priority 3: Center Cell (4)

    [Fact]
    public void SelectMove_WhenNoWinOrBlockAndCenterAvailable_SelectsCenterCell4()
    {
        // X | . | .
        // . | . | . (center 4 is empty)
        // . | . | .
        var board = new Board(new Player?[]
        {
            Player.X, null, null,
            null,     null, null,
            null,     null, null
        });

        var move = _strategy.SelectMove(board);

        Assert.Equal(4, move.Value);
    }

    #endregion

    #region Priority 4: Corners (0, 2, 6, 8)

    [Fact]
    public void SelectMove_WhenCenterOccupied_SelectsFirstAvailableCornerInOrder0_2_6_8()
    {
        // . | . | .
        // . | X | . (center 4 occupied)
        // . | . | .
        var board = new Board(new Player?[]
        {
            null, null,     null,
            null, Player.X, null,
            null, null,     null
        });

        var move = _strategy.SelectMove(board);

        // First corner in [0, 2, 6, 8] is 0
        Assert.Equal(0, move.Value);
    }

    [Fact]
    public void SelectMove_WhenCenterAndCorner0Occupied_SelectsCorner2()
    {
        // O | . | . (corner 0 occupied)
        // . | X | . (center 4 occupied)
        // . | . | .
        var board = new Board(new Player?[]
        {
            Player.O, null,     null,
            null,     Player.X, null,
            null,     null,     null
        });

        var move = _strategy.SelectMove(board);

        // Next corner after 0 is 2
        Assert.Equal(2, move.Value);
    }

    [Fact]
    public void SelectMove_WhenCenterAndCorners0And2Occupied_SelectsCorner6()
    {
        // O | . | X (corners 0 and 2 occupied)
        // . | X | . (center 4 occupied)
        // . | . | .
        // No winning line or block:
        // O at 0, X at 2, X at 4. (X at 2 and 4 threatens cell 6!)
        // To avoid block triggering, place X at 1 and 4 (threatens 7, not corner):
        // O | X | .
        // . | X | . (threatens 7!)
        // But we want to test corner 6 without block.
        // Let's use:
        // O | . | O (threatens 1! That's a win for O)
        // Let's ensure NO win and NO block:
        // O | . | X (0 is O, 2 is X)
        // . | O | . (center 4 is O -> threatens 8! That's win)
        // Let center be X:
        // O | . | X
        // . | X | . (2 and 4 threatens 6!)
        // What if X is at 4 and 8? (threatens 0, which is occupied by O, so no threat!)
        // O | . | .
        // . | X | .
        // . | . | X
        // Center 4 and corner 8 occupied by X. Corner 0 occupied by O.
        // Corner 0 is occupied. Next corner is 2! Let's occupy 2 with O:
        // O | . | O (threatens 1 - O win).
        // Let's occupy 2 with null, corner 0 with O, corner 2 with X, corner 8 with X:
        // O | . | X
        // . | . | . (center is free? No, center must be occupied).
        // If center is O:
        // O at 0, X at 1, O at 4, X at 8:
        // O | X | .
        // . | O | . (0 and 4 threatens 8, which is occupied by X! No win.)
        // . | . | X (X has 1 and 8: no line.)
        // Free corners are 2 and 6. Let's make corner 2 occupied:
        // O | X | X (corner 0=O, edge 1=X, corner 2=X)
        // . | O | . (center 4=O)
        // . | . | X (corner 8=X)
        // Lines:
        // O: (0, 4) -> 8 (occupied by X)
        // X: (1, 2) -> 0 (occupied by O)
        // X: (2, 8) -> 5 (threatens 5! That's an edge, cell 5.)
        // If X threatens 5, strategy will block 5!
        // To prevent X from threatening 5, put O at 5:
        // O | X | X
        // . | O | O (O at 4, 5 -> threatens 3! O win!)
        // Instead, simply set up a board where no line has 2 of the same mark:
        // Let's verify systematically:
        // O at 0, X at 2, X at 4:
        // Wait, does (2, 4) threaten 6? Yes (diagonal 2, 4, 6).
        // If corner 6 is threatened by X, strategy selects 6 by BLOCK rule (Priority 2).
        // What if corners 0, 2 are occupied and center 4 is occupied, with NO threats?
        // O at 0, O at 8 (threatens 4, but 4 is occupied by X).
        // X at 2, X at 7 (threatens nothing).
        // Board:
        // O | . | X
        // . | X | .
        // . | X | O
        // Wait, X at 2, 4, 7:
        // X at (2, 4) threatens 6.
        // What if X at 4, X at 3 (threatens 5, an edge)?
        // What if O is at 5 (blocks threat)?
        // Let's check:
        // [0]=O, [1]=null, [2]=X
        // [3]=X, [4]=O,    [5]=X
        // [6]=null, [7]=null, [8]=null
        // Check lines:
        // Row 0: O, null, X (no pair)
        // Row 1: X, O, X (no pair for either)
        // Col 0: O, X, null (no pair)
        // Col 1: null, O, null (no pair)
        // Col 2: X, X, null -> threatens cell 8!
        // So put O at 8:
        // [0]=O, [1]=null, [2]=X
        // [3]=X, [4]=O,    [5]=X
        // [6]=null, [7]=null, [8]=O
        // Check lines:
        // Row 0: O, null, X (no pair)
        // Row 1: X, O, X (no pair)
        // Row 2: null, null, O (no pair)
        // Col 0: O, X, null (no pair)
        // Col 1: null, O, null (no pair)
        // Col 2: X, X, O (blocked)
        // Diag 0,4,8: O, O, O -> already won!
        // So put X at 8 instead:
        // [0]=O, [1]=null, [2]=X
        // [3]=null, [4]=X, [5]=null
        // [6]=null, [7]=null, [8]=null
        // If [0]=O, [2]=X, [4]=X:
        // Diag 2,4,6 threatens 6. If it threatens 6, it blocks 6.
        // What if [2]=O, [0]=X, [4]=X?
        // Row 0: X, null, O
        // Diag 0,4,8: X at 0, X at 4 -> threatens 8!
        // Then O would block 8.
        // What if [4]=O (center occupied by O)?
        // [0]=O, [2]=X, [4]=O -> Diag (0,4,8) threatens 8 (O win).
        // Put X at 8:
        // [0]=O, [2]=X, [4]=O, [8]=X
        // Diag 0,4,8 is O, O, X (blocked!).
        // Diag 2,4,6 is X, O, null (no pair!).
        // Row 0: O, null, X (no pair).
        // Col 0: O, null, null (no pair).
        // Col 2: X, null, X -> Col 2 has X at 2 and 8! Threatens 5!
        // Block 5 would happen.
        // Put O at 5 to block it!
        // [0]=O, [1]=null, [2]=X
        // [3]=null, [4]=O, [5]=O -> Row 1 has O at 4 and 5! Threatens 3 (O win).
        // To prevent Row 1 threat, put X at 3:
        // [0]=O, [1]=null, [2]=X
        // [3]=X, [4]=O, [5]=O -> Row 1 has X, O, O. Threatens nothing because cell 3 is X!
        // Wait, if [3]=X, [4]=O, [5]=O, row 1 has 3 cells: 3, 4, 5. All 3 are occupied!
        // Let's verify all lines for:
        // 0: O, 1: null, 2: X
        // 3: X, 4: O,    5: O
        // 6: null, 7: null, 8: X
        // Row 0: O, null, X (no pair)
        // Row 1: X, O, O (occupied)
        // Row 2: null, null, X (no pair)
        // Col 0: O, X, null (no pair)
        // Col 1: null, O, null (no pair)
        // Col 2: X, O, X (occupied)
        // Diag 0,4,8: O, O, X (occupied)
        // Diag 2,4,6: X, O, null (no pair)
        // ANY WIN FOR O?
        // O is at 0, 4, 5. Lines: (0,1,2: O,null,X), (3,4,5: X,O,O), (0,3,6: O,X,null), (1,4,7: null,O,null), (2,4,6: X,O,null), (0,4,8: O,O,X).
        // No line has two O's and an empty cell!
        // ANY THREAT FOR X?
        // X is at 2, 3, 8. Lines: (0,1,2: O,null,X), (6,7,8: null,null,X), (0,3,6: O,X,null), (2,5,8: X,O,X), (2,4,6: X,O,null).
        // No line has two X's and an empty cell!
        // Now check empty cells:
        // 1 (edge), 6 (corner), 7 (edge).
        // Corners available: ONLY corner 6 (since 0=O, 2=X, 8=X are occupied)!
        // Center 4 is occupied (by O).
        // Neither win nor block is available.
        // Therefore, corner priority must select corner 6!
        var board = new Board(new Player?[]
        {
            Player.O, null,     Player.X,
            Player.X, Player.O, Player.O,
            null,     null,     Player.X
        });

        var move = _strategy.SelectMove(board);

        Assert.Equal(6, move.Value);
    }

    [Fact]
    public void SelectMove_WhenCenterAndCorners0_2_6Occupied_SelectsCorner8()
    {
        // Corners 0, 2, 6 occupied. Center 4 occupied. Corner 8 available.
        // Ensure no win and no block:
        // 0: O, 1: X, 2: O (Row 0 occupied)
        // 3: O, 4: X, 5: X (Row 1 occupied)
        // 6: X, 7: null, 8: null
        // Check lines:
        // Row 0: O, X, O (full)
        // Row 1: O, X, X (full)
        // Row 2: X, null, null (1 mark)
        // Col 0: O, O, X (full)
        // Col 1: X, X, null -> threatens 7 for X!
        // To prevent Col 1 threat, make 1: O and 4: O? No, that's O win.
        // How about:
        // 0: X, 1: O, 2: X
        // 3: X, 4: O, 5: null
        // 6: O, 7: null, 8: null
        // Lines:
        // Row 0: X, O, X (full)
        // Col 0: X, X, O (full)
        // Col 1: O, O, null -> threatens 7 for O (Priority 1 Win!).
        // Put X at 7:
        // Row 0: X, O, X
        // Row 1: X, O, null
        // Row 2: O, X, null
        // Check O at (1, 4, 6):
        // Col 1: O, O, X (full)
        // Diag 2,4,6: X, O, O (full)
        // Check X at (0, 2, 3, 7):
        // Col 0: X, X, O (full)
        // Row 0: X, O, X (full)
        // Row 1: X, O, null (1 mark)
        // Row 2: O, X, null (1 mark)
        // Col 2: X, null, null (1 mark)
        // Diag 0,4,8: X, O, null (1 mark each)
        // Check remaining cells:
        // 5 (edge), 8 (corner).
        // Free cells: 5, 8.
        // No wins for O, no blocks for X.
        // Center 4 is occupied.
        // Corners: 0, 2, 6 are occupied. Corner 8 is empty!
        // Strategy must select corner 8!
        var board = new Board(new Player?[]
        {
            Player.X, Player.O, Player.X,
            Player.X, Player.O, null,
            Player.O, Player.X, null
        });

        var move = _strategy.SelectMove(board);

        Assert.Equal(8, move.Value);
    }

    #endregion

    #region Priority 5: Fallback to Any Available Cell in 0..8

    [Fact]
    public void SelectMove_WhenCenterAndAllCornersOccupied_SelectsFirstAvailableEdgeInNaturalOrder()
    {
        // Center 4 occupied. Corners 0, 2, 6, 8 all occupied.
        // Remaining cells are edges: 1, 3, 5, 7.
        // Ensure no win and no block:
        // 0: X, 1: null, 2: O
        // 3: null, 4: O, 5: null
        // 6: O, 7: null, 8: X
        // Check O at (2, 4, 6): Diag 2,4,6 is O, O, O (already won!).
        // Let's swap:
        // 0: X, 1: null, 2: X -> threatens 1!
        // To avoid threat, alternate:
        // 0: X, 2: O, 6: X, 8: O
        // Diag 0,4,8: X, center, O -> blocked!
        // Diag 2,4,6: O, center, X -> blocked!
        // Center 4: X.
        // Rows:
        // 0: X, null, O (no pair)
        // 1: null, X, null (no pair)
        // 2: X, null, O (no pair)
        // Cols:
        // 0: X, null, X -> Col 0 has X at 0 and 6! Threatens cell 3!
        // To prevent Col 0 threat, make 0: O, 6: X, 2: X, 8: O:
        // Col 0: O, null, X (no pair)
        // Col 2: X, null, O (no pair)
        // Row 0: O, null, X (no pair)
        // Row 2: X, null, O (no pair)
        // Diag 0,4,8: O, 4, O -> threatens 4, but 4 is occupied!
        // If center 4 is X: Diag 0,4,8 is O, X, O (blocked!).
        // Diag 2,4,6: X, X, X -> that's 3 X's!
        // So center 4 cannot be X if 2 and 6 are X.
        // What if:
        // 0: O, 2: O (threatens 1).
        // What if corner 0: X, 2: O, 6: O, 8: X?
        // Row 0: X, null, O
        // Row 2: O, null, X
        // Col 0: X, null, O
        // Col 2: O, null, X
        // Diag 0,4,8: X, center, X -> center is 4, which is occupied!
        // If center 4 is O:
        // Diag 0,4,8 is X, O, X (blocked!).
        // Diag 2,4,6 is O, O, O (O win!).
        // What if corner marks are:
        // 0: X, 2: O, 6: O, 8: null (then corner 8 is free, not edges).
        // What if we occupy edge 1 with X?
        // 0: X, 1: X -> threatens 2, which is occupied by O!
        // Let's check:
        // 0: X, 1: X, 2: O
        // 3: null, 4: O, 5: null
        // 6: O, 7: null, 8: X
        // Check X at (0, 1, 8):
        // Row 0: X, X, O (full)
        // Col 0: X, null, O (no pair)
        // Col 1: X, O, null (no pair)
        // Col 2: O, null, X (no pair)
        // Diag 0,4,8: X, O, X (full)
        // Diag 2,4,6: O, O, O -> 2,4,6 are all O!
        // Change 6 to X:
        // 0: X, 1: X, 2: O
        // 3: null, 4: O, 5: null
        // 6: X, 7: null, 8: X
        // Row 2: X, null, X -> threatens 7 for X!
        // If we put O at 7:
        // 0: X, 1: X, 2: O
        // 3: null, 4: O, 5: null
        // 6: X, 7: O, 8: X
        // Check lines:
        // Row 0: X, X, O (full)
        // Row 1: null, O, null (1 mark)
        // Row 2: X, O, X (full)
        // Col 0: X, null, X -> Col 0 has X at 0 and 6! Threatens 3 for X!
        // If X threatens 3, strategy will block 3.
        // But cell 3 is an edge! If strategy blocks 3, it's selecting Priority 2 (block), not Priority 5!
        // We want Priority 5 (fallback edge selection).
        // To have NO threats and NO wins with corners 0, 2, 6, 8 and center 4 all occupied:
        // Notice on a 3x3 board, there are 8 winning lines.
        // Can 5 marks (4 corners + center) have no line of 2?
        // If 0: X, 2: O, 4: X, 6: O, 8: X?
        // X has 0, 4, 8 (that's a diagonal win!).
        // What if 0: O, 2: X, 4: O, 6: X, 8: O?
        // O has 0, 4, 8 (that's a diagonal win!).
        // What if 0: X, 2: O, 4: O, 6: X, 8: X?
        // Diag 0,4,8: X, O, X (no win).
        // Diag 2,4,6: O, O, X (no win).
        // Row 0: X, 1, O (no pair).
        // Row 2: X, 7, X -> threatens 7!
        // What if cell 7 is occupied by O?
        // Row 2: X, O, X (full).
        // Col 0: X, 3, X -> threatens 3!
        // What if cell 3 is occupied by O?
        // Col 0: X, O, X (full).
        // Col 2: O, 5, X (no pair).
        // Row 1: O, O, 5 -> Row 1 has O at 3 and 4! Threatens 5!
        // What if 5 is occupied by X?
        // Row 1: O, O, X (full).
        // Then remaining unoccupied cell is ONLY 1!
        // Let's verify:
        // 0: X, 1: null, 2: O
        // 3: O, 4: O,    5: X
        // 6: X, 7: O,    8: X
        // Check lines:
        // Row 0: X, null, O (1 mark each, no pair)
        // Row 1: O, O, X (full)
        // Row 2: X, O, X (full)
        // Col 0: X, O, X (full)
        // Col 1: null, O, O -> Col 1 has O at 4 and 7! Threatens 1 for O!
        // That's an O win at cell 1 (Priority 1).
        // Can we make Col 1 NOT an O win?
        // If 7 is X and 8 is O?
        // Let's test with a board where:
        // 0: X, 2: O, 4: O, 6: X, 8: X (all corners + center occupied).
        // If 3 is O, 5 is X, 7 is X, 1 is null:
        // 0: X, 1: null, 2: O
        // 3: O, 4: O,    5: X
        // 6: X, 7: X,    8: O
        // Let's check:
        // Row 0: X, null, O (no pair)
        // Row 1: O, O, X (full)
        // Row 2: X, X, O (full)
        // Col 0: X, O, X (full)
        // Col 1: null, O, X (no pair)
        // Col 2: O, X, O (full)
        // Diag 0,4,8: X, O, O -> Diag 0,4,8 has O at 4 and 8! But cell 0 is X! (full)
        // Diag 2,4,6: O, O, X -> Diag 2,4,6 has O at 2 and 4! But cell 6 is X! (full)
        // Is there ANY line of 2 matching marks with an empty cell?
        // Only empty cell is 1!
        // Lines passing through 1:
        // Row 0: X at 0, O at 2 -> No pair!
        // Col 1: O at 4, X at 7 -> No pair!
        // NO WIN FOR O!
        // NO BLOCK FOR X!
        // Center 4 is occupied!
        // Corners 0, 2, 6, 8 are all occupied!
        // Edge 1 is empty.
        // Therefore, Priority 5 (any available in 0..8) must select cell 1!
        var board = new Board(new Player?[]
        {
            Player.X, null,     Player.O,
            Player.O, Player.O, Player.X,
            Player.X, Player.X, Player.O
        });

        var move = _strategy.SelectMove(board);

        Assert.Equal(1, move.Value);
    }

    #endregion

    #region Legality, Invariants, Immutability & Determinism

    [Fact]
    public void SelectMove_NeverReturnsOccupiedCell()
    {
        // 0, 1, 4 occupied
        var board = new Board(new Player?[]
        {
            Player.X, Player.O, null,
            null,     Player.X, null,
            null,     null,     null
        });

        var move = _strategy.SelectMove(board);

        Assert.False(board.IsOccupied(move));
    }

    [Fact]
    public void SelectMove_ReturnsValidCellIndex_InRange0To8()
    {
        var board = new Board();

        var move = _strategy.SelectMove(board);

        Assert.InRange(move.Value, 0, 8);
    }

    [Fact]
    public void SelectMove_DoesNotMutateSuppliedBoard()
    {
        var initialCells = new Player?[]
        {
            Player.X, null,     Player.O,
            null,     Player.X, null,
            null,     null,     null
        };
        var board = new Board(initialCells);
        var initialOccupiedCount = board.OccupiedCount;

        var move = _strategy.SelectMove(board);

        Assert.Equal(initialOccupiedCount, board.OccupiedCount);
        for (int i = 0; i < Board.CellCount; i++)
        {
            Assert.Equal(initialCells[i], board.Cells[i]);
        }
    }

    [Fact]
    public void SelectMove_WhenBoardFull_ThrowsInvalidOperationException()
    {
        var fullBoard = new Board(new Player?[]
        {
            Player.X, Player.O, Player.X,
            Player.X, Player.O, Player.O,
            Player.O, Player.X, Player.X
        });

        Assert.Throws<InvalidOperationException>(() => _strategy.SelectMove(fullBoard));
    }

    [Fact]
    public void SelectMove_WhenNullBoard_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => _strategy.SelectMove(null!));
    }

    [Fact]
    public void SelectMove_Across100RepetitionsOnSameBoard_ProducesIdenticalCell()
    {
        var board = new Board(new Player?[]
        {
            Player.X, null,     null,
            null,     Player.O, null,
            null,     null,     null
        });

        var firstMove = _strategy.SelectMove(board);

        for (int i = 0; i < 100; i++)
        {
            var repeatedMove = _strategy.SelectMove(board);
            Assert.Equal(firstMove.Value, repeatedMove.Value);
        }
    }

    #endregion
}
