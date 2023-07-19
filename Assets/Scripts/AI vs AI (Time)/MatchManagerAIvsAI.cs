using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MatchManagerAIvsAI : MonoBehaviour
{
    [SerializeField] private         Core  cs;
    [SerializeField] private BoardHandler  bh;
    [SerializeField] private        Timer tmr;

    [SerializeField] private  bool FixedTimePerMove = false;
    [SerializeField] private  bool   RandomOpenings = false;
    [SerializeField] private  bool       CanAdjourn = false;
    [SerializeField] private float AdjournWinMargin =  5.0f;

    public ChessBoard BoardPosition;
    public  MatchData Data;

    private IPlayer[] Players;
    private int Side2Move;

    public int EndState;
    public int EndResult;
    public int EndPrediction;


    private void
    Start()
    {
        cs.Init();
        BoardPosition = new ChessBoard(cs.StartFen);
        bh.InitializeBoard(ref BoardPosition);
        Players = new IPlayer[2];
    }


    public IEnumerator
    StartNewGame(string engine_white, string engine_black, List<int> opening_moves)
    {
        Data = new MatchData();

        if (RandomOpenings)
            yield return StartCoroutine( PlayOpening(opening_moves) );

        Side2Move = (BoardPosition.pColor == 1) ? 0 : 1;

        Players[0] = new ChessEngine(engine_white, BoardPosition.Fen(), FixedTimePerMove, false);
        Players[1] = new ChessEngine(engine_black, BoardPosition.Fen(), FixedTimePerMove, false);

        tmr.Init(Side2Move);
        yield return StartCoroutine( PlayGame() );
    }


    private IEnumerator
    PlayOpening(List<int> opening)
    {
        // Play all moves from the opening_book
        foreach (int move in opening)
        {
            BoardPosition.MakeMove(move);
            Data.Add(move, 0, tmr.AllotedTimePerSide, BoardPosition.GenerateHashKey(ref cs.HashIndex) );

            bh.Recreate(ref BoardPosition);
            yield return new WaitForSeconds(0.1f);
        }
        // yield break;
    }

    //! TODO Cut the game immediately if one player time reaches zero.

    private IEnumerator
    PlayGame()
    {
        while ((EndState = IsGameOver()) == -1)
        {
            yield return StartCoroutine( Players[Side2Move].Play(BoardPosition, Data.LastPlayedMove()) );      
            var (move, eval) = Players[Side2Move].GetResults();

            UpdateBoardElements(move, eval);

            if (CanAdjourn && (EndPrediction != 0))
                yield break;

            // Next Turn
            yield return new WaitForSeconds(0.2f);
            tmr.ClockUnfreeze();
            Side2Move ^= 1;
        }

        EndResult = GetResultFromState();

        if (Players[0] != null) Players[0].Stop();
        if (Players[1] != null) Players[1].Stop();
    }


    public void
    UpdateBoardElements(int move, float eval)
    {
        tmr.SwitchPlayer();
        tmr.ClockFreeze();

        BoardPosition.MakeMove(move);
        Data.Add(move, eval, tmr.ChessClocks[Side2Move ^ 1],
            BoardPosition.GenerateHashKey(ref cs.HashIndex) );

        bh.BoardReset(false);
        bh.MarkPlayedMove(move);
        bh.Recreate(ref BoardPosition);

        if (EndPrediction == 0)
            EndPrediction = PredictionCall();
    }


    private int
    IsGameOver()
    {
        // Refer to GameOverScreen() for various-states.
        MoveList moveslist = cs.mg.GenerateMoves(ref BoardPosition);

        // checkmate/stalemate check
        if (moveslist.moveCount == 0)
            return (moveslist.KingAttackers > 0) ? 1 + (Side2Move ^ 1) : 3;

        // Insufficient material check
        if (cs.InsufficientMaterial(BoardPosition)) return 4;

        // 3-fold repetition and 50-move-rule
        if (Data.ThreeMoveRepetitionDraw()) return 5;
        if (Data.FiftyMoveRuleDraw()) return 6;

        // Check if lost on time
        if (tmr.ChessClocks[Side2Move] < 0f)
            return 7 + (Side2Move ^ 1);

        return -1;
    }


    private int
    PredictionCall()
    {
        var (__x, __y) = Data.LastEvalPair();

        if (Mathf.Min(__x, __y) >  AdjournWinMargin)             // Both bots thinks white is winning
            return 1;
        if (Mathf.Min(__x, __y) < -AdjournWinMargin)             // Both bots thinks black is winning
            return -1;
        if (Data.DrawCounter(0.25f) > 60)
            return 2;

        return 0;
    }


    private int
    GetResultFromState()
    {
        // Game Ends in Adjournment
        if (CanAdjourn && (EndPrediction != 0))
            return EndPrediction;

        if (EndState == 1 || EndState == 7) return  1;
        if (EndState == 2 || EndState == 8) return -1;
        
        return 2;
    }


    private void
    OnApplicationQuit()
    {
        if (Players[0] != null) Players[0].Stop();
        if (Players[1] != null) Players[1].Stop();

        Application.Quit();
    }
}