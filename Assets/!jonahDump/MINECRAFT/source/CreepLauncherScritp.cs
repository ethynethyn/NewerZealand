using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CreepLauncherScritp : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Drag the object that has the Animator. If the Animator is on a CHILD, drag the child.")]
    [SerializeField] private Animator launcherAnimator;
    [SerializeField] private GameObject explosion;

    [Header("Animator state names")]
    [Tooltip("These must match the STATE names in the Animator Controller, not the clip names.")]
    [SerializeField] private string fireState = "cp";
    [SerializeField] private string idleState = "cm";

    [Header("Timing")]
    [Tooltip("How long the explosion stays alive. 0.2s is very short - most bursts need 0.5-1.5s.")]
    [SerializeField, Min(0f)] private float explosionDuration = 0.2f;

    [Header("Input")]
    [Tooltip("0 = left, 1 = right, 2 = middle")]
    [SerializeField] private int mouseButton = 1;

    // ---------------------------------------------------------------- SHOOTING

    [Header("Shot - Aim")]
    [Tooltip("The camera the shot fires from. Leave empty to use Camera.main.")]
    [SerializeField] private Transform aimSource;

    [Tooltip("How far in front of the camera the shot begins. Raise this if the shot keeps hitting the held creeper model.")]
    [SerializeField, Min(0f)] private float startDistance = 0f;

    [Header("Shot - Shape")]
    [Tooltip("Thickness of the shot. 0 = a thin laser. Higher = a fat cylinder that catches more.")]
    [SerializeField, Min(0f)] private float radius = 0.5f;

    [Tooltip("How far the shot reaches, in metres, measured from the start point.")]
    [SerializeField, Min(0f)] private float maxDistance = 100f;

    [Tooltip("Which physics layers the shot can see. Set this to exclude the player/creeper layer.")]
    [SerializeField] private LayerMask hitLayers = ~0;

    [SerializeField] private QueryTriggerInteraction hitTriggers = QueryTriggerInteraction.Ignore;

    [Header("Shot - Targets")]
    [Tooltip("Only objects with this exact tag get destroyed. The tag must exist in the Tag Manager.")]
    [SerializeField] private string targetTag = "Minecraft";

    [Tooltip("ON = the shot punches through and destroys every match along its path. OFF = only the nearest match.")]
    [SerializeField] private bool pierce = true;

    [Tooltip("ON = an untagged collider (a wall) stops the shot. OFF = the shot ignores walls entirely.")]
    [SerializeField] private bool blockedByObstacles = false;

    [Tooltip("Safety cap on how many objects a single shot can destroy.")]
    [SerializeField, Min(1)] private int maxTargets = 20;

    [Tooltip("ON = destroys the whole prefab root. OFF = destroys only the object the collider sits on.")]
    [SerializeField] private bool destroyEntireRoot = false;

    [Tooltip("Delay before the object actually disappears. Useful if it has its own death animation.")]
    [SerializeField, Min(0f)] private float destroyDelay = 0f;

    [Header("Shot - Debug")]
    [Tooltip("Draws the shot in the Scene view when this object is selected.")]
    [SerializeField] private bool drawGizmo = true;

    [Tooltip("Draws the shot in the Scene view for a moment each time you fire. Needs Gizmos enabled.")]
    [SerializeField] private bool drawOnFire = true;

    [Tooltip("Prints every object the shot touches to the Console.")]
    [SerializeField] private bool logHits = false;

    // ------------------------------------------------------------------------

    private Animator explosionAnimator;
    private ParticleSystem[] explosionParticles;
    private Coroutine routine;

    // Reused so firing does not allocate garbage every click.
    private readonly RaycastHit[] hitBuffer = new RaycastHit[64];

    private static readonly IComparer<RaycastHit> ByDistance =
        Comparer<RaycastHit>.Create((a, b) => a.distance.CompareTo(b.distance));

    private void Awake()
    {
        if (launcherAnimator == null)
        {
            Debug.LogError($"{name}: launcherAnimator not assigned.", this);
            enabled = false;
            return;
        }

        if (explosion == null)
        {
            Debug.LogError($"{name}: explosion not assigned.", this);
            enabled = false;
            return;
        }

        if (aimSource == null && Camera.main != null)
        {
            aimSource = Camera.main.transform;
        }

        if (aimSource == null)
        {
            Debug.LogError($"{name}: no Aim Source, and no camera is tagged MainCamera.", this);
            enabled = false;
            return;
        }

        // Cache these once. GetComponent every click is wasteful and hides null refs.
        // 'true' includes inactive children, which matters because explosion starts disabled.
        explosionAnimator = explosion.GetComponent<Animator>();
        explosionParticles = explosion.GetComponentsInChildren<ParticleSystem>(true);

        ValidateState(fireState);
        ValidateState(idleState);
        ValidateTag();

        explosion.SetActive(false);
    }

    private void ValidateState(string stateName)
    {
        // Catches the "clip is called cp but the state box is called New State" trap.
        if (!launcherAnimator.HasState(0, Animator.StringToHash(stateName)))
        {
            Debug.LogError(
                $"{name}: Animator has no state named '{stateName}' on layer 0. " +
                "Check the STATE name in the controller, not the clip name.", this);
        }
    }

    private void ValidateTag()
    {
        // CompareTag throws if the tag was never created, which would break every shot.
        try
        {
            gameObject.CompareTag(targetTag);
        }
        catch (UnityException)
        {
            Debug.LogError(
                $"{name}: the tag '{targetTag}' does not exist. " +
                "Add it under Edit > Project Settings > Tags and Layers.", this);
            enabled = false;
        }
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(mouseButton))
        {
            Fire();
        }
    }

    public void Fire()
    {
        // Passing 0f as normalizedTime forces a restart from frame 0 even if
        // the state is already playing. Play(stateName) alone does not.
        launcherAnimator.Play(fireState, 0, 0f);

        explosion.SetActive(true);

        if (explosionAnimator != null)
        {
            explosionAnimator.Rebind();   // reset to the controller's default state
            explosionAnimator.Update(0f); // apply this frame so there is no 1-frame pose pop
        }

        foreach (ParticleSystem ps in explosionParticles)
        {
            ps.Clear(false);
            ps.Play(false);
        }

        Shoot();

        // Kill the previous timer so a fast second click cannot cancel this explosion.
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(EndBurst());
    }

    /// <summary>
    /// Straight out of the centre of the camera view.
    /// Shared by firing and the gizmo so the two can never disagree.
    /// </summary>
    private bool GetShotRay(out Vector3 origin, out Vector3 direction)
    {
        Transform aim = aimSource != null
            ? aimSource
            : (Camera.main != null ? Camera.main.transform : null);

        if (aim == null)
        {
            origin = Vector3.zero;
            direction = Vector3.forward;
            return false;
        }

        direction = aim.forward;
        origin = aim.position + direction * startDistance;
        return true;
    }

    private void Shoot()
    {
        if (!GetShotRay(out Vector3 origin, out Vector3 direction)) return;

        int count = radius > 0f
            ? Physics.SphereCastNonAlloc(origin, radius, direction, hitBuffer, maxDistance, hitLayers, hitTriggers)
            : Physics.RaycastNonAlloc(origin, direction, hitBuffer, maxDistance, hitLayers, hitTriggers);

        // NonAlloc casts return hits in arbitrary order, so sort before walking outward.
        System.Array.Sort(hitBuffer, 0, count, ByDistance);

        int destroyed = 0;

        for (int i = 0; i < count; i++)
        {
            Collider col = hitBuffer[i].collider;
            if (col == null) continue;

            GameObject target = destroyEntireRoot ? col.transform.root.gameObject : col.gameObject;

            if (logHits)
            {
                Debug.Log($"Shot hit '{target.name}' (tag '{target.tag}') at {hitBuffer[i].distance:F2}m", target);
            }

            if (target.CompareTag(targetTag))
            {
                Destroy(target, destroyDelay);
                destroyed++;

                if (!pierce || destroyed >= maxTargets) break;
            }
            else if (blockedByObstacles)
            {
                break; // a wall swallows the rest of the shot
            }
        }

        if (drawOnFire)
        {
            Debug.DrawRay(origin, direction * maxDistance, Color.red, 1f);
        }
    }

    private IEnumerator EndBurst()
    {
        yield return new WaitForSeconds(explosionDuration);

        launcherAnimator.Play(idleState, 0, 0f);
        explosion.SetActive(false);
        routine = null;
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawGizmo) return;
        if (!GetShotRay(out Vector3 origin, out Vector3 direction)) return;

        Vector3 end = origin + direction * maxDistance;

        Gizmos.color = Color.red;
        Gizmos.DrawLine(origin, end);

        if (radius > 0f)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.35f);
            Gizmos.DrawWireSphere(origin, radius);
            Gizmos.DrawWireSphere(end, radius);
        }
    }
}