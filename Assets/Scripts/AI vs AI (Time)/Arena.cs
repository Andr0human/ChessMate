using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;

class Player_data {

    int wins_with_white, loss_with_white, draws_with_white;
    int wins_with_black, loss_with_black, draws_with_black;

    public Player_data() {
        wins_with_black = wins_with_white = 0;
        loss_with_black = loss_with_white = 0;
        draws_with_black = draws_with_white = 0;
    }

    public void Add_entry(char color, int result) {
        if (color == 'w') {
            if (result == 1) wins_with_white++;
            else if (result == 2) draws_with_white++;
            else if (result == -1) loss_with_white++;
            return;
        }
        if (result == -1) wins_with_black++;
        else if (result == 2) draws_with_black++;
        else if (result == 1) loss_with_black++;
        return;
    }

    public string Show_white() {
        string res = "| Wins : " + wins_with_white.ToString() + " | Loss : " + loss_with_white.ToString();
        res += " | Draws : " + draws_with_white.ToString() + " |";
        return res;
    }

    public string Show_black() {
        string res = "| Wins : " + wins_with_black.ToString() + " | Loss : " + loss_with_black.ToString();
        res += " | Draws : " + draws_with_black.ToString() + " |";
        return res;
    }

    public string Show_total() {
        string res = "| Wins : " + (wins_with_white + wins_with_black).ToString();
        res += " | Loss : " + (loss_with_white + loss_with_black).ToString();
        res += " | Draws : " + (draws_with_white + draws_with_black).ToString() + " |";
        return res;
    }

    public string Score() {
        int points = 2 * (wins_with_black + wins_with_white);
        points += draws_with_white + draws_with_black;
        string res = points.ToString();
        return res;
    }

    public int Score_int() {
        int points = 2 * (wins_with_black + wins_with_white);
        points += draws_with_white + draws_with_black;
        return points;
    }

};

class Score_Sheet {

    List<Vector2Int> scores;
    List<int> elsa_wins, cb_wins;

    public Score_Sheet() {
        scores = new List<Vector2Int>();
        elsa_wins = new List<int>();
        cb_wins = new List<int>();
    }

    public void Add(int elsa, int cb, int game_no, int res, char elsa_cl, char cb_cl) {
        scores.Add(new Vector2Int(elsa, cb));

        if (res == 1) {
            if (elsa_cl == 'w') elsa_wins.Add(game_no);
            if (cb_cl == 'w') cb_wins.Add(game_no);
        }

        if (res == -1) {
            if (elsa_cl == 'b') elsa_wins.Add(game_no);
            if (cb_cl == 'b') cb_wins.Add(game_no);
        }
    }

    public void Print_ScoreSheet() {
        string path = Application.streamingAssetsPath + "/arena/ScoreSheet.txt";

        File.WriteAllText(path, "Score Sheet : \n");
        File.AppendAllText(path, "Elsa Wins : ");
        foreach (int i in elsa_wins)
            File.AppendAllText(path, i.ToString() + " ");
        File.AppendAllText(path, "\n");

        File.AppendAllText(path, "Master Wins : ");
        foreach (int i in cb_wins)
            File.AppendAllText(path, i.ToString() + " ");
        File.AppendAllText(path, "\n");

        File.AppendAllText(path, "Elsa : ");
        foreach (Vector2Int i in scores)
            File.AppendAllText(path, i.x.ToString() + " ");
        File.AppendAllText(path, "\n");
        File.AppendAllText(path, "Master : ");
        foreach (Vector2Int i in scores)
            File.AppendAllText(path, i.y.ToString() + " ");
        File.AppendAllText(path, "\n");

        return;
    }

};

class PGN_DATA {

    string event_name, site, date;
    string white, black;
    string round, result;

    public PGN_DATA() {
        event_name = "ElsaBot update Testing!";
        site = "?";
        System.DateTime theTime = System.DateTime.Now;
        date = theTime.Year + "." + theTime.Month + "." + theTime.Day;
    }

