using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Engine_AvA_Time : MonoBehaviour {

    #region Variables

    public Core cs;
    [SerializeField] private ChessBot cb;
    [SerializeField] private Elsa eb;
    [SerializeField] private BoardHandler bh;
    [SerializeField] private Timer tmr;

    public ChessBoard primary = new ChessBoard();
    public MoveList vMoves;
    public PGN game_pgn;

    private char elsa_color;
    private List<ulong> occured_positions;
    private int bot_move, drawCounter = 0;

    // For Debugging
    private float bot_eval;
    [SerializeField] private bool Can_adjourn = false;      // For Ending Games Sooner
    [SerializeField] private bool RandomOpenings = false;
    // [0 -> No pred. made, 1 -> White will win, -1 -> Black will win, 2 -> Game will end in draw]
    public int prediction_result;                           // For Testing Purpose
    private readonly float margin_eval = 5.0f;
    [HideInInspector] public int elsa_loss_on_time = 0, cb_loss_on_time = 0;

    #endregion

    #region Board

    private void Start() {
        game_pgn = new PGN();
        occured_positions = new List<ulong>();

        cs.init();
        FindObjectOfType<BookMaker>().GetOpeningBook();
        primary.LoadFromFEN(cs.startPosition);
        vMoves = new MoveList();
        bh.Initialize_board(ref primary);
        GetMoves();
    }

    void Add_to_table(ulong key, int __move) {
        int pt = (__move >> 12) & 7, cpt = (__move >> 15) & 7;
        if (pt == 1 || cpt != 0)
            occured_positions.Clear();
        occured_positions.Add(key);
    }

    public void Get_botmove(int __m, double __e) {
        bot_move = __m;
        bot_eval = (float)__e;
    }

    public int Different_evals(float margin) {
        int res = 0;
        List<Vector2> evaluations = game_pgn.get_eval();
        foreach (Vector2 eval in evaluations) {
            float white = Mathf.Abs(eval.x), black = Mathf.Abs(eval.y);
            if (Mathf.Abs(eval.x - eval.y) > margin && white < 15f && black < 15f) res++;
        }
        return res;
    }

    #endregion

    #region Prediction & GameOver

    private void Prediction_call() {
        if (prediction_result != 0 || primary.pColor == 1) return;

        Vector2 eval = game_pgn.LastOf_Eval();

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

    private int Is_Game_Over() {
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

        if (vMoves.moveCount == 0) {                        // Check for checkmate/stalemate
            if (vMoves.KingAttackers > 0)
                return primary.pColor == 1 ? 2 : 1;
            return 3;
        }

        if (cs.Insufficient_material(primary)) return 4;    // Check for insufficient material
        if (occured_positions.Count >= 100) return 6;       // Check for 50-move rule

        if (occured_positions.Count > 0) {                  // Check for 3-fold repetition
            int cnt = 0, last = occured_positions.Count - 1;
            ulong curr_pos = occured_positions[last];
            foreach (ulong pos in occured_positions)
                if (pos == curr_pos) cnt++;
            if (cnt >= 3) return 5;
        }

        if (tmr.clock_black <= 0) {                         // Check if black lost on time
            if (elsa_color == 'b') elsa_loss_on_time++;
            else cb_loss_on_time++;
            return 7;
        }
        if (tmr.clock_white <= 0) {                         // Check if white lost on time
            if (elsa_color == 'w') elsa_loss_on_time++;
            else cb_loss_on_time++;
            return 8;
        }

        return -1;
    }

    private void Game_Ended(int state, int _pr) {

        eb.StopBot();
        cb.StopBot();

        int game_res = 2;
        if (state == 1 || state == 7) game_res = 1;                     // White won
        else if (state == 2 || state == 8) game_res = -1;               // Black won
        FindObjectOfType<Arena>().Ending_reached(game_res, _pr);        // Game ended in draw
    }

    private void Game_Adjourned(int _pr) {
        eb.StopBot();
        cb.StopBot();
        FindObjectOfType<Arena>().Ending_reached(_pr, _pr);
    }

    public void Start_new_game(char to_white, List<int> opening) {

        char cb_color = to_white == 'c' ? 'w' : 'b';
        elsa_color = to_white == 'e' ? 'w' : 'b';               // Set Elsa color

        prediction_result = drawCounter = 0;                    // Reset prev prediction & drawCounter

        game_pgn.Clear_list();                                  // Clear last match pgn
        occured_positions.Clear();                              // Clear occured pos from last game
        primary.LoadFromFEN(cs.startPosition);                  // Load Starting pos. to chessBoard class

        if (RandomOpenings)
            StartCoroutine(PlayOpening(opening, elsa_color, cb_color));
        else {
            eb.Init(elsa_color, cs.startPosition);
            cb.Init(cb_color, cs.startPosition);
            vMoves = cs.GenerateMoves(ref primary);             // Generate all legal moves in current pos.
            tmr.Clock_reset(primary.pColor);                    // Reset clock for next match
            StartCoroutine(BotInput());                         // Ready to request moves from bots
        }
    }

    private IEnumerator PlayOpening(List<int> opening, char eb_color, char cb_color) {

        float time_left = tmr.Get_alloted_time();
        foreach (int move in opening) {
            game_pgn.Add(primary.pColor, move, 0, time_left, false);
            primary.MakeMove(move);
            Add_to_table(primary.Generate_HashKey(ref cs.HashIndex), move);
            bh.Recreate(ref primary);
            yield return new WaitForSeconds(0.1f);
        }

        string tmp_fen = primary.FENGenerator();
        eb.Init(eb_color, tmp_fen);
        cb.Init(cb_color, tmp_fen);

        vMoves = cs.GenerateMoves(ref primary);                 // Generate all legal moves in current pos.
        tmr.Clock_reset(primary.pColor);                        // Reset clock for next match

        StartCoroutine(BotInput());                             // Ready to request moves from bots
    }

    #endregion

    #region GamePlay

    private void ReceiveMove(char bot) {
        if (bot == 'e') {
            bot_move = eb.received_move;
            bot_eval = (float)eb.received_move_eval;
            return;
        }
        bot_move = cb.received_move;
        bot_eval = (float)cb.received_move_eval;
    }

    private int GetMoves() {
        vMoves = cs.GenerateMoves(ref primary);     // Generate & store all legal moves in current pos.
        int state = Is_Game_Over();                 // Check if the game is Over
        return state;
    }

    private IEnumerator BotInput() {

        //eb.MakeMove(ref primary, game_pgn.get_last_move());
        //yield return new WaitUntil(() => eb.received_move != 0);
        //ReceiveMove('e');

        if (primary.pColor == 1) {
            // If white side to move
            if (elsa_color == 'w') {
                eb.MakeMove(ref primary, game_pgn.get_last_move());
                yield return new WaitUntil(() => eb.received_move != 0);
                ReceiveMove('e');
            }
            else {
                cb.MakeMove(ref primary, game_pgn.get_last_move());
                yield return new WaitUntil(() => cb.received_move != 0);
                ReceiveMove('c');
            }
        }
        else {
            // If Black side to move
            if (elsa_color == 'b') {
                eb.MakeMove(ref primary, game_pgn.get_last_move());
                yield return new WaitUntil(() => eb.received_move != 0);
                ReceiveMove('e');
            }
            else {
                cb.MakeMove(ref primary, game_pgn.get_last_move());
                yield return new WaitUntil(() => cb.received_move != 0);
                ReceiveMove('c');
            }
        }

        ValidateMove();                                         // Validate the move played
    }

    public void ValidateMove() {
        if (bot_move == 0) return;
        Next_turn(bot_move);
    }

    private void Next_turn(int move) {
        tmr.Switch_player();                        // Switch side to play
        tmr.Clock_freeze();                         // Freeze the clocks

        // Store move, time_left & evaluation
        float time_left = primary.pColor == 1 ? tmr.clock_white : tmr.clock_black;
        game_pgn.Add(primary.pColor, bot_move, bot_eval, time_left);

        Prediction_call();                          // Take a prediction of results

        primary.MakeMove(move);                     // Make Move on board
        Add_to_table(primary.Generate_HashKey(ref cs.HashIndex), move);   // Add to occured pos. Table

        bh.Board_reset(false);                      // Reset Physical Board
        bh.MarkplayedMove(move);                    // Mark the move played on board
        bh.Recreate(ref primary);                   // Create the new position on physical board

        vMoves = cs.GenerateMoves(ref primary);     // Generate & store all legal moves in current pos.
        int state = Is_Game_Over();                 // Check if the game is Over

        if (state != -1) {
            // Someone has lost, immediately cut the game
            Game_Ended(state, prediction_result);
        }
        else if (Can_adjourn && prediction_result != 0) {
            // Allowed to adjourn the game if a prediction has been made
            Game_Adjourned(prediction_result);
        }
        else {
            // Game is still left, request for next move
            StartCoroutine(Ask_For_Next_Move());
        }
        return;
    }

    private IEnumerator Ask_For_Next_Move() {
        yield return new WaitForSeconds(0.05f);
        FindObjectOfType<Timer>().Clock_unfreeze();     // Unfreeze the clocks
        StartCoroutine(BotInput());                     // Ask the bot to make a move
    }

    #endregion

}
