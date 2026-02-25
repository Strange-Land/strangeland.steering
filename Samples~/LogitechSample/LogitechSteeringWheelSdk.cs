using System;
using System.Text;
using UnityEngine;
using StrangeLand.Steering;


public class LogitechSteeringWheelSdk : ISteeringWheelSdk
{

    // Update is called once per frame
    public bool Initialize()
    {
        return LogitechGSDK.LogiSteeringInitialize(false);
    }

    public void Shutdown()
    {
        LogitechGSDK.LogiSteeringShutdown();
    }

    public void Update()
    {
        LogitechGSDK.LogiUpdate();
    }

    public int GetMaxController()
    {
        return LogitechGSDK.LOGI_MAX_CONTROLLERS;
    }
    public bool IsConnected(int index)
    {
       return LogitechGSDK.LogiIsConnected(index);
    }

    public bool TryGetState(int index, out WheelState outState)
    {

        outState = WheelState.Default;
        LogitechGSDK.DIJOYSTATE2ENGINES state;
        state = LogitechGSDK.LogiGetStateCSharp(index);
        WheelButtons buttons = WheelButtons.None;
        
        
        if (ISteeringWheelSdk.AnyPressed(state.rgbButtons, 0,1,2,3,7,11,23))
            buttons |= WheelButtons.Horn;
        
        if (ISteeringWheelSdk.AnyPressed(state.rgbButtons, 6,10))
            buttons |= WheelButtons.HighBeams;
        
        if (state.rgbButtons[5] > 0)
            buttons |= WheelButtons.LeftInd;
        
        if (state.rgbButtons[4] > 0)
            buttons |= WheelButtons.RightInd;
                
        float steer = Mathf.Clamp(state.lX / 32768f,-1,1);
        float throttle = Mathf.Clamp01(((state.lY) / (-32768f)));
        float brake = Mathf.Clamp01((state.lRz) / (-32768f));

       
        outState = new WheelState(steer, throttle, brake, buttons);
        return true;

    }

    public bool TryGetProductName(int index, out string name)
    {
        name = String.Empty;
            int bufferSize = 256;
        StringBuilder productName = new StringBuilder(bufferSize);

        if (LogitechGSDK.LogiGetFriendlyProductName(index, productName, bufferSize))
        {
            name=  productName.ToString();
            return true;
        }

        return false;
    }

    public bool PlayConstantForce(int index, int force)
    {
        return  LogitechGSDK.LogiPlayConstantForce(index, force);
    }

    public bool PlayDamperForce(int index, int force)
    {
        return  LogitechGSDK.LogiPlayDamperForce(index, force);
    }

    public bool PlaySpringForce(int index, int offset, int saturation, int coefficient)
    {
        return LogitechGSDK.LogiPlaySpringForce(index, offset, saturation, coefficient);
    }

    public bool StopSpringForce(int index)
    {
        return LogitechGSDK.LogiStopSpringForce(index);
    }

  
}
