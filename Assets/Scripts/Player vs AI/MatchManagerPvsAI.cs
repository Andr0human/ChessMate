using UnityEngine;
using System.Collections;

class MatchManagerPvsAI : MonoBehaviour
{
    [SerializeField] private         Core  cs;
    [SerializeField] private BoardHandler  bh;
    [SerializeField] private        Timer tmr;

    public  GameObject EndScreen;
    private ChessBoard BoardPosition;
    private MatchData Data;

    private IPlayer[] Players;
    private int Side2Move;

    private string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";


    private void
    Start()
    {
        cs.Init();
        BoardPosition = new ChessBoard();
        BoardPosition.LoadFromFEN(StartFen);
        bh.InitializeBoard(ref BoardPosition);
    }


    public void
    StartNewGame(int human_color)
    {
        Data = new MatchData();
        Side2Move = 0;
        BoardPosition.LoadFromFEN(StartFen);

        Players = new IPlayer[2];
        Players[human_color] = new HumanPlayer();
        Players[human_color ^ 1] = new ChessEngine("bot", StartFen, true);

        tmr.Init(Side2Move);
        StartCoroutine( PlayGame() );
    }

    #region GAME-OVER

    private int
    IsGameOver()
    {
        // Refer to GameOverScreen() for various-states.

        MoveList moveslist = cs.mg.GenerateMoves(ref BoardPosition);

        // checkmate/stalemate check
        if (moveslist.moveCount == 0)
            return (moveslist.KingAttackers > 0) ? (Side2Move ^ 1) : 3;

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


    #endregion


    private IEnumerator
    PlayGame()
    {
        int state;

        while ((state = IsGameOver()) == -1)
        {
            // PlayNextMove();
            Players[Side2Move].Play(ref BoardPosition, Data.LastPlayedMove());
            StartCoroutine( Players[Side2Move].ReadOutputCoroutine() );
            yield return new WaitUntil(() => Players[Side2Move].MoveMade());
            
            var (move, eval) = Players[Side2Move].GetResults();

            // UnityEngine.Debug.Log(Side2Move + " " + move + " " + eval);

            UpdateBoardElements(move, eval);

            // Next Turn
            yield return new WaitForSeconds(0.2f);
            tmr.ClockUnfreeze();
            Side2Move ^= 1;
        }

        GameOverScreen(state);
    }


    private void
    PlayNextMove()
    {
        Players[Side2Move].Play(ref BoardPosition, Data.LastPlayedMove());
        StartCoroutine( Players[Side2Move].ReadOutputCoroutine() );
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
    }
}