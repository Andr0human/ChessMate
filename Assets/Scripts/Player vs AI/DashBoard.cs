using UnityEngine;
using UnityEngine.UI; 

public class DashBoard : MonoBehaviour
{
    [SerializeField] private Timer tmr;
    [SerializeField] private GameObject[] ChessClocksText;

    public int TimeOption;
    public int SideOption;
    public float FixedTime, IncTime;


    public void
    TimeDropDownMenu(int option)
    {
        TimeOption = option;

        if (option == 0)
            return;
        
        if (option == 1)
        {
            FixedTime = 180;
            IncTime = 2;
        }
        else if (option == 2)
        {
            FixedTime = 60;
            IncTime = 1;
        }
        else if (option == 3)
        {
            FixedTime = 600;
            IncTime = 5;
        }
        else
        {
            //! TODO for custom time format
        }
    }


    public void
    SideDropDownMenu(int option)
    {
        SideOption = (option == 2) ? Random.Range(0, 2) : option;
    }


    public void
    PlayButton()
    {
        GameObject.Find("Play Button").SetActive(false);
        GameObject.Find("Time Format").SetActive(false);
        GameObject.Find("Color Format").SetActive(false);
        GameObject.Find("BackBoard").SetActive(false);

        if (TimeOption == 0) {}
            // tmr.enabled = false;
        else
        {
            tmr.SetTime(FixedTime, IncTime);

            ChessClocksText[0].SetActive(true);
            ChessClocksText[1].SetActive(true);
        }


        string player_white = (SideOption == 0) ? "human" : "bot";
        string player_black = (SideOption == 1) ? "human" : "bot";

        //! TODO code <FixedMoveTime>
        // StartCoroutine(
        //     GameObject.FindObjectOfType<MatchManager>().StartNewGame(
        //         player_white, player_black, new List<int>(),
        //         <FixedMoveTime>, true, false
        //     )
        // );
    }


    public void
    ExitButton()
    {
        //! TODO Exit Button (Calls to stop players)
    }

}