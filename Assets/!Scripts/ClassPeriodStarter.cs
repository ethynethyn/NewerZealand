using UnityEngine;

// Put this in a Class Halls scene. When the scene LOADS, it sends every NPC (and
// the player's door/lock system) straight to the CURRENT period's class — the
// period carried over from the TriggerNextPeriod that loaded this scene.
//
// It works by raising the SAME SchoolTimeController.OnStateChanged event the NPCs
// already listen to, so nothing in NPCBrain needs to change. Because time no
// longer advances during class, this event replaces the old time-driven trigger.
//
// Note: NPCs in a class scene should have 'spawnAtDoor' OFF (i.e. already present
// in the halls) so they respond to this and walk to their seats.
public class ClassPeriodStarter : MonoBehaviour
{
    [Tooltip("Override the carried-over period. -1 = use the period set by TriggerNextPeriod " +
             "(handy for testing a class scene on its own).")]
    public int overridePeriodIndex = -1;

    void Start()
    {
        SendStudentsToClass();
    }

    // Sends everyone to their seats for the current period. Runs automatically on
    // scene load; also public so you can re-trigger it manually if ever needed.
    public void SendStudentsToClass()
    {
        int period = ResolvePeriod();
        Debug.Log($"[ClassPeriodStarter] Sending students to Period {period + 1} (index {period}).");
        SchoolTimeController.OnStateChanged?.Invoke(SchoolState.Class, period);
    }

    int ResolvePeriod()
    {
        if (overridePeriodIndex >= 0) return overridePeriodIndex;
        if (SceneProgressionManager.Instance != null)
            return SceneProgressionManager.Instance.GetClassPeriod();
        return 0;
    }
}
