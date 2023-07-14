using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class Engine_AvA_Time : MonoBehaviour {

    #region Variables

    public Core cs;

    [SerializeField] private BoardHandler bh;
    [SerializeField] private Timer tmr;

    private ChessEngine engine_white;
    private ChessEngine engine_black;

    public ChessBoard primary = new ChessBoard();
    public MoveList vMoves;
    public PGN game_pgn;

    private char elsa_color;
    private List<ulong> occured_positions;
    private int bot_move, drawCounter = 0;

    // For Debugging
    private float bot_eval;
    [SerializeField] private bool can_adjourn = false;      // For Ending Games Sooner
    [SerializeField] private bool random_openings = false;
    // [0 -> No pred. made, 1 -> White will win, -1 -> Black will win, 2 -> Game will end in draw]
    public int prediction_result;                           // For Testing Purpose
    private readonly float margin_eval = 5.0f;

    #endregion

    #region Board

    private void
    Start()
    {
        game_pgn = new PGN();
        occured_positions = new List<ulong>();

        cs.Init();
        primary.LoadFromFEN(cs.StartPosition);
        vMoves = new MoveList();
        bh.InitializeBoard(ref primary);
        GetMoves();
    }

    private void
    AddToTable(ulong key, int __move)
    {
        int pt = (__move >> 12) & 7, cpt = (__move >> 15) & 7;
        if (pt == 1 || cpt != 0)
            occured_positions.Clear();
        occured_positions.Add(key);
    }

    public void
    GetBotmove(int __m, double __e)
    {
        bot_move = __m;
        bot_eval = (float)__e;
    }

    public int
    DifferentEvals(float margin)
    {
        int res = 0;
        List<Vector2> evaluations = game_pgn.GetEval();
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

    private void
    PredictionCall()
    {
        if (prediction_result != 0 || primary.pColor == 1) return;

        Vector2 eval = game_pgn.LastOfEval();

        if (eval.x > margin_eval && eval.y > margin_eval)
            prediction_result = 1;
        else if (eval.x < -margin_eval && eval.y < -margin_eval)
            prediction_result = -1;
        else {
            if (Mathf.Abs(eval.x) < 0.25f && Mathf.Abs(eval.y) < 0.25f)
                drawCounter++;
            else
                drawCounter = 0;
            if (drawCounter > 30) prediction_result = 2;
        }
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

        if (vMoves.moveCount == 0)
        {
            // Check for checkmate/stalemate
            if (vMoves.KingAttackers > 0)
                return primary.pColor == 1 ? 2 : 1;
            return 3;
        }

        if (cs.InsufficientMaterial(primary)) return 4;     // Check for insufficient material
        if (occured_positions.Count >= 100) return 6;       // Check for 50-move rule

        if (occured_positions.Count > 0)
        {
            // Check for 3-fold repetition
            int cnt = 0, last = occured_positions.Count - 1;
            ulong curr_pos = occured_positions[last];
            foreach (ulong pos in occured_positions)
                if (pos == curr_pos) cnt++;
            if (cnt >= 3) return 5;
        }

        if (tmr.clock_black <= 0) return 7;                 // Black lost on time
        if (tmr.clock_white <= 0) return 8;                 // White lost on time

        return -1;
    }

    private void
    GameEnded(int state, int _pr)
    {
        engine_white.Stop();
        engine_black.Stop();

        int game_res = 2;
        if (state == 1 || state == 7) game_res = 1;                     // White won
        else if (state == 2 || state == 8) game_res = -1;               // Black won
        FindObjectOfType<Arena>().EndingReached(game_res, state, _pr);  // Game ended in draw
    }

    private void
    GameAdjourned(int predict_res)
    {
        engine_white.Stop();
        engine_black.Stop();

        FindObjectOfType<Arena>().EndingReached(predict_res, predict_res, 0);
    }

    public void
    StartNewGame(string __engine_white, string __engine_black, List<int> opening)
    {
        prediction_result = drawCounter = 0;                    // Reset prev prediction & drawCounter

        game_pgn.ClearList();                                   // Clear last match pgn
        occured_positions.Clear();                              // Clear occured pos from last game
        primary.LoadFromFEN(cs.StartPosition);                  // Load Starting pos. to chessBoard class

        if (random_openings)
        {
            StartCoroutine(PlayOpening(__engine_white, __engine_black, opening));
        }
        else
        {
            // Initial both engines
            engine_white = new ChessEngine(__engine_white, primary.FenGenerator(), false, false);
            engine_black = new ChessEngine(__engine_black, primary.FenGenerator(), false, false);

            vMoves = cs.mg.GenerateMoves(ref primary);          // Generate all legal moves in current pos.
            tmr.ClockReset(primary.pColor);                     // Reset clock for next match

            StartCoroutine(BotInput());                         // Ready to request moves from bots
        }
    }

    private IEnumerator
    PlayOpening(string __engine_white, string __engine_black, List<int> opening)
    {
        float time_left = tmr.GetAllotedTime();
        foreach (int move in opening)
        {
            game_pgn.Add(primary.pColor, move, 0, time_left, false);
            primary.MakeMove(move);
            AddToTable(primary.GenerateHashKey(ref cs.HashIndex), move);
            bh.Recreate(ref primary);
            yield return new WaitForSeconds(0.1f);
        }

        string tmp_fen = primary.FenGenerator();

        // Initial both engines
        engine_white = new ChessEngine(__engine_white, primary.FenGenerator(), tmr);
        engine_black = new ChessEngine(__engine_black, primary.FenGenerator(), tmr);

        vMoves = cs.mg.GenerateMoves(ref primary);                 // Generate all legal moves in current pos.
        tmr.ClockReset(primary.pColor);                        // Reset clock for next match

        StartCoroutine(BotInput());                             // Ready to request moves from bots
    }

    #endregion

    #region GamePlay

    private void
    ReceiveMove()
    {
        if (primary.pColor == 1)
            (bot_move, bot_eval) = (engine_white.engine_move, engine_white.engine_eval);
        else
            (bot_move, bot_eval) = (engine_black.engine_move, engine_black.engine_eval);
    }

    private int
    GetMoves()
    {
        vMoves = cs.mg.GenerateMoves(ref primary);      // Generate & store all legal moves in current pos.
        int state = IsGameOver();                       // Check if the game is Over
        return state;
    }

    private IEnumerator
    BotInput()
    {
        if (primary.pColor == 1)
        {
            engine_white.Play(ref primary, game_pgn.GetLastMove());

            StartCoroutine(engine_white.ReadOutputCoroutine());
            yield return new WaitUntil(() => engine_white.engine_move != 0);
        }
        else
        {
            engine_black.Play(ref primary, game_pgn.GetLastMove());

            StartCoroutine(engine_black.ReadOutputCoroutine());
            yield return new WaitUntil(() => engine_black.engine_move != 0);
        }
        ReceiveMove();
        ValidateNonNullMove();                      // Validate the move played
    }

    public void
    ValidateNonNullMove()
    {
        if (bot_move == 0) return;
        NextTurn(bot_move);
    }

    private void
    NextTurn(int move)
    {

        UnityEngine.Debug.Log("In Next Turn");
        UnityEngine.Debug.Log("Last Move => " + bot_move + " | " + bot_eval);

        tmr.SwitchPlayer();                        // Switch side to play
        tmr.ClockFreeze();                         // Freeze the clocks

        // Store move, time_left & evaluation
        float time_left = primary.pColor == 1 ? tmr.clock_white : tmr.clock_black;
        game_pgn.Add(primary.pColor, bot_move, bot_eval, time_left);

        PredictionCall();                          // Take a prediction of results

        primary.MakeMove(move);                     // Make Move on board
        AddToTable(primary.GenerateHashKey(ref cs.HashIndex), move);   // Add to occured pos. Table

        bh.BoardReset(false);                       // Reset Physical Board
        bh.MarkPlayedMove(move);                    // Mark the move played on board
        bh.Recreate(ref primary);                   // Create the new position on physical board

        vMoves = cs.mg.GenerateMoves(ref primary);     // Generate & store all legal moves in current pos.
        int state = IsGameOver();                 // Check if the game is Over

        if (state != -1) {
            // Someone has lost, immediately cut the game
            GameEnded(state, prediction_result);
        }
        else if (can_adjourn && prediction_result != 0) {
            // Allowed to adjourn the game if a prediction has been made
            GameAdjourned(prediction_result);
        }
        else {
            // Game is still left, request for next move
            StartCoroutine(AskForNextMove());
        }
        return;
    }

    private IEnumerator
    AskForNextMove()
    {
        yield return new WaitForSeconds(0.05f);
        FindObjectOfType<Timer>().ClockUnfreeze();     // Unfreeze the clocks
        StartCoroutine(BotInput());                     // Ask the bot to make a move
    }

    #endregion

}
