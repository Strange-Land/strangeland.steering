/*
 * Copyright (C) 2016, Jaguar Land Rover
 * This program is licensed under the terms and conditions of the
 * Mozilla Public License, version 2.0.  The full text of the
 * Mozilla Public License is at https://www.mozilla.org/MPL/2.0/
 * Modified singificantly by the StrangeLand Team.
 */

using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Core.Networking;
using Unity.Netcode;


namespace StrangeLand.Steering
{
    public class SteeringWheelManager : MonoBehaviour
    {
        /// <summary>
        /// This can be overwritten by who ever impliments the ISteeringWheelSdk interface.
        /// Look at the logitech example of how we use  RuntimeinilizeLoadType... to replace this public variable.
        /// Important to note it doesn't not support multiple wheel providers if that what you need youd need to
        /// create your own custom class the interfaces with the interfaces and then contexts to the sdks separatly.
        /// 
        /// </summary>
        public static ISteeringWheelSdk Sdk = new NullSteeringWheelSdk(); 
        
        private class SteeringWheelData
        {
            public int wheelIndex;
            public float steerInput;
            public float accelInput;
            public int constant;
            public int damper;
            public int springSaturation;
            public int springCoefficient;
            public bool running;
            public bool forceFeedbackPlaying;
            public float gas;
            public float brake;

            public bool L_IndSwitch;
            public bool R_IndSwitch;
            public bool HornButton;
            public bool HighBeamButton;

            public SteeringWheelData(int index)
            {
                steerInput = 0f;
                wheelIndex = index;
                accelInput = 0f;
                constant = 0;
                damper = 0;
                springSaturation = 0;
                springCoefficient = 0;
                forceFeedbackPlaying = false;
                gas = 0;
                brake = 0;
                running = false;
                L_IndSwitch = false;
                R_IndSwitch = false;
                HornButton = false;
                HighBeamButton = false;
            }
        }

        private bool ready = false;
     

        public float FFBGain = 1f;
        

        private Dictionary<ParticipantOrder, SteeringWheelData> ActiveWheels =
            new Dictionary<ParticipantOrder, SteeringWheelData>();

        private const String FileName = "ActiveWheels.conf";
        public static SteeringWheelManager Instance { get; private set; }

        private void SetSingleton()
        {
            Instance = this;
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }

            SetSingleton();
            DontDestroyOnLoad(gameObject);

            StartCoroutine(InitializationRoutine());
        }

        IEnumerator InitializationRoutine()
        {
            // Use yield here
            yield return new WaitUntil(() => NetworkManager.Singleton != null);
            NetworkManager.Singleton.OnServerStarted += StartingInternal;

        }

        private void StartingInternal()
        {
            Init();
        }

        void Start()
        {
            FFBGain = 1.0f;
        }

        public void Init()
        {
            ready = true;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            try
            {
                Sdk.Initialize();
            }
            catch (DllNotFoundException e)
            {
                Debug.LogError("Logitech DLL missing! Wheel support disabled.");
                Debug.Log(e);
            }
#endif
            
            
            AssignSteeringWheels();
            var initForceFeedback = InitForceFeedback();
            StartCoroutine(initForceFeedback);
        }


        public static int IntRemap(float value, float inLow = -10000, float inHigh = 10000, float outLow = -100,
            float outHigh = 100)
        {
            float returnVal = (value - inLow) / (inHigh - inLow) * (outHigh - outLow) + outLow;
            return (int)returnVal;
        }

        // when this gets destroyed or the application quits, we need to clean up the steering wheel

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        void OnApplicationQuit()
        {
            CleanUp();
            Sdk.Shutdown();
        }

        private void OnDisable()
        {
            CleanUp();
            Sdk.Shutdown();
        }

        private void OnDestroy()
        {
            CleanUp();
            Sdk.Shutdown();
        }
#endif


        IEnumerator SpringforceFix()
        {
            yield return new WaitForSeconds(1f);
            StopSpringForce();
            yield return new WaitForSeconds(0.5f);
            InitSpringForce(0, 0);
        }

        void AssignSteeringWheels()
        {
            ParticipantOrder po = ParticipantOrder.A;
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            for (int i = 0; i < GetNumberOfConnectedDevices(); i++)
            {
                Debug.Log("We got the input controller called" + GetProductName(i) +
                          "Assigning it to participant: " + po.ToString());
                ActiveWheels.Add(po, new SteeringWheelData(i));
                po++;
            }
#endif
        }

        public string GetProductName(int index)
        {
         
            if (Sdk.TryGetProductName(index, out string productName))
            {
                return productName;
            }
            
                return "Unknown";
            
        }

        public static int GetNumberOfConnectedDevices()
        {
            int connectedDevicesCount = 0;
            for (int i = 0; i < Sdk.GetMaxController(); i++)
            {
                if (Sdk.IsConnected(i))
                {
                    connectedDevicesCount++;
                }
            }

            return connectedDevicesCount;
        }


