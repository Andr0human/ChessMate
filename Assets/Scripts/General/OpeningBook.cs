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

    public Dictionary<ulong, List<int>> Book;
    public List<List<int>> openingLines;
    private int openingLineNum = 0;

    private int
    UciToEncodeMove(string uci_move, ref ChessBoard pos)
    {
        // Opening Book does not have promotions, so need to code for it
        int Index(char row, char col) => (int)(row - 'a') + (int)(col - '1') * 8;

        int ip = Index(uci_move[0], uci_move[1]);
        int fp = Index(uci_move[2], uci_move[3]);

        int ipt = pos.board[ip] & 7;
        int fpt = pos.board[fp] & 7;

        int  pos_bits = (fp << 6) | ip;
        int type_bits = (fpt << 15) | (ipt << 12);
        int color_bit = pos.color << 20;

        return pos_bits | type_bits | color_bit;
    }


    public void
    GetOpeningLines(string file_name)
    {
        string[] lines = File.ReadAllLines(Application.streamingAssetsPath + "/Utility/" + file_name);
        openingLines = new List<List<int>>();

        foreach (string line in lines)
        {
            string[] moves = line.Split();
            List<int> currentLine = new List<int>();
            ChessBoard board = new ChessBoard(StartFen);

            foreach (string move in moves)
            {
                int e_move = UciToEncodeMove(move, ref board);
                currentLine.Add(e_move);
                board.MakeMove(e_move);
            }
            openingLines.Add(currentLine);
        }
    }


    public List<int>
    NextOpeningLine()
    {
        int index = openingLineNum;
        openingLineNum = (openingLineNum + 1) % openingLines.Count;
        return openingLines[index];
    }


    public void
    GetOpeningBook()
    {
        Book = new Dictionary<ulong, List<int>>();
        string[] lines = File.ReadAllLines(Application.streamingAssetsPath + "/Utility/Opening Book.txt");

        foreach (string line in lines)
        {
            string[] moves = line.Split();
            ChessBoard position = new ChessBoard(StartFen);

            foreach (string move in moves)
            {
                ulong key = position.hashvalue;
                if (!Book.ContainsKey(key))
                    Book[key] = new List<int>();

                int e_move = UciToEncodeMove(move, ref position);

                if (Book[key].Contains(e_move) == false)
                    Book[key].Add(e_move);
                position.MakeMove(e_move);
            }
        }
        UnityEngine.Debug.Log("Opening Book Generated!");
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

}