    public void CreateEntry(int game_num, int res) {
        round = game_num.ToString();
        white = game_num % 2 == 1 ? "Elsa" : "Master";
        black = game_num % 2 == 0 ? "Elsa" : "Master";

        if (res == 1) result = "White Wins";
        else if (res == -1) result = "Black Wins";
        else result = "Draw";

    }

    public void Print(string status) {
        string path = Application.streamingAssetsPath + "/arena/Games/game" + round + " " + status + ".pgn";
        File.WriteAllText(path, "");

        File.AppendAllText(path, "[Event \""  + event_name + "\"]\n");
        File.AppendAllText(path, "[Site \""   + site   + "\"]\n");
        File.AppendAllText(path, "[Date \""   + date   + "\"]\n");
        File.AppendAllText(path, "[Round \""  + round  + "\"]\n");
        File.AppendAllText(path, "[White \""  + white  + "\"]\n");
        File.AppendAllText(path, "[Black \""  + black  + "\"]\n");
        File.AppendAllText(path, "[Result \"" + result + "\"]\n");

        return;
    }

};

public class Arena : MonoBehaviour {

    public Engine_AvA_Time ce;
    [SerializeField] Core cs;
    [SerializeField] private int games_to_be_played = 10;
    private int game_num = 1;
    private int prediction_attempt, prediction_success;

    public TextMeshProUGUI Game_field, Time_field;
    public TextMeshProUGUI curr_game_text, Est_time_text;

    Player_data Elsa = new Player_data(), CB = new Player_data();
    Score_Sheet sheet = new Score_Sheet();
    PGN_DATA extra_pgn = new PGN_DATA();
    Stopwatch sw = new Stopwatch();
    List<int> Opening = new List<int>();

    public void Next_game() {
        if (game_num > games_to_be_played) {
            Arena_over();
            return;
        }
        char to_white = game_num % 2 == 1 ? 'e' : 'c';
        curr_game_text.text = "Game Number : " + game_num.ToString();

        if (game_num % 2 == 1)
            Opening = FindObjectOfType<BookMaker>().GetRandomOpening();
        ce.Start_new_game(to_white, Opening);
    }

    public void Ending_reached(int game_res, int p_res) {

        char elsa_color = game_num % 2 == 1 ? 'w' : 'b';
        char cb_color = game_num % 2 == 1 ? 'b' : 'w';

        Elsa.Add_entry(elsa_color, game_res);
        CB.Add_entry(cb_color, game_res);
        sheet.Add(Elsa.Score_int(), CB.Score_int(), game_num, game_res, elsa_color, cb_color);

        if (p_res != 0) {
            prediction_attempt++;
            if (p_res == game_res) prediction_success++;
        }
        int res = Is_interesting_game(p_res, game_res);
        Print_interesting_game(res, game_res);
        Display_estimated_time();
        if (game_num != 0 && game_num % 2 == 0) {
            Print_arena_result();
            sheet.Print_ScoreSheet();
        }
        game_num++;
        StartCoroutine(SpendTime());
    }

    private void Arena_over() {
        curr_game_text.text = "Games completed!";
        sw.Stop();
        Print_arena_result();
    }

    private void Print_arena_result() {
        string path = Application.streamingAssetsPath + "/arena/log.txt";
        File.WriteAllText(path, "####     Arena INFO    #####\n" +
            "Games played : " + games_to_be_played.ToString() + "\n\nElsa Stats :\n" +
            "Playing with White -- \n" + Elsa.Show_white() + "\nPlaying with Black -- \n"
            + Elsa.Show_black() + "\n" + "Total -- \n" + Elsa.Show_total() + "\nScore -> "
            + Elsa.Score() + "\n\n" + "Chessbot Stats : \nPlaying with White -- \n"
            + CB.Show_white() + "\nPlaying with Black -- \n" + CB.Show_black() + "\nTotal --\n"
            + CB.Show_total() + "\nScore -> " + CB.Score() + "\n\nPrediction Acc. -> "
            + prediction_success.ToString() + " / " + prediction_attempt.ToString() + "\n");

        File.AppendAllText(path, "Elsa losses in time : " + ce.elsa_loss_on_time.ToString() +
            "\nCB losses in time : " + ce.cb_loss_on_time.ToString() + "\nAvg. Game Time : " +
            ((float)sw.Elapsed.TotalSeconds / game_num).ToString() + " sec.");
    }