        public void CleanUp()
        {
            foreach (SteeringWheelData steeringWheelData in ActiveWheels.Values)
            {
                steeringWheelData.forceFeedbackPlaying = false;
                steeringWheelData.constant = 0;
                steeringWheelData.damper = 0;
            }
        }

        public void SetConstantForce(int force, ParticipantOrder po)
        {
            if (ActiveWheels.ContainsKey(po))
            {
                var steeringWheelData = ActiveWheels[po];
                steeringWheelData.constant = force;
            }
        }

        public void SetDamperForce(int force, ParticipantOrder po)
        {
            if (ActiveWheels.ContainsKey(po))
            {
                var steeringWheelData = ActiveWheels[po];
                steeringWheelData.damper = force;
            }
        }

        public void SetSpringForce(int sat, int coeff, ParticipantOrder po)
        {
            if (ActiveWheels.ContainsKey(po))
            {
                var steeringWheelData = ActiveWheels[po];
                steeringWheelData.springCoefficient = coeff;
                steeringWheelData.springSaturation = sat;
            }
        }


        public void InitSpringForce(int sat, int coeff)
        {
            StartCoroutine(_InitSpringForce(sat, coeff));
        }

        public void StopSpringForce()
        {
            foreach (SteeringWheelData swd in ActiveWheels.Values)
            {
                swd.forceFeedbackPlaying = false;

                Debug.Log("stopping spring" + Sdk.StopSpringForce(swd.wheelIndex));
            }
        }

        private IEnumerator _InitSpringForce(int sat, int coeff)
        {
            yield return new WaitForSeconds(1f);
            bool res = false;
            int tries = 0;
            foreach (SteeringWheelData swd in ActiveWheels.Values)
            {
                while (res == false)
                {
                    res = Sdk.PlaySpringForce(swd.wheelIndex, 0, IntRemap(sat * FFBGain),
                        IntRemap(coeff * FFBGain));
                    Debug.Log("starting spring for the wheel" + res);

                    tries++;
                    if (tries > 150)
                    {
                        Debug.Log("Coudn't init spring force for the steering wheel. Aborting");
                        break;
                    }

                    yield return null;
                }
            }
        }

        private ParticipantOrder CallibrationTarget = ParticipantOrder.None;

        public void OnGUI() // ToDo turn this into a canvas interface
        {
            if (ConnectionAndSpawning.Instance == null) return; 
            if (ConnectionAndSpawning.Instance.ServerStateEnum.Value == EServerState.WaitingRoom)
            {
                if (CallibrationTarget != ParticipantOrder.None)
                {
                    foreach (SteeringWheelData swd in ActiveWheels.Values)
                    {
                        if (swd.HornButton)
                        {
                            switchSteeringWheels(swd.wheelIndex, CallibrationTarget);
                            CallibrationTarget = ParticipantOrder.None;
                            break;
                        }
                    }

                    GUI.Label(new Rect(160 + 75, 10, 25 * 6, 50),
                        "Press a button for " + CallibrationTarget.ToString());
                    return;
                }

                List<ParticipantOrder> tmp =
                    new List<ParticipantOrder>(Enum.GetValues(typeof(ParticipantOrder)).Cast<ParticipantOrder>()
                        .ToArray());
                tmp.Remove(ParticipantOrder.None);
                int x = 75;

                foreach (var v in tmp)
                {
                    if (GUI.Button(new Rect(160 + x, 10, 25, 25), v.ToString()))
                    {
                        CallibrationTarget = v;
                        return;
                    }

                    GUI.Label(new Rect(160 + x, 25, 25, 25), v.ToString());
                    var text = "";
                    if (ActiveWheels.ContainsKey(v))
                    {
                        text = ActiveWheels[v].wheelIndex.ToString();
                    }

                    var prev = text;
                    text = GUI.TextField(new Rect(150 + x, 50, 25, 25), text, 2);
                    if (prev != text)
                    {
                        if (int.TryParse(text, out int newIndex))
                        {
                            switchSteeringWheels(newIndex, v);
                            text = prev;
                        }
                    }

                    x += 25;
                }
            }
        }

        private void switchSteeringWheels(int newIndex, ParticipantOrder v)
        {
            List<ParticipantOrder> tmp =
                new List<ParticipantOrder>(Enum.GetValues(typeof(ParticipantOrder)).Cast<ParticipantOrder>().ToArray());
            tmp.Remove(ParticipantOrder.None);

            ParticipantOrder switchPartner = ParticipantOrder.None;
            foreach (var switchOrder in tmp)
            {
                if (ActiveWheels.ContainsKey(switchOrder) && ActiveWheels[switchOrder].wheelIndex == newIndex)
                {
                    switchPartner = switchOrder;
                    break;
                }
            }

            if (switchPartner != ParticipantOrder.None)
            {
                if (ActiveWheels.ContainsKey(v))
                {
                    (ActiveWheels[v], ActiveWheels[switchPartner]) =
                        (ActiveWheels[switchPartner], ActiveWheels[v]);
                    return;
                }
                else
                {
                    ActiveWheels[v] = ActiveWheels[switchPartner];
                    ActiveWheels.Remove(switchPartner);
                    return;
                }
            }
        }

