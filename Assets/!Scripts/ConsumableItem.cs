using UnityEngine;
using UnityEngine.InputSystem;

public class ConsumableItem : MonoBehaviour
{
    [Header("Consume Settings")]
    public Key consumeKey = Key.E;

    [Header("Target Settings")]
    [Tooltip("Leave blank — will auto-find the GameObject named 'Player' at runtime")]
    public string statToModify = "Fun";
    public float statChangeAmount = 10f;

    [Header("Destruction Settings")]
    public bool destroyParent = true;
    public float destroyDelay = 0f;

    private PlayerPickUp playerPickUp;
    private Character targetCharacter;

    void Start()
    {
        // Find the Character component on the GameObject named "Player"
        GameObject playerObj = GameObject.Find("`PLAYER");
        if (playerObj != null)
            targetCharacter = playerObj.GetComponent<Character>();

        if (targetCharacter == null)
            Debug.LogWarning("ConsumableItem: Could not find a Character component on a GameObject named 'Player'.");
    }

    void Update()
    {
        if (playerPickUp == null)
            playerPickUp = FindObjectOfType<PlayerPickUp>();

        if (playerPickUp == null || targetCharacter == null) return;

        bool isHeld = playerPickUp.GetHeldObject() == gameObject;
        if (!isHeld) return;

        if (Keyboard.current != null && Keyboard.current[consumeKey].wasPressedThisFrame)
            Consume();
    }

    void Consume()
    {
        targetCharacter.ModifyStat(statToModify, statChangeAmount);
        Debug.Log($"[ConsumableItem] '{statToModify}' +{statChangeAmount} applied to {targetCharacter.characterName}");

        playerPickUp.ForceDropHeldObject();

        GameObject target = destroyParent && transform.parent != null
            ? transform.parent.gameObject
            : gameObject;

        if (destroyDelay > 0f)
            Destroy(target, destroyDelay);
        else
            Destroy(target);
    }
}