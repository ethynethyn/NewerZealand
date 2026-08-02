using UnityEngine;

public class GrindTrigger : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;

    private void OnTriggerEnter(Collider other)
    {
        var controller = other.GetComponent<SlopeSlideController>();
        if (controller != null)
        {
            controller.StartGrind(startPoint.position, endPoint.position);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var controller = other.GetComponent<SlopeSlideController>();
        if (controller != null)
        {
            controller.EndGrind();
        }
    }
}