using BepInEx;
using HarmonyLib;
using Oc;
using Oc.Em;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Threading;
using UnityEngine;


namespace SummonEnemy
{
    [BepInPlugin(PLUGIN_GUID, PLUGIN_NAME, PLUGIN_VER)]
    [BepInProcess(PROCESS_NAME)]

    public class Plugin : BaseUnityPlugin
    {
        #region 定数

        public const string PLUGIN_GUID = "craftopia.misc.summon_enemy";
        public const string PLUGIN_NAME = "SummonEnemy";
        public const string PLUGIN_VER = "1.0.0.0";

        private const string PROCESS_NAME = "Craftopia.exe";

        private const string AUTHOR_NAME = "@chiaki_p";

        private const int MAX_COLUMN_LENGTH = 5;

        private const string TEXT_HALF_BLANK = " ";

        #endregion

        #region ウィンドウ設定値

        private static readonly float WindowWidth = Mathf.Min(Screen.width, 700);
        private static readonly float WindowHeight = 400;

        private Rect WindowRect = new Rect((Screen.width - WindowWidth) / 2f,
                                           (Screen.height - WindowHeight) / 2f,
                                           WindowWidth,
                                           WindowHeight);

        private string WindowTitle = PLUGIN_NAME + " v" + PLUGIN_VER;

        private Vector2 ScrollViewVector = Vector2.zero;

        private Color SliderStringColor = new Color(232f / 255f, 190f / 255f, 002f / 255f);

        #endregion

        #region 状態変数

        public static bool WindowState = false;
        public static bool PressClosed = false;

        #endregion

        private Dictionary<OcEmType, string> emDic;

        void Awake()
        {
            //check culture and set en, if not ja.
            if (!Thread.CurrentThread.CurrentUICulture.Name.StartsWith("ja") &&
                 !Thread.CurrentThread.CurrentUICulture.Name.StartsWith("en"))
            {
                Thread.CurrentThread.CurrentUICulture = new CultureInfo("en", false);
            }

            OutputLog(LogLevel.Info, PLUGIN_NAME + TEXT_HALF_BLANK + GetCultureString("Version") + TEXT_HALF_BLANK + PLUGIN_VER);
            OutputLog(LogLevel.Info, PLUGIN_NAME + TEXT_HALF_BLANK + GetCultureString("LoadStart"));

            try
            {
                var harmony = new Harmony(PLUGIN_GUID);
                harmony.PatchAll(Assembly.GetExecutingAssembly());

                emDic = new Dictionary<OcEmType, string>();
            }
            catch (Exception ex)
            {
                OutputLog(LogLevel.Warning, GetCultureString("Error") + ex.Message.ToString());
            }
            finally
            {
                OutputLog(LogLevel.Info, PLUGIN_NAME + TEXT_HALF_BLANK + GetCultureString("LoadEnd"));
            }
        }

