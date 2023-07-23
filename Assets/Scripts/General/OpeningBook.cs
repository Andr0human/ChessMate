using UnityEngine;
using System.IO;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;

public class OpeningBook : MonoBehaviour
{
    [SerializeField] private  BoardHandler bh;
    [SerializeField] private MoveGenerator mg;

    private string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    [SerializeField] private int alloted_run_time = 10;
    [SerializeField] private int moveCount_threshold = 10;

    public Dictionary<ulong, List<int>> Book;

    public void
    GetOpeningBook()
    {
        Book = new Dictionary<ulong, List<int>>();
        string[] array = File.ReadAllLines(Application.streamingAssetsPath + "/Utility/Opening Book.txt");
        for (int i = 0; i < array.Length; i++)
        {
            string[] array2 = array[i].Split();
            if (array2[0] == "")
                break;
            ulong key = System.UInt64.Parse(array2[0]);
            List<int> list = new List<int>();
            for (int j = 1; j < array2.Length; j++) {
                list.Add(int.Parse(array2[j]));
            }
            Book[key] = list;
        }
    }

    public bool
    PositionInOpeningBook(ref ChessBoard __pos)
    {
        ulong key = __pos.GenerateHashKey();

        return Book.ContainsKey(key) ?
            PositionValidityCheck(key, ref __pos) : false;
    }

    private bool
    PositionValidityCheck(ulong key, ref ChessBoard __pos)
    {
        List<int> moves_from_book = Book[key];
        MoveList move_list = mg.GenerateMoves(ref __pos);

        foreach (int move in moves_from_book)
            if (move_list.ContainsMove(move) == false) return false;

        return true;
    }
    
    public int
    PlayBookMove(ref ChessBoard __pos)
    {
        ulong key = __pos.GenerateHashKey();
        List<int> moves = Book[key];

        int random_index = UnityEngine.Random.Range(0, moves.Count - 1);
        return moves[random_index];
    }


    // Generate Opening Book

    public IEnumerator
    AddToBook()
    {
        HashSet<string> hashSet = new HashSet<string>();

        // Initialize Stopwatch
        Stopwatch sw = new Stopwatch();
        sw.Reset();
        sw.Start();

        ChessBoard primary = new ChessBoard(StartFen);
        
        string path = System.Environment.CurrentDirectory + "/bb_engine.txt";
        File.WriteAllText(path, "");


        int positions_searched = 0;
        int mCount = 0;

        while (true)
        {
            while (true)
            {
                if (PositionInOpeningBook(ref primary) == false)
                    break;

                int move = PlayBookMove(ref primary);
                primary.MakeMove(move);
                bh.Recreate(ref primary);
                yield return new WaitForSeconds(0.02f);
                mCount++;
            }

            string pos = primary.Fen();

            if (!hashSet.Contains(pos))
            {
                positions_searched++;
                if (mCount < moveCount_threshold)
                {
                    print("Adding To Table");
                    hashSet.Add(pos);
                    File.AppendAllText(path, pos + "\n");
                }
            }

            primary.LoadFromFEN(StartFen);
            yield return new WaitForSeconds(0.1f);
            mCount = 0;
            if ((int)sw.Elapsed.TotalSeconds > alloted_run_time) break;

        }
        print("Positions Searched : " + positions_searched.ToString());
    }

    public List<int>
    GetRandomOpening()
    {
        List<int> opening = new List<int>();
        ChessBoard board = new ChessBoard(StartFen);

        while (true)
        {
            if (PositionInOpeningBook(ref board) == false)
                break;
            
            int move = PlayBookMove(ref board);
            opening.Add(move);
            board.MakeMove(move);
        }

        return opening;
    }
}