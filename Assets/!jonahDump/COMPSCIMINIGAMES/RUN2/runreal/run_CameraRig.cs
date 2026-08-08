using UnityEngine;

// Sits inside the tunnel behind the player, looking down it.
// Never rolls — the tunnel rotates instead, which is what makes
// face-switching feel like Run.
// All positioning is done in the manager's local space, so the
// whole minigame can live anywhere in your scene (any position
// or rotation) and the camera still works.
public class run_CameraRig : MonoBehaviour
{
    run_GameManager gm;
    public Camera Cam { get; private set; }
    float shakeTimer;
    Vector3 localPos; // camera position in gm-local space

    public void Init(run_GameManager manager)
    {
        gm = manager;

        if (gm.cam.createCamera)
        {
            var go = new GameObject("run_Camera");
            go.transform.SetParent(transform, false);
            Cam = go.AddComponent<Camera>();
            if (gm.cam.addAudioListener && FindObjectOfType<AudioListener>() == null)
                go.AddComponent<AudioListener>();
        }
        else
        {
            Cam = Camera.main;
        }

        if (Cam == null)
        {
            Debug.LogWarning("run_CameraRig: no camera found (enable Create Camera or tag one MainCamera)");
            return;
        }

        Cam.fieldOfView = gm.cam.fov;
        Cam.nearClipPlane = 0.05f;
        Cam.clearFlags = CameraClearFlags.SolidColor;
        Cam.backgroundColor = gm.colors.backgroundColor;
        SnapToPlayer();
    }

    public void Shake() { shakeTimer = gm.cam.shakeTime; }

    public void SnapToPlayer()
    {
        if (gm.Player == null || Cam == null) return;
        localPos = DesiredLocalPos();
        Apply(localPos);
    }

    Vector3 DesiredLocalPos()
    {
        return new Vector3(0f, -gm.Tunnel.Apothem + gm.cam.height, gm.Player.Z - gm.cam.distanceBehind);
    }

    Vector3 LookLocalPos()
    {
        return new Vector3(0f, -gm.Tunnel.Apothem + gm.cam.lookHeight, gm.Player.Z + gm.cam.lookAhead);
    }

    void LateUpdate()
    {
        if (Cam == null || gm.Player == null) return;
        float k = 1f - Mathf.Exp(-gm.cam.followSmoothing * Time.deltaTime);
        localPos = Vector3.Lerp(localPos, DesiredLocalPos(), k);

        Vector3 shake = Vector3.zero;
        if (shakeTimer > 0f)
        {
            shakeTimer -= Time.deltaTime;
            shake = (Vector3)(Random.insideUnitCircle * gm.cam.shakeAmount * (shakeTimer / gm.cam.shakeTime));
        }

        Apply(localPos + shake);
    }

    void Apply(Vector3 lp)
    {
        Vector3 world = gm.transform.TransformPoint(lp);
        Vector3 lookWorld = gm.transform.TransformPoint(LookLocalPos());
        Cam.transform.position = world;
        Cam.transform.rotation = Quaternion.LookRotation(lookWorld - world, gm.transform.up);
    }
}