    private int Is_interesting_game(int _pr, int game_res) {
        // A Game is Interesting if :

        // Win-Loss Prediction Failed
        if (_pr != 0 && Mathf.Abs(_pr) == 1 && _pr != game_res) return 1;

        // Draw prediction Failed
        if (_pr != 0 && Mathf.Abs(_pr) == 2 && _pr != game_res) return 2;

        // If huge evaluation difference in more than 3 places in a game
        float eval_cutoff = 1.5f;
        if (ce.Different_evals(eval_cutoff) > 5) return 3;

        // Game ended with huge material on board
        int weight_cutoff = 5000;
        if (ce.cs.MaterialCount(ref ce.primary) > weight_cutoff) return 4;

        // If is one of first six games
        if (game_num <= 4) return 0;

        return -1;
    }

    public void Print_interesting_game(int _res, int game_res) {

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

        List<Vector2Int> moveList = ce.game_pgn.get_pgn();
        ChessBoard tmp_board = new ChessBoard();
        tmp_board.LoadFromFEN(cs.startPosition);

        // Printing PGN to a new File

        extra_pgn.CreateEntry(game_num, game_res);
        extra_pgn.Print(status);
        for (int i = 0; i < moveList.Count; i++) {
            int x = moveList[i].x, y = moveList[i].y;
            string text2 = (i + 1).ToString() + " ";
            if (x != 0) {
                text2 += FindObjectOfType<MoveGenerator>().Print_move(x, ref tmp_board);
                text2 += " ";
                tmp_board.MakeMove(x);
            }
            if (y != 0) {
                text2 += FindObjectOfType<MoveGenerator>().Print_move(y, ref tmp_board);
                tmp_board.MakeMove(y);
            }
            text2 += "\n";
            File.AppendAllText(path1, text2);
        }

        // Printing Evaluations to a new File
        List<Vector2> data = ce.game_pgn.get_eval();
        File.WriteAllText(path2, "");
        foreach (Vector2 eval in data)
            File.AppendAllText(path2, eval.x.ToString() + " " + eval.y.ToString() + "\n");

        // Printing Time_left List to a new File
        data = ce.game_pgn.get_time();
        File.WriteAllText(path3, "");
        foreach (Vector2 t_time in data)
            File.AppendAllText(path3, t_time.x.ToString() + " " + t_time.y.ToString() + "\n");

        return;
    }

    public string Print_time(double _x) {
        string res;
        ulong dur_sec = (ulong)_x;
        ulong hr = dur_sec / 3600;
        dur_sec %= 3600;
        ulong mn = dur_sec / 60, sec = dur_sec % 60;
        res = hr.ToString() + " hr, " + mn.ToString() + " min, " + sec.ToString() + " sec.";
        return res;
    }

    private IEnumerator SpendTime() {
        yield return new WaitForSeconds(2);
        Next_game();
    }

    private void Display_estimated_time() {
        double num = sw.Elapsed.TotalSeconds / game_num;
        double est_time = num * (games_to_be_played - game_num);
        Est_time_text.text = "Est. Time Left : " + Print_time(est_time);
    }

    private int ToNum(string res) {
        int num = 0;
        for (int i = 0; i < res.Length; i++) {
            if ('0' <= res[i] & res[i] <= '9') {
                int x = res[i] - '0';
                num = 10 * num + x;
            }
        }
        return num;
    }

    public void Set_sample_size() {
        string text = Game_field.text;
        games_to_be_played = ToNum(text);
    }

    public void Set_time_format() {
        string[] array = Time_field.text.Split();
        int x = 60, y = 0;
        if (array.Length == 0) return;
        if (array.Length >= 1) x = ToNum(array[0]);
        if (array.Length >= 2) y = ToNum(array[1]);
        FindObjectOfType<Timer>().Set_time(x, y);
    }

    public void InitializeArena() {
        GameObject.Find("Game Amount").SetActive(false);
        GameObject.Find("Time Format").SetActive(false);
        sw.Reset();
        sw.Start();
        Next_game();

    }

}

