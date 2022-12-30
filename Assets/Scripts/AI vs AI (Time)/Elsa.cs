using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class Elsa : MonoBehaviour {

    #region Variables

    [SerializeField] private Timer tmr;
    [SerializeField] private Core cs;

    public bool use_fixed_time = false;
    private int self_color, made_move_count;
    public int received_move;
    public double received_move_eval;
    Process process;

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

        int _move = cs.PlayBookMove(ref _cb);
        if (_move != 0) {
            received_move = _move;
            received_move_eval = 0;
            return;
        }

        if (made_move_count % 45 == 0) {
            StopBot();
            InitializeBot(_cb.FENGenerator());
        }

        received_move = 0;
        received_move_eval = 0;
        made_move_count++;

        float time_left = self_color == 1 ? tmr.clock_white : tmr.clock_black;
        float increment = tmr.increment_time;
        Vector2 alloted_time;

        if (use_fixed_time) {
            alloted_time.x = 1f;
            alloted_time.y = 0;
        }
        else {
            alloted_time = cs.Decide_Time(time_left, increment, ref _cb);
        }

        CreateEntry(alloted_time, last_move);
        StartCoroutine(ReceiveMove());

    }

    private IEnumerator ReceiveMove() {
        yield return new WaitUntil(Get_Entry);
    }

    #endregion

}
