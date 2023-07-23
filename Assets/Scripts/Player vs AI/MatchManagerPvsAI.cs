/* using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

class MatchManagerPvsAI : MonoBehaviour
{
    public  Core  cs;
    public Timer tmr;

    public GameObject EndScreen;

    public IEnumerator
    StartNewGame(int human_color)
    {
        cs.Data = new MatchData();
        cs.Side2Move = 0;
        cs.BoardPosition = new ChessBoard(cs.StartFen);

        cs.EndState = -1;

        cs.Players[human_color] = new HumanPlayer();
        cs.Players[human_color ^ 1] = new ChessEngine("bot", cs.StartFen, true, false);

        tmr.Init(cs.Side2Move);
        yield return StartCoroutine( PlayGame() );
    }

    private IEnumerator
    PlayGame()
    {
        while ((cs.EndState = cs.IsGameOver()) == -1)
        {
            // Let player make his move
            yield return StartCoroutine( cs.RequestMove() );

            // Time runs out before player making a move
            if (cs.TimeLeftForSearch() == false)
                break;

            // Retrieve player move
            var (move, eval) = cs.Players[cs.Side2Move].GetResults();

            // Update board elements after making move
            yield return StartCoroutine( cs.UpdateBoardElements(move, eval) );

            // Switch sides and next turn
            cs.Side2Move ^= 1;
        }

        tmr.ClockFreeze();

        if (cs.Players[0] != null) cs.Players[0].Stop();
        if (cs.Players[1] != null) cs.Players[1].Stop();

        GameOverScreen(cs.EndState);
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
} */