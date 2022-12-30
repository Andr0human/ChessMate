using UnityEngine;

public class Starter : MonoBehaviour {


    private void Start() {

        ChessBoard board = new ChessBoard();
        board.LoadFromFEN("rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");

        FindObjectOfType<Core>().init();
        FindObjectOfType<BoardHandler>().Initialize_board(ref board);

    }

    public void StartTraining() {

        GameObject.Find("Train Button").SetActive(false);
        FindObjectOfType<UserInput_Train>().enabled = true;
        FindObjectOfType<EngineTrain>().Start_new_game('p');
        
    }

    public void OnApplicationQuit() {
        FindObjectOfType<TrainBot>().StopBot();
        Application.Quit();
    }

}
