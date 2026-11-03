using System;
using UnityEngine;

// Wolves only share the position where the target was last seen, never a live reference to it.
public static class WolfAlertBus
{
    public static event Action<FSMAgentWolf, Vector3> AlertRaised;

    public static void Raise(FSMAgentWolf source, Vector3 lastKnownPosition)
    {
        AlertRaised?.Invoke(source, lastKnownPosition);
    }

    // Domain reload may be disabled, so stale subscribers must be dropped explicitly.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        AlertRaised = null;
    }
}
