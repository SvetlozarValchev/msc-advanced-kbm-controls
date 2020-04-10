using System;
using System.Reflection;
using MSCLoader;
using UnityEngine;
using Harmony;
using HutongGames.PlayMaker;

namespace AdvancedKBMControls
{
    public class AdvancedKBMControls : Mod
    {
        public override string ID => "AdvancedKBMControls"; //Your mod ID (unique)
        public override string Name => "Advanced KB&M Controls"; //You mod name
        public override string Author => "cbethax"; //Your Username
        public override string Version => "1.0.1"; //Version
        public override bool UseAssetsFolder => false;

        readonly Keybind disableSteerKey = new Keybind("DisableSteer", "Disable Steer", KeyCode.LeftControl);
        readonly Keybind toggleSticky = new Keybind("ToggleSticky", "Toggle Sticky Throttle/Brake", KeyCode.CapsLock);

        readonly Keybind[] throttleKeys = new Keybind[] {
            new Keybind("Throttle1", "Throttle #1", KeyCode.T),
            new Keybind("Throttle2", "Throttle #2", KeyCode.R),
            new Keybind("Throttle3", "Throttle #3", KeyCode.E),
            new Keybind("Throttle4", "Throttle #4", KeyCode.W),
            new Keybind("Throttle5", "Throttle #5", KeyCode.Q),
        };

        readonly Keybind[] brakeKeys = new Keybind[] {
            new Keybind("Brake1", "Brake #1", KeyCode.G),
            new Keybind("Brake2", "Brake #2", KeyCode.F),
            new Keybind("Brake3", "Brake #3", KeyCode.D),
            new Keybind("Brake4", "Brake #4", KeyCode.S),
            new Keybind("Brake5", "Brake #5", KeyCode.A),
        };

        readonly Keybind[] clutchKeys = new Keybind[] {
            new Keybind("clutch1", "Clutch #1", KeyCode.B),
            new Keybind("clutch2", "Clutch #2", KeyCode.V),
            new Keybind("clutch3", "Clutch #3", KeyCode.C),
            new Keybind("clutch4", "Clutch #4", KeyCode.X),
            new Keybind("clutch5", "Clutch #5", KeyCode.Z),
        };

        public static Settings enableGUI;
        public static Settings enableMouseSteer;
        public static Settings centerMouseOnEnable;
        public static Settings steeringSensitivity;
        public static Settings keyboardHShifter;
        public static Settings enableAdvancedKeys;
        public static Settings enableStickyKeys;
        public static Settings enableClutch;
        public static Settings enableClutchSticky;

        public static Settings[] throttleSettings = new Settings[5];
        public static Settings[] brakeSettings = new Settings[5];
        public static Settings[] clutchSettings = new Settings[5];

        GameObject player;
        GameObject fpsCameraParent;
        GameObject crosshair;
        public static GameObject currentVehicle;

        AxisCarController axisCarController;
        Drivetrain drivetrain;

        MonoBehaviour mouseLookX;
        MouseLook mouseLookY;

        public static bool isDriveModeActive = false;
        public static bool isRotateModeActive = false;

        public static float steerAngle;
        public static float throttleInput;
        public static float brakeInput;
        public static float clutchInput;

        Vector3 playerRotation;
        float fpsCameraRotation;

        bool vehicleSteeringAssistance;
        bool vehicleSmoothInput;
        bool vehicleAutoClutch;
        bool vehicleShifter;

        int lastPressedThrottle = -1;
        int lastPressedBrake = -1;
        int lastPressedClutch = -1;

        GameObject guiThrottle;
        GameObject guiThrottleBar;
        GameObject guiBrake;
        GameObject guiBrakeBar;
        GameObject guiClutch;
        GameObject guiClutchBar;

