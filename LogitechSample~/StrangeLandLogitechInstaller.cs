using StrangeLand.Steering;
using UnityEngine;

public static class StrangeLandLogitechInstaller
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Install()
    {
        // already replaced by another provider?
        if (SteeringWheelManager.Sdk is not NullSteeringWheelSdk)
        {
            Debug.LogError(
                $"StrangeLand: Steering wheel provider already set to " +
                $"{SteeringWheelManager.Sdk.GetType().Name}. " +
                $"Logitech provider will NOT install."
            );
            return;
        }

        SteeringWheelManager.Sdk = new LogitechSteeringWheelSdk();

        Debug.Log("StrangeLand: Logitech steering wheel SDK installed.");
    }
}
