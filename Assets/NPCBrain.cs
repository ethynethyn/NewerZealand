using UnityEngine;
using UnityEngine.AI;
using System.Collections;

public class NPCBrain : MonoBehaviour
{
    public NavMeshAgent agent;
    public NPCSchedule schedule;

    [Header("Spawn Settings")]
    public bool spawnAtDoor = false;
    public Transform spawnPoint;
    public Transform entrancePoint;
    public Vector2 arrivalWindow = new Vector2(5f, 9f);

    [Header("Timing")]
    public float maxSpawnDelay = 10f;
    public float stateChangeDelay = 3f;
    public Vector2 leaveWindow = new Vector2(15f, 16f);
    public float maxLeaveDelay = 10f;

    [Header("Leave Settings")]
    public float arrivalDistanceThreshold = 1.2f;

    private float arrivalHour;
    private float spawnDelay;
    private bool hasEnteredSchool = false;
    private bool waitingToEnter = false;

    private float leaveHour;
    private float leaveDelay;
    private bool hasLeftSchool = false;
    private bool waitingToLeave = false;
    private bool isWalkingToExit = false;

    private bool dayResetting = false;

    private ClassroomZone currentClassroom;
    private Transform currentSeat;
    private Transform currentFocus;

    private bool isSeated = false;
    private bool rotationDone = false;

    private SpriteBillboardTwoSided billboard;

    [Header("Role")]
    public bool isTeacher = false;

    [Header("Recess Target")]
    public string preferredRecessZone;

    // Cached controller (lazy-found, then reused).
    private SchoolTimeController controller;
    private SchoolTimeController Controller
    {
        get
        {
            if (controller == null) controller = FindObjectOfType<SchoolTimeController>();
            return controller;
        }
    }

    void Start()
    {
        agent.updateRotation = false;

        billboard = GetComponentInChildren<SpriteBillboardTwoSided>();

        if (spawnAtDoor && spawnPoint != null)
            InitDay();
    }

    // ---- Conversation freeze / face ----
    private bool inConversation = false;

    public void EnterConversation(Transform lookAt)
    {
        if (inConversation) return;
        inConversation = true;

        // Stop where they are
        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        // Face the player via the billboard (no transform rotation, matches your setup)
        if (billboard != null && lookAt != null)
            billboard.SetFocus(lookAt);
    }

    public void ExitConversation()
    {
        if (!inConversation) return;
        inConversation = false;

        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = false;

        // Re-evaluate what they should be doing now (covers a period change that
        // happened mid-conversation) and restores the correct facing.
        if (hasEnteredSchool && !hasLeftSchool)
            SnapToCurrentState();
        else
            ClearBillboardFocus();
    }

    void InitDay()
    {
        StopAllCoroutines();

        agent.isStopped = true;
        agent.ResetPath();
        agent.Warp(spawnPoint.position);

        float rawTime = Random.Range(arrivalWindow.x, arrivalWindow.y);
        arrivalHour = Mathf.Floor(rawTime * 4f) / 4f;
        spawnDelay = Random.Range(0f, maxSpawnDelay);

        float rawLeave = Random.Range(leaveWindow.x, leaveWindow.y);
        leaveHour = Mathf.Floor(rawLeave);
        leaveDelay = Random.Range(0f, maxLeaveDelay);

        hasEnteredSchool = false;
        hasLeftSchool = false;
        waitingToEnter = true;
        waitingToLeave = false;
        isWalkingToExit = false;
        dayResetting = false;

        float currentHour = GetCurrentHour();

        if (currentHour >= arrivalHour && currentHour < leaveHour)
        {
            waitingToEnter = false;
            hasEnteredSchool = true;
            waitingToLeave = true;

            agent.Warp(entrancePoint.position);
            agent.isStopped = false;

            SnapToCurrentState();
        }
    }

    void Update()
    {
        if (inConversation) return;   
        float currentHour = GetCurrentHour();

        if (!dayResetting && hasLeftSchool && currentHour < leaveWindow.x)
        {
            dayResetting = true;
            InitDay();
            return;
        }

        // ENTER
        if (spawnAtDoor && !hasEnteredSchool && waitingToEnter)
        {
            if (currentHour >= arrivalHour)
            {
                waitingToEnter = false;
                StartCoroutine(EnterWithDelay());
            }
        }

        // LEAVE
        if (spawnAtDoor && hasEnteredSchool && !hasLeftSchool && waitingToLeave && !isWalkingToExit)
        {
            if (currentHour >= leaveHour)
            {
                waitingToLeave = false;
                StartCoroutine(LeaveWithDelay());
            }
        }

        // EXIT ARRIVAL
        if (isWalkingToExit)
        {
            bool done =
                !agent.pathPending &&
                agent.remainingDistance <= arrivalDistanceThreshold &&
                agent.velocity.sqrMagnitude < 0.05f;

            if (done)
            {
                isWalkingToExit = false;
                hasLeftSchool = true;

                agent.isStopped = true;
                agent.ResetPath();

                agent.Warp(spawnPoint.position);
            }
        }

        // 🪑 SEAT ARRIVAL
        if (currentSeat != null && !rotationDone)
        {
            bool arrived =
                !agent.pathPending &&
                agent.remainingDistance <= agent.stoppingDistance &&
                agent.velocity.sqrMagnitude < 0.05f;

            if (arrived)
            {
                rotationDone = true;
                isSeated = true;

                agent.isStopped = true;
                agent.ResetPath();

                ApplyFocusToBillboard();
            }
        }
    }