        public AdvancedKBMControls()
        {
            enableGUI = new Settings("enableGUI", "Enable GUI", true, () => ApplySettings());
            enableMouseSteer = new Settings("enableMouseSteer", "Enable Mouse Steering", true, () => ApplySettings());
            centerMouseOnEnable = new Settings("centerMouseOnEnable", "Reset Camera On Enable", true, () => ApplySettings());
            steeringSensitivity = new Settings("steeringSensitivity", "Steering Sensitivity", 50f, () => ApplySettings());
            keyboardHShifter = new Settings("keyboardHShifter", "Keyboard H-Shifter", false, () => ApplySettings());
            enableAdvancedKeys = new Settings("enableAdvancedKeys", "Enable Advanced Throttle/Brake", true, () => ApplySettings());
            enableStickyKeys = new Settings("enableStickyKeys", "Enable Sticky Throttle/Brake", false, () => ApplySettings());
            enableClutch = new Settings("enableClutch", "Enable Advanced Clutch", false, () => ApplySettings());
            enableClutchSticky = new Settings("enableClutchSticky", "Enable Sticky Clutch", true, () => ApplySettings());

            float[] brakeSlices = new float[] { 10f, 20f, 50f, 80f, 100f };
            float[] clutchSlices = new float[] { 20f, 50f, 80f, 90f, 100f };

            for (int i = 0; i < throttleSettings.Length; i++)
            {
                int di = i + 1;

                throttleSettings[i] = new Settings("throttle" + di, "Throttle #" + di, di * 20f, () => ApplySettings());
                brakeSettings[i] = new Settings("brake" + di, "Brake #" + di, brakeSlices[i], () => ApplySettings());
                clutchSettings[i] = new Settings("clutch" + di, "Clutch #" + di, clutchSlices[i], () => ApplySettings());
            }
        }

        public override void OnLoad()
        {
            var harmony = HarmonyInstance.Create("AdvancedKBMControls");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            player = GameObject.Find("PLAYER");
            fpsCameraParent = GameObject.Find("PLAYER/Pivot/AnimPivot/Camera/FPSCamera");
            crosshair = GameObject.Find("GUI/Icons/GUITexture");

            UnityEngine.Object.Destroy(FindMouseLook(fpsCameraParent));

            mouseLookX = FindMouseLook(player);

            mouseLookY = fpsCameraParent.AddComponent<MouseLook>();
            mouseLookY.axes = MouseLook.RotationAxes.MouseY;
            mouseLookY.sensitivityX = 0f;
            mouseLookY.sensitivityY = 1.193452f;
            mouseLookY.minimumX = 0f;
            mouseLookY.maximumX = 0f;
            mouseLookY.minimumY = -80f;
            mouseLookY.maximumY = 80f;

            Keybind.Add(this, disableSteerKey);
            Keybind.Add(this, toggleSticky);

            for (int i = 0; i < throttleKeys.Length; i++)
            {
                Keybind.Add(this, throttleKeys[i]);
            }

            for (int i = 0; i < brakeKeys.Length; i++)
            {
                Keybind.Add(this, brakeKeys[i]);
            }

            for (int i = 0; i < clutchKeys.Length; i++)
            {
                Keybind.Add(this, clutchKeys[i]);
            }

            InitGUI();
        }

        MonoBehaviour FindMouseLook(GameObject gameobject)
        {
            var components = gameobject.GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component.GetType().Name == "MouseLook")
                {
                    return component;
                }
            }

