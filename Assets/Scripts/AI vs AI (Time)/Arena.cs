using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;

class PlayerData
{
    string engine_name;
    // [Game_number % 2] in which the player is white
    int my_white;

    int wins_with_white, loss_with_white, draws_with_white;
    int wins_with_black, loss_with_black, draws_with_black;

    int loss_on_time;

    public
    PlayerData(string __engine_name, int __my_white)
    {
        engine_name      = __engine_name;
        my_white         = __my_white;
        wins_with_black  = wins_with_white = 0;
        loss_with_black  = loss_with_white = 0;
        draws_with_black = draws_with_white = 0;
    }

    public void
    AddEntry(int game_no, int result, int state)
    {
        if (game_no % 2 == my_white)
        {
            if (result == 1) wins_with_white++;
            else if (result == 2) draws_with_white++;
            else if (result == -1) {
                if (state == 8) loss_on_time++;
                loss_with_white++;
            }
            return;
        }
        if (result == -1) wins_with_black++;
        else if (result == 2) draws_with_black++;
        else if (result == 1) {
            if (state == 7) loss_on_time++;
            loss_with_black++;
        }
    }

    public string
    ShowWhite()
    {
        string res = "| Wins : " + wins_with_white.ToString() + " | Loss : " + loss_with_white.ToString();
        res += " | Draws : " + draws_with_white.ToString() + " |";
        return res;
    }

    public string
    ShowBlack()
    {
        string res = "| Wins : " + wins_with_black.ToString() + " | Loss : " + loss_with_black.ToString();
        res += " | Draws : " + draws_with_black.ToString() + " |";
        return res;
    }

    public string
    ShowTotal()
    {
        string res = "| Wins : " + (wins_with_white + wins_with_black).ToString();
        res += " | Loss : " + (loss_with_white + loss_with_black).ToString();
        res += " | Draws : " + (draws_with_white + draws_with_black).ToString() + " |";
        return res;
    }

    public int
    Score()
    {
        return 2 * (wins_with_black + wins_with_white)
            + draws_with_white + draws_with_black;
    }

    public string
    name()
    { return engine_name; }

    public int
    games_loss_on_time()
    { return loss_on_time; }
};

class ScoreSheet
{
    List<Vector2Int> scores;
    string engine1;
    string engine2;
    List<int> engine1_wins, engine2_wins;

    public
    ScoreSheet(string __engine1, string __engine2)
    {
        engine1 = __engine1;
        engine2 = __engine2;
        scores = new List<Vector2Int>();
        engine1_wins = new List<int>();
        engine2_wins = new List<int>();
    }

    public void
    Add(int score1, int score2, int game_no, int game_res)
    {
        scores.Add(new Vector2Int(score1, score2));

        if (game_res == 1)
        {
            if (game_no % 2 == 1) engine1_wins.Add(game_no);
            else engine2_wins.Add(game_no);
        }
        else if (game_res == -1)
        {
            if (game_no % 2 == 0) engine1_wins.Add(game_no);
            else engine2_wins.Add(game_no);
        }
    }

    public void
    PrintScoreSheet()
    {
        string path = Application.streamingAssetsPath + "/arena/ScoreSheet.txt";

        File.WriteAllText(path, "Score Sheet : \n");

        File.AppendAllText(path, engine1 + " Wins : ");
        foreach (int i in engine1_wins)
            File.AppendAllText(path, i.ToString() + " ");
        File.AppendAllText(path, "\n");

        File.AppendAllText(path, engine2 + " Wins : ");
        foreach (int i in engine2_wins)
            File.AppendAllText(path, i.ToString() + " ");
        File.AppendAllText(path, "\n");

        File.AppendAllText(path, engine1 + " : ");
        foreach (Vector2Int i in scores)
            File.AppendAllText(path, i.x.ToString() + " ");
        File.AppendAllText(path, "\n");
        
        File.AppendAllText(path, engine2 + " : ");
        foreach (Vector2Int i in scores)
            File.AppendAllText(path, i.y.ToString() + " ");
        File.AppendAllText(path, "\n");
    }

};

class PgnData
{
    string event_name, site, date;
    string white, black;
    string round, result;

    string engine1, engine2;

    public
    PgnData(string __engine1, string __engine2)
    {
        engine1 = __engine1;
        engine2 = __engine2;
        event_name = "ChessEngine update Testing!";
        site = "?";
        System.DateTime theTime = System.DateTime.Now;
        date = theTime.Year + "." + theTime.Month + "." + theTime.Day;
    }

    public void
    CreateEntry(int game_no, int res)
    {
        round = game_no.ToString();
        white = game_no % 2 == 1 ? engine1 : engine2;
        black = game_no % 2 == 1 ? engine2 : engine1;

        if (res == 1) result = "White Wins";
        else if (res == -1) result = "Black Wins";
        else result = "Draw";
    }

