using UnityEngine;

public class StevenTracker : MonoBehaviour
{

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            if (JonahStaticManager.leftSteven)
            {
                JonahStaticManager.leftSteven = false;
                JonahStaticManager.leftSteven2 = true;
            }
        }

    }


}
