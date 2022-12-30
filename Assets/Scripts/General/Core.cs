using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Core : MonoBehaviour {

    [SerializeField] private MoveGenerator mg;
    [HideInInspector] public List<ulong> HashIndex;
    public Dictionary<ulong, List<int>> Book;

    public string startPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

    #region MoveGenerator

    public MoveList GenerateMoves(ref ChessBoard cb) {
        MoveList moveList = new MoveList(cb.pColor);
        mg.Set_piece(ref cb);
        mg.Generate_AttackedSquares(ref cb);
        KAinfo kAinfo = mg.FindkingAttackers(ref cb);
        moveList.pColor = cb.pColor;
        moveList.KingAttackers = kAinfo.attackers;
        if (kAinfo.attackers == 0) {
            mg.Ka_zero_pieceMovement(ref cb, ref moveList);
        }
        if (kAinfo.attackers == 1) {
            mg.Ka_pieceMovement(ref cb, ref moveList, ref kAinfo);
        }
        mg.KingMoves(ref cb, ref moveList);
        return moveList;
    }

    public int Position_Weight(ref ChessBoard _cb) {
        int res = 0;

        res += 100 * _cb.PpCnt(_cb.Pieces[6] | _cb.Pieces[8]);
        res += 320 * _cb.PpCnt(_cb.Pieces[5] | _cb.Pieces[9]);
        res += 300 * _cb.PpCnt(_cb.Pieces[4] | _cb.Pieces[10]);
        res += 500 * _cb.PpCnt(_cb.Pieces[3] | _cb.Pieces[11]);
        res += 925 * _cb.PpCnt(_cb.Pieces[2] | _cb.Pieces[12]);

        return res;
    }

    #endregion

    #region Utility

    public void init() {
        GetHashKeys();
        GetOpeningBook();
    }

    private void GetHashKeys() {
        HashIndex = new List<ulong>();
        string path = Application.streamingAssetsPath + "/Utility/RandomNumbers.txt";
        string[] nums = File.ReadAllLines(path);
        foreach (string i in nums) {
            string[] res = i.Split();
            for (int j = 0; j < 10; j++) {
                HashIndex.Add(System.Convert.ToUInt64(res[j]));
            }
        }
    }

    private void GetOpeningBook() {
        Book = new Dictionary<ulong, List<int>>();
        string[] array = File.ReadAllLines(Application.streamingAssetsPath + "/Utility/Opening Book.txt");
        for (int i = 0; i < array.Length; i++) {
            string[] array2 = array[i].Split();
            if (array2[0] == "") {
                break;
            }
            ulong key = System.Convert.ToUInt64(array2[0]);
            List<int> list = new List<int>();
            for (int j = 1; j < array2.Length; j++) {
                list.Add(System.Convert.ToInt32(array2[j]));
            }
            Book[key] = list;
        }
    }

    public int PlayBookMove(ref ChessBoard _cb) {
        ulong key = _cb.Generate_HashKey(ref HashIndex);
        if (Book.ContainsKey(key)) {
            List<int> list = Book[key];
            if (Confirmatory_check(ref list, ref _cb)) {
                int index = Random.Range(0, list.Count - 1);
                return list[index];
            }
        }
        return 0;
    }

    private bool Confirmatory_check(ref List<int> arr, ref ChessBoard _cb) {
        MoveList moveList = GenerateMoves(ref _cb);
        foreach (int current in arr) {
            if (!moveList.Is_present(current)) {
                return false;
            }
        }
        return true;
    }

    public Vector2 Decide_Time(float time_left, float increment, ref ChessBoard _cb) {
        int mins_left = (int)time_left / 60;

        float base_time, full_time, mn, extra_sec;
        float max_moves = 30f, max_weight = 8000f;
        float weight = Position_Weight(ref _cb);
        float moves_to_go = max_moves - ((max_weight - weight) / 500 * 1.5f);

        if (increment == 0) {
            moves_to_go *= 1.75f;
            base_time = time_left / moves_to_go;
            mn = Mathf.Min(12f, 2.5f * mins_left + 2);
            full_time = time_left / (moves_to_go - mn);
            extra_sec = full_time - base_time;
            if (time_left < 15f) extra_sec = 0;
            return new Vector2(base_time, extra_sec);
        }

        moves_to_go = max_moves - ((max_weight - weight) / 500f * 1.5f);
        float time_in_hand = time_left + increment;
        mn = Mathf.Min(12f, mins_left * (mins_left + 3));
        base_time = time_in_hand / moves_to_go;
        full_time = time_in_hand / (moves_to_go - mn);
        extra_sec = full_time - base_time;
        return new Vector2(base_time, extra_sec);

    }

    public int Convert(float _x) {
        bool neg = false;
        if (_x < 0) {
            neg = true;
            _x *= -1;
        }

        int res = (int)(_x + 0.5f);

        if (neg) res *= -1;
        return res;
    }

    #endregion

    #region Engine

    public bool Insufficient_material(ChessBoard _cb) {

        int wPawns = _cb.PpCnt(_cb.Pieces[8]), bPawns = _cb.PpCnt(_cb.Pieces[6]);
        int wBishops = _cb.PpCnt(_cb.Pieces[9]), bBishops = _cb.PpCnt(_cb.Pieces[5]);
        int wKnights = _cb.PpCnt(_cb.Pieces[10]), bKnights = _cb.PpCnt(_cb.Pieces[4]);
        int wRooks = _cb.PpCnt(_cb.Pieces[11]), bRooks = _cb.PpCnt(_cb.Pieces[3]);
        int wQueens = _cb.PpCnt(_cb.Pieces[12]), bQueens = _cb.PpCnt(_cb.Pieces[2]);
        int wPieces = wBishops + wKnights + wRooks + wQueens;
        int bPieces = bBishops + bKnights + bRooks + bQueens;

        if (wPawns + wPieces + bPawns + bPieces == 0) return true;
        if (wPawns > 0 || bPawns > 0) return false;

        if (wPieces == 1 && bPieces == 0)
            if (wBishops == 1 || wKnights == 1) return true;
        if (wPieces == 0 && bPieces == 1)
            if (bBishops == 1 || bKnights == 1) return true;

        if (wPieces == 1 && bPieces == 1)
            if ((wBishops == 1 || wKnights == 1) && (bBishops == 1 || bKnights == 1)) return true;

        if (wPieces + bPieces == 2)
            if (wKnights == 2 || bKnights == 2) return true;

        return false;
    }

    public int MaterialCount(ref ChessBoard _cb) {
        int weight = 0;
        weight += 100 * (_cb.PpCnt(_cb.Pieces[6]) + _cb.PpCnt(_cb.Pieces[8]));
        weight += 320 * (_cb.PpCnt(_cb.Pieces[5]) + _cb.PpCnt(_cb.Pieces[9]));
        weight += 320 * (_cb.PpCnt(_cb.Pieces[4]) + _cb.PpCnt(_cb.Pieces[10]));
        weight += 500 * (_cb.PpCnt(_cb.Pieces[3]) + _cb.PpCnt(_cb.Pieces[11]));
        weight += 925 * (_cb.PpCnt(_cb.Pieces[2]) + _cb.PpCnt(_cb.Pieces[12]));
        return weight;
    }

    #endregion

}
