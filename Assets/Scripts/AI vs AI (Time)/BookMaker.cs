using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class BookMaker : MonoBehaviour
{
    [SerializeField] private MoveGenerator mg;
    [SerializeField] private BoardHandler bh;
    [SerializeField] private Core cs;

    private ChessBoard primary = new ChessBoard();
    private string start_position = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    [SerializeField] private int alloted_run_time = 10;
    [SerializeField] private int moveCount_threshold = 10;
    private int positions_searched;
    private List<ulong> HashIndex;
    private Dictionary<ulong, List<int>> Book;
    private Stopwatch sw = new Stopwatch();

    public IEnumerator
    AddToBook()
    {
        HashSet<string> hashSet;
        hashSet = new HashSet<string>();
        sw.Reset();
        sw.Start();
        positions_searched = 0;
        primary.LoadFromFEN(start_position);
        string path = System.Environment.CurrentDirectory + "/bb_engine.txt";
        File.WriteAllText(path, "");
        int mCount = 0;

        while (true)
        {
            while (true)
            {
                if (cs.PositionInOpeningBook(ref primary) == false)
                    break;

                int move = cs.PlayBookMove(ref primary);
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

            primary.LoadFromFEN(start_position);
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

        ChessBoard board = new ChessBoard(start_position);

        while (true)
        {
            if (cs.PositionInOpeningBook(ref board) == false)
                break;
            
            int move = cs.PlayBookMove(ref board);
            opening.Add(move);
            board.MakeMove(move);
        }

        return opening;
    }

}
