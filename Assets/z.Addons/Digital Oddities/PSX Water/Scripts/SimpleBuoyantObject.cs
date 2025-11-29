using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class SimpleBuoyantObject : MonoBehaviour
{
    #region Editor Data
    [Header("Wave Attributes - Match these to the wave properties of the water material")]
    [SerializeField] float _waveSpeed = 1;
    [SerializeField] float _waveStrength = 1;
    [SerializeField] float _waveLength = 1;
    
    [Header("Water Object - We need this to know the y value of the water")]
    [SerializeField] Transform _water;

    [Header("Buoyancy")]
    [SerializeField ,Range(1, 5)] float strength = 1f;
    [SerializeField, Range(0.2f, 5)] public float objectDepth = 1f;

    [Space(10)]

    [SerializeField] float velocityDrag = 0.99f;
    [SerializeField] float angularDrag = 0.5f;

    [Header("Effectors")]
    [SerializeField] Transform[] effectors;
    #endregion

    #region Internal Data
    private Rigidbody _rb;
    private Vector3[] _effectorProjections;
    #endregion

    #region INIT
    void Awake()
    {
        // Get rigidbody
        _rb = GetComponent<Rigidbody>();
        _rb.useGravity = false;

        // Setup Effectors
        _effectorProjections = new Vector3[effectors.Length];
        for (int i = 0; i < effectors.Length; i++) _effectorProjections[i] = effectors[i].position;
    }

    void OnDisable()
    {
        _rb.useGravity = true;
    }
    #endregion

    #region Ticks
    void FixedUpdate()
    {
        if (effectors.Length <= 0) return;

        ApplyBuoyancy();
    }
    #endregion

    #region Buoyancy Logic
    private void ApplyBuoyancy()
    {
        int effectorAmount = effectors.Length;

        for (int i = 0; i < effectorAmount; i++)
        {
            Vector3 effectorPosition = effectors[i].position;

            _effectorProjections[i] = effectorPosition;
            _effectorProjections[i].y = _water.transform.position.y + GetWaveDisplacementAtLocation(effectorPosition, _waveSpeed, _waveStrength, _waveLength);

            // gravity
            _rb.AddForceAtPosition(Physics.gravity / effectorAmount, effectorPosition, ForceMode.Acceleration);

            float waveHeight = _effectorProjections[i].y;
            float effectorHeight = effectorPosition.y;

            if (effectorHeight < waveHeight) // submerged
            {
                float submersion = Mathf.Clamp01(waveHeight - effectorHeight) / objectDepth;
                float buoyancy = Mathf.Abs(Physics.gravity.y) * submersion * strength;

                // buoyancy
                _rb.AddForceAtPosition(Vector3.up * buoyancy, effectorPosition, ForceMode.Acceleration);

                // drag
                _rb.AddForce(-_rb.linearVelocity * velocityDrag * Time.fixedDeltaTime, ForceMode.VelocityChange);

                // torque
                _rb.AddTorque(-_rb.angularVelocity * angularDrag * Time.fixedDeltaTime, ForceMode.Impulse);
            }
        }
    }
    #endregion

    #region Utils
    private float GetWaveDisplacementAtLocation(Vector3 position, float waveSpeed, float waveStrength, float waveLength)
    {
        float magic = position.x + position.z;
        magic *= _waveLength;

        var speed = waveSpeed * Time.time;
        magic += speed;

        var sin = Mathf.Sin(magic);
        sin *= waveStrength;

        return sin;
    }
    #endregion



    

    #region Gizmos
    private Color red = new Color(0.92f, 0.25f, 0.2f);
    private Color green = new Color(0.2f, 0.92f, 0.51f);
    private Color blue = new Color(0.2f, 0.67f, 0.92f);
    private Color orange = new Color(0.97f, 0.79f, 0.26f);
    private void OnDrawGizmos()
    {
        if (effectors == null) return;

        for (int i = 0; i < effectors.Length; i++)
        {
            if (!Application.isPlaying && effectors[i] != null)
            {
                Gizmos.color = green;
                Gizmos.DrawSphere(effectors[i].position, 0.06f);
            }

            else
            {
                if (effectors[i] == null) return;

                if (effectors[i].position.y < _effectorProjections[i].y) Gizmos.color = red; //submerged
                else Gizmos.color = green;

                Gizmos.DrawSphere(effectors[i].position, 0.06f);

                Gizmos.color = orange;
                Gizmos.DrawSphere(_effectorProjections[i], 0.06f);

                Gizmos.color = blue;
                Gizmos.DrawLine(effectors[i].position, _effectorProjections[i]);
            }
        }
    }
    #endregion
}
