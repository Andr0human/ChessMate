using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using TMPro;
using UnityEngine;


public class Arena : MonoBehaviour
{
    [SerializeField] Core cs;
    [SerializeField] MatchManagerAIvsAI mm;


    [SerializeField] private int GamesToPlay = 10;

    private int CurrentGameNum = 1;

    public TextMeshProUGUI GameAmountField;
    public TextMeshProUGUI TimeFormatField;
    public TextMeshProUGUI EngineNameField;
    public TextMeshProUGUI CurrentGameNumText;
    public TextMeshProUGUI EstimateTimeText;

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
    PrintIfInterestingGame(int result, int prediction)
    {
        int value = InterestingGame(result, prediction);
        if (value == -1)
            return;
        
        //! TODO ... print game, eval, time in a file
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
        EstimateTimeText.text = "Estimated Time Left : " + timeString;
    }

    public void
    SetSampleSize()
    {
        string text = GameAmountField.text;
        text = cs.RemoveNonAlphaNumeric(text);

        if (text.Length == 0)
            return;

        GamesToPlay = int.Parse(text);
    }

    public void
    SetTimeFormat()
    {
        string[] values = TimeFormatField.text.Split();

        float time_per_side = 60, increment = 0;

        if (values.Length == 0)
            return;

        if (values.Length >= 1)
            time_per_side = float.Parse(cs.RemoveNonAlphaNumeric( values[0] ));

        if (values.Length >= 2)
            increment = float.Parse(cs.RemoveNonAlphaNumeric( values[1] ));

        FindObjectOfType<Timer>().SetTime(time_per_side, increment);
    }

    public void
    SetEngines()
    {
        ArenaEngines = new string[2];
        string[] names = EngineNameField.text.Split();

        if (names.Length < 2)
            return;
        
        ArenaEngines[0] = cs.RemoveNonAlphaNumeric( names[0] );
        ArenaEngines[1] = cs.RemoveNonAlphaNumeric( names[1] );
    }


    public void
    InitArena()
    {
        GameObject.Find("Game Amount").SetActive(false);
        GameObject.Find("Time Format").SetActive(false);
        GameObject.Find("Engine Names").SetActive(false);

        ScoreSheet = new ArenaScoreSheet(ArenaEngines[0], ArenaEngines[1]);
        sw = new Stopwatch();

        StartCoroutine( PlayArena() );
    }

    private void
    UpdateArenaElements(int s2s, int end_result, int end_state, int prediction)
    {
        // Match Ended
        ScoreSheet.Add(end_result, end_state, prediction);

        // Display time to complete all remaining games
        DisplayEstimatedTime();

        // Print Results when new game pair starts
        if (s2s == 1)
        {
            ScoreSheet.PrintArenaResult();
            //! TODO ... ScoreSheet.PrintScoreList();
        }
    }


    public IEnumerator
    PlayArena()
    {
        List<int> RandomOpening = new List<int>();
        int side2start = 0;
        sw.Reset();

        while (CurrentGameNum <= GamesToPlay)
        {
            // Set Current Game Number Text on Board
            CurrentGameNumText.text = "Game Number : " + CurrentGameNum.ToString();

            if (side2start == 0)
                RandomOpening = FindObjectOfType<BookMaker>().GetRandomOpening();

            // Start Match
            yield return StartCoroutine( mm.StartNewGame(
                ArenaEngines[side2start], ArenaEngines[side2start ^ 1], RandomOpening )
            );

            UpdateArenaElements(side2start, mm.EndResult, mm.EndState, mm.EndPrediction);

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