            return null;
        }

        void InitGUI()
        {
            float y = 6.0f;

            if (GameObject.Find("GUI/HUD/Alcohol"))
            {
                y -= 0.4f;
            }

            guiThrottle = UnityEngine.GameObject.Instantiate<GameObject>(GameObject.Find("GUI/HUD/Hunger"));
            guiThrottle.transform.SetParent(GameObject.Find("GUI/HUD").transform);
            guiThrottle.name = "Throttle";
            guiThrottle.transform.localPosition = new Vector3(-11.5f, y, 0.0f);
            y -= 0.4f;

            TextMesh guiThrottleText = guiThrottle.transform.FindChild("HUDLabel").GetComponent<TextMesh>();
            guiThrottleText.text = "THROTTLE";
            guiThrottleText.transform.FindChild("HUDLabelShadow").GetComponent<TextMesh>().text = "THROTTLE";

            guiThrottleBar = guiThrottle.transform.FindChild("Pivot").gameObject;
            UnityEngine.Object.Destroy(guiThrottleBar.GetComponent<PlayMakerFSM>());
            guiThrottleBar.transform.localScale = new Vector3(0f, 1f, 1f);

            guiBrake = UnityEngine.GameObject.Instantiate<GameObject>(GameObject.Find("GUI/HUD/Hunger"));
            guiBrake.transform.SetParent(GameObject.Find("GUI/HUD").transform);
            guiBrake.name = "Brake";
            guiBrake.transform.localPosition = new Vector3(-11.5f, y, 0.0f);
            y -= 0.4f;

            TextMesh guiBrakeText = guiBrake.transform.FindChild("HUDLabel").GetComponent<TextMesh>();
            guiBrakeText.text = "Brake";
            guiBrakeText.transform.FindChild("HUDLabelShadow").GetComponent<TextMesh>().text = "Brake";

            guiBrakeBar = guiBrake.transform.FindChild("Pivot").gameObject;
            UnityEngine.Object.Destroy(guiBrakeBar.GetComponent<PlayMakerFSM>());
            guiBrakeBar.transform.localScale = new Vector3(0f, 1f, 1f);

            guiClutch = UnityEngine.GameObject.Instantiate<GameObject>(GameObject.Find("GUI/HUD/Hunger"));
            guiClutch.transform.SetParent(GameObject.Find("GUI/HUD").transform);
            guiClutch.name = "Clutch";
            guiClutch.transform.localPosition = new Vector3(-11.5f, y, 0.0f);
            y -= 0.4f;

            TextMesh guiClutchText = guiClutch.transform.FindChild("HUDLabel").GetComponent<TextMesh>();
            guiClutchText.text = "Clutch";
            guiClutchText.transform.FindChild("HUDLabelShadow").GetComponent<TextMesh>().text = "Clutch";

            guiClutchBar = guiClutch.transform.FindChild("Pivot").gameObject;
            UnityEngine.Object.Destroy(guiClutchBar.GetComponent<PlayMakerFSM>());
            guiClutchBar.transform.localScale = new Vector3(0f, 1f, 1f);

            guiThrottle.SetActive(false);
            guiBrake.SetActive(false);
            guiClutch.SetActive(false);
        }

        public override void ModSettings()
        {
            Settings.AddHeader(this, "Base Settings");
            Settings.AddCheckBox(this, enableGUI);
            Settings.AddCheckBox(this, keyboardHShifter);
            Settings.AddHeader(this, "Mouse Steering");
            Settings.AddCheckBox(this, enableMouseSteer);
            Settings.AddCheckBox(this, centerMouseOnEnable);
            Settings.AddSlider(this, steeringSensitivity, 1f, 100f);

            Settings.AddHeader(this, "Advanced Throttle/Brake");
            Settings.AddCheckBox(this, enableAdvancedKeys);
            Settings.AddCheckBox(this, enableStickyKeys);

            for (int i = 0; i < throttleSettings.Length; i++)
            {
                Settings.AddSlider(this, throttleSettings[i], 0f, 100f);
            }

            for (int i = 0; i < brakeSettings.Length; i++)
            {
                Settings.AddSlider(this, brakeSettings[i], 0f, 100f);
            }

            Settings.AddHeader(this, "Advanced Clutch");
            Settings.AddCheckBox(this, enableClutch);
            Settings.AddCheckBox(this, enableClutchSticky);

            for (int i = 0; i < clutchSettings.Length; i++)
            {
                Settings.AddSlider(this, clutchSettings[i], 0f, 100f);
            }
        }

        public override void OnSave()
        {
            // Called once, when save and quit
            // Serialize your save file here.
        }

        public override void OnGUI()
        {
            // Draw unity OnGUI() here
        }

        public override void Update()
        {
            bool isSeated = FsmVariables.GlobalVariables.FindFsmString("PlayerCurrentVehicle").Value != "";

            // override for ChaseCamera
            mouseLookY.enabled = true;

            if (toggleSticky.IsDown())
            {
                enableStickyKeys.Value = !(bool)enableStickyKeys.GetValue();
            }

            if (isSeated && !isDriveModeActive) // activate drive mode
            {
                steerAngle = 0f;
                currentVehicle = player.transform.root.gameObject;
                playerRotation = player.transform.localEulerAngles;
                fpsCameraRotation = mouseLookY.rotationY;

                if (currentVehicle.name == "BOAT")
                {
                    return;
                }

                isDriveModeActive = true;

                if ((bool)enableMouseSteer.GetValue())
                {
                    mouseLookX.enabled = false;
                    mouseLookY.SetState(false);
                    crosshair.SetActive(false);
                }

                axisCarController = currentVehicle.GetComponent<AxisCarController>();
                drivetrain = currentVehicle.GetComponent<Drivetrain>();

                if (axisCarController != null && drivetrain != null)
                {
                    vehicleSteeringAssistance = axisCarController.steerAssistance;
                    vehicleSmoothInput = axisCarController.smoothInput;
                    vehicleAutoClutch = drivetrain.autoClutch;
                    vehicleShifter = drivetrain.shifter;
                }

                if ((bool)enableAdvancedKeys.GetValue() && (bool)enableGUI.GetValue())
                {
                    guiThrottle.SetActive(true);
                    guiBrake.SetActive(true);

                    if ((bool)enableClutch.GetValue())
                    {
                        guiClutch.SetActive(true);
                    }
                }

                if ((bool)keyboardHShifter.GetValue())
                {
                    drivetrain.shifter = true;
                }
            }
            else if (!isSeated && isDriveModeActive) // deactivate drive mode
            {
                isDriveModeActive = false;

                if ((bool)enableMouseSteer.GetValue())
                {
                    crosshair.SetActive(true);
                    mouseLookX.enabled = true;
                    mouseLookY.SetState(true);
                }

                axisCarController = currentVehicle.GetComponent<AxisCarController>();
                drivetrain = currentVehicle.GetComponent<Drivetrain>();

                if (axisCarController != null && drivetrain != null)
                {
                    axisCarController.steerAssistance = vehicleSteeringAssistance;
                    axisCarController.smoothInput = vehicleSmoothInput;
                    drivetrain.autoClutch = vehicleAutoClutch;
                    drivetrain.shifter = vehicleShifter;
                }

                currentVehicle = null;

                lastPressedThrottle = -1;
                lastPressedBrake = -1;
                lastPressedClutch = -1;

                guiThrottle.SetActive(false);
                guiBrake.SetActive(false);
                guiClutch.SetActive(false);
            }

            if (isDriveModeActive && !FsmVariables.GlobalVariables.FindFsmBool("PlayerInMenu").Value)
            {
                throttleInput = 0f;
                brakeInput = 0f;
                clutchInput = 0f;

                if ((bool)enableMouseSteer.GetValue())
                {
                    if (disableSteerKey.IsPressed() && !isRotateModeActive) // activate rotate mode
                    {
                        isRotateModeActive = true;
                        mouseLookX.enabled = true;
                        mouseLookY.SetState(true);
                        crosshair.SetActive(true);
                    }
                    else if (!disableSteerKey.IsPressed() && isRotateModeActive) // deactivate rotate mode
                    {
                        isRotateModeActive = false;

                        if ((bool)centerMouseOnEnable.GetValue())
                        {
                            player.transform.localEulerAngles = playerRotation;
                            mouseLookY.rotationY = fpsCameraRotation;
                        }

                        mouseLookY.UpdateAndDisable();
                        crosshair.SetActive(false);
                    }
                }

                if (!isRotateModeActive)
                {
                    if ((bool)enableMouseSteer.GetValue())
                    {
                        mouseLookX.enabled = false;

                        float mouseMoveX = Input.GetAxis("Mouse X");

                        if (Mathf.Abs(mouseMoveX) > 0.01f)
                        {
                            float sensitivity = float.Parse(steeringSensitivity.GetValue().ToString()) * 0.0002f;

                            steerAngle = Mathf.Clamp(steerAngle + mouseMoveX * sensitivity, -1f, 1f);
                        }
                    }
                }

                if ((bool)enableAdvancedKeys.GetValue())
                {
                    int hasPressedThrottle = -1;
                    int hasPressedBrake = -1;

                    for (int i = 0; i < throttleSettings.Length; i++)
                    {
                        if ((bool)enableStickyKeys.GetValue() && throttleKeys[i].IsDown() || !(bool)enableStickyKeys.GetValue() && throttleKeys[i].IsPressed())
                        {
                            hasPressedThrottle = i;
                            throttleInput = float.Parse(throttleSettings[i].GetValue().ToString()) * 0.01f;
                        }
                    }

                    for (int i = 0; i < brakeSettings.Length; i++)
                    {
                        if ((bool)enableStickyKeys.GetValue() && brakeKeys[i].IsDown() || !(bool)enableStickyKeys.GetValue() && brakeKeys[i].IsPressed())
                        {
                            hasPressedBrake = i;
                            brakeInput = float.Parse(brakeSettings[i].GetValue().ToString()) * 0.01f;
                        }
                    }

                    if ((bool)enableStickyKeys.GetValue())
                    {
                        if (hasPressedThrottle > -1)
                        {
                            if (lastPressedThrottle == hasPressedThrottle)
                            {
                                throttleInput = 0f;
                                lastPressedThrottle = -1;
                            }
                            else
                            {
                                lastPressedThrottle = hasPressedThrottle;
                            }

                            brakeInput = 0f;
                            lastPressedBrake = -1;
                        }
                        else if (lastPressedThrottle > -1)
                        {
                            throttleInput = float.Parse(throttleSettings[lastPressedThrottle].GetValue().ToString()) * 0.01f;
                        }

                        if (hasPressedBrake > -1)
                        {
                            if (lastPressedBrake == hasPressedBrake)
                            {
                                brakeInput = 0f;
                                lastPressedBrake = -1;
                            }
                            else
                            {
                                lastPressedBrake = hasPressedBrake;
                            }

                            throttleInput = 0f;
                            lastPressedThrottle = -1;
                        }
                        else if (lastPressedBrake > -1)
                        {
                            brakeInput = float.Parse(brakeSettings[lastPressedBrake].GetValue().ToString()) * 0.01f;
                        }
                    } else
                    {
                        lastPressedThrottle = -1;
                        lastPressedBrake = -1;
                    }

                    guiThrottleBar.transform.localScale = new Vector3(throttleInput, 1f, 1f);
                    guiBrakeBar.transform.localScale = new Vector3(brakeInput, 1f, 1f);

                    if ((bool)enableClutch.GetValue())
                    {
                        int hasPressedClutch = -1;

                        for (int i = 0; i < clutchSettings.Length; i++)
                        {
                            if ((bool)enableClutchSticky.GetValue() && clutchKeys[i].IsDown() || !(bool)enableClutchSticky.GetValue() && clutchKeys[i].IsPressed())
                            {
                                hasPressedClutch = i;
                                clutchInput = float.Parse(clutchSettings[i].GetValue().ToString()) * 0.01f;
                            }
                        }

                        if ((bool)enableClutchSticky.GetValue())
                        {
                            if (hasPressedClutch > -1)
                            {
                                if (lastPressedClutch == hasPressedClutch)
                                {
                                    clutchInput = 0f;
                                    lastPressedClutch = -1;
                                }
                                else
                                {
                                    lastPressedClutch = hasPressedClutch;
                                }
                            }
                            else if (lastPressedClutch > -1)
                            {
                                clutchInput = float.Parse(clutchSettings[lastPressedClutch].GetValue().ToString()) * 0.01f;
                            }
                        } else
                        {
                            lastPressedClutch = -1;
                        }

                        guiClutchBar.transform.localScale = new Vector3(clutchInput, 1f, 1f);
                    }
                }
            }
        }

        void ApplySettings()
        {
        }
    }

    [HarmonyPatch(typeof(CarController))]
    [HarmonyPatch("Update")]
    class CarController_Update_Patch
    {
        static void Postfix(CarController __instance, Drivetrain ___drivetrain)
        {
            //bool steerAssistance = __instance.steerAssistance;
            //bool smoothInput = __instance.smoothInput;
            if (__instance.gameObject == AdvancedKBMControls.currentVehicle)
            {
                if ((bool)AdvancedKBMControls.enableMouseSteer.GetValue())
                {
                    __instance.smoothInput = false;
                    __instance.steerAssistance = false;
                    __instance.steerInput = AdvancedKBMControls.steerAngle;
                }

                if ((bool)AdvancedKBMControls.enableAdvancedKeys.GetValue())
                {
                    __instance.throttleInput = AdvancedKBMControls.throttleInput;
                    __instance.brakeInput = AdvancedKBMControls.brakeInput;

                    if ((bool)AdvancedKBMControls.enableClutch.GetValue())
                    {
                        ___drivetrain.autoClutch = false;
                        __instance.clutchInput = AdvancedKBMControls.clutchInput;
                    }
                }
            }
        }
    }

    [HarmonyPatch(typeof(AxisCarController))]
    [HarmonyPatch("GetInput")]
    class AxisCarController_GetInput_Patch
    {
        static void Postfix(CarController __instance, ref int targetGear, Drivetrain ___drivetrain)
        {
            if ((bool)AdvancedKBMControls.keyboardHShifter.GetValue())
            {
                if (cInput.GetButtonDown("reverse"))
                    targetGear = 0;
                else if (cInput.GetButtonDown("neutral"))
                    targetGear = 1;
                else if (cInput.GetButtonDown("first"))
                    targetGear = 2;
                else if (cInput.GetButtonDown("second"))
                    targetGear = 3;
                else if (cInput.GetButtonDown("third"))
                    targetGear = 4;
                else if (cInput.GetButtonDown("fourth"))
                    targetGear = 5;
                else if (cInput.GetButtonDown("fifth"))
                    targetGear = 6;
                else if (cInput.GetButtonDown("sixth"))
                    targetGear = 7;
                else
                    targetGear = ___drivetrain.gear;
            }
        }
    }
}