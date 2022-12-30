using UnityEngine;
using TMPro;

public class Timer : MonoBehaviour {

    [SerializeField] private float alloted_time_per_side = 60f;
    public float clock_white = 100f, clock_black = 100f;
    public float increment_time = 2f;
    public bool white_tick, black_tick;
    [SerializeField] private TextMeshProUGUI white_time, black_time;
    [SerializeField] private Color high_time_color;
    [SerializeField] private Color med_time_color;
    [SerializeField] private Color low_time_color;

    private int saved_state;

    public void Update() {
        if (!white_tick && !black_tick) {
            return;
        }
        if (white_tick) {
            clock_white -= 1f * Time.deltaTime;
        }
        if (black_tick) {
            clock_black -= 1f * Time.deltaTime;
        }
        Text_color_change(ref white_time, clock_white);
        Text_color_change(ref black_time, clock_black);
        if (clock_white <= 0f || clock_black <= 0f) {
            white_tick = black_tick = false;
        }
        white_time.text = Remaining_time(clock_white);
        black_time.text = Remaining_time(clock_black);
    }

    public void Text_color_change(ref TextMeshProUGUI __t, float value) {
        if (value < 15f) {
            __t.color = low_time_color;
            return;
        }
        if (value < 45f) {
            __t.color = med_time_color;
            return;
        }
        __t.color = high_time_color;
    }

    public void Clock_reset(int color) {
        clock_white = clock_black = alloted_time_per_side;
        black_tick = false;
        white_tick = false;

        if (color == 1) white_tick = true;
        if (color == -1) black_tick = true;

    }

    public void Switch_player() {
        if (white_tick) {
            clock_white += increment_time;
            white_tick = false;
            black_tick = true;
            return;
        }
        clock_black += increment_time;
        black_tick = false;
        white_tick = true;
    }

    public void Clock_freeze() {
        if (black_tick) {
            saved_state = -1;
        }
        else if (white_tick) {
            saved_state = 1;
        }
        black_tick = white_tick = false;
    }

    public void Clock_unfreeze() {
        if (saved_state == 1) {
            white_tick = true;
            return;
        }
        if (saved_state == -1) {
            black_tick = true;
        }
    }

    public string Remaining_time(double time_left) {
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

    public void Set_time(int _x, int _y) {
        alloted_time_per_side = _x;
        increment_time = _y;
    }

    public float Get_alloted_time() {
        return alloted_time_per_side;
    }

    public void Init() {
        clock_white = alloted_time_per_side;
        clock_black = alloted_time_per_side;
    }



}
