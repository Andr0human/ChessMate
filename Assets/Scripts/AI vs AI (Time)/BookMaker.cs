using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class BookMaker : MonoBehaviour {

    [SerializeField] private MoveGenerator mg;
    [SerializeField] private BoardHandler bh;
    private ChessBoard primary = new ChessBoard();
    private string startPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    [SerializeField] private int alloted_run_time = 10;
    [SerializeField] private int moveCount_threshold = 10;
    private int positions_searched;
    private List<ulong> HashIndex;
    private Dictionary<ulong, List<int>> Book;
    private Stopwatch sw = new Stopwatch();

    public System.Collections.IEnumerator Add_To_Book() {

        HashSet<string> hashSet;
        hashSet = new HashSet<string>();
        sw.Reset();
        sw.Start();
        positions_searched = 0;
        primary.LoadFromFEN(startPosition);
        string path = System.Environment.CurrentDirectory + "/bb_engine.txt";
        File.WriteAllText(path, "");
        int mCount = 0;

        while (true) {

            while (true) {
                int move = PlayBookMove(ref primary);
                if (move == 0) break;
                primary.MakeMove(move);
                bh.Recreate(ref primary);
                yield return new WaitForSeconds(0.02f);
                mCount++;
            }

            string pos = primary.FENGenerator();

            if (!hashSet.Contains(pos)) {
                positions_searched++;
                if (mCount < moveCount_threshold) {
                    print("Adding To Table");
                    hashSet.Add(pos);
                    File.AppendAllText(path, pos + "\n");
                }
            }

            primary.LoadFromFEN(startPosition);
            yield return new WaitForSeconds(0.1f);
            mCount = 0;
            if ((int)sw.Elapsed.TotalSeconds > alloted_run_time) break;

        }
        print("Positions Searched : " + positions_searched.ToString());
    }

    private MoveList GenerateMoves(ref ChessBoard cb) {
        MoveList moveList = new MoveList(cb.pColor);
        mg.Set_piece(ref cb);
        moveList.pColor = cb.pColor;
        mg.Generate_AttackedSquares(ref cb);
        KAinfo kAinfo = mg.FindkingAttackers(ref cb);
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

    private int PlayBookMove(ref ChessBoard _cb) {
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
            if (!moveList.Is_present(current)) return false;
        }
        return true;
    }

    public void GetOpeningBook() {
        HashIndex = GetHashKeys();
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

    private List<ulong> GetHashKeys() {
        List<ulong> list = new List<ulong>();
        string[] array = File.ReadAllLines(Application.streamingAssetsPath + "/Utility/RandomNumbers.txt");
        for (int i = 0; i < array.Length; i++) {
            string[] array2 = array[i].Split();
            for (int j = 0; j < 10; j++) {
                list.Add(System.Convert.ToUInt64(array2[j]));
            }
        }
        return list;
    }

    public List<int> GetRandomOpening() {
        List<int> opening = new List<int>();

        ChessBoard board = new ChessBoard();
        board.LoadFromFEN(startPosition);

        while (true) {
            int move = PlayBookMove(ref board);
            if (move == 0) break;
            opening.Add(move);
            board.MakeMove(move);
        }

        return opening;
    }

}
