using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEngine;

class ponder_move {

    public int move;
    public int eval;

    public int[] line;
    public string[] sline;

    public ponder_move(int val, int[] l, string[] sl) {
        eval = val;
        line = l;
        sline = sl;
    }

};

public class TrainBot : MonoBehaviour {

    #region Variables

    [SerializeField] private Core cs;

    private int self_color, made_move_count;

    [HideInInspector]
    public int received_move;

    [HideInInspector]
    public double received_move_eval;

    Process process;
    Process ponder_process;

    private Dictionary<int, ponder_move> pdata = new Dictionary<int, ponder_move>();

    #endregion

    #region UTILITY

    public void CreateEntry(Vector2 alloted_time, int last_move) {
        string path = Application.streamingAssetsPath + "/elsa_in.txt";

        string command;
        command = "-p ";
        if (last_move != 0)
            command += "-m " + last_move.ToString() + " ";
        command += "-tm " + alloted_time.x.ToString("0.##") + " " + alloted_time.y.ToString("0.##");
        command += " -e";

        File.WriteAllText(path, command);
        return;
    }

    private bool Get_Entry() {
        string path = Application.streamingAssetsPath + "/elsa_out.txt";
        FileStream logFile = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        StreamReader logFileReader = new StreamReader(logFile);
        string text = logFileReader.ReadLine();
        if (text == null) return false;

        string[] array = text.Split();
        int len = array.Length;
        if (array[len - 1] == "-out") {
            received_move = System.Convert.ToInt32(array[0]);
            received_move_eval = System.Convert.ToDouble(array[1]);
            File.WriteAllText(path, "");
            return true;
        }
        return false;

    }

    public void InitializeBot(string fen) {

        string in_path = Application.streamingAssetsPath + "/elsa_in.txt";
        string out_path = Application.streamingAssetsPath + "/elsa_out.txt";
        File.WriteAllText(in_path, "");
        File.WriteAllText(out_path, "");

        ProcessStartInfo startInfo = new ProcessStartInfo(Application.streamingAssetsPath + "/ElsaBot.exe") {
            WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = "play " + "\"" + fen + "\"",
            WorkingDirectory = Application.streamingAssetsPath,
        };
        process = Process.Start(startInfo);

    }

    public void StopBot() {
        if (!process.HasExited) process.Kill();
    }

    public void Init(char color, string fen) {
        made_move_count = 1;
        self_color = color == 'w' ? 1 : -1;
        InitializeBot(fen);
    }

    #endregion

    #region PLAY

    public void MakeMove(ref ChessBoard _cb, int last_move) {

        // if (_cb.pColor != self_color) return;

        if (!ponder_process.HasExited) {
            ponder_process.WaitForExit();
        }

        int _move = cs.PlayBookMove(ref _cb);
        if (_move != 0) {
            received_move = _move;
            received_move_eval = 0;
            return;
        }

        if (made_move_count % 30 == 0) {
            StopBot();
            InitializeBot(_cb.FENGenerator());
        }

        received_move = 0;
        received_move_eval = 0;
        made_move_count++;

        Vector2 alloted_time = new Vector2(1f, 0.5f);

        CreateEntry(alloted_time, last_move);
        StartCoroutine(ReceiveMove());

    }

    private IEnumerator ReceiveMove() {
        yield return new WaitUntil(Get_Entry);
    }

    #endregion

    #region PONDER_SEARCH

    public void Ponder(ref ChessBoard board) {

        ProcessStartInfo startInfo = new ProcessStartInfo(Application.streamingAssetsPath + "/ElsaBot.exe") {
            WindowStyle = ProcessWindowStyle.Normal,
            Arguments = "ponder " + "\"" + board.FENGenerator() + "\"",
            WorkingDirectory = Application.streamingAssetsPath,
        };
        ponder_process = Process.Start(startInfo);
        StartCoroutine(ReceivePonderData());
    }

    private IEnumerator ReceivePonderData() {
        yield return new WaitUntil(() => ponder_process.HasExited);

        string path = Application.streamingAssetsPath + "/elsa_out.txt";
        FileStream logFile = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        StreamReader logFileReader = new StreamReader(logFile);

        int N = int.Parse(logFileReader.ReadLine());
        int[] line, move;
        string[] strline;

        for (int i = 0; i < N; i++) {
            move = System.Array.ConvertAll(logFileReader.ReadLine().Trim().Split(), int.Parse);
            line = System.Array.ConvertAll(logFileReader.ReadLine().Trim().Split(), int.Parse);
            strline = logFileReader.ReadLine().Trim().Split();
            pdata.Add(move[0], new ponder_move(move[1], line, strline));
        }

        

    }

    #endregion

}
