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

    void Start()
    {
        if (spawnAtDoor && spawnPoint != null)
            InitDay();
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

        // 🕐 Late wake catch-up: if the current hour is already past arrival,
        // skip the wait and snap the NPC to wherever they should be right now.
        float currentHour = GetCurrentHour();
        if (currentHour >= arrivalHour && currentHour < leaveHour)
        {
            waitingToEnter = false;
            hasEnteredSchool = true;
            waitingToLeave = true;
            agent.Warp(entrancePoint.position);
            agent.isStopped = false;

            // Ask the time controller what state is active right now and go there
            SnapToCurrentState();

            Debug.Log($"{name} [LATE WAKE] snapped at hour {currentHour:0.00}");
        }
        else
        {
            Debug.Log($"{name} [NEW DAY] arrives {arrivalHour:0.00} (+{spawnDelay}s), leaves {leaveHour} (+{leaveDelay}s)");
        }
    }

    // Reads the current school state directly and moves the NPC immediately,
    // without waiting for the next OnStateChanged event.
    void SnapToCurrentState()
    {
        var controller = FindObjectOfType<SchoolTimeController>();
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

    void Update()
    {
        float currentHour = GetCurrentHour();

        // 🔁 DAILY RESET — clock has looped back before the leave window starts,
        // meaning a new day has begun. Covers overnight reset AND handles a player
        // who slept through part of the day correctly on next load.
        if (!dayResetting && hasLeftSchool && currentHour < leaveWindow.x)
        {
            dayResetting = true;
            InitDay();
            return;
        }

        // 🟢 ENTER SCHOOL
        if (spawnAtDoor && !hasEnteredSchool && waitingToEnter)
        {
            if (currentHour >= arrivalHour)
            {
                waitingToEnter = false;
                StartCoroutine(EnterWithDelay());
            }
        }

        // 🔴 TRIGGER LEAVE WALK
        if (spawnAtDoor && hasEnteredSchool && !hasLeftSchool && waitingToLeave && !isWalkingToExit)
        {
            if (currentHour >= leaveHour)
            {
                waitingToLeave = false;
                StartCoroutine(LeaveWithDelay());
            }
        }

        // 🚪 CHECK IF NPC HAS REACHED THE ENTRANCE TO EXIT
        if (isWalkingToExit)
        {
            bool pathDone = !agent.pathPending
                && agent.remainingDistance <= arrivalDistanceThreshold
                && (!agent.hasPath || agent.velocity.sqrMagnitude < 0.1f);

            if (pathDone)
            {
                isWalkingToExit = false;
                hasLeftSchool = true;
                agent.isStopped = true;
                agent.ResetPath();

                if (spawnPoint != null)
                {
                    bool warped = agent.Warp(spawnPoint.position);
                    if (!warped) transform.position = spawnPoint.position;
                }

                Debug.Log($"🏠 {name} LEFT SCHOOL");
            }
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

        Debug.Log($"🚪 {name} ENTERED");
        GoToRecess();
    }

    IEnumerator LeaveWithDelay()
    {
        yield return new WaitForSeconds(leaveDelay);
        if (hasLeftSchool) yield break;

        Debug.Log($"🚶 {name} walking to exit...");
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
        if (spawnAtDoor && !hasEnteredSchool) return;
        if (hasLeftSchool) return;
        if (isWalkingToExit) return;

        StartCoroutine(HandleStateWithDelay(state, period));
    }

    IEnumerator HandleStateWithDelay(SchoolState state, int period)
    {
        float delay = Random.Range(0f, stateChangeDelay);
        yield return new WaitForSeconds(delay);

        if (isWalkingToExit || hasLeftSchool) yield break;

        if (state == SchoolState.Class)
            GoToClass(period);
        else if (state == SchoolState.Recess || state == SchoolState.AfterSchool)
            GoToRecess();
    }

    void GoToClass(int period)
    {
        string className = GetClass(period);
        ClassroomZone zone = ClassroomRegistry.Instance.GetClassroom(className);
        if (zone == null) return;

        Transform seat = zone.GetFreeSeat();
        if (seat == null) return;

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(seat.position);
    }

    void GoToRecess()
    {
        if (ClassroomRegistry.Instance.recessZone == null) return;

        Transform spot = ClassroomRegistry.Instance.recessZone.GetRandomSpot();
        if (spot == null) return;

        agent.isStopped = false;
        agent.ResetPath();
        agent.SetDestination(spot.position);
    }

    string GetClass(int period)
    {
        switch (period)
        {
            case 0: return schedule.period1Class;
            case 1: return schedule.period2Class;
            case 2: return schedule.period3Class;
            case 3: return schedule.period4Class;
        }
        return "";
    }

    float GetCurrentHour()
    {
        var controller = FindObjectOfType<SchoolTimeController>();
        if (controller == null || controller.character == null)
            return 0f;
        return controller.character.GetStatValue(controller.timeStatName) % 24f;
    }
}