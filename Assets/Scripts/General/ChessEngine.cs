using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class ChessEngine
{
    private Core cs;
    private static Timer tmr;

    private Process process;

    private string engine_name;
    private string engine_path;
    private string engine_input_path;
    private string engine_output_path;

    public bool fixed_move_time;
    public bool allow_opening_book;

    public int engine_move;
    public float engine_eval;


    public
    ChessEngine(string __engine, string start_fen, bool __fixed_move_time=false,
        bool __allow_opening_book=true)
    {
        cs                 = GameObject.FindObjectOfType<Core>();
        tmr                = GameObject.FindObjectOfType<Timer>();
        engine_name        = __engine;
        fixed_move_time    = __fixed_move_time;
        allow_opening_book = __allow_opening_book;

        engine_path        = Application.streamingAssetsPath + "/" + __engine + ".exe";
        engine_input_path  = Application.streamingAssetsPath + "/" + __engine +  ".in";
        engine_output_path = Application.streamingAssetsPath + "/" + __engine + ".out";

        // Create input and output files for commands
        if (!File.Exists( engine_input_path)) File.Create( engine_input_path).Dispose();
        if (!File.Exists(engine_output_path)) File.Create(engine_output_path).Dispose();

        process = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo(engine_path)
        {
            // WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = "play input " + engine_input_path + " output "
                + engine_output_path + " position \"" + start_fen + "\"",
            WorkingDirectory = Application.streamingAssetsPath,
        };

        process = Process.Start(startInfo);
    }

    private static (float, float)
    GetAvailableTime(ref ChessBoard __pos)
    {
        return (__pos.pColor == 1)
            ? (tmr.clock_white, tmr.increment_time)
            : (tmr.clock_black, tmr.increment_time);
    }

    private static float 
    DecideTimeForSearch(ref ChessBoard __pos)
    {
        var (time_left, increment) = GetAvailableTime(ref __pos);

        float max_moves = 32;
        float max_weight = 7880f;
        float current_weight = __pos.PositionWeight();

        float moves_to_go = max_moves -
            (((max_weight - current_weight) / 400f) * 1.2f);

        return ((time_left + increment) / moves_to_go) + (increment / 2);
    }

    public void
    Play(ref ChessBoard __pos, int last_move)
    {
        engine_move = 0;
        engine_eval = 0f;

        // Play Book Move if possible
        if (allow_opening_book && cs.PositionInOpeningBook(ref __pos))
        {
            engine_move = cs.PlayBookMove(ref __pos);
            engine_eval = 0;
            return;
        }

        float search_time =
            fixed_move_time ? 1f : DecideTimeForSearch(ref __pos);

        WriteInput(search_time, last_move);
    }

    private void
    WriteInput(float alloted_time, int last_move)
    {
        using (FileStream inputFileStream = new FileStream(engine_input_path, FileMode.Create, FileAccess.Write, FileShare.Read))
        using (StreamWriter inputFileWriter = new StreamWriter(inputFileStream))
        {
            string commandline = "time " + alloted_time.ToString("0.###") + " ";

            if (last_move != 0)
                commandline += "moves " + last_move.ToString() + " ";

            commandline += "go";

            inputFileWriter.WriteLine(commandline);
        }
    }

    private bool
    ReadOutput()
    {
        using (FileStream outputFileStream = new FileStream(engine_output_path, FileMode.Open, FileAccess.Read, FileShare.Write))
        using (StreamReader outputFileReader = new StreamReader(outputFileStream))
        {
            string line = outputFileReader.ReadLine();
            if (line == null) return false;

            string[] values = line.Split();

            engine_move = int.Parse(values[0]);
            engine_eval = float.Parse(values[1]);

            File.WriteAllText(engine_output_path, "");
            return true;
        }
    }


    public IEnumerator
    ReadOutputCoroutine()
    {
        yield return new WaitUntil(ReadOutput);
    }

    public void
    Stop()
    {
        if (!process.HasExited)
        {
            process.CloseMainWindow();
            process.WaitForExit();

            process.Close();
            process.Dispose();
        }

        // Delete both input and output files along with their meta files

        string  input_meta_file =  engine_input_path + ".meta";
        string output_meta_file = engine_output_path + ".meta";

        if (File.Exists(engine_input_path)) File.Delete(engine_input_path);
        if (File.Exists(  input_meta_file)) File.Delete(  input_meta_file);

        if (File.Exists(engine_output_path)) File.Delete(engine_output_path);
        if (File.Exists(  output_meta_file)) File.Delete(  output_meta_file);
    }
}