    public void
    Print(string status)
    {
        string path = Application.streamingAssetsPath + "/arena/Games/game" + round + " " + status + ".pgn";
        File.WriteAllText(path, "");

        File.AppendAllText(path, "[Event \""  + event_name + "\"]\n");
        File.AppendAllText(path, "[Site \""   + site   + "\"]\n");
        File.AppendAllText(path, "[Date \""   + date   + "\"]\n");
        File.AppendAllText(path, "[Round \""  + round  + "\"]\n");
        File.AppendAllText(path, "[White \""  + white  + "\"]\n");
        File.AppendAllText(path, "[Black \""  + black  + "\"]\n");
        File.AppendAllText(path, "[Result \"" + result + "\"]\n");
    }

};

public class Arena : MonoBehaviour
{
    public Engine_AvA_Time ce;
    [SerializeField] Core cs;
    [SerializeField] private int games_to_be_played = 10;
    private int game_num = 1;
    private int prediction_attempt, prediction_success;

    public TextMeshProUGUI Game_field, Time_field;
    public TextMeshProUGUI curr_game_text, Est_time_text;

    PlayerData PlayerEngine1;
    PlayerData PlayerEngine2;

    ScoreSheet sheet;
    PgnData extra_pgn;

    Stopwatch sw = new Stopwatch();
    List<int> RandomOpening;

    public void
    NextGame()
    {
        if (game_num > games_to_be_played)
        {
            ArenaOver();
            return;
        }

        curr_game_text.text = "Game Number : " + game_num.ToString();
        if (game_num % 2 == 1)
        {
            RandomOpening = FindObjectOfType<BookMaker>().GetRandomOpening();
            ce.StartNewGame(PlayerEngine1.name(), PlayerEngine2.name(), RandomOpening);
        }
        else
        {
            ce.StartNewGame(PlayerEngine2.name(), PlayerEngine1.name(), RandomOpening);
        }
    }

    public void
    EndingReached(int game_res, int state, int p_res)
    {
        PlayerEngine1.AddEntry(game_num, game_res, state);
        PlayerEngine2.AddEntry(game_num, game_res, state);

        sheet.Add(PlayerEngine1.Score(), PlayerEngine2.Score(), game_num, game_res);

        if (p_res != 0)
        {
            prediction_attempt++;
            if (p_res == game_res) prediction_success++;
        }
        int res = IsInterestingGame(p_res, game_res);
        PrintInterestingGame(res, game_res);
        DisplayEstimatedTime();
        if (game_num != 0 && game_num % 2 == 0) {
            PrintArenaResult();
            sheet.PrintScoreSheet();
        }
        game_num++;
        StartCoroutine(SpendTime());
    }

    private void
    ArenaOver()
    {
        curr_game_text.text = "Games completed!";
        sw.Stop();
        PrintArenaResult();
    }

    private void
    PrintArenaResult() {
        string path = Application.streamingAssetsPath + "/arena/log.txt";

        File.WriteAllText(path,
            "####     Arena INFO    #####\n" +
            "Games played : " + games_to_be_played.ToString()
            + "\n\n" + PlayerEngine1.name() + " Stats :"
            + "\nPlaying with White -- \n" + PlayerEngine1.ShowWhite()
            + "\nPlaying with Black -- \n" + PlayerEngine1.ShowBlack()
            + "\nTotal -- \n"              + PlayerEngine1.ShowTotal()
            + "\nScore -> "                + PlayerEngine1.Score().ToString()
            + "\n\n" + PlayerEngine2.name() + " Stats : \n"
            + "Playing with White -- \n"   + PlayerEngine2.ShowWhite()
            + "\nPlaying with Black -- \n" + PlayerEngine2.ShowBlack()
            + "\nTotal --\n"               + PlayerEngine2.ShowTotal()
            + "\nScore -> "                + PlayerEngine2.Score().ToString()
            + "\n\nPrediction Acc. -> "    + prediction_success.ToString()
            + " / " + prediction_attempt.ToString() + "\n"
        );

        File.AppendAllText(path,
            PlayerEngine1.name() + " losses in time : " + PlayerEngine1.games_loss_on_time().ToString() +
            PlayerEngine2.name() + " losses in time : " + PlayerEngine2.games_loss_on_time().ToString() +
            "\nAvg. Game Time : " + ((float)sw.Elapsed.TotalSeconds / game_num).ToString() + " sec."
        );
    }

