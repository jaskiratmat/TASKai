using System;
using BepInEx;
using UnityEngine;
using HarmonyLib;
using JoelG.ENA4;
using System.Reflection;
using System.Collections;
using System.IO;
using Rewired;
using UnityEngineInternal;
using UnityEngine.UI;
using UnityEngine.EventSystems;


namespace TASKai
{
    [HarmonyPatch]
    public class PlayerMoverPatch
    {
        static System.Type targetType = AccessTools.TypeByName("PlayerMover");
        static System.Type viewerType = AccessTools.TypeByName("PlayerViewer");
        //public static System.Type inputType = AccessTools.TypeByName("NativeInputSystem");

        static string teleportMethodName = "Teleport";
        static string rotationMethodName = "ImmediatelySetRotation";
        static string velocityMethodName = "SetImmediateVelocity";
        static string moveToLookMethodName = "MoveToLookTarget";
        //static string inputMethodName = "QueueInputEvent";
        //static string moveToLookMethodName = "SetLookTarget";

        //private static FieldInfo _jumpField = AccessTools.Field(targetType, "jumpDownInputQueue");
        //private static FieldInfo _inputField = AccessTools.Field(targetType, "input");
        //private static FieldInfo _crouchingField = AccessTools.Field(targetType, "crouchInputToggle");
        //private static PropertyInfo _sprintingProperty = AccessTools.Property(targetType, "Sprinting");

        // Cooking
        //private static FieldInfo playerCoreField = AccessTools.Field(targetType, "player");
        //private static FieldInfo playerInputCoreField = AccessTools.Field(typeof(PlayerCore), "Input");
        //private static FieldInfo _playerField = AccessTools.Field(targetType, "player");
        //private static MethodInfo _inputMethod = AccessTools.Method(typeof(Player), "SetButtonValue");

        //public static System.Reflection.MethodInfo GetSimulatedInputMethod()
        //{
        //    System.Type[] parameterTypes = new System.Type[] { typeof(int), typeof(bool) }; // Example
        //    System.Reflection.MethodInfo simulatedInputMethod = AccessTools.Method(typeof(Player), "SetButtonValue", parameterTypes);
        //    return simulatedInputMethod;
        //}

        //public static System.Reflection.MethodInfo GetInputMethod()
        //{
        //    System.Type[] parameterTypes = new System.Type[] { typeof(IntPtr) }; // Example  , typeof(float)
        //    System.Reflection.MethodInfo inputMethod = AccessTools.Method(inputType, inputMethodName, parameterTypes);
        //    return inputMethod;
        //}

        public static System.Reflection.MethodInfo GetLookMethod()
        {
            System.Type[] parameterTypes = new System.Type[] { typeof(Transform) }; // Example  , typeof(float)
            System.Reflection.MethodInfo lookMethod = AccessTools.Method(viewerType, moveToLookMethodName, parameterTypes);
            return lookMethod;
        }
        public static System.Reflection.MethodInfo GetVelocityMethod()
        {
            System.Type[] parameterTypes = new System.Type[] { typeof(Vector3) }; // Example
            System.Reflection.MethodInfo velocityMethod = AccessTools.Method(targetType, velocityMethodName, parameterTypes);
            return velocityMethod;
        }

        public static System.Reflection.MethodInfo GetTeleportMethod()
        {
            System.Type[] parameterTypes = new System.Type[] { typeof(Vector3), typeof(bool), typeof(bool) }; // Example

            System.Reflection.MethodInfo teleportMethod = AccessTools.Method(targetType, teleportMethodName, parameterTypes);
            return teleportMethod;
        }

