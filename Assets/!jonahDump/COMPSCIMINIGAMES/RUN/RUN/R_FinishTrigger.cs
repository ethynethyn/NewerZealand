using UnityEngine;

// Sits on the green end cap. When the player passes through, you win.
[DisallowMultipleComponent]
public class R_FinishTrigger : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (R_GameManager.IsGameOver) return;
        if (other.GetComponent<R_PlayerController>() != null)
            R_GameManager.WinGame();
    }
}
