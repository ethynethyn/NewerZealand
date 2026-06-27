using UnityEngine;

/// <summary>
/// Add this to a collider (a trigger Box Collider stretched across a doorway works great)
/// to block Granny's INITIAL lock-on when the sightline between you and her passes through it.
///
/// - No layer or mask setup needed — Granny finds this component along the sightline automatically.
/// - The collider can be a TRIGGER so it doesn't block your movement through the doorway.
/// - Once Granny is already chasing you, she IGNORES this and follows you straight through.
///
/// So: opposite sides of this volume = she won't lock on. Same side = she can. Already chasing = she follows.
/// </summary>
[RequireComponent(typeof(Collider))]
public class GrannyDetectionBlocker : MonoBehaviour { }