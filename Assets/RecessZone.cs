using UnityEngine;

public class RecessZone : MonoBehaviour
{
    public Transform[] hangoutPoints;

    public Transform GetRandomSpot()
    {
        if (hangoutPoints.Length == 0) return null;

        return hangoutPoints[Random.Range(0, hangoutPoints.Length)];
    }
}