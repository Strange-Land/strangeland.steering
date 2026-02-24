using System;
using UnityEngine;
using StrangeLand.Steering;

using Core.Networking;
using UnityEngine.InputSystem;

public class SteeringWheelDebug : MonoBehaviour
{
    public ParticipantOrder participant = ParticipantOrder.A;
    public bool logEveryFrame = false;


    private void Start()
    {
        SteeringWheelManager.Instance.Init();
    }

    void Update()
    {
        var mgr = SteeringWheelManager.Instance;
        if (mgr == null) return;

        float steer = mgr.GetSteerInput(participant);
        float accel = mgr.GetAccelInput(participant);

        bool leftInd  = mgr.GetLeftIndicatorInput(participant);
        bool rightInd = mgr.GetRightIndicatorInput(participant);
        bool horn     = mgr.GetHornButtonInput(participant);
        bool highBeam = mgr.GetHighBeamButtonInput(participant);

       
        if (Keyboard.current?.spaceKey.wasPressedThisFrame ?? false)
        {
            Debug.Log(
                $"[{participant}] " +
                $"Steer: {steer:F2} | " +
                $"Accel: {accel:F2} | " +
                $"L:{leftInd} R:{rightInd} " +
                $"Horn:{horn} HB:{highBeam}"
            );
        }

        // optional: test force feedback
        if (Keyboard.current?.fKey.wasPressedThisFrame ?? false)
        {
            Debug.Log("FFB test ON");
            mgr.SetConstantForce(5000, participant);
        }

        if (Keyboard.current?.fKey.wasReleasedThisFrame ?? false)
        {
            Debug.Log("FFB test OFF");
            mgr.SetConstantForce(0, participant);
        }
    }
}
