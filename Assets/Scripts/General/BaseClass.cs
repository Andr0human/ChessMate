using System.Collections.Generic;
using UnityEngine;
using System;


public class MoveList {
    public int KingAttackers;
    public int pColor, moveCount;
    public ulong startIndex;
    public ulong[] endIndex;

    public MoveList(int pc = 0) {
        KingAttackers = moveCount = 0;
        pColor = pc;
        startIndex = 0;
        endIndex = new ulong[64];
    }

    public void Add(int idx, ulong val) {
        if (val != 0) {
            startIndex |= 1UL << (idx);
            endIndex[idx] |= val;
            moveCount++;
        }
    }

    public void Clear() {
        startIndex = 0;
        moveCount = KingAttackers = 0;
        for (int i = 0; i < 64; i++) endIndex[i] = 0;
    }

    public bool InitialKey(int ip) {
        if ((startIndex & (1UL << ip)) != 0) return true;
        return false;
    }

    public bool StartEndPair(int ip, int fp) {
        if ((startIndex & (1UL << ip)) != 0)
            if ((endIndex[ip] & (1UL << fp)) != 0) return true;
        return false;
    }

    public bool ContainsMove(int move) {
        int ip = move & 63, fp = (move >> 6) & 63;
        if ((startIndex & (1UL << ip)) != 0)
            if ((endIndex[ip] & (1UL << fp)) != 0) return true;
        return false;
    }


};

public class KAinfo {
    public int attackers = 0;
    public ulong area, ppos;

    public void Add(ulong tmp, ulong tmp2) {
        attackers++;
        area = tmp;
        ppos = tmp2;
    }

};


public class PGN
{
    private readonly List<Vector2Int> playlist;
    private readonly List<Vector2> evals;
    private readonly List<Vector2> time_left;
    private readonly List<int> playedMoves;

    public
    PGN()
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
    ClearList() {
        playlist.Clear();
        evals.Clear();
        time_left.Clear();
        playedMoves.Clear();
    }

    public int MoveCountFull()
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

    public int GetLastMove()
    {
        if (playedMoves.Count == 0) return 0;
        return playedMoves[playedMoves.Count - 1];
    }

    public List<int> GetPlayedMoves()
    { return playedMoves; }
}
