using UnityEngine;

public class TileSet : MonoBehaviour {

    [SerializeField] private SpriteRenderer sr;
    [SerializeField] private Color primary;
    [SerializeField] private Color highlight_color;
    [SerializeField] private Color move_start;
    [SerializeField] private Color move_end;

    public void reset_back(bool spare_last_move = false) {
        if (spare_last_move && (sr.color == move_end || sr.color == move_start))
            return;
        sr.color = primary;
    }

    public void mark_start_move() {
        sr.color = move_start;
    }

    public void high_light() {
        sr.color = highlight_color;
    }

    public void mark_end_move() {
        sr.color = move_end;
    }

}
