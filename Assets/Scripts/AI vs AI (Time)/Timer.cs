using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    [SerializeField] private float AllotedTimePerSide = 60f;

    public float[] ChessClocks;
    public float IncrementTime = 1f;

    [SerializeField] private TextMeshProUGUI[] TimeInText;

    [SerializeField] private Color HighTimeColor;
    [SerializeField] private Color MedTimeColor;
    [SerializeField] private Color LowTimeColor;

    private int Side2Tick = 3;


    public void
    Start()
    {
        ChessClocks = new float[2];
    }

    public void
    Init(int StartingSide)
    {
        Side2Tick = StartingSide;

        if (AllotedTimePerSide != 0)
        {
            TimeInText[0].enabled = true;
            TimeInText[1].enabled = true;
        }
    }

    private void
    Update()
    {
        if (Side2Tick >= 2)
            return;

        ChessClocks[Side2Tick] -= 1f * Time.deltaTime;

        TextColorChange(ref TimeInText[0], ChessClocks[0]);
        TextColorChange(ref TimeInText[1], ChessClocks[1]);

        TimeInText[0].text = RemainingTime(ChessClocks[0]);
        TimeInText[1].text = RemainingTime(ChessClocks[1]);
    }

    public void
    TextColorChange(ref TextMeshProUGUI __t, float time_left)
    {
        if (time_left < 15f)
        {
            __t.color = LowTimeColor;
            return;
        }
        if (time_left < 45f)
        {
            __t.color = MedTimeColor;
            return;
        }
        __t.color = HighTimeColor;
    }

    public void
    ClockReset(int color)
    {
        ChessClocks[0] = ChessClocks[1] = AllotedTimePerSide;
        Side2Tick = 0;      // !TODO
    }

    public void
    SwitchPlayer()
    {
        if (Side2Tick < 2)
            ChessClocks[Side2Tick] += IncrementTime;
        Side2Tick ^= 1;
    }

    public void
    ClockFreeze()
    { Side2Tick += 2; }

    public void
    ClockUnfreeze()
    { Side2Tick -= 2; }

    public string
    RemainingTime(double time_left)
    {
        if (time_left < 15.0) {
            return time_left.ToString("0.##");
        }
        int sec = (int)time_left;
        int hr = sec / 3600;
        sec %= 3600;
        int mn = sec / 60;
        sec %= 60;
        string res = hr.ToString() + ":" + mn.ToString() + ":" + sec.ToString();
        return res;
    }

    public void
    SetTime(float _x, float _y)
    {
        AllotedTimePerSide = _x;
        ChessClocks[0] = ChessClocks[1] = _x;
        IncrementTime = _y;
    }

    public float
    GetAllotedTime()
    { return AllotedTimePerSide; }

}
