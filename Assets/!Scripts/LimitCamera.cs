using UnityEngine;

public class LimitCamera : MonoBehaviour
{

    public GameObject Player;

    // Update is called once per frame
    private void LateUpdate()
    {
        transform.position = new Vector3(Player.transform.position.x, 40, Player.transform.position.z);
    }
}
