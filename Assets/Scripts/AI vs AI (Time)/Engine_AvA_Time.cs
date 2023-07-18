using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Engine_AvA_Time : MonoBehaviour
{
    #region Variables

    public Core cs;

    [SerializeField] private BoardHandler  bh;
    [SerializeField] private        Timer tmr;

    /* private ChessEngine engine_white;
    private ChessEngine engine_black; */

    private ChessEngine[]   Engines;
    private           int Side2Move;

    public ChessBoard    BoardPosition;
    public   MoveList MovesForPosition;
    public        Pgn          GamePgn;

    private   int BotMove;
    private float BotEval;

    [SerializeField] private  bool      RandomOpenings = false;
    [SerializeField] private  bool          CanAdjourn = false;
    [SerializeField] private float WinMarginForAdjourn =  5.0f;
    [SerializeField] private  bool UseFixedTimePerMove = false;

    private List<ulong> OccuredPositions;


    #endregion

    #region Board

    private void
    Start()
    {
        cs.Init();

        BoardPosition    = new ChessBoard();
        MovesForPosition = new MoveList();
        GamePgn          = new Pgn();
        OccuredPositions = new List<ulong>();

        bh.InitializeBoard(ref BoardPosition);
        GetMoves();
    }

    private void
    AddToTable(ulong key, int __move)
    {
        int pt = (__move >> 12) & 7, cpt = (__move >> 15) & 7;
        if (pt == 1 || cpt != 0)
            OccuredPositions.Clear();
        OccuredPositions.Add(key);
    }

    public int
    DifferentEvals(float margin)
    {
        int res = 0;
        List<Vector2> evaluations = GamePgn.GetEval();
        foreach (Vector2 eval in evaluations)
        {
            float white = Mathf.Abs(eval.x);
            float black = Mathf.Abs(eval.y);
            if ((Mathf.Abs(eval.x - eval.y) > margin) && (white < 15f) && (black < 15f))
                res++;
        }
        return res;
    }

    #endregion

    #region Prediction & GameOver

    private int
    PredictionCall()
    {
        if (BoardPosition.pColor == 1) return 0;

        Vector2 eval = GamePgn.LastOfEval();

        if (Mathf.Min(eval.x, eval.y) >  WinMarginForAdjourn)             // Both bots thinks white is winning
            return 1;
        if (Mathf.Min(eval.x, eval.y) < -WinMarginForAdjourn)             // Both bots thinks black is winning
            return -1;
        if (GamePgn.DrawCounter(0.25f) > 30)
            return 2;

        return 0;
    }

    private int
    IsGameOver()
    {
        /***
         *  States :
         * -1 -> Game not Over
         *  1 -> White won by checkmate
         *  2 -> Black won by checkmate
         *  3 -> Draw by stalemate
         *  4 -> Draw by insufficient material
         *  5 -> Draw by 3-fold repetition
         *  6 -> Draw by 50-move rule
         *  7 -> White won on time
         *  8 -> Black won on time
        ***/

        if (MovesForPosition.moveCount == 0)
        {
            // Check for checkmate/stalemate
            if (MovesForPosition.KingAttackers > 0)
                return BoardPosition.pColor == 1 ? 2 : 1;
            return 3;
        }

        // Check for insufficient material
        if (cs.InsufficientMaterial(BoardPosition)) return 4;

        // Check for 50-move rule
        if (OccuredPositions.Count >= 100) return 6;

        if (OccuredPositions.Count > 0)
        {
            // Check for 3-fold repetition
            int cnt = 0, last = OccuredPositions.Count - 1;
            ulong curr_pos = OccuredPositions[last];
            foreach (ulong pos in OccuredPositions)
                if (pos == curr_pos) cnt++;
            if (cnt >= 3) return 5;
        }

        // Black lost on time
        if (tmr.ChessClocks[1] <= 0) return 7;
        
        // White lost on time                 
        if (tmr.ChessClocks[0] <= 0) return 8;

        return -1;
    }

    private void
    GameEnded(int state, int _pr)
    {
        Engines[0].Stop();
        Engines[1].Stop();

        int game_res = 2;
        if (state == 1 || state == 7) game_res = 1;                     // White won
        else if (state == 2 || state == 8) game_res = -1;               // Black won
        FindObjectOfType<Arena>().EndingReached(game_res, state, _pr);  // Game ended in draw
    }

    private void
    GameAdjourned(int predict_res)
    {
        Engines[0].Stop();
        Engines[1].Stop();

        FindObjectOfType<Arena>().EndingReached(predict_res, predict_res, 0);
    }

    public void
    StartNewGame(string __engine_white, string __engine_black, List<int> opening)
    {
        GamePgn.ClearList();
        OccuredPositions.Clear();
        BoardPosition.LoadFromFEN(cs.StartPosition);

        if (RandomOpenings)
        {
            StartCoroutine(PlayOpening(__engine_white, __engine_black, opening));
        }
        else
        {
            // Initial both engines
            /* engine_white = new ChessEngine(__engine_white, BoardPosition.Fen(), false, false);
            engine_black = new ChessEngine(__engine_black, BoardPosition.Fen(), false, false); */
            
            // 0 (white), 1 (black)
            Side2Move = 0;

            // Initial both engines
            Engines[0] = new ChessEngine(__engine_white, BoardPosition.Fen(), UseFixedTimePerMove, false);
            Engines[1] = new ChessEngine(__engine_black, BoardPosition.Fen(), UseFixedTimePerMove, false);

            MovesForPosition = cs.mg.GenerateMoves(ref BoardPosition);

            // Reset clock for next match
            tmr.ClockReset(BoardPosition.pColor);

            // Ready to request moves from bots
            StartCoroutine(BotInput());
        }
    }

    private IEnumerator
    PlayOpening(string __engine_white, string __engine_black, List<int> opening)
    {
        // Play all moves from the opening_book
        foreach (int move in opening)
        {
            GamePgn.Add(BoardPosition.pColor, move, 0, tmr.GetAllotedTime(), false);
            BoardPosition.MakeMove(move);
            AddToTable(BoardPosition.GenerateHashKey(ref cs.HashIndex), move);
            bh.Recreate(ref BoardPosition);
            yield return new WaitForSeconds(0.1f);
        }

        // Initial both engines
        Engines[0] = new ChessEngine(__engine_white, BoardPosition.Fen(), UseFixedTimePerMove, false);
        Engines[1] = new ChessEngine(__engine_black, BoardPosition.Fen(), UseFixedTimePerMove, false);

        MovesForPosition = cs.mg.GenerateMoves(ref BoardPosition);

        // Reset clock for next match
        tmr.ClockReset(BoardPosition.pColor);

        // Ready to request moves from bots
        StartCoroutine(BotInput());
    }

    #endregion

    #region GamePlay

    /* private void
    ReceiveMove()
    {
        if (BoardPosition.pColor == 1)
            (BotMove, BotEval) = (engine_white.engine_move, engine_white.engine_eval);
        else
            (BotMove, BotEval) = (engine_black.engine_move, engine_black.engine_eval);
        
        (BotMove, BotEval) = (Engines[Side2Move].EngineMove, Engines[Side2Move].EngineEval)
    } */

    private int
    GetMoves()
    {
        // Generate & store all legal moves in current pos.
        MovesForPosition = cs.mg.GenerateMoves(ref BoardPosition);

        // Check if the game is Over
        int state = IsGameOver();
        return state;
    }

    private IEnumerator
    BotInput()
    {
        Engines[Side2Move].Play(ref BoardPosition, GamePgn.GetLastMove());

        StartCoroutine(Engines[Side2Move].ReadOutputCoroutine());
        yield return new WaitUntil(() => Engines[Side2Move].EngineMove != 0);

        NextTurn(Engines[Side2Move].EngineMove, Engines[Side2Move].EngineEval);
    }

    private void
    NextTurn(int move, float eval)
    {
        // Switch side to play
        tmr.SwitchPlayer();

        // Freeze the clocks
        tmr.ClockFreeze();

        // Store move, time_left & evaluation

        int __side = -((BoardPosition.pColor - 1) / 2);
        float time_left = tmr.ChessClocks[__side];

        GamePgn.Add(BoardPosition.pColor, move, eval, time_left);

        int prediction = PredictionCall();

        BoardPosition.MakeMove(move);
        Side2Move ^= 1;

        // Add to occured pos. Table    
        AddToTable(BoardPosition.GenerateHashKey(ref cs.HashIndex), move);

        // Reset Physical Board
        bh.BoardReset(false);
        // Mark the move played on board                                        
        bh.MarkPlayedMove(move);
        // Create the new position on physical board
        bh.Recreate(ref BoardPosition);


        MovesForPosition = cs.mg.GenerateMoves(ref BoardPosition);
        int game_state = IsGameOver();

        if (game_state != -1)
        {
            // Someone has lost, immediately cut the game
            GameEnded(game_state, prediction);
        }
        else if (CanAdjourn && prediction != 0)
        {
            // Allowed to adjourn the game if a prediction has been made
            GameAdjourned(prediction);
        }
        else
        {
            // Game is still left, request for next move
            StartCoroutine(AskForNextMove());
        }
    }

    private IEnumerator
    AskForNextMove()
    {
        yield return new WaitForSeconds(0.05f);
        FindObjectOfType<Timer>().ClockUnfreeze();     // Unfreeze the clocks
        StartCoroutine(BotInput());                     // Ask the bot to make a move
    }

    #endregion


    public void
    OnApplicationQuit()
    {
        Engines[0].Stop();
        Engines[1].Stop();
        Application.Quit();
    }
}