    private int
    IsInterestingGame(int _pr, int game_res) {
        // A Game is Interesting if :

        // Win-Loss Prediction Failed
        if (_pr != 0 && Mathf.Abs(_pr) == 1 && _pr != game_res) return 1;

        // Draw prediction Failed
        if (_pr != 0 && Mathf.Abs(_pr) == 2 && _pr != game_res) return 2;

        // If huge evaluation difference in more than 3 places in a game
        float eval_cutoff = 1.5f;
        if (ce.DifferentEvals(eval_cutoff) > 5) return 3;

        // Game ended with huge material on board
        int weight_cutoff = 4800;
        if (ce.primary.PositionWeight() > weight_cutoff) return 4;

        // If is one of first six games
        if (game_num <= 4) return 0;

        return -1;
    }

    public void
    PrintInterestingGame(int _res, int game_res)
    {
        //if (_res == -1) return;

        string status = "";

        if (_res == 1) status = "(Win-Loss Failed Prediction)";
        else if (_res == 2) status = "(Draw Failed Prediction)";
        else if (_res == 3) status = "(Diff Evals)";
        else if (_res == 4) status = "(Huge Material)";

        string number = game_num.ToString();
        string path1 = Application.streamingAssetsPath + "/arena/Games/game" + number + " " + status + ".pgn";
        string path2 = Application.streamingAssetsPath + "/arena/Evals/Eval" + number + " " + status + ".txt";
        string path3 = Application.streamingAssetsPath + "/arena/Time/Time" + number + " " + status + ".txt";

        List<Vector2Int> moveList = ce.game_pgn.GetPgn();
        ChessBoard tmp_board = new ChessBoard();
        tmp_board.LoadFromFEN(cs.StartPosition);

        // Printing PGN to a new File

        extra_pgn.CreateEntry(game_num, game_res);
        extra_pgn.Print(status);
        for (int i = 0; i < moveList.Count; i++) {
            int x = moveList[i].x, y = moveList[i].y;
            string text2 = (i + 1).ToString() + " ";
            if (x != 0) {
                text2 += FindObjectOfType<MoveGenerator>().PrintMove(x, ref tmp_board);
                text2 += " ";
                tmp_board.MakeMove(x);
            }
            if (y != 0) {
                text2 += FindObjectOfType<MoveGenerator>().PrintMove(y, ref tmp_board);
                tmp_board.MakeMove(y);
            }
            text2 += "\n";
            File.AppendAllText(path1, text2);
        }

        // Printing Evaluations to a new File
        List<Vector2> data = ce.game_pgn.GetEval();
        File.WriteAllText(path2, "");
        foreach (Vector2 eval in data)
            File.AppendAllText(path2, eval.x.ToString() + " " + eval.y.ToString() + "\n");

        // Printing Time_left List to a new File
        data = ce.game_pgn.GetTime();
        File.WriteAllText(path3, "");
        foreach (Vector2 t_time in data)
            File.AppendAllText(path3, t_time.x.ToString() + " " + t_time.y.ToString() + "\n");

        return;
    }

    public string
    PrintTime(double _x)
    {
        string res;
        ulong dur_sec = (ulong)_x;
        ulong hr = dur_sec / 3600;
        dur_sec %= 3600;
        ulong mn = dur_sec / 60, sec = dur_sec % 60;
        res = hr.ToString() + " hr, " + mn.ToString() + " min, " + sec.ToString() + " sec.";
        return res;
    }

    private IEnumerator
    SpendTime()
    {
        yield return new WaitForSeconds(2);
        NextGame();
    }

    private void
    DisplayEstimatedTime()
    {
        double num = sw.Elapsed.TotalSeconds / game_num;
        double est_time = num * (games_to_be_played - game_num);
        Est_time_text.text = "Est. Time Left : " + PrintTime(est_time);
    }

    public void SetSampleSize() {
        string text = Game_field.text;
        games_to_be_played = int.Parse(text);
    }

    public void SetTimeFormat()
    {
        string[] array = Time_field.text.Split();
        int x = 60, y = 0;
        if (array.Length == 0) return;
        if (array.Length >= 1) x = int.Parse(array[0]);
        if (array.Length >= 2) y = int.Parse(array[1]);
        FindObjectOfType<Timer>().SetTime(x, y);
    }

    public void InitializeArena()
    {
        GameObject.Find("Game Amount").SetActive(false);
        GameObject.Find("Time Format").SetActive(false);

        PlayerEngine1 = new PlayerData("elsa_v1", 1);
        PlayerEngine2 = new PlayerData("elsa_v2", 0);

        sheet =  new ScoreSheet(PlayerEngine1.name(), PlayerEngine2.name());
        extra_pgn = new PgnData(PlayerEngine1.name(), PlayerEngine2.name());

        sw.Reset();
        sw.Start();
        NextGame();
    }

}

