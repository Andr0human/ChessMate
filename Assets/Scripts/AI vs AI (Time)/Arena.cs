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


    private int
    InterestingGame(int result, int prediction)
    {
        // Prediction made and failed
        if ((prediction != 0) && (result != prediction))
            return Mathf.Abs(prediction);

        // If huge evaluation difference in more than 5 places in a game
        if (mm.Data.DifferentEvalCount(3f) > 5)
            return 3;
        
        // Game ended with huge material on board
        int weight_cutoff = 4000;
        if (mm.BoardPosition.PositionWeight() > weight_cutoff) return 4;

        // If is one of first six games
        if (CurrentGameNum <= 4) return 0;

        return -1;
    }


    private void
    PrintIfInterestingGame()
    {
        // int value = InterestingGame(result, prediction);
        // string value = InterestingGame(result, prediction);
        // if (value == "")
        //     return;
        
        // //! TODO ... print game, eval, time in a file

        // string dir = Application.streamingAssetsPath + "/arena/";
        // string game_no = CurrentGameNum.ToString();

        // string path_pgn = dir + "Games/game" + game_no + "_" + value + ".pgn";
        

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
        //! TODO PrintIfInterestingGame(end_result, prediction);

        // Print Results when new game pair starts
        if (s2s == 1)
        {
            ScoreSheet.PrintArenaResult();
            //! TODO ... ScoreSheet.PrintScoreList();
        }
    }

    // 


    public void
    InitArena()
    {
        ScoreSheet = new ArenaScoreSheet(ArenaEngines[0], ArenaEngines[1]);
        sw = new Stopwatch();

        CurrentGameNumText.gameObject.SetActive(true);
        RemainingTimeText.gameObject.SetActive(true);

        StartCoroutine( PlayArena() );
    }


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

            //! TODO GetRandomOpening
            // if (side2start == 0)
            //     opening_moves = FindObjectOfType<OpeningBook>().GetRandomOpening();

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
            UnityEngine.Debug.Log("Waited for 3 seconds before next game.");
        }

        // All games ended
        CurrentGameNumText.text = "Games completed!";
        sw.Stop();
    }
}

