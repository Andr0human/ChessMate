using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Engine_PvAI : MonoBehaviour {

    #region Variables

    [SerializeField] private Core cs;
    [SerializeField] private BoardHandler bh;
    [SerializeField] private Timer tmr;

    [HideInInspector] public int ip, fp;
    public ChessBoard primary = new ChessBoard();
    public MoveList vMoves;
    public PGN game_pgn;
    public string StartPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public GameObject gameEnd;

    private ChessEngine bot;

    public int bot_color;
    [SerializeField] private GameObject promotion_table;
    private List<ulong> occured_positions;
    private int bot_move;

    // For Debugging
    private float bot_eval;

    #endregion

    #region Board

    private void
    Start()
    {
        game_pgn = new PGN();
        occured_positions = new List<ulong>();

        cs.Init();

        primary.LoadFromFEN(StartPosition);
        vMoves = new MoveList();
        bh.InitializeBoard(ref primary);
        GetMoves();

        bot = new ChessEngine("elsa", StartPosition, true, true);
    }

    private void
    AddToTable(ulong key, int __move)
    {
        int pt = (__move >> 12) & 7, cpt = (__move >> 15) & 7;
        if (pt == 1 || cpt != 0)
            occured_positions.Clear();
        occured_positions.Add(key);
    }

    public int
    DifferentEvals(float margin)
    {
        int res = 0;
        List<Vector2> evaluations = game_pgn.GetEval();
        foreach (Vector2 eval in evaluations)
        {
            float white = Mathf.Abs(eval.x), black = Mathf.Abs(eval.y);
            if (Mathf.Abs(eval.x - eval.y) > margin && white < 15f && black < 15f) res++;
        }
        return res;
    }

    #endregion

    #region USER

    public bool
    AvailableMoves(Vector2Int pos)
    { return vMoves.InitialKey(8 * pos.y + pos.x); }

    public void
    ShowEndSquares(Vector2Int pos)
    {
        bh.BoardReset(true);
        bh.BoardHighLight(vMoves.endIndex[8 * pos.y + pos.x], ref primary);
    }

    public void
    RequestBoardReset()
    {
        bh.BoardReset(true);
        return;
    }

    public void
    PrintGame()
    {
        ChessBoard tmp_board = new ChessBoard();
        tmp_board.LoadFromFEN(StartPosition);
        string path1 = System.Environment.CurrentDirectory + "/game.txt";
        var moveList = game_pgn.GetPgn();
        foreach (Vector2Int current in moveList)
        {
            int x = current.x;
            int y = current.y;
            string text2 = "";
            if (x != 0)
            {
                text2 += FindObjectOfType<MoveGenerator>().PrintMove(x, ref tmp_board);
                text2 += " ";
                tmp_board.MakeMove(x);
            }
            if (y != 0)
            {
                text2 += FindObjectOfType<MoveGenerator>().PrintMove(y, ref tmp_board);
                tmp_board.MakeMove(y);
            }
            text2 += "\n";
            File.AppendAllText(path1, text2);
        }
    }

    public void
    ValidateMove(bool request_by_human = false)
    {
        if (primary.pColor == bot_color)
        {
            if (request_by_human) return;

            NextTurn(bot_move);
            return;
        }

        if (!vMoves.StartEndPair(ip, fp)) return;

        ulong Rank18 = 18374686479671623935;
        int pmove, pColor = primary.pColor;
        pmove = ((primary.board[fp] * -pColor) << 15) ^ ((primary.board[ip] * pColor) << 12) ^ (fp << 6) ^ ip;
        if (pColor == 1) pmove |= 1 << 20;


        if (primary.board[ip] == 1 && ((1UL << fp) & Rank18) != 0)
        {
            // If move moved is pawn and final square is 1st or 8th Rank
            StartCoroutine(AskforPromotedPiece(pmove));
            return;
        }

        // Condition for promotion

        NextTurn(pmove);
    }

    private IEnumerator
    AskforPromotedPiece(int _move)
    {
        Vector3 invalid_point = new Vector3(100f, 100f, 100f);
        FindObjectOfType<UserInput_PvsAI>().vector = invalid_point;
        FindObjectOfType<UserInput_PvsAI>().castle_in_place = true;
        promotion_table.SetActive(true);
        Vector3 MousePos;

        while (true)
        {
            MousePos = FindObjectOfType<UserInput_PvsAI>().vector;
            if (MousePos != invalid_point)
                break;
            yield return new WaitForSeconds(0f);
        }

        Vector2Int pos = new Vector2Int(cs.Convert(MousePos.x), cs.Convert(MousePos.y));

        int pp = -1;

        if (primary.pColor == 1 && pos.x == -2)
        {
            if (pos.y == 7) pp = 3;
            else if (pos.y == 6) pp = 0;
            else if (pos.y == 5) pp = 1;
            else if (pos.y == 4) pp = 2;
        }
        else if (primary.pColor == -1) {

        }

        FindObjectOfType<UserInput_PvsAI>().castle_in_place = false;
        promotion_table.SetActive(false);
        if (pp != -1)
        {
            _move |= pp << 18;
            NextTurn(_move);
        }
        else {
            RequestBoardReset();
        }
    }

    #endregion

    #region Prediction & GameOver

    private int IsGameOver()
    {
        /***
         *  States (-1 -> Game not Over)
         * 1 -> White won by checkmate
         * 2 -> Black won by checkmate
         * 3 -> Draw by stalemate
         * 4 -> Draw by insufficient material
         * 5 -> Draw by 3-fold repetition
         * 6 -> Draw by 50-move rule
         * 7 -> White won on time
         * 8 -> Black won on time
         ***/
        if (vMoves.moveCount == 0) {                        // Check for checkmate/stalemate
            if (vMoves.KingAttackers > 0)
                return primary.pColor == 1 ? 2 : 1;
            return 3;
        }

        if (cs.InsufficientMaterial(primary)) return 4;    // Check for insufficient material
        if (occured_positions.Count >= 100) return 6;       // Check for 50-move rule

        if (occured_positions.Count > 0) {                  // Check for 3-fold repetition
            int cnt = 0, last = occured_positions.Count - 1;
            ulong curr_pos = occured_positions[last];
            foreach (ulong pos in occured_positions)
                if (pos == curr_pos) cnt++;
            if (cnt >= 3) return 5;
        }

        if (tmr.clock_black <= 0) return 7;                 // Check if black lost on time
        if (tmr.clock_white <= 0) return 8;                 // Check if white lost on time

        return -1;
    }

    public void
    StartNewGame(char to_white)
    {
        bot_color = to_white == 'c' ? 'w' : 'b';                // Set bot color

        game_pgn.ClearList();                                  // Clear last match pgn
        occured_positions.Clear();                              // Clear occured pos from last game

        primary.LoadFromFEN(StartPosition);                     // Load Starting pos. to chessBoard class
        vMoves = cs.mg.GenerateMoves(ref primary);                 // Generate all legal moves in current pos.
        tmr.ClockReset(primary.pColor);                        // Reset clock for next match
        tmr.Init();
        StartCoroutine(BotInput());                             // Ready to request moves from bots
    }

    public void
    GameEndings(int state)
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

        gameEnd.GetComponent<TMPro.TextMeshProUGUI>().text = res;
        gameEnd.SetActive(true);

    }

    #endregion

    #region GamePlay

    private void
    ReceiveMove()
    {
        bot_move = bot.engine_move;
        bot_eval = (float)bot.engine_eval;
    }

    private int
    GetMoves()
    {
        vMoves = cs.mg.GenerateMoves(ref primary);     // Generate & store all legal moves in current pos.
        int state = IsGameOver();                 // Check if the game is Over
        return state;
    }

    private IEnumerator
    BotInput()
    {
        if (primary.pColor == bot_color)
        {
            float time_left = (bot_color == 1) ? tmr.clock_white : tmr.clock_black;
            bot.Play(ref primary, game_pgn.GetLastMove());

            // Wait till the bot make a move
            StartCoroutine(bot.ReadOutputCoroutine());
            yield return new WaitUntil(() => bot.engine_move != 0);
        }

        ReceiveMove();
        ValidateMove();                                         // Validate the move played
    }

    private void
    NextTurn(int move)
    {
        tmr.SwitchPlayer();                        // Switch side to play
        tmr.ClockFreeze();                         // Freeze the clocks

        // Store move, time_left & evaluation
        float time_left = primary.pColor == 1 ? tmr.clock_white : tmr.clock_black;
        game_pgn.Add(primary.pColor, move, bot_eval, time_left);

        primary.MakeMove(move);                     // Make Move on board
        AddToTable(primary.GenerateHashKey(ref cs.HashIndex), move);   // Add to occured pos. Table

        bh.BoardReset(false);                      // Reset Physical Board
        bh.MarkPlayedMove(move);                    // Mark the move played on board
        bh.Recreate(ref primary);                   // Create the new position on physical board

        vMoves = cs.mg.GenerateMoves(ref primary);     // Generate & store all legal moves in current pos.
        int state = IsGameOver();                 // Check if the game is Over

        if (state != -1) {
            // Someone has lost, immediately cut the game
            GameEndings(state);
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
        yield return new WaitForSeconds(0.2f);
        FindObjectOfType<Timer>().ClockUnfreeze();     // Unfreeze the clocks
        StartCoroutine(BotInput());                     // Ask the bot to make a move
    }

    #endregion
}
