using System.Collections;
using System.Diagnostics;
using System.IO;
using UnityEngine;


public interface IPlayer
{

    void Play(ref ChessBoard position, int last_move);

    IEnumerator ReadOutputCoroutine();

    bool MoveMade();

    (int, float) GetResults();
}


public class ChessEngine : IPlayer
{
    private Core cs;
    private static Timer tmr;

    private Process    EngineProcess;
    private  string       EngineName;
    private  string       EnginePath;
    private  string  EngineInputPath;
    private  string EngineOutputPath;

    public bool    FixedMoveTime;
    public bool AllowOpeningBook;

    public   int EngineMove;
    public float EngineEval;


    public
    ChessEngine(string __engine, string start_fen, bool __fixed_move_time=false,
        bool __allow_opening_book=true)
    {
        cs  = GameObject.FindObjectOfType<Core>();
        tmr = GameObject.FindObjectOfType<Timer>();

        EngineName       = __engine;
        FixedMoveTime    = __fixed_move_time;
        AllowOpeningBook = __allow_opening_book;

        EnginePath       = Application.streamingAssetsPath + "/" + __engine + ".exe";
        EngineInputPath  = Application.streamingAssetsPath + "/" + __engine +  ".in";
        EngineOutputPath = Application.streamingAssetsPath + "/" + __engine + ".out";

        // Create input and output files for commands
        if (!File.Exists( EngineInputPath)) File.Create( EngineInputPath).Dispose();
        if (!File.Exists(EngineOutputPath)) File.Create(EngineOutputPath).Dispose();

        File.WriteAllText(EngineInputPath, "");
        File.WriteAllText(EngineOutputPath, "");

        EngineProcess = new Process();
        ProcessStartInfo startInfo = new ProcessStartInfo(EnginePath)
        {
            // WindowStyle = ProcessWindowStyle.Hidden,
            Arguments = "play input " + EngineInputPath + " output "
                + EngineOutputPath + " position \"" + start_fen + "\"",
            WorkingDirectory = Application.streamingAssetsPath,
        };

        EngineProcess = Process.Start(startInfo);
    }

    private static (float, float)
    GetAvailableTime(ref ChessBoard __pos)
    {
        int __side = -((__pos.pColor - 1) / 2);
        return (tmr.ChessClocks[__side], tmr.IncrementTime);
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
        EngineMove = 0;
        EngineEval = 0f;

        // Play Book Move if possible
        if (AllowOpeningBook && cs.PositionInOpeningBook(ref __pos))
        {
            EngineMove = cs.PlayBookMove(ref __pos);
            EngineEval = 0;
            return;
        }

        float search_time =
            FixedMoveTime ? 0.2f : DecideTimeForSearch(ref __pos);

        WriteInput(search_time, last_move);
        // StartCoroutine( ReadOutputCoroutine() );
    }

    private void
    WriteInput(float alloted_time, int last_move)
    {
        using (FileStream inputFileStream = new FileStream(EngineInputPath, FileMode.Create, FileAccess.Write, FileShare.Read))
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
        using (FileStream outputFileStream = new FileStream(EngineOutputPath, FileMode.Open, FileAccess.Read, FileShare.Write))
        using (StreamReader outputFileReader = new StreamReader(outputFileStream))
        {
            string line = outputFileReader.ReadLine();
            if (line == null) return false;

            string[] values = line.Split();

            EngineMove = int.Parse(values[0]);
            EngineEval = float.Parse(values[1]);

            File.WriteAllText(EngineOutputPath, "");
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
        if (!EngineProcess.HasExited)
        {
            EngineProcess.CloseMainWindow();
            EngineProcess.WaitForExit();

            EngineProcess.Close();
            EngineProcess.Dispose();
        }

        // Delete both input and output files along with their meta files

        string  input_meta_file =  EngineInputPath + ".meta";
        string output_meta_file = EngineOutputPath + ".meta";

        if (File.Exists(EngineInputPath)) File.Delete(EngineInputPath);
        if (File.Exists(  input_meta_file)) File.Delete(  input_meta_file);

        if (File.Exists(EngineOutputPath)) File.Delete(EngineOutputPath);
        if (File.Exists(  output_meta_file)) File.Delete(  output_meta_file);
    }

    public bool
    MoveMade()
    { return EngineMove != 0; }

    
    public (int, float)
    GetResults()
    { return (EngineMove, EngineEval); }
}


public class HumanPlayer : IPlayer
{
    private UserInput ui;
    private MoveGenerator mg;

    private int HumanMove;
    private int HumanEval;

    ChessBoard BoardPosition;

    public
    HumanPlayer()
    {
        ui = GameObject.FindObjectOfType<UserInput>();
        mg = GameObject.FindObjectOfType<MoveGenerator>();
    }

    public void
    Play(ref ChessBoard position, int last_move)
    {
        HumanMove = 0;
        HumanEval = 0;
        BoardPosition = position;
        // StartCoroutine( AskUserForSquares(position) );
    }

    public int
    GenerateEncodeMoveForUser()
    {
        int color = BoardPosition.pColor;

        int init_index = ui.InitSquare;
        int dest_index = ui.DestSquare;

        int          piece = BoardPosition.board[init_index] * color;
        int captured_piece = BoardPosition.board[dest_index] * (-color);
        int promoted_piece = ui.PromotedPiece;

        int       pos_bits = (dest_index << 6) | init_index;
        int      type_bits = (captured_piece << 15) | (piece << 12);
        int      color_bit = ((1 + color) / 2) << 20;
        int promotion_bits = promoted_piece << 18;

        int move = promotion_bits | color_bit | type_bits | pos_bits;
        return move;
    }

    public IEnumerator
    ReadOutputCoroutine()
    {
        MoveList movelist = mg.GenerateMoves(ref BoardPosition);
        ui.GetSquares(ref movelist);

        yield return new WaitUntil(() => (ui.InitSquare != -1) && (ui.DestSquare != -1));

        HumanMove = GenerateEncodeMoveForUser();
        HumanEval = 0;
    }

    public bool
    MoveMade()
    { return HumanMove != 0; }

    
    public (int, float)
    GetResults()
    { return (HumanMove, HumanEval); }
}

