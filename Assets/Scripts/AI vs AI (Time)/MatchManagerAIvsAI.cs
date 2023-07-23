/* using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class MatchManagerAIvsAI : MonoBehaviour
{
    public  Core  cs;
    public Timer tmr;

    [SerializeField] private  bool FixedTimePerMove = false;
    [SerializeField] private  bool   RandomOpenings = false;
    [SerializeField] private  bool       CanAdjourn = false;
    [SerializeField] private float AdjournWinMargin =  5.0f;

    public int EndResult;
    public int EndPrediction;


    public IEnumerator
    StartNewGame(string engine_white, string engine_black, List<int> opening_moves)
    {
        cs.Data = new MatchData();
        cs.Side2Move = 0;
        cs.BoardPosition = new ChessBoard(cs.StartFen);

        if (RandomOpenings)
            yield return StartCoroutine( PlayOpening(opening_moves) );

        cs.Players[0] = new ChessEngine(engine_white, cs.BoardPosition.Fen(), FixedTimePerMove, false);
        cs.Players[1] = new ChessEngine(engine_black, cs.BoardPosition.Fen(), FixedTimePerMove, false);

        tmr.Init(cs.Side2Move);
        yield return StartCoroutine( PlayGame() );
    }


    private IEnumerator
    PlayOpening(List<int> opening)
    {
        float time_left = tmr.AllotedTimePerSide;

        // Play all moves from the opening_book
        foreach (int move in opening)
        {
            cs.BoardPosition.MakeMove(move);
            cs.Data.Add(move, 0, time_left, cs.BoardPosition.GenerateHashKey(ref cs.HashIndex) );

            cs.bh.Recreate(ref cs.BoardPosition);
            yield return new WaitForSeconds(0.1f);

            cs.Side2Move ^= 1;
        }
    }


    private IEnumerator
    PlayGame()
    {
        while ((cs.EndState = cs.IsGameOver()) == -1)
        {
            // Let player make his move
            yield return StartCoroutine( cs.RequestMove() ) ;

            // Time runs out before player making a move
            if (cs.TimeLeftForSearch() == false)
                break;

            // Retrieve player move
            var (move, eval) = cs.Players[cs.Side2Move].GetResults();

            // If no prediction made so far
            if (EndPrediction == 0)
                EndPrediction = PredictionCall();

            // If we have a prediction and adjournment is allowed.
            if (CanAdjourn && (EndPrediction != 0))
                yield break;

            // Update board elements after making move
            yield return StartCoroutine( cs.UpdateBoardElements(move, eval) );

            // Switch sides and next turn
            cs.Side2Move ^= 1;
        }

        tmr.ClockFreeze();

        if (cs.Players[0] != null) cs.Players[0].Stop();
        if (cs.Players[1] != null) cs.Players[1].Stop();
    }


    private int
    PredictionCall()
    {
        var (__x, __y) = cs.Data.LastEvalPair();

        // Both bots thinks white is winning
        if (Mathf.Min(__x, __y) >  AdjournWinMargin)
            return 1;
        
        // Both bots thinks black is winning
        if (Mathf.Max(__x, __y) < -AdjournWinMargin)             
            return -1;

        // If position is draw for last __x moves
        if (cs.Data.DrawnPositionForContinuousMoves(0.25f, 60))
            return 2;

        return 0;
    }
} */