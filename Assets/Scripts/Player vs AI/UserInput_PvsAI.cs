using UnityEngine;

public class UserInput_PvsAI : MonoBehaviour
{
    [SerializeField] private Engine_PvAI ce;
    [HideInInspector] public bool movestarted;
    [HideInInspector] public bool castle_in_place;
    [HideInInspector] public bool gamehasended;
    public Vector2Int WorldPos;
    public Vector3 vector;

    private void
    Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) {
            ce.PrintGame();
            return;
        }

        if (!Input.GetMouseButtonDown(0))
            return;

        vector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        WorldPos = new Vector2Int((int)(vector.x + 0.5f), (int)(vector.y + 0.5f));

        if (castle_in_place) 
            return;
        if (movestarted)
            ToFinalPoints();
        else
            ToInitialPoints();
    }

    private void
    ToInitialPoints()
    {
        if (WorldPos.x < 0 || WorldPos.y < 0 || WorldPos.x >= 8 || WorldPos.y >= 8) {
            return;
        }
        if (ce.AvailableMoves(WorldPos))
        {
            movestarted = true;
            ce.ip = 8 * WorldPos.y + WorldPos.x;
        }
        ce.ShowEndSquares(WorldPos);
    }

    private void
    ToFinalPoints()
    {
        if (WorldPos.x < 0 || WorldPos.y < 0 || WorldPos.x >= 8 || WorldPos.y >= 8) {
            return;
        }
        if (8 * WorldPos.y + WorldPos.x == ce.ip)
        {
            ce.RequestBoardReset();
            movestarted = false;
            return;
        }
        if (ce.AvailableMoves(WorldPos))
        {
            ToInitialPoints();
            return;
        }
        ce.fp = 8 * WorldPos.y + WorldPos.x;
        ce.ValidateMove(true);
    }

    public int
    Convert(float _x)
    {
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
