using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EngineTrain : MonoBehaviour {

    #region Variables

    [SerializeField] private Core cs;
    [SerializeField] private TrainBot mb;
    [SerializeField] private BoardHandler bh;

    [HideInInspector] public int ip, fp;
    public ChessBoard primary = new ChessBoard();
    public MoveList vMoves;
    public PGN game_pgn;
    public string startPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    public GameObject gameEnd;

    public int bot_color;
    [SerializeField] private GameObject promotion_table;
    private List<ulong> occured_positions;
    private int bot_move;

    // For Debugging
    private float bot_eval;

    #endregion

    #region Board

    public void Start_new_game(char to_white) {

        bot_color = to_white == 'e' ? 1 : -1;                   // Set bot color
        mb.InitializeBot(startPosition);
        game_pgn = new PGN();                                   // Clear last match pgn
        occured_positions = new List<ulong>();                  // Clear occured pos from last game

        primary.LoadFromFEN(startPosition);                     // Load Starting pos. to chessBoard class
        vMoves = cs.GenerateMoves(ref primary);                 // Generate all legal moves in current pos.
        StartCoroutine(BotInput());                             // Ready to request moves from bots

    }

    void Add_to_table(ulong key, int __move) {
        int pt = (__move >> 12) & 7, cpt = (__move >> 15) & 7;
        if (pt == 1 || cpt != 0)
            occured_positions.Clear();
        occured_positions.Add(key);
    }

    #endregion

    #region USER

    public bool AvailableMoves(Vector2Int pos) {
        return vMoves.Initial_Key(8 * pos.y + pos.x);
    }

    public void Show_endSquares(Vector2Int pos) {
        bh.Board_reset(true);
        bh.Board_highLight(vMoves.endIndex[8 * pos.y + pos.x], ref primary);
    }

    public void Request_board_reset() {
        bh.Board_reset(true);
    }

    public void ValidateMove(bool request_by_human = false) {

        if (primary.pColor == bot_color) {
            if (request_by_human) return;
            Next_turn(bot_move);
            return;
        }

        if (!vMoves.Start_end_pair(ip, fp)) return;

        ulong Rank18 = 18374686479671623935;
        int pmove, pColor = primary.pColor;
        pmove = ((primary.board[fp] * -pColor) << 15) ^ ((primary.board[ip] * pColor) << 12) ^ (fp << 6) ^ ip;
        if (pColor == 1) pmove |= 1 << 20;


        if (primary.board[ip] == 1 && ((1UL << fp) & Rank18) != 0) {
            // If move moved is pawn and final square is 1st or 8th Rank
            StartCoroutine(Ask_for_promoted_piece(pmove));
            return;
        }

        // Condition for promotion

        Next_turn(pmove);
    }

    private IEnumerator Ask_for_promoted_piece(int _move) {

        Vector3 invalid_point = new Vector3(100f, 100f, 100f);
        FindObjectOfType<UserInput_PvsAI>().vector = invalid_point;
        FindObjectOfType<UserInput_PvsAI>().castle_in_place = true;
        promotion_table.SetActive(true);
        Vector3 MousePos;

        while (true) {
            MousePos = FindObjectOfType<UserInput_PvsAI>().vector;
            if (MousePos != invalid_point)
                break;
            yield return new WaitForSeconds(0f);
        }

        Vector2Int pos = new Vector2Int(cs.Convert(MousePos.x), cs.Convert(MousePos.y));

        int pp = -1;

        if (primary.pColor == 1 && pos.x == -2) {
            if (pos.y == 7) pp = 3;
            else if (pos.y == 6) pp = 0;
            else if (pos.y == 5) pp = 1;
            else if (pos.y == 4) pp = 2;
        }
        else if (primary.pColor == -1) {

        }

        FindObjectOfType<UserInput_PvsAI>().castle_in_place = false;
        promotion_table.SetActive(false);
        if (pp != -1) {
            _move |= pp << 18;
            Next_turn(_move);
        }
        else {
            Request_board_reset();
        }
    }

    #endregion

    #region Endings

    private int Is_Game_Over() {
        /***
         *  States (-1 -> Game not Over)
         * 1 -> White won by checkmate
         * 2 -> Black won by checkmate
         * 3 -> Draw by stalemate
         * 4 -> Draw by insufficient material
         * 5 -> Draw by 3-fold repetition
         * 6 -> Draw by 50-move rule
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

        return -1;
    }

    public void GameEndings(int state) {
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

    private void ReceiveMove() {
        bot_move = mb.received_move;
        bot_eval = (float)mb.received_move_eval;
    }

    private int GetMoves() {
        vMoves = cs.GenerateMoves(ref primary);     // Generate & store all legal moves in current pos.
        int state = Is_Game_Over();                 // Check if the game is Over
        return state;
    }

    private IEnumerator BotInput() {
        mb.received_move = 0;
        if (primary.pColor == bot_color) {
            mb.MakeMove(ref primary, game_pgn.get_last_move());
            // Wait till the bot make a move
            yield return new WaitUntil(() => mb.received_move != 0);
        } else {
            mb.Ponder(ref primary);
        }

        ReceiveMove();
        ValidateMove();                                         // Validate the move played
    }

    private void Next_turn(int move) {

        // Store move, time_left & evaluation
        game_pgn.Add(primary.pColor, move, bot_eval, 0);

        primary.MakeMove(move);                     // Make Move on board
        Add_to_table(primary.Generate_HashKey(ref cs.HashIndex), move);   // Add to occured pos. Table

        bh.Board_reset(false);                      // Reset Physical Board
        bh.MarkplayedMove(move);                    // Mark the move played on board
        bh.Recreate(ref primary);                   // Create the new position on physical board

        vMoves = cs.GenerateMoves(ref primary);     // Generate & store all legal moves in current pos.
        int state = Is_Game_Over();                 // Check if the game is Over

        if (state != -1) {
            // Someone has lost, immediately cut the game
            GameEndings(state);
        }
        else {
            // Game is still left, request for next move
            StartCoroutine(Ask_For_Next_Move());
        }
        return;
    }

    private IEnumerator Ask_For_Next_Move() {
        yield return new WaitForSeconds(0.2f);
        StartCoroutine(BotInput());                     // Ask the bot to make a move
    }

    #endregion

}
