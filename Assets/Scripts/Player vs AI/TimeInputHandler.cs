using UnityEngine;

public class TimeInputHandler : MonoBehaviour
{

    [SerializeField] private Timer tmr;
    [SerializeField] private GameObject white_clock, black_clock;
    
    private string StartFen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    private bool fixed_search_time;
    private ChessEngine bot;


    public void
    Dropdown(int val)
    {
        if (val == 0)
        {
            fixed_search_time = true;
            tmr.SetTime(0, 0);
        }
        else
        {
            fixed_search_time = false;
            if (val == 1)
                tmr.SetTime(180, 2);
            if (val == 2)
                tmr.SetTime(60, 1);
            if (val == 3)
                tmr.SetTime(300, 5);
            if (val == 4) {

            }
        }
    }


    public void
    StartGame()
    {
        bot = new ChessEngine("elsa", StartFen, fixed_search_time, true);

        GameObject.Find("Play Button").SetActive(false);
        GameObject.Find("Time Button").SetActive(false);
        GameObject.Find("Time Format").SetActive(false);
        GameObject.Find("BackBoard").SetActive(false);

        FindObjectOfType<UserInput_PvsAI>().enabled = true;
        FindObjectOfType<Engine_PvAI>().bot_color = -1;

        if (fixed_search_time) {
            tmr.enabled = false;
        }
        else {
            tmr.Init();
            white_clock.SetActive(true);
            black_clock.SetActive(true);
        }
    }


    public void
    OnApplicationQuit()
    {
        bot.Stop();
        Application.Quit();
    }
}