        void Update()
        {
            try
            {
                bool isInGame = (SingletonMonoBehaviour<OcGameMng>.Inst?.IsInGame) ?? false;

                if (isInGame)
                {
                    if (emDic.Count == 0)
                    {
                        var inst = SingletonMonoBehaviour<Oc.OcEmMng>.Inst?.SoEmArray;
                        var instEm = inst?.EmArray;

                        var len = (instEm?.Length) ?? 0;

                        for (int i = 0; i < len; i++)
                        {
                            OcEm em = instEm?[i];

                            if (em != null)
                            {

                                var type = em.GetType().Name;

                                //type check
                                switch (type)
                                {
                                    case "OcEm_PvMonster":
                                    case "OcEm_NPC_Mob":
                                    case "OcEm_NPC_Reception":
                                    case "OcEm_NPC_Event":
                                    case "OcEm_NPC_Merchant":
                                    case "OcEm_Lumberjack":
                                        continue;
                                }

                                if (em.SoEm.IsVehicle) continue;

                                var emName = Traverse.Create(em).Field("SoEmData").GetValue<Oc.SoEnemyData>().Name;
                                var emType = instEm[i].EmType;

                                if (!emDic.ContainsKey(emType))
                                {
                                    emDic.Add(emType, emName);
                                }
                            }
                        }

                        OutputLog(LogLevel.Info, GetCultureString("DataGetComplete")
                                                 + TEXT_HALF_BLANK + "Count=" + emDic.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                OutputLog(LogLevel.Warning, GetCultureString("Error") + ex.Message.ToString());
            }
        }

        void OnGUI()
        {
            try
            {
                if (WindowState)
                {
                    Cursor.lockState = CursorLockMode.None;
                    Cursor.visible = true;

                    WindowRect = GUI.Window(0, WindowRect, WindowFunc, WindowTitle);
                }
            }
            catch(Exception ex)
            {
                OutputLog(LogLevel.Warning, GetCultureString("Error") + ex.Message.ToString());
            }
        }

        OcEmType selectEmType = 0;
        int levelSliderValue = 1;
        int numSliderValue = 1;

        void WindowFunc(int windowId)
        {
            try
            {
                ScrollViewVector = GUILayout.BeginScrollView(ScrollViewVector, false, false);

                GUILayout.BeginVertical();

                var applyStyle = new GUIStyle(GUI.skin.button)
                {
                    richText = true
                };
                applyStyle.normal.textColor = Color.cyan;

                var sliderStringStyle = new GUIStyle(GUI.skin.label)
                {
                    fontStyle = FontStyle.Bold,
                    richText = true,
                    normal = new GUIStyleState { textColor = SliderStringColor }
                };

                GUILayout.BeginVertical(GUI.skin.box);

                GUILayout.BeginHorizontal();

                int count = 1;

                foreach (var kvp in emDic)
                {
                    //make selected encs red
                    var style = new GUIStyle(GUI.skin.button)
                    {
                        richText = true
                    };

                    if (kvp.Key == selectEmType)
                    {
                        style.normal.textColor = Color.red;
                    }

                    if (GUILayout.Button(kvp.Value, style))
                    {
                        selectEmType = kvp.Key;
                    }

                    GUILayout.FlexibleSpace();

                    if (count < MAX_COLUMN_LENGTH)
                    {
                        count++;
                    }
                    else
                    {
                        count = 1;

                        GUILayout.EndHorizontal();
                        GUILayout.BeginHorizontal();
                    }
                }

                for (int i = count; i < MAX_COLUMN_LENGTH; i++)
                {
                    GUILayout.FlexibleSpace();
                }

                GUILayout.EndHorizontal();

                GUILayout.EndVertical();

                //level slider
                GUILayout.Label("Level : " + levelSliderValue.ToString(), sliderStringStyle);
                levelSliderValue = (int)GUILayout.HorizontalSlider(levelSliderValue, 1, 255);

                //enemy number slider
                GUILayout.Label("Num : " + numSliderValue.ToString(), sliderStringStyle);
                numSliderValue = (int)GUILayout.HorizontalSlider(numSliderValue, 1, 94);

                GUILayout.BeginHorizontal();
                GUILayout.FlexibleSpace();

                if (GUILayout.Button(GetCultureString("SummonButton"), applyStyle))
                {
                    DoSummon();
                }

                GUILayout.FlexibleSpace();

                if (GUILayout.Button(GetCultureString("CloseButton")))
                {
                    CloseWindow();
                }

                GUILayout.FlexibleSpace();
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.EndScrollView();

            }
            catch (Exception ex)
            {
                OutputLog(LogLevel.Warning, GetCultureString("Error") + ex.Message.ToString());
            }
        }

        void DoSummon()
        {
            try
            {
                WindowState = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                var inst = SingletonMonoBehaviour<OcPlMng>.Inst;
                var plV3 = inst.getPlPos(0).Value;

                var pl = inst.getPl(0);

                if (pl != null && plV3 != null)
                {
                    plV3 += pl.Cmd.calcLookMoveDir() * 8f;

                    for (int i = 0; i < numSliderValue; i++)
                    {
                        Vector3 v3 = new Vector3();
                        v3 = plV3;
                        v3.x += UnityEngine.Random.Range(0f, 0.5f);
                        v3.z += UnityEngine.Random.Range(0f, 0.5f);

                        SingletonMonoBehaviour<OcEmMng>.Inst.doSpawn_FreeSlot_CheckHost_WithRayCheck(selectEmType, false, v3, (byte)levelSliderValue, true, null, null);
                    }
                }
            }
            catch (Exception ex)
            {
                OutputLog(LogLevel.Warning, GetCultureString("Error") + ex.Message.ToString());
            }
            finally
            {
                //initialize
                selectEmType = 0;

                levelSliderValue = 1;
                numSliderValue = 1;
            }
        }

        void CloseWindow()
        {
            try
            {
                WindowState = false;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;

                //initialize
                selectEmType = 0;

                levelSliderValue = 1;
                numSliderValue = 1;
            }
            catch (Exception ex)
            {
                OutputLog(LogLevel.Warning, GetCultureString("Error") + ex.Message.ToString());
            }
        }

        private string GetCultureString(string name)
        {
            return Properties.Resources.ResourceManager.GetString(name) ?? "";
        }

        public enum LogLevel
        {
            Info = 0,
            Warning = 1,
            Debug = 2,
            Error = 9,
        }

        public void OutputLog(LogLevel logLevel, string logString)
        {
            switch (logLevel)
            {
                case LogLevel.Info:
                    Logger.LogInfo(logString);
                    break;
                case LogLevel.Warning:
                    Logger.LogWarning(logString);
                    break;
                case LogLevel.Debug:
                    Logger.LogDebug(logString);
                    break;
                case LogLevel.Error:
                    Logger.LogError(logString);
                    break;
                default:
                    return;
            }
        }
    }
}
