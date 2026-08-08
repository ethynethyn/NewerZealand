using UnityEngine;

/// <summary>
/// Put this on the trigger box at the bottom of the hole. When the boss falls in,
/// it ends the fight via BossController.Defeat().
///
/// The collider is set to "Is Trigger" automatically when you add this component.
/// </summary>
[RequireComponent(typeof(Collider))]
public class BossDefeatTrigger : MonoBehaviour
{
    [Tooltip("Drag the boss here. (If left empty, ANY BossController that enters will end the fight.)")]
    public BossController boss;

    void Reset()
    {
        GetComponent<Collider>().isTrigger = true;
    }

    void OnTriggerEnter(Collider other)
    {
        // GetComponentInParent works whether the collider sits on the boss root or a child.
        BossController entered = other.GetComponentInParent<BossController>();
        if (entered == null) return;
        if (boss != null && entered != boss) return;   // ignore anything that isn't our boss

        entered.Defeat();
    }
}
