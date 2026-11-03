using System;
using UnityEngine;

// Raised by the Hunter; wolves react without ever referencing it.
public static class HunterSignals
{
    public static event Action HunterDied;

    public static void RaiseHunterDied() => HunterDied?.Invoke();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        HunterDied = null;
    }
}
