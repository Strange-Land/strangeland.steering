
# StrangeLand Steering (UPM)

A small Unity package that standardizes steering wheel input + force feedback behind a single interface:

- Default: `NullSteeringWheelSdk` (no hardware required)
- Optional providers via **Samples** (e.g., Logitech Wheels)

## Install (Unity Package Manager)

Add this package to your project `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.strangeland.steering": "https://github.com/Strange-Land/strangeland.steering.git"
  }
}
```

To pin a specific version/commit:

```json
{
  "dependencies": {
    "com.strangeland.steering": "https://github.com/Strange-Land/strangeland.steering.git#<tag-or-commit>"
  }
}
```

## Samples (recommended)

Open **Window → Package Manager → (select) StrangeLand Steering → Samples** and import what you need.
Unity copies imported samples into `Assets/Samples/...` so you can edit them. (Unity “Samples” workflow)
[https://docs.unity3d.com/Manual/cus-samples.html](https://docs.unity3d.com/Manual/cus-samples.html)


### 1) Logitech Steering Wheel Integration (Sample)

This package **does not ship Logitech’s SDK binaries** (they are distributed by Logitech).
To enable Logitech wheels, import the **Logitech Steering Wheel Integration** sample and then install the SDK files below.

#### Step A — Download Logitech Steering Wheel SDK

Download the **STEERING WHEEL SDK** from Logitech Partner Developer Lab:

```text
https://www.logitechg.com/en-my/programs/partner-developer-lab
```

(You need the “DOWNLOAD FOR WINDOWS” entry under “STEERING WHEEL SDK”.)

#### Step B — Copy the required files into your Unity project

From the Logitech SDK, copy:

* `Include/LogitechGSDK.cs`
* `Lib/GameEnginesWrapper/x64/LogitechSteeringWheelEnginesWrapper.dll`

Into your project (suggested location):

```text
Assets/Plugins/Logitech/
  LogitechGSDK.cs
  LogitechSteeringWheelEnginesWrapper.dll
```

After this, press Play. The sample’s installer will register the Logitech provider.



## Implementing other providers 

If you want support for other wheels (beyond Logitech), implement `ISteeringWheelSdk` and provide your own sample (recommended). Your provider should output normalized values:

* `Steer` in `[-1, 1]`
* `Throttle` in `[0, 1]`
* `Brake` in `[0, 1]`
* Buttons as a bitmask (`[Flags] enum`)

Skeleton:

```csharp
public sealed class MyWheelSdk : ISteeringWheelSdk
{
    public void Initialize() {}
    public void Shutdown() {}
    public void Update() {}

    public int GetMaxController() => 4;
    public bool IsConnected(int index) => false;
    public bool TryGetProductName(int index, out string name) { name = "Unknown"; return false; }

    public bool TryGetState(int index, out WheelState state)
    {
        state = WheelState.Default;
        return false;
    }

    public bool PlayConstantForce(int index, int force) => false;
    public bool PlayDamperForce(int index, int force) => false;
    public bool PlaySpringForce(int index, int offset, int saturation, int coefficient) => false;
    public bool StopSpringForce(int index) => false;
}
```

Then register it via a small installer in your sample:

```csharp
[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
static void Install()
{
    if (!(SteeringWheelManager.Sdk is NullSteeringWheelSdk)) return;
    SteeringWheelManager.Sdk = new MyWheelSdk();
}
```

## Notes

* Logitech integration requires Windows (native DLL).
* If no provider is installed, the system safely falls back to `NullSteeringWheelSdk`.


[1]: https://www.logitechg.com/en-my/innovation/developer-lab.html "Partner Developer Lab | Logitech G"
