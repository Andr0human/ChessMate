using System.Collections.Generic;
using UnityEngine;
using System;
using System.IO;


public static class TT
{
    public static ulong[] HashIndex;

    static ulong x = 1237;

    static ulong xorshift64star()
    {
        x ^= x >> 12;
        x ^= x << 25;
        x ^= x >> 27;
        return x * 0x2545F4914F6CDD1DUL;
    }


    public static void
    Init()
    {
        HashIndex = new ulong[860];

        //! TODO -> Starting Seed

        for (int i = 0; i < 860; i++)
            HashIndex[i] = xorshift64star();
    }
}


public class MoveList
{
    public int KingAttackers;
    public int pColor, moveCount;
    public ulong startIndex;
    public ulong[] endIndex;

    public
    MoveList(int pc = 0)
    {
        KingAttackers = moveCount = 0;
        pColor = pc;
        startIndex = 0;
        endIndex = new ulong[64];
    }

    public void
    Add(int idx, ulong val)
    {
        if (val != 0)
        {
            startIndex |= 1UL << (idx);
            endIndex[idx] |= val;
            moveCount++;
        }
    }

    public void
    Clear()
    {
        startIndex = 0;
        moveCount = KingAttackers = 0;
        for (int i = 0; i < 64; i++) endIndex[i] = 0;
    }

    public bool
    ValidInitSquare(int ip)
    { return (startIndex & (1UL << ip)) != 0; }

    public bool
    ValidDestSquare(int ip, int fp)
    {
        return (  startIndex & (1UL << ip)) != 0
            && (endIndex[ip] & (1UL << fp)) != 0;
    }

    public bool
    ContainsMove(int move)
    {
        int ip = move & 63, fp = (move >> 6) & 63;
        if ((startIndex & (1UL << ip)) != 0)
            if ((endIndex[ip] & (1UL << fp)) != 0) return true;
        return false;
    }
};


public class KAinfo
{
    public int attackers = 0;
    public ulong area, ppos;

    public void Add(ulong tmp, ulong tmp2)
    {
        attackers++;
        area = tmp;
        ppos = tmp2;
    }
};


public class MatchData
{
    private List<int> moves;
    private List<float> evals;
    private List<float> time_left;
    private List<ulong> occured_positions;


    public MatchData()
    {
        moves = new List<int>();
        evals = new List<float>();
        time_left = new List<float>();
        occured_positions = new List<ulong>();
    }

    public void
    Add(int move, float eval, float r_time, ulong key)
    {
        moves.Add(move);
        evals.Add(eval);
        time_left.Add(r_time);

        // If moved piece is a pawn or there is a captured piece
        if ((((move >> 12) & 7) == 1) || (((move >> 15) & 7) != 0))
            occured_positions.Clear();

        occured_positions.Add(key);
    }

    public int
    LastPlayedMove()
    { return (moves.Count > 0) ? (moves[moves.Count - 1]) : (-1); }

    public bool
    FiftyMoveRuleDraw()
    { return occured_positions.Count >= 100; }

    public bool
    ThreeMoveRepetitionDraw()
    {
        if (occured_positions.Count < 3)
            return false;
        ulong last_key = occured_positions[occured_positions.Count - 1];
        int count = 0;

        foreach (var key in occured_positions)
            if (key == last_key) count++;
        
        return count >= 3;
    }

    public int
    DifferentEvalCount(float margin)
    {
        int count = 0;
        for (int i = 1; i < evals.Count; i += 2)
        {
            float eval_diff = Mathf.Abs(evals[i] - evals[i - 1]);
            float max_eval = Mathf.Max(Mathf.Abs(evals[i]), Mathf.Abs(evals[i - 1]));

            if ((eval_diff >= margin) && (max_eval <= 12f)) count++;
        }
        return count;
    }

    public (float, float)
    LastEvalPair()
    {
        int n = evals.Count;
        if (n < 2)
            return (0f, 0f);
        
        return (evals[n - 2], evals[n - 1]);
    }

    public bool
    DrawnPositionForContinuousMoves(float draw_margin, int length)
    {
        if (evals.Count < length)
            return false;
        
        int count = 0;
        foreach (float eval in evals)
        {
            count = (Mathf.Abs(eval) < draw_margin) ? (count + 1) : (0);

            if (count >= length)
                return true;

            if (count + (length - evals.Count) < length)
                return false;
        }

        return false;
    }
}


class ArenaScoreSheet
{
    private string engine1;
    private string engine2;

    private int prediction_attempt;
    private int prediction_success;

    List<int> results;

    public ArenaScoreSheet(string __engine1, string __engine2)
    {
        engine1 = __engine1;
        engine2 = __engine2;

        prediction_attempt = prediction_success = 0;
        results = new List<int>();
    }


    public void
    Add(int result, int prediction)
    {
        results.Add(result);

        if (prediction != 0)
        {
            prediction_attempt++;
            if (prediction == result)
                prediction_success++;
        }
    }

    private (int, int)
    CalculateWins(int win_value)
    {
        int count1 = 0, count2 = 0;

        for (int i = 0; i < results.Count; i += 2)
            if (results[i] == win_value) count1++;
        
        for (int i = 1; i < results.Count; i += 2)
            if (results[i] == -win_value) count2++;
        
        return (count1, count2);
    }

    public void
    PrintArenaResult()
    {
        var ( e1_wins_w,  e1_wins_b) = CalculateWins(1);
        var ( e2_wins_w,  e2_wins_b) = CalculateWins(-1);
        var (e1_draws_w, e1_draws_b) = CalculateWins(0);

        int  e1_wins_t =  e1_wins_w +  e1_wins_b;
        int  e2_wins_t =  e2_wins_w +  e2_wins_b;
        int e1_draws_t = e1_draws_w + e1_draws_b;

        string file_path = Application.streamingAssetsPath + "/arena/results.txt";

        string result_str = "Results => ";

        foreach (var res in results)
            result_str += res.ToString() + " ";

        File.WriteAllText(file_path,
            "####     Arena RESULTS     #####\n"
            + "Games played : " + (results.Count).ToString() + "\n"
            + engine1 + " vs " + engine2 + "\n"
            + "White => | Wins : " + e1_wins_w + " | Draws : " + e1_draws_w + " | Loss : " + e2_wins_b + " |\n"
            + "Black => | Wins : " + e1_wins_b + " | Draws : " + e1_draws_b + " | Loss : " + e2_wins_w + " |\n"
            + "Total => | Wins : " + e1_wins_t + " | Draws : " + e1_draws_t + " | Loss : " + e2_wins_t + " |\n\n"
            + "Prediction Accuracy => " + prediction_success.ToString() + "/" +  prediction_attempt.ToString() + "\n\n"
            + result_str
        );

        //! TODO ... Prediction Accuracy + loss_on_time.
    }
}

