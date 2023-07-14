using UnityEngine;

public class UserInput_AvA_Time : MonoBehaviour
{
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Equals))
        {
            FindObjectOfType<Arena>().InitializeArena();
            return;
        }
        if (Input.GetKeyDown(KeyCode.Backslash))
        {
            StartCoroutine(FindObjectOfType<BookMaker>().AddToBook());
            return;
        }
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 vector = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            Vector2Int WorldPos = new Vector2Int((int)(vector.x + 0.5f), (int)(vector.y + 0.5f));
            return;
        }
    }

}
