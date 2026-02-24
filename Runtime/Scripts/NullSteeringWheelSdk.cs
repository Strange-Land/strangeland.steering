namespace StrangeLand.Steering
{
    public sealed class NullSteeringWheelSdk : ISteeringWheelSdk
    {
        
        public bool Initialize()
        {
            return true;
        }

        public void Shutdown() { }
        public void Update() { }
        public int GetMaxController()
        {
            return 0;
        }

        public bool IsConnected(int index) => false;
        public bool TryGetFriendlyName(int index, out string name) { name = ""; return false; }
        public bool TryGetState(int index, out WheelState state) { state = default; return false; }
        public bool TryGetProductName(int index, out string name)
        {
            name = "NullSteeringWheelSdk";
          return true; // True because its more likley debuggers might see this and start investiagate why their wheel doesnt work!
          
        }

        public bool PlayConstantForce(int index, int force)
        {
            return false;
        }

        public bool PlayDamperForce(int index, int force)
        {
            return false;
        }

        public bool PlaySpringForce(int index, int offset, int saturation, int coefficient)
        {
            return false;
        }

        public bool StopSpringForce(int index)
        {
            return true;
        }
        
    }
}