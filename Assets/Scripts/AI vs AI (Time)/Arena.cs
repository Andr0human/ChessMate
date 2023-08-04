using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;


public class Arena : MonoBehaviour
{
    [SerializeField] private MatchManager mm;

    public  int    GamesToPlay = 10;
    private int CurrentGameNum = 1;

    [HideInInspector] public float FixedTimePerGame = 30f;
    [HideInInspector] public float IncrementPerGame =  1f;

    public bool fixedTimePerMove = false;
    public bool      Adjournment = false;

    public TextMeshProUGUI CurrentGameNumText;
    public TextMeshProUGUI  RemainingTimeText;

    public string[] ArenaEngines;
    private ArenaScoreSheet ScoreSheet;
    private Stopwatch sw;

    private string
    InterestingGame(int result, int state, int prediction)
    {
        // Prediction made and failed
        if ((prediction != 0) && (result != prediction))
            return "win-loss prediction-failed";

        // If huge evaluation difference in more than 5 places in a game
        if (mm.Data.DifferentEvalCount(3f) > 5)
            return "eval-diff";
        
        // Game ended with huge material on board
        int weight_cutoff = 4000;
        if (mm.BoardPosition.PositionWeight() > weight_cutoff)
            return "huge material";

        // If is one of first six games
        if (CurrentGameNum <= 4)
            return "first games";

        if (state == 5)
            return "draw by 3-move repetition";

        return "";
    }

    private void
    PrintIfInterestingGame(int result, int state, int prediction)
    {
        string value = InterestingGame(result, state, prediction);

        if (value == "") return;
        
        //! TODO ... print time in a file

        string dir_games = Application.streamingAssetsPath + "/arena/Games/";
        string dir_evals = Application.streamingAssetsPath + "/arena/Evals/";
        string game_no = CurrentGameNum.ToString();

        // Create the directory if it does not exist
        if (!Directory.Exists(dir_games))
            Directory.CreateDirectory(dir_games);
        
        if (!Directory.Exists(dir_evals))
            Directory.CreateDirectory(dir_evals);

        string path_pgn  = dir_games + "game" + game_no + "_" + value + ".pgn";
        string path_eval = dir_evals + "eval" + game_no + "_" + value;

        string played_moves = mm.Data.GetMoveList(mm.mg);
        string move_evals = mm.Data.GetMovesEval();

        string pgn = ScoreSheet.GeneratePgnPreData(CurrentGameNum, result) + played_moves;

        File.WriteAllText(path_pgn ,        pgn);
        File.WriteAllText(path_eval, move_evals);
    }


    private int
    GetResultFromState(int state, int prediction)
    {
        // Game Ends in Adjournment
        if (Adjournment && (prediction != 0))
            return prediction;

        // Game ended normally (one side wins)
        if (state == 1 || state == 7) return  1;
        if (state == 2 || state == 8) return -1;
        
        // Game ends in a draws
        return 0;
    }

    //! Test remaining time left.
    private void
    DisplayEstimatedTime()
    {
        double avg_game_time = sw.Elapsed.TotalSeconds / CurrentGameNum;
        double est_time = avg_game_time * (GamesToPlay - CurrentGameNum);

        int seconds = (int)est_time;
        int hours = seconds / 3600;
        int minutes = (seconds % 3600) / 60;
        int remainingSeconds = seconds % 60;

        string timeString = $"{hours} hr, {minutes} min, {remainingSeconds} secs";
        RemainingTimeText.text = timeString;
    }


    private void
    UpdateArenaElements(int s2s, int end_state, int prediction)
    {
        int end_result = GetResultFromState(end_state, prediction);

        // Update Wins, Loss, Draw
        ScoreSheet.Add(end_result, prediction);

        // Display time to complete all remaining games
        DisplayEstimatedTime();

        // Print Game pgn if found interesting
        PrintIfInterestingGame(end_result, end_state, prediction);

        // Print Results when new game pair starts
        if (s2s == 1)
        {
            ScoreSheet.PrintArenaResult();
            //! TODO ... ScoreSheet.PrintScoreList();
        }
    }


    public void
    InitArena()
    {
        ScoreSheet = new ArenaScoreSheet(ArenaEngines[0], ArenaEngines[1]);
        sw = new Stopwatch();

        CurrentGameNumText.gameObject.SetActive(true);
        RemainingTimeText.gameObject.SetActive(true);

        StartCoroutine( PlayArena() );
    }

    //! TODO Set Adjournment

    public IEnumerator
    PlayArena()
    {
        List<int> opening_moves = new List<int>();
        int side2start = 0;
        sw.Reset();
        sw.Start();

        while (CurrentGameNum <= GamesToPlay)
        {
            // Set Current Game Number Text on Board
            CurrentGameNumText.text = "Game Number : " + CurrentGameNum.ToString();

            if (side2start == 0)
                opening_moves = FindObjectOfType<OpeningBook>().GetRandomOpening();

            GameObject.FindObjectOfType<Timer>().SetTime(FixedTimePerGame, IncrementPerGame);

            // Play Current Match
            yield return StartCoroutine( mm.StartNewGame(
                ArenaEngines[side2start], ArenaEngines[side2start ^ 1], opening_moves,
                fixedTimePerMove, false, Adjournment
            ));

            UpdateArenaElements(side2start, mm.EndState, mm.EndPrediction);

            // To next game
            CurrentGameNum++;
            side2start ^= 1;

            // Wait before starting next game
            yield return new WaitForSeconds(3f);
        }

        // All games ended
        CurrentGameNumText.text = "Games completed!";
        sw.Stop();
    }
}

