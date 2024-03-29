using UnityEngine;
using System.Collections;
using System.Diagnostics;
using System.Collections.Generic;


public class MatchManager : MonoBehaviour
{
    [SerializeField] public  MoveGenerator mg;
    [SerializeField] private Timer tmr;
    [SerializeField] private BoardHandler bh;
    [SerializeField] private OpeningBook ob;

    [SerializeField] private GameObject EndScreen;

    [HideInInspector] public ChessBoard BoardPosition;
    [HideInInspector] public  MatchData Data;
    [HideInInspector] public  IPlayer[] Players;

    [HideInInspector] public int     Side2Move;
    [HideInInspector] public int      EndState;
    [HideInInspector] public int EndPrediction;

    private float AdjournWinMargin = 5.0f;

    private string startFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private int    gameNo = 1;


    private void
    Start()
    {
        TT.Init();

        BoardPosition = new ChessBoard(startFen);
        bh.InitializeBoard(ref BoardPosition);
        Players = new IPlayer[2];
    }


    #region MATCH_UTILS


    private IEnumerator
    PlayOpening(string opening)
    {
        if (ob.IsFen(opening))
        {
            Data = new MatchData(opening);
            BoardPosition = new ChessBoard(opening);

            bh.BoardReset();
            bh.Recreate(ref BoardPosition);

            Side2Move = BoardPosition.color ^ 1;

            yield return new WaitForSeconds(0.3f);
        }
        else
        {
            Data = new MatchData(startFen);
            List<int> opening_line = ob.ExtractLine(opening);
            float time_left = tmr.AllotedTimePerSide;

            // Play all moves of opening_line
            foreach (int move in opening_line)
            {
                ulong prevHash = BoardPosition.hashvalue;
                BoardPosition.MakeMove(move);
                Data.Add(move, 0, time_left, prevHash);

                bh.Recreate(ref BoardPosition);
                yield return new WaitForSeconds(0.1f);

                Side2Move ^= 1;
            }
        }        
    }


    public int
    IsGameOver()
    {
        // Refer to GameOverScreen() for various-states.

        MoveList moveslist = mg.GenerateMoves(ref BoardPosition);

        // checkmate/stalemate check
        if (moveslist.moveCount == 0)
            return (moveslist.KingAttackers > 0) ? 1 + (Side2Move ^ 1) : 3;

        // Insufficient material check
        if (InsufficientMaterial(ref BoardPosition)) return 4;

        // 3-fold repetition and 50-move-rule
        if (Data.ThreeMoveRepetitionDraw()) return 5;
        if (Data.FiftyMoveRuleDraw()) return 6;

        // Check if lost on time
        if (tmr.ChessClocks[Side2Move] < 0f)
            return 7 + (Side2Move ^ 1);

        return -1;
    }


    public bool
    TimeLeftForSearch()
    {
        return tmr.ChessClocks[Side2Move] > 0f;
    }


    public bool
    InsufficientMaterial(ref ChessBoard pos)
    {
        int w = 8, b = 0;

        int wPawns   = pos.PopCount(pos.Pawn(w))  , bPawns   = pos.PopCount(pos.Pawn(b));
        int wBishops = pos.PopCount(pos.Bishop(w)), bBishops = pos.PopCount(pos.Bishop(b));
        int wKnights = pos.PopCount(pos.Knight(w)), bKnights = pos.PopCount(pos.Knight(b));
        int wRooks   = pos.PopCount(pos.Rook(w))  , bRooks   = pos.PopCount(pos.Rook(b));
        int wQueens  = pos.PopCount(pos.Queen(w)) , bQueens  = pos.PopCount(pos.Queen(b));

        int wPieces = wBishops + wKnights + wRooks + wQueens;
        int bPieces = bBishops + bKnights + bRooks + bQueens;

        if (wPawns + wPieces + bPawns + bPieces == 0) return true;
        if (wPawns > 0 || bPawns > 0) return false;

        if (wPieces == 1 && bPieces == 0)
            if (wBishops == 1 || wKnights == 1) return true;
        if (wPieces == 0 && bPieces == 1)
            if (bBishops == 1 || bKnights == 1) return true;

        if (wPieces == 1 && bPieces == 1)
            if ((wBishops == 1 || wKnights == 1) && (bBishops == 1 || bKnights == 1)) return true;

        if (wPieces + bPieces == 2)
            if (wKnights == 2 || bKnights == 2) return true;

        return false;
    }


    private void
    OnApplicationQuit()
    {
        if (Players[0] != null) Players[0].Stop();
        if (Players[1] != null) Players[1].Stop();

        Application.Quit();
    }

