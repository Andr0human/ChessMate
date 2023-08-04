using UnityEngine;
using TMPro;


public class ArenaDashboard : MonoBehaviour
{
    [SerializeField] private Arena  ar;
    [SerializeField] private Timer tmr;

    public TextMeshProUGUI GameAmountField;
    public TextMeshProUGUI TimeFormatField;
    public TextMeshProUGUI EngineNameField;
    public UnityEngine.UI.Toggle AdjournToggle;

    [SerializeField] private GameObject[] ChessClocksText;


    public void
    SetSampleSize()
    {
        string text = GameAmountField.text;
        text = RemoveNonAlphaNumeric(text);

        if (text.Length == 0)
            return;

        ar.GamesToPlay = int.Parse(text);
    }


    public void
    SetTimeFormat()
    {
        string[] values = TimeFormatField.text.Split();
        float time_per_side = 60, increment = 0;

        if (values.Length == 0)
            return;

        if (values.Length >= 1)
            time_per_side = float.Parse(RemoveNonAlphaNumeric( values[0] ));

        if (values.Length >= 2)
            increment = float.Parse(RemoveNonAlphaNumeric( values[1] ));

        ar.FixedTimePerGame = time_per_side;
        ar.IncrementPerGame = increment;
    }


    public void
    SetEngines()
    {
        ar.ArenaEngines = new string[2];
        string[] names = EngineNameField.text.Split();

        if (names.Length < 2)
            return;
        
        ar.ArenaEngines[0] = RemoveNonAlphaNumeric( names[0] );
        ar.ArenaEngines[1] = RemoveNonAlphaNumeric( names[1] );
    }


    public void
    SetAdjournment(bool toggle)
    {
        GameObject.FindObjectOfType<Arena>().Adjournment = AdjournToggle.isOn;
    }


    public void
    ArenaStartButton()
    {
        GameObject.Find("BackBoard").SetActive(false);
        GameObject.Find("Game Amount").SetActive(false);
        GameObject.Find("Time Format").SetActive(false);
        GameObject.Find("Engine Names").SetActive(false);
        GameObject.Find("Start Arena Button").SetActive(false);

        AdjournToggle.gameObject.SetActive(false);

        if (tmr.AllotedTimePerSide == 0f) {}
            // tmr.enabled = false;
        else
        {
            ChessClocksText[0].SetActive(true);
            ChessClocksText[1].SetActive(true);
        }

        /*
        GameObject.Find("BackBoard").SetActive(false);

        GameAmountField.SetActive(false);
        TimeFormatField.SetActive(false);
        EngineNameField.SetActive(false);
        AdjournToggle.SetActive(false); */

        ar.InitArena();
    }


    public string
    RemoveNonAlphaNumeric(string text)
    {
        string result = string.Empty;
        foreach (char ch in text)
        {
            if (char.IsLetterOrDigit(ch) || (ch == '_') || (ch == '-') || (ch == '.'))
                result += ch;
        }
        return result;
    }


    public void
    ExitButton()
    {
        UnityEngine.Debug.Log("Exit Button called!");
        //! TODO Exit Button (Calls to stop players)
    }

}