        public static System.Reflection.MethodInfo GetRotationMethod()
        {
            System.Type[] parameterTypes = new System.Type[] { typeof(float), typeof(float) };

            System.Reflection.MethodInfo rotationMethod = AccessTools.Method(viewerType, rotationMethodName, parameterTypes);
            return rotationMethod;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "GetButtonDown", new Type[] { typeof(int) })] // "GetButton" is also used everywher
        public static bool GetButtonDownPrefix(Player __instance, int actionId, ref bool __result)
        {
            TASKai.Instance.playerInstance = __instance;
            //TASKai.Instance.active = actionId;
            if (actionId == 2 && TASKai.Instance.startJumping) // Jumping
            {
                __result = true;
                TASKai.Instance.startJumping = false;
                return false;
            }
            if (actionId == 7 && TASKai.Instance.startPause) // Pause and Dialouge skip somehow
            {
                __result = true;
                TASKai.Instance.startPause = false;
                return false;
            }
            if ((actionId == 5 || actionId == 36) && TASKai.Instance.startInteract) // Interact
            {
                __result = true;
                TASKai.Instance.startInteract = false;
                return false;
            }
            //if ((actionId == 35) && TASKai.Instance.verticalUI) // Interact
            //{
            //    __result = true;
            //    TASKai.Instance.verticalUI = false;
            //    return false;
            //}
            if ((actionId == 40 || actionId == 43) && TASKai.Instance.startSkip) // Dialouge Skip
            {
                __result = true;
                TASKai.Instance.startSkip = false;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "GetButton", new Type[] { typeof(int) })] 
        public static bool GetButtonPrefix(Player __instance, int actionId, ref bool __result)
        {
            if (actionId == 12 && TASKai.Instance.startSprinting) // Sprint
            {
                __result = true;
                return false;
            }
            if (actionId == 14 && TASKai.Instance.startCrouching) // Crouch
            {
                __result = true;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Player), "GetAxis", new Type[] { typeof(int) })] 
        public static bool GetAxisPrefix(Player __instance, int actionId, ref float __result)
        {
            if (actionId == 0 && TASKai.Instance.startMovingPlayer != Vector2.zero) // X Axis
            {
                __result = TASKai.Instance.startMovingPlayer.x;
                return false;
            }
            if (actionId == 1 && TASKai.Instance.startMovingPlayer != Vector2.zero) // Z Axis
            {
                __result = TASKai.Instance.startMovingPlayer.y;
                return false;
            }
            return true;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerMover), "FixedUpdatePlayerControl")]
        private static void Prefix(PlayerMover __instance)
        {
            TASKai.Instance.playerMoverInstance = __instance;
            //TASKai.Instance.playerInputInstance = playerInputCoreField.GetValue(playerCoreField.GetValue(__instance));
            //if (__instance != null)
            //{
            //    // Cooking continued
            //    //object playerCoreInstance = playerCoreField.GetValue(__instance);
            //    //object playerInputInstance = playerInputCoreField.GetValue(playerCoreInstance);
            //    //MethodInfo setButtonValueMethod = playerInputInstance.GetType().GetMethod("SetButtonValue", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);


            //    //float strafe = 0f;
            //    //if (String.Equals(TASKai.Instance.strafeDirection, "none")) strafe = 0f;
            //    //else if (String.Equals(TASKai.Instance.strafeDirection, "left")) strafe = -1f;
            //    //else if (String.Equals(TASKai.Instance.strafeDirection, "right")) strafe = 1f;
            //    //if (TASKai.Instance.startMovingPlayer) _inputField.SetValue(__instance, new Vector3(strafe, 0, 1f));
            //    //if (TASKai.Instance.startSprinting) _sprintingProperty.SetValue(__instance, true);
            //    //if (TASKai.Instance.startCrouching) _crouchingField.SetValue(__instance, true);
            //    //if (TASKai.Instance.startCrouching == false) _crouchingField.SetValue(__instance, false);
            //    //if (TASKai.Instance.startJumping)
            //    //{
            //    //    _jumpField.SetValue(__instance, true ? 0.1f : 0f);
            //    //    TASKai.Instance.startJumping = false;
            //    //}
            //}
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(PlayerViewer), "Update")]
        private static void Prefix(PlayerViewer __instance)
        {
            TASKai.Instance.playerViewerInstance = __instance;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(Input), "GetButtonDown", new Type[] { typeof(string) })]
        private static bool inputPrefix(Input __instance, string buttonName, ref bool __result)
        {
            if (buttonName == "Submit")
            {
                __result = true;
                return false;
            }
            return true;
            //TASKai.Instance.playerViewerInstance = __instance;
        }

        //[HarmonyPrefix]
        //[HarmonyPatch(inputType, "Update")]
        //private static void inputPrefix(inputType __instance)
        //{
        //    TASKai.Instance.playerViewerInstance = __instance;
        //}
    }

    [BepInPlugin("com.kaijou.dreambbq", "7Kai TAS", "1.0.0")]
    public class TASKai : BaseUnityPlugin
    {
        public PlayerMover playerMoverInstance;
        public PlayerViewer playerViewerInstance;
        public Player playerInstance;
        //public object playerInputInstance;
        public static TASKai Instance;


        // Usage of `getNativeInputSystemPtr` can now be done indirectly through reflection.
        //public Player playerInput;

        public Vector2 startMovingPlayer;
        public bool startSprinting = false;
        public bool startJumping = false;
        public bool startCrouching = false;
        public bool startSkip = false;
        public bool startInteract = false;
        public bool startPause = false;
        //public bool verticalUI = false;
        //public string strafeDirection = "none";
        //public Robot robot;

        private string inputFilePath;
        
        System.Reflection.PropertyInfo ViewPoint = AccessTools.Property(typeof(PlayerViewer), "ViewPoint");

        System.Reflection.FieldInfo camY = AccessTools.Field(typeof(PlayerViewer), "cameraAngleBase");
        System.Reflection.FieldInfo camX = AccessTools.Field(typeof(PlayerViewer), "playerRotationBase");

        System.Reflection.MethodInfo teleportMethod = PlayerMoverPatch.GetTeleportMethod();
        System.Reflection.MethodInfo rotateMethod = PlayerMoverPatch.GetRotationMethod();
        System.Reflection.MethodInfo velocityMethod = PlayerMoverPatch.GetVelocityMethod();
        System.Reflection.MethodInfo lookMethod = PlayerMoverPatch.GetLookMethod();
        //System.Reflection.MethodInfo inputMethod = PlayerMoverPatch.GetInputMethod();
        //System.Reflection.MethodInfo simulatedInputMethod = PlayerMoverPatch.GetSimulatedInputMethod();

        //public int active;

        private void Awake()
        {
            inputFilePath = Path.Combine(Paths.PluginPath, "DreamBBQTAS", "tas_input.txt");
            Directory.CreateDirectory(Path.GetDirectoryName(inputFilePath));

            Instance = this;
            
            var harmony = new Harmony("com.Kaijou.dreambbq");
            harmony.PatchAll();

            Logger.LogInfo("KaiTAS Plugin Loaded!");
            Logger.LogInfo(AccessTools.TypeByName("NativeInputSystem"));
        }

        private void Update()
        {
           // Logger.LogInfo(active);
            if (Input.GetKeyDown(KeyCode.F1))
            {
                StartCoroutine(MoveWithDelay());
            }
            else if (Input.GetKeyDown(KeyCode.F2))
            {
                StopAllCoroutines();
                startMovingPlayer = Vector2.zero;
                startSprinting = false;
                startJumping = false;
            }
            //else if (Input.GetKeyDown(KeyCode.F3))
            //{
            //    inputMethod.Invoke(playerMoverInstance, new object[] { 2 });
            //}
        }

        IEnumerator MoveWithDelay()
        {
            if (!File.Exists(inputFilePath))
            {
                Logger.LogError($"Input file not found: {inputFilePath}");
            }

            int loopLocation = 0;
            int loopCount = 0;

            string[] lines = File.ReadAllLines(inputFilePath);
            for (int i = 0; i < lines.Length; i++)
            {
                if (string.IsNullOrWhiteSpace(lines[i])) continue;
                if (lines[i].StartsWith("#")) continue;
                if (string.Equals(lines[i], "jump", StringComparison.OrdinalIgnoreCase))
                {
                    startJumping = true;
                }
                else if (string.Equals(lines[i], "sprint", StringComparison.OrdinalIgnoreCase))
                {
                    startSprinting = true;
                }
                else if (string.Equals(lines[i], "skip", StringComparison.OrdinalIgnoreCase))
                {
                    startSkip = true;
                }
                else if (string.Equals(lines[i], "inter", StringComparison.OrdinalIgnoreCase))
                {
                    startInteract = true;
                }
                else if (string.Equals(lines[i], "pause", StringComparison.OrdinalIgnoreCase))
                {
                    startPause = true;
                }
                //else if (string.Equals(lines[i], "vui", StringComparison.OrdinalIgnoreCase))
                //{
                //    verticalUI = true;
                //}
                else if (lines[i].StartsWith("tp"))
                {
                    string[] parts = lines[i].Split(' ');
                    if (parts.Length == 4)
                    {
                        float x = float.Parse(parts[1]);
                        float y = float.Parse(parts[2]);
                        float z = float.Parse(parts[3]);
                        TeleportPlayer(new Vector3(x, y, z), false, true);
                    }
                }
                else if (lines[i].StartsWith("move"))
                {
                    string[] parts = lines[i].Split(' ');
                    if (parts.Length == 3)
                    {
                        float x = float.Parse(parts[1]);
                        float y = float.Parse(parts[2]);
                        startMovingPlayer = new Vector2(x, y);
                    }
                }
                else if (string.Equals(lines[i], "crouch", StringComparison.OrdinalIgnoreCase))
                {
                    startCrouching = true;
                }
                else if (lines[i].StartsWith("stop"))
                {
                    string[] parts = lines[i].Split(' ');
                    if (parts.Length == 2)
                    {
                        string stopType = parts[1];
                        if (stopType.Equals("sprint", StringComparison.OrdinalIgnoreCase))
                        {
                            startSprinting = false;
                        }
                        else if (stopType.Equals("move", StringComparison.OrdinalIgnoreCase))
                        {
                            startMovingPlayer = Vector2.zero;
                        }
                        else if (stopType.Equals("crouch", StringComparison.OrdinalIgnoreCase))
                        {
                            startCrouching = false;
                        }
                    }
                }
                else if (lines[i].StartsWith("rot"))
                {
                    string[] parts = lines[i].Split(' ');
                    if (parts.Length == 3)
                    {

                        float y = float.Parse(parts[1]) + (float)camY.GetValue(playerViewerInstance);
                        float x = float.Parse(parts[2]) + (float)camX.GetValue(playerViewerInstance);
                        RotatePlayer(y, x);
                    }
                }
                else if (lines[i].StartsWith("absrot"))
                {
                    string[] parts = lines[i].Split(' ');
                    if (parts.Length == 3)
                    {

                        float y = float.Parse(parts[1]);
                        float x = float.Parse(parts[2]);
                        RotatePlayer(y, x);
                    }
                }
                else if (lines[i].StartsWith("wait"))
                {
                    string[] parts = lines[i].Split(' ');
                    if (parts.Length == 2)
                    {
                        float waitTime = float.Parse(parts[1]);
                        yield return new WaitForSeconds(waitTime);
                    }
                }
                else if (lines[i].StartsWith("loop"))
                {

                    string[] parts = lines[i].Split(' ');
                    if (parts.Length == 2)
                    {
                        loopCount = int.Parse(parts[1]);
                        loopLocation = i;
                    }
                }
                else if (lines[i].StartsWith("end"))
                {
                    if (loopCount > 0)
                    {
                        loopCount--;
                        i = loopLocation;
                    }
                }
                else if (lines[i].StartsWith("leap"))
                {
                    string[] parts = lines[i].Split(' ');
                    if (parts.Length == 4)
                    {
                        float x = float.Parse(parts[1]);
                        float y = float.Parse(parts[2]);
                        float z = float.Parse(parts[3]);
                        velocityMethod.Invoke(playerMoverInstance, new object[] { new Vector3(x, y, z) });
                    }
                }
                else if (lines[i].StartsWith("look"))
                {
                    string[] parts = lines[i].Split(' ');
                    if (parts.Length == 4)
                    {
                        float x = float.Parse(parts[1]);
                        float y = float.Parse(parts[2]);
                        float z = float.Parse(parts[3]);
                        Transform PlayerPosition = (Transform)ViewPoint.GetValue(playerViewerInstance);
                        GameObject lookAtTarget = new GameObject("ForcedLookTarget");
                        print($"{PlayerPosition.position}");
                        lookAtTarget.transform.position = PlayerPosition.position;
                        lookMethod.Invoke(playerViewerInstance, new object[] { lookAtTarget.transform });
                        //RotationValue.SmoothDamp()
                    }
                }
                //else if (lines[i].StartsWith("stop"))
                //{
                //    string[] parts = lines[i].Split(' ');
                //    if (parts.Length == 2)
                //    {
                //        string stopType = parts[1];
                //        if (stopType.Equals("jump", StringComparison.OrdinalIgnoreCase))
                //        {
                //            startJumping = false;
                //        }
                //        else if (stopType.Equals("sprint", StringComparison.OrdinalIgnoreCase))
                //        {
                //            startSprinting = false;
                //        }
                //        else if (stopType.Equals("move", StringComparison.OrdinalIgnoreCase))
                //        {
                //            startMovingPlayer = false;
                //        }
                //    }
                //else if (lines[i].StartsWith("click"))
                //{
                //    string[] parts = lines[i].Split(' ');
                //    if (parts.Length == 3) // Ensure there are exactly 2 parts: "click" and the flag
                //    {
                //        if (Enum.TryParse(parts[1], out MouseOperations.MouseEventFlags flag1) && Enum.TryParse(parts[2], out MouseOperations.MouseEventFlags flag2)) // Safely parse the string to the enum
                //        {
                //            MouseOperations.MouseEvent(flag1);
                //            MouseOperations.MouseEvent(flag2);
                //        }
                //        else
                //        {
                //            Logger.LogError($"Invalid MouseEventFlag: {parts[1]}");
                //        }
                //    }
                //    else
                //    {
                //        Logger.LogError("Invalid 'click' command format. Expected format: 'click <MouseEventFlag>'");
                //    }
                //}
                //else if (lines[i].StartsWith("sim"))
                //{
                //    string[] parts = lines[i].Split(' ');
                //    if (parts.Length == 2)
                //    {
                //        string key = parts[1];
                //        if (Enum.TryParse(key, out Key parsedKey)) 
                //        {
                //            //robot.KeyPress(parsedKey);
                //        }
                //        else
                //        {
                //            Logger.LogError($"Invalid key: {key}");
                //        }
                //    }
                //}
                else
                {
                    Logger.LogError($"Unknown command in input file: {lines[i]}");
                }
            }
            Logger.LogInfo("Coroutine Finished");
        }

        public void RotatePlayer(float cameraAngle, float playerRotation)
        {
            if (playerViewerInstance != null)
            {
                if (rotateMethod != null)
                {
                    rotateMethod.Invoke(playerViewerInstance, new object[] { cameraAngle, playerRotation });
                }
                else
                {
                    Logger.LogError("Rotate Method not found on PlayerMover.");
                }
            }
        }

        public void TeleportPlayer(Vector3 position, bool inheritVelocity = true, bool clearSafeLocations = true)
        {
            if (playerMoverInstance != null)
            {

                if (teleportMethod != null)
                {
                    teleportMethod.Invoke(playerMoverInstance, new object[] { position, inheritVelocity, clearSafeLocations });
                    Logger.LogInfo($"Teleported player to {position}");
                }
                else
                {
                    Logger.LogError($"Teleport method not found on PlayerMover.");
                }
            }
            else
            {
                Logger.LogError("playerMoverInstance is null. Cannot teleport.");
            }
        }
    }
}

