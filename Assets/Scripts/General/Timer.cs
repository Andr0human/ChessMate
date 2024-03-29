using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour
{
    public float AllotedTimePerSide = 60f;
    public float IncrementTime = 1f;

    public float[] ChessClocks;

    [SerializeField] private TextMeshProUGUI[] TimeInText;

    private int Side2Tick = 2;


    private void
    Start()
    {
        ChessClocks = new float[2];
    }

    public void
    Init(int StartingSide)
    {
        Side2Tick = StartingSide;
        ChessClocks[0] = ChessClocks[1] = AllotedTimePerSide;

        ClockUnfreeze();

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
            __t.color = new Color(0.844f, 0.086f, 0.0267f);
        else if (time_left < 45f)
            __t.color = new Color(1f, 0.901f,0.156f);
        else
            __t.color = new Color(0.1297f, 0.5f, 0.16841f);
    }

    public void
    ClockReset(int color)
    {
        ChessClocks[0] = ChessClocks[1] = AllotedTimePerSide;
        Side2Tick = 2;
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
    {
        if (Side2Tick < 2)
            Side2Tick += 2;
    }

    public void
    ClockUnfreeze()
    {
        if (Side2Tick > 1)
            Side2Tick -= 2;
    }

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
        IncrementTime = _y;
    }
}