        private IEnumerator InitForceFeedback()
        {
            foreach (SteeringWheelData swd in ActiveWheels.Values)
            {
                swd.constant = 0;
                swd.damper = 0;
                swd.springCoefficient = 0;
                swd.springSaturation = 0;
            }

            yield return new WaitForSeconds(0.5f);
            foreach (SteeringWheelData swd in ActiveWheels.Values)
            {
                swd.forceFeedbackPlaying = true;
            }
        }


        void Update()
        {
            if (!ready || ActiveWheels == null) return;
            if (Application.platform == RuntimePlatform.OSXEditor) return;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
            Sdk.Update();
            ready = GetNumberOfConnectedDevices() > 0;
#endif

       //    Debug.Log($"ActiveWheels.Count(): {ActiveWheels.Count()}");
            foreach (SteeringWheelData swd in ActiveWheels.Values)
            {


                bool success = Sdk.TryGetState(swd.wheelIndex, out WheelState state);
                if (!success) continue;

                swd.steerInput = state.Steer;
                swd.gas = 0.9f * swd.gas + 0.1f * (state.Throttle);
                swd.brake = state.Brake;
                swd.accelInput = swd.gas  -swd.brake ;

                swd.L_IndSwitch = (state.Buttons & WheelButtons.LeftInd) != 0;
                swd.R_IndSwitch = (state.Buttons & WheelButtons.RightInd)!=0;

                swd.HornButton =(state.Buttons & WheelButtons.Horn)!=0;

                swd.HighBeamButton = (state.Buttons & WheelButtons.HighBeams)!=0;

                if (swd.forceFeedbackPlaying)
                {
                    float constRaw = swd.constant * FFBGain;
                    float damperRaw = swd.damper * FFBGain;
                    float springSatRaw = (swd.springSaturation <= 0 ? 1 : swd.springSaturation) * FFBGain;
                    float springCoefRaw = swd.springCoefficient;

                    int constInt = IntRemap(constRaw);
                    int damperInt = IntRemap(damperRaw);
                    int springSatInt = IntRemap(springSatRaw);
                    int springCoefInt = IntRemap(springCoefRaw);

//                    Debug.Log(
                  //      $"FFB RAW  c:{constRaw:F4} d:{damperRaw:F4} sat:{springSatRaw:F4} coef:{springCoefRaw:F4} | " +
                 //       $"INT  c:{constInt} d:{damperInt} sat:{springSatInt} coef:{springCoefInt}"
                 //   );
                    Sdk.PlayConstantForce(swd.wheelIndex, constInt);
                    Sdk.PlayDamperForce(swd.wheelIndex, damperInt);
                    Sdk.PlaySpringForce(swd.wheelIndex, 0, springSatInt, springCoefInt);
                }

                
            }
        }

        public void GetAccelBrakeInput(out float accel, out float brk, ParticipantOrder po)
        {
            if (ActiveWheels.ContainsKey(po))
            {
                accel = ActiveWheels[po].gas;
                brk = ActiveWheels[po].brake;
            }
            else
            {
                accel = 0;
                brk = 0;
            }
        }

        public bool GetLeftIndicatorInput(ParticipantOrder po)
        {
            if (ActiveWheels.ContainsKey(po))
            {
                return ActiveWheels[po].L_IndSwitch;
            }
            else
            {
                return false;
            }
        }

        public bool GetRightIndicatorInput(ParticipantOrder po)
        {
            if (ActiveWheels.ContainsKey(po))
            {
                return ActiveWheels[po].R_IndSwitch;
            }
            else
            {
                return false;
            }
        }

        public bool GetHornButtonInput(ParticipantOrder po)
        {
            if (ActiveWheels.ContainsKey(po))
            {
                return ActiveWheels[po].HornButton;
            }

            if (po == ParticipantOrder.A && Input.GetKey(KeyCode.A) && Input.GetKey(KeyCode.LeftShift))
            {
                return true;
            }

            if (po == ParticipantOrder.B && Input.GetKey(KeyCode.B) && Input.GetKey(KeyCode.LeftShift))
            {
                return true;
            }

            return false;
        }

        public bool GetHighBeamButtonInput(ParticipantOrder po)
        {
            if (ActiveWheels.ContainsKey(po))
            {
                return ActiveWheels[po].HighBeamButton;
            }

            return false;
        }

        public float GetAccelInput(ParticipantOrder po)
        {
            if (ActiveWheels.ContainsKey(po))
            {
                return ActiveWheels[po].accelInput;
            }
            else
            {
                return -2;
            }
        }

        public float GetSteerInput(ParticipantOrder po)
        {
            if (ActiveWheels.ContainsKey(po))
            {
                return ActiveWheels[po].steerInput;
            }
            else
            {
                return -2;
            }
        }

        public float GetHandBrakeInput()
        {
            return 0f;
        }
    }
}