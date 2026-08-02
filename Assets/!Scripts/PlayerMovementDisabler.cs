using UnityEngine;
using System.Reflection;

// Uses reflection so it works no matter which assembly your scripts live in
// (StarterAssets ships in its own assembly, which otherwise causes "type not
// found" compile errors). Zeroes MoveSpeed + SprintSpeed while this object is
// active, restores them when it's disabled. Looking is untouched.
public class PlayerMovementDisabler : MonoBehaviour
{
    [Tooltip("Leave empty to auto-find by tag.")]
    public GameObject player;
    public string playerTag = "Player";

    private MonoBehaviour fpController;
    private FieldInfo moveSpeedField;
    private FieldInfo sprintSpeedField;
    private float savedMoveSpeed;
    private float savedSprintSpeed;
    private bool applied = false;

    void OnEnable()
    {
        if (Resolve())
            ApplyFreeze();
    }

    void OnDisable()
    {
        RestoreSpeed();
    }

    bool Resolve()
    {
        if (fpController != null) return true;

        GameObject p = player;
        if (p == null && !string.IsNullOrEmpty(playerTag))
            p = GameObject.FindWithTag(playerTag);

        if (p == null)
        {
            Debug.LogWarning("[PlayerMovementDisabler] No player assigned or found by tag.");
            return false;
        }

        foreach (var mb in p.GetComponentsInChildren<MonoBehaviour>(true))
        {
            if (mb != null && mb.GetType().Name == "FirstPersonController")
            {
                fpController = mb;
                break;
            }
        }

        if (fpController == null)
        {
            Debug.LogWarning("[PlayerMovementDisabler] FirstPersonController not found on player.");
            return false;
        }

        var t = fpController.GetType();
        moveSpeedField = t.GetField("MoveSpeed", BindingFlags.Public | BindingFlags.Instance);
        sprintSpeedField = t.GetField("SprintSpeed", BindingFlags.Public | BindingFlags.Instance);

        if (moveSpeedField == null)
            Debug.LogWarning("[PlayerMovementDisabler] 'MoveSpeed' field not found on FirstPersonController.");

        return moveSpeedField != null;
    }

    void ApplyFreeze()
    {
        if (applied || fpController == null) return;

        if (moveSpeedField != null)
        {
            savedMoveSpeed = (float)moveSpeedField.GetValue(fpController);
            moveSpeedField.SetValue(fpController, 0f);
        }
        if (sprintSpeedField != null)
        {
            savedSprintSpeed = (float)sprintSpeedField.GetValue(fpController);
            sprintSpeedField.SetValue(fpController, 0f);
        }

        applied = true;
    }

    void RestoreSpeed()
    {
        if (!applied || fpController == null) return;

        if (moveSpeedField != null)
            moveSpeedField.SetValue(fpController, savedMoveSpeed);
        if (sprintSpeedField != null)
            sprintSpeedField.SetValue(fpController, savedSprintSpeed);

        applied = false;
    }
}