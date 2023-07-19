using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class Core : MonoBehaviour
{
    [HideInInspector] public MoveGenerator mg;
    [HideInInspector] public List<ulong> HashIndex;
    public Dictionary<ulong, List<int>> OpeningBook;

    public string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";


    #region Utility

    public void
    Init()
    {
        GetHashKeys();
        GetOpeningBook();
    }

    private void
    GetHashKeys()
    {
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

    public int
    Convert(float _x)
    {
        bool neg = false;
        if (_x < 0) {
            neg = true;
            _x *= -1;
        }

        int res = (int)(_x + 0.5f);

        if (neg) res *= -1;
        return res;
    }

    public string
    RemoveNonAlphaNumeric(string text)
    {
        string result = string.Empty;
        foreach (char ch in text)
        {
            if (char.IsLetterOrDigit(ch) || (ch == '_') || (ch == '-') || (ch == '.'))
                result += ch;
        }
        return result;
    }

    #endregion

    #region OpeningBook

    private void
    GetOpeningBook()
    {
        OpeningBook = new Dictionary<ulong, List<int>>();
        string[] array = File.ReadAllLines(Application.streamingAssetsPath + "/Utility/Opening Book.txt");
        for (int i = 0; i < array.Length; i++)
        {
            string[] array2 = array[i].Split();
            if (array2[0] == "")
                break;
            ulong key = System.Convert.ToUInt64(array2[0]);
            List<int> list = new List<int>();
            for (int j = 1; j < array2.Length; j++) {
                list.Add(System.Convert.ToInt32(array2[j]));
            }
            OpeningBook[key] = list;
        }
    }

    public bool
    PositionInOpeningBook(ref ChessBoard __pos)
    {
        ulong key = __pos.GenerateHashKey(ref HashIndex);

        return OpeningBook.ContainsKey(key) ?
            PositionValidityCheck(key, ref __pos) : false;
    }

    private bool
    PositionValidityCheck(ulong key, ref ChessBoard __pos)
    {
        List<int> moves_from_book = OpeningBook[key];
        MoveList move_list = mg.GenerateMoves(ref __pos);

        foreach (int move in moves_from_book)
            if (move_list.ContainsMove(move) == false) return false;

        return true;
    }
    
    public int
    PlayBookMove(ref ChessBoard __pos)
    {
        ulong key = __pos.GenerateHashKey(ref HashIndex);
        List<int> moves = OpeningBook[key];

        int random_index = Random.Range(0, moves.Count - 1);
        return moves[random_index];
    }

    #endregion

    #region Engine

    public bool InsufficientMaterial(ChessBoard _cb)
    {
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

    #endregion

}
