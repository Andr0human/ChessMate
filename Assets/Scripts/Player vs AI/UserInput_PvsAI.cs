using UnityEngine;

public class UserInput_PvsAI : MonoBehaviour {


    [SerializeField] private Engine_PvAI ce;
    [HideInInspector] public bool movestarted;
    [HideInInspector] public bool castle_in_place;
    [HideInInspector] public bool gamehasended;
    public Vector2Int WorldPos;
    public Vector3 vector;

    private void Update() {

        if (Input.GetKeyDown(KeyCode.R)) {
            ce.print_game();
            return;
        }

        if (!Input.GetMouseButtonDown(0))
            return;

        vector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        WorldPos = new Vector2Int((int)(vector.x + 0.5f), (int)(vector.y + 0.5f));

        if (castle_in_place) 
            return;
        if (movestarted)
            To_final_points();
        else
            To_initial_points();
    }

    private void To_initial_points() {
        if (WorldPos.x < 0 || WorldPos.y < 0 || WorldPos.x >= 8 || WorldPos.y >= 8) {
            return;
        }
        if (ce.AvailableMoves(WorldPos)) {
            movestarted = true;
            ce.ip = 8 * WorldPos.y + WorldPos.x;
        }
        ce.Show_endSquares(WorldPos);
    }

    private void To_final_points() {
        if (WorldPos.x < 0 || WorldPos.y < 0 || WorldPos.x >= 8 || WorldPos.y >= 8) {
            return;
        }
        if (8 * WorldPos.y + WorldPos.x == ce.ip) {
            ce.Request_board_reset();
            movestarted = false;
            return;
        }
        if (ce.AvailableMoves(WorldPos)) {
            To_initial_points();
            return;
        }
        ce.fp = 8 * WorldPos.y + WorldPos.x;
        ce.ValidateMove(true);
    }

    public int Convert(float _x) {
        bool neg = false;
        if (_x < 0) {
            neg = true;
            _x *= -1;
        }

        int res = (int)(_x + 0.5f);

        if (neg) res *= -1;
        return res;
    }

}
