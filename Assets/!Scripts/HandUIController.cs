using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class HandUIController : MonoBehaviour
{
    [Header("References")]
    public Image handImage;
    public PlayerPickUp playerPickUp;
    public Transform playerTransform;

    [Header("Animation Sets")]
    public HandAnimationSet idle;
    public HandAnimationSet sprint;
    public HandAnimationSet holding;
    public HandAnimationSet npc;
    public HandAnimationSet punch;

    [HideInInspector] public bool npcNearby = false;

    private HandState currentState;
    private Coroutine animationRoutine;
    private bool isPunching = false;

    private bool lockState = false;

    void Start()
    {
        if (handImage != null)
            handImage.gameObject.SetActive(true);

        currentState = HandState.Idle;
        StartAnimation(idle);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && !isPunching)
            StartCoroutine(PlayPunchOnce());

        if (!isPunching)
            DetermineState();
    }

    public void SetNPCNearby(bool value)
    {
        npcNearby = value;
    }

    void DetermineState()
    {
        if (lockState) return;

        bool isHolding = false;

        if (playerPickUp != null)
        {
            isHolding = playerPickUp.IsHoldingObject();

            if (playerPickUp.gameObject == null)
                isHolding = false;
        }

        if (npcNearby)
        {
            SetState(HandState.NPCNearby);
        }
        else if (isHolding)
        {
            SetState(HandState.Holding);
        }
        else if (Input.GetKey(KeyCode.LeftShift))
        {
            SetState(HandState.Sprinting);
        }
        else
        {
            SetState(HandState.Idle);
        }
    }

    public void ForceIdle()
    {
        isPunching = false;
        currentState = HandState.Idle;
        StartAnimation(idle);

        lockState = true;
        StartCoroutine(UnlockStateNextFrame());
    }

    IEnumerator UnlockStateNextFrame()
    {
        yield return null;
        lockState = false;
    }

    void SetState(HandState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        StartAnimation(GetSet(newState));
    }

    void StartAnimation(HandAnimationSet set)
    {
        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        if (set != null && set.frames.Length > 0)
            animationRoutine = StartCoroutine(PlayAnimation(set));
    }

    HandAnimationSet GetSet(HandState state)
    {
        switch (state)
        {
            case HandState.Punching: return punch;
            case HandState.NPCNearby: return npc;
            case HandState.Holding: return holding;
            case HandState.Sprinting: return sprint;
            default: return idle;
        }
    }

    IEnumerator PlayPunchOnce()
    {
        isPunching = true;

        if (animationRoutine != null)
            StopCoroutine(animationRoutine);

        if (punch != null && punch.frames.Length > 0)
        {
            for (int i = 0; i < punch.frames.Length; i++)
            {
                handImage.sprite = punch.frames[i];
                yield return new WaitForSeconds(punch.frameRate);
            }
        }

        isPunching = false;
        currentState = HandState.Idle;
        StartAnimation(idle);
    }

    IEnumerator PlayAnimation(HandAnimationSet set)
    {
        int index = 0;

        while (true)
        {
            if (set.frames.Length == 0) yield break;

            handImage.sprite = set.frames[index];
            index = (index + 1) % set.frames.Length;

            yield return new WaitForSeconds(set.frameRate);
        }
    }
}

public enum HandState
{
    Idle,
    Sprinting,
    Holding,
    NPCNearby,
    Punching
}