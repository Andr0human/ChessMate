using System.Collections.Generic;
using UnityEngine;
using System;


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


public class Pgn
{
    private readonly List<Vector2Int> playlist;
    private readonly List<Vector2> evals;
    private readonly List<Vector2> time_left;
    private readonly List<int> playedMoves;

    public
    Pgn()
    {
        playlist = new List<Vector2Int>();
        evals = new List<Vector2>();
        time_left = new List<Vector2>();
        playedMoves = new List<int>();
    }

    public void
    Add(int cl, int move, float t_eval, float t_time, bool bot_move = true)
    {
        if (cl == 1)
        {
            playlist.Add(new Vector2Int(move, 0));
            evals.Add(new Vector2(t_eval, 0));
            time_left.Add(new Vector2(t_time, 0));
        }
        else
        {
            int index = playlist.Count - 1;
            if (index == -1)
            {
                playlist.Add(new Vector2Int(0, 0));
                evals.Add(new Vector2(0, 0));
                time_left.Add(new Vector2(t_time, 0));
                index = 0;
            }
            playlist[index] = new Vector2Int(playlist[index].x, move);
            evals[index] = new Vector2(evals[index].x, t_eval);
            time_left[index] = new Vector2(time_left[index].x, t_time);
        }
        if (bot_move) playedMoves.Add(move);
    }

    public void
    ClearList()
    {
        playlist.Clear();
        evals.Clear();
        time_left.Clear();
        playedMoves.Clear();
    }

    public int
    MoveCountFull()
    {
        int num = playlist.Count;
        if (playlist[num - 1].y == 0) num--;
        return num;
    }

    public List<Vector2Int> GetPgn()
    { return playlist; }

    public List<Vector2> GetEval()
    { return evals; }

    public List<Vector2> GetTime()
    { return time_left; }

    public Vector2 LastOfEval()
    { return evals[evals.Count - 1]; }

    public int
    DrawCounter(float margin)
    {
        int count = 0;
        foreach (var eval in evals)
            count = (eval.x < margin) && (eval.y < margin) ? (count + 1) : (0);
        return count;
    }


    public int GetLastMove()
    {
        if (playedMoves.Count == 0) return 0;
        return playedMoves[playedMoves.Count - 1];
    }

    public List<int> GetPlayedMoves()
    { return playedMoves; }
}


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
}