    // 🔥 BILLBOARD FOCUS SYSTEM (NO TRANSFORM ROTATION)
    void ApplyFocusToBillboard()
    {
        if (billboard == null) return;

        if (currentFocus != null)
        {
            billboard.SetFocus(currentFocus);
            Debug.Log("🪑 NPC BILLBOARD FACING CLASS FOCUS");
        }
    }

    void ClearBillboardFocus()
    {
        if (billboard != null)
        {
            billboard.ClearFocus();
        }
    }

    IEnumerator EnterWithDelay()
    {
        yield return new WaitForSeconds(spawnDelay);
        if (hasLeftSchool) yield break;

        agent.Warp(entrancePoint.position);
        agent.isStopped = false;

        hasEnteredSchool = true;
        waitingToLeave = true;

        GoToRecess();
    }

    IEnumerator LeaveWithDelay()
    {
        yield return new WaitForSeconds(leaveDelay);
        if (hasLeftSchool) yield break;

        ClearBillboardFocus();

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(entrancePoint.position);

        isWalkingToExit = true;
    }

    void OnEnable()
    {
        SchoolTimeController.OnStateChanged += HandleState;
    }

    void OnDisable()
    {
        SchoolTimeController.OnStateChanged -= HandleState;
    }

    void HandleState(SchoolState state, int period)
    {
        if (inConversation) return;
        if (spawnAtDoor && !hasEnteredSchool) return;
        if (hasLeftSchool) return;
        if (isWalkingToExit) return;

        StartCoroutine(HandleStateWithDelay(state, period));
    }

    IEnumerator HandleStateWithDelay(SchoolState state, int period)
    {
        yield return new WaitForSeconds(Random.Range(0f, stateChangeDelay));
        if (inConversation) yield break;
        if (state == SchoolState.Class)
            GoToClass(period);
        else
            GoToRecess();   // Recess AND Lunch both route here
    }

    void GoToClass(int period)
    {
        string className = GetClass(period);
        ClassroomZone zone = ClassroomRegistry.Instance.GetClassroom(className);
        if (zone == null) return;

        Transform seat = zone.GetFreeSeat();
        if (seat == null) return;

        currentClassroom = zone;
        currentSeat = seat;
        currentFocus = zone.focusTarget;

        isSeated = false;
        rotationDone = false;

        ClearBillboardFocus();

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(seat.position);
    }

    void GoToRecess()
    {
        RecessZone zone = null;

        // 1. Try named zone first
        if (!string.IsNullOrEmpty(preferredRecessZone))
        {
            zone = ClassroomRegistry.Instance.GetRecessZone(preferredRecessZone);
        }

        // 2. If not found, fallback to random
        if (zone == null)
        {
            zone = ClassroomRegistry.Instance.GetRandomRecessZone();
        }

        if (zone == null) return;

        // 3. Restriction check
        if (zone.restricted)
        {
            if (zone.teachersOnly && !isTeacher)
            {
                Debug.Log($"{name} denied entry (teachers only zone)");
                zone = ClassroomRegistry.Instance.GetRandomRecessZone();
            }
        }

        Transform spot = zone.GetFreeSpot();
        if (spot == null) return;

        currentFocus = null;
        ClearBillboardFocus();

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(spot.position);
    }

    void SnapToCurrentState()
    {
        var controller = Controller;
        if (controller == null) return;

        float hour = GetCurrentHour();

        SchoolPeriod active = controller.periods[0];

        for (int i = 0; i < controller.periods.Length; i++)
        {
            if (controller.IsInPeriodPublic(hour, controller.periods[i]))
            {
                active = controller.periods[i];
                break;
            }
        }

        if (active.state == SchoolState.Class)
            GoToClass(active.periodIndex);
        else
            GoToRecess();
    }

    // Uses today's A/B day automatically.
    string GetClass(int period)
    {
        if (schedule == null) return "";
        bool isADay = (Controller != null) && Controller.IsADay();
        return schedule.GetClass(period, isADay);
    }

    float GetCurrentHour()
    {
        var c = Controller;
        if (c == null || c.character == null)
            return 0f;

        return c.character.GetStatValue(c.timeStatName) % 24f;
    }
}