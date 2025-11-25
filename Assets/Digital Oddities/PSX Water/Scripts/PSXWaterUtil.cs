using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// This is a simple utility script to simplify the process of setting up the water in a case where it consists of two meshes.
/// </summary>
[ExecuteInEditMode]
public class PSXWaterUtil : MonoBehaviour
{
    #region Editor Data
    [Header("Water Material Helper")]
    [SerializeField] MeshRenderer _surfaceMesh;
	[SerializeField] Material _waterMaterial;
    [SerializeField] MeshRenderer _secondarySurfaceMesh;
	[SerializeField] Material _secondaryWaterMaterial;

    [Header("Y Scale Helper")]
    [SerializeField] bool _lockYScaleToOne = false;
    #endregion

    #region Validation
    private void OnValidate()
    {
        if (_waterMaterial == null || _secondarySurfaceMesh == null || _surfaceMesh == null || _secondarySurfaceMesh == null) return;

        _surfaceMesh.sharedMaterial = _waterMaterial;
        _secondarySurfaceMesh.sharedMaterial = _secondaryWaterMaterial;
    }
    #endregion

    #region Ticks
    private void Update()
    {
        if (!Application.isPlaying && _lockYScaleToOne) // Doesn't affect runtime
        {
            Vector3 scale = transform.localScale;
            if (scale.y != 1f)
            {
                transform.localScale = new Vector3(scale.x, 1f, scale.z);
            }
        }
    }
    #endregion
}
