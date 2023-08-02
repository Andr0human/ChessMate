using System.Collections;
using UnityEngine;

public class BoardHandler : MonoBehaviour
{
    private GameObject WhiteTile, BlackTile;

    [SerializeField]
    private GameObject[] ChessPiece = new GameObject[12];
    private readonly GameObject[] PieceAt = new GameObject[64];
    private readonly GameObject[]  TileAt = new GameObject[64];

    public void
    InitializeBoard(ref ChessBoard _cb)
    {
        WhiteTile = GameObject.Find("White Tile");
        BlackTile = GameObject.Find("Black Tile");
        StartCoroutine(BoardGenerator(_cb));
    }

    private IEnumerator
    BoardGenerator(ChessBoard _cb)
    {
        int[] arr = new int[64];
        for (int i = 0; i < 64; i++) arr[i] = i;

        for (int i = 0; i < 64; i++)
        {
            int j = Random.Range(100, 1000) % (i + 1);
            int tmp = arr[i];
            arr[i] = arr[j];
            arr[j] = tmp;
        }

        for (int i = 0; i < 64; i++)
        {
            int idx = arr[i];
            int x = idx & 7, y = (idx - x) >> 3;

            if ((x + y) % 2 == 0) TileAt[idx] = Instantiate(BlackTile);
            else TileAt[idx] = Instantiate(WhiteTile);

            TileAt[idx].transform.position = new Vector3(x, y, 0);
            yield return new WaitForSeconds(0.01f);
        }

        for (int piece_type = 1; piece_type <= 6; piece_type++)
        {
            for (int side = 0; side <= 1; side++)
            {
                ulong piece = _cb.pieces[8 * side + piece_type];
                while (piece != 0)
                {
                    int sq = _cb.LsbIdx(piece);
                    SpawnPiece(sq, 8 * side + piece_type);
                    piece &= piece - 1;
                    yield return new WaitForSeconds(0.01f);
                }
            }
        }
    }

    private void
    SpawnPiece(int square, int piece)
    {
        Destroy(PieceAt[square]);
        if ((piece & 7) == 0)
            return;

        int id = (piece & 7) + ((piece >> 3) * 6) - 1;
        int x = square & 7, y = (square - x) >> 3;

        PieceAt[square] = Instantiate(ChessPiece[id]);
        PieceAt[square].transform.position = new Vector3((float)x, (float)y, 0f);
    }

    public void
    BoardReset(bool spare_last_move = false)
    {
        for (int i = 0; i < 64; i++)
        {
            TileAt[i].GetComponent<TileSet>().reset_back(spare_last_move);
        }
    }

    public void
    BoardHighLight(ulong end)
    {
        for (int sq = 0; sq < 64; sq++)
        {
            if (((1UL << sq) & end) != 0)
                TileAt[sq].GetComponent<TileSet>().high_light();
        }
    }

    public void
    Recreate(ref ChessBoard _cb)
    {
        for (int i = 0; i < 64; i++)
        {
            SpawnPiece(i, _cb.board[i]);
        }
    }

    public void
    MarkPlayedMove(int move)
    {
        int ip = move & 63, fp = (move >> 6) & 63;
        TileAt[ip].GetComponent<TileSet>().mark_start_move();
        TileAt[fp].GetComponent<TileSet>().mark_end_move();
    }
}