//public class MouseOperations
//{
//    [Flags]
//    public enum MouseEventFlags
//    {
//        LeftDown = 0x00000002,
//        LeftUp = 0x00000004,
//        MiddleDown = 0x00000020,
//        MiddleUp = 0x00000040,
//        Move = 0x00000001,
//        Absolute = 0x00008000,
//        RightDown = 0x00000008,
//        RightUp = 0x00000010
//    }

//    [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
//    [return: MarshalAs(UnmanagedType.Bool)]
//    private static extern bool SetCursorPos(int x, int y);

//    [DllImport("user32.dll")]
//    [return: MarshalAs(UnmanagedType.Bool)]
//    private static extern bool GetCursorPos(out MousePoint lpMousePoint);

//    [DllImport("user32.dll")]
//    private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

//    public static void SetCursorPosition(int x, int y)
//    {
//        SetCursorPos(x, y);
//    }

//    public static void SetCursorPosition(MousePoint point)
//    {
//        SetCursorPos(point.X, point.Y);
//    }

//    public static MousePoint GetCursorPosition()
//    {
//        MousePoint currentMousePoint;
//        var gotPoint = GetCursorPos(out currentMousePoint);
//        if (!gotPoint) { currentMousePoint = new MousePoint(0, 0); }
//        return currentMousePoint;
//    }

//    public static void MouseEvent(MouseEventFlags value)
//    {
//        MousePoint position = GetCursorPosition();

//        mouse_event
//            ((int)value,
//             position.X,
//             position.Y,
//             0,
//             0)
//            ;
//    }

//    [StructLayout(LayoutKind.Sequential)]
//    public struct MousePoint
//    {
//        public int X;
//        public int Y;

//        public MousePoint(int x, int y)
//        {
//            X = x;
//            Y = y;
//        }
//    }
//}

