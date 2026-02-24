
using System;

namespace StrangeLand.Steering
{
    
    [System.Flags]
    public enum WheelButtons : uint
    {
        None      = 0,
        Horn      = 1 << 0,
        HighBeams = 1 << 1,
        Select    = 1 << 2,
        LeftInd     = 1 << 3,
        RightInd    = 1 << 4,
        // add as needed
    }

    public readonly struct WheelState
    {
        public readonly float Steer, Throttle, Brake;
        public readonly WheelButtons Buttons;

        public WheelState(float steer, float throttle, float brake, WheelButtons buttons)
        {
            Steer = steer;
            Throttle = throttle;
            Brake = brake;
            Buttons = buttons;
        }

        public static readonly WheelState Default =
            new WheelState(0f, 0f, 0f, WheelButtons.None);
    }
    public interface ISteeringWheelSdk
    {
        bool Initialize();
        void Shutdown();
        void Update();
        int GetMaxController();
      
        bool IsConnected(int index);
        bool TryGetState(int index, out WheelState state);

        bool TryGetProductName(int index, out string name);
        
        bool PlayConstantForce(int index, int force);
        bool PlayDamperForce(int index, int force);
        bool PlaySpringForce(int index, int offset, int saturation, int coefficient);
        bool StopSpringForce(int index);
        
        static bool AnyPressed(byte[] b, params int[] idx)
        {
            for (int i = 0; i < idx.Length; i++)
                if ((uint)idx[i] < (uint)b.Length && b[idx[i]] > 0)
                    return true;
            return false;
        }
        
        
    }
    
}