using UnityEngine;

public class TimeInputHandler : MonoBehaviour {

    [SerializeField] private Timer tmr;
    [SerializeField] private Elsa eb;
    [SerializeField] private GameObject white_clock, black_clock;
    private string startPosition = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";


    public void Dropdown(int val) {
        if (val == 0) {
            eb.use_fixed_time = true;
            tmr.Set_time(0, 0);
        }
        else {
            eb.use_fixed_time = false;
            if (val == 1)
                tmr.Set_time(180, 2);
            if (val == 2)
                tmr.Set_time(60, 1);
            if (val == 3)
                tmr.Set_time(300, 5);
            if (val == 4) {

            }
        }
    }


    public void StartGame() {
        eb.InitializeBot(startPosition);

        GameObject.Find("Play Button").SetActive(false);
        GameObject.Find("Time Button").SetActive(false);
        GameObject.Find("Time Format").SetActive(false);
        GameObject.Find("BackBoard").SetActive(false);

        FindObjectOfType<UserInput_PvsAI>().enabled = true;
        FindObjectOfType<Engine_PvAI>().bot_color = -1;

        if (eb.use_fixed_time) {
            tmr.enabled = false;
        }
        else {
            tmr.Init();
            white_clock.SetActive(true);
            black_clock.SetActive(true);
        }

    }


    public void OnApplicationQuit() {
        eb.StopBot();
        Application.Quit();
    }

}
