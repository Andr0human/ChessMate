using System.Collections;
using UnityEngine;

public class BoardHandler : MonoBehaviour
{
    private GameObject white_tile, black_tile;

    [SerializeField] private GameObject[] objects;
    private readonly GameObject[] pieces = new GameObject[64];
    private readonly GameObject[] tiles = new GameObject[64];

    public void
    InitializeBoard(ref ChessBoard _cb)
    {
        white_tile = GameObject.Find("White Tile");
        black_tile = GameObject.Find("Black Tile");
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
            if ((x + y) % 2 == 0) tiles[idx] = Instantiate(black_tile);
            else tiles[idx] = Instantiate(white_tile);
            tiles[idx].transform.position = new Vector3(x, y, 0);
            yield return new WaitForSeconds(0.01f);
        }

        for (int i = 1; i <= 6; i++)
        {
            ulong tmp = _cb.Pieces[7 + i], val;
            while (tmp != 0) {
                val = tmp - (tmp & (tmp - 1));
                tmp &= tmp - 1;
                SpawnPiece(_cb.idxs[val % 67], i);
                yield return new WaitForSeconds(0.01f);
            }
            tmp = _cb.Pieces[7 - i];
            while (tmp != 0) {
                val = tmp - (tmp & (tmp - 1));
                tmp &= tmp - 1;
                SpawnPiece(_cb.idxs[val % 67], -i);
                yield return new WaitForSeconds(0.01f);
            }
        }
    }

    private void
    SpawnPiece(int idx, int id)
    {
        Destroy(pieces[idx]);
        if (id == 0) return;
        if (id > 0) id--;
        id += 6;
        int x = idx & 7, y = idx - x >> 3;
        pieces[idx] = Instantiate(objects[id]);
        pieces[idx].transform.position = new Vector3((float)x, (float)y, 0f);
    }

    public void
    BoardReset(bool spare_last_move = false)
    {
        for (int i = 0; i < 64; i++)
        {
            tiles[i].GetComponent<TileSet>().reset_back(spare_last_move);
        }
    }

    public void
    BoardHighLight(ulong end, ref ChessBoard cb)
    {
        while (end != 0)
        {
            ulong num = end - (end & end - 1uL);
            end &= end - 1uL;
            int num2 = cb.idxs[num % 67];
            tiles[num2].GetComponent<TileSet>().high_light();
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
        tiles[ip].GetComponent<TileSet>().mark_start_move();
        tiles[fp].GetComponent<TileSet>().mark_end_move();
    }
}