    #endregion


    public IEnumerator
    StartNewGame(string playerWhite, string playerBlack, string opening,
                 bool fixedTimePerMove, bool allowOpeningBook)
    {
        // Reset game data and board position
        Side2Move = 0;
        BoardPosition = new ChessBoard(startFen);

        // Reset Match Parameters
        EndState = -1;
        EndPrediction = 0;

        EndScreen.SetActive(false);

        // Play the opening moves, if any
        if (opening.Length > 0)
            yield return StartCoroutine(PlayOpening(opening));

        string fen = BoardPosition.Fen();

        // Create players
        Players[0] = (playerWhite == "human")
                      ? new HumanPlayer()
                      : new ChessEngine(playerWhite, fen, gameNo, fixedTimePerMove, allowOpeningBook);

        Players[1] = (playerBlack == "human")
                      ? new HumanPlayer()
                      : new ChessEngine(playerBlack, fen, gameNo, fixedTimePerMove, allowOpeningBook);

        yield return new WaitForSeconds(1);
        gameNo++;

        // Initialize timer and start the game
        tmr.Init(Side2Move);
        yield return StartCoroutine( PlayGame() );
    }


    private IEnumerator
    PlayGame()
    {
        while ((EndState = IsGameOver()) == -1)
        {
            // Let player make his move
            yield return StartCoroutine( RequestMove() ) ;

            // Time runs out before player making a move
            if (TimeLeftForSearch() == false)
                break;

            // Retrieve player move
            var (move, eval) = Players[Side2Move].GetResults();

            // If no prediction made so far
            if (EndPrediction == 0)
                EndPrediction = PredictionCall();

            // Update board elements after making move
            yield return StartCoroutine( UpdateBoardElements(move, eval) );

            // Switch sides and next turn
            Side2Move ^= 1;
        }

        tmr.ClockFreeze();
        GameOverScreen(EndState);

        if (Players[0] != null) Players[0].Stop();
        if (Players[1] != null) Players[1].Stop();
    }


    public IEnumerator
    UpdateBoardElements(int move, float eval)
    {
        tmr.SwitchPlayer();
        tmr.ClockFreeze();

        BoardPosition.MakeMove(move);
        Data.Add(move, eval, tmr.ChessClocks[Side2Move ^ 1],
            BoardPosition.GenerateHashKey() );

        // Board Update
        bh.BoardReset(false);
        bh.MarkPlayedMove(move);
        bh.Recreate(ref BoardPosition);

        // Give a slight delay after each board update
        yield return new WaitForSeconds(0.1f);

        tmr.ClockUnfreeze();
    }


    public IEnumerator
    RequestMove()
    {
        bool moveFound = false;
        bool timeLeftForSearch = true;

        // Start the player's move calculation
        StartCoroutine( Players[Side2Move].Play( BoardPosition, Data.LastPlayedMove() ) );

        // Wait until the move is found or no time is left
        yield return new WaitUntil(() =>
        {
            moveFound = Players[Side2Move].MoveFound();
            timeLeftForSearch = TimeLeftForSearch();
            return moveFound || !timeLeftForSearch;
        });

        if (!timeLeftForSearch) {
            EndState = 7 + (Side2Move ^ 1);
            Players[Side2Move].StopReadOutput();
        }
    }


    private void
    GameOverScreen(int state)
    {
        string res = "";

        if (state == 1) res = "White wins by checkmate!";
        else if (state == 2) res = "Black wins by checkmate!";
        else if (state == 3) res = "Draw by stalemate!";
        else if (state == 4) res = "Draw by insufficient material!";
        else if (state == 5) res = "Draw by 3-fold repetition!";
        else if (state == 6) res = "Draw by 50-move rule!";
        else if (state == 7) res = "White wins on time!";
        else if (state == 8) res = "Black wins on time!";

        EndScreen.GetComponent<TMPro.TextMeshProUGUI>().text = res;
        EndScreen.SetActive(true);
    }


    private int
    PredictionCall()
    {
        var (__x, __y) = Data.LastEvalPair();

        // Both bots thinks white is winning
        if (Mathf.Min(__x, __y) >  AdjournWinMargin)
            return 1;
        
        // Both bots thinks black is winning
        if (Mathf.Max(__x, __y) < -AdjournWinMargin)             
            return -1;

        // If position is draw for last __x moves
        if (Data.DrawnPositionForContinuousMoves(0.25f, 60))
            return 2;

        return 0;
    }
}

