using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.UI.Screens;

namespace SimpleBoiloff
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class SimpleBoiloffUI:MonoBehavior
    {
        Vessel activeVessel;
        int partCount = 0;
        ModuleBoiloffController boiler;


        public void Start()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                //RenderingManager.AddToPostDrawQueue(0, DrawCapacitorGUI);
                FindData();
                Utils.LogWarn(windowID.ToString());
            }
        }

        public static void ToggleBoiloffWindow()
        {
            showBoiloffWindow = !showBoiloffWindow;
        }

        public void FindController()
        {
            activeVessel = FlightGlobals.ActiveVessel;
            partCount = activeVessel.parts.Count;

            //Debug.Log("NFE: Capacitor Manager: Finding Capcitors");
            boiler = activeVessel.GetComponent<ModuleBoiloffController>();
        }

        // GUI VARS
        // ----------
        public Rect windowPos = new Rect(200f, 200f, 500f, 200f);
        public Vector2 scrollPosition = Vector2.zero;
        static bool showReactorWindow = false;
        int windowID = new System.Random(3256231).Next();
        bool initStyles = false;

        GUIStyle gui_bg;
        GUIStyle gui_text;
        GUIStyle gui_header;
        GUIStyle gui_header2;
        GUIStyle gui_toggle;

        GUIStyle gui_window;

        GUIStyle gui_btn_shutdown;
        GUIStyle gui_btn_start;

        // Set up the GUI styles
        private void InitStyles()
        {
            gui_window = new GUIStyle(HighLogic.Skin.window);
            gui_header = new GUIStyle(HighLogic.Skin.label);
            gui_header.fontStyle = FontStyle.Bold;
            gui_header.alignment = TextAnchor.UpperLeft;
            gui_header.fontSize = 12;
            gui_header.stretchWidth = true;

            gui_header2 = new GUIStyle(gui_header);
            gui_header2.alignment = TextAnchor.MiddleLeft;

            gui_text = new GUIStyle(HighLogic.Skin.label);
            gui_text.fontSize = 11;
            gui_text.alignment = TextAnchor.MiddleLeft;

            gui_bg = new GUIStyle(HighLogic.Skin.textArea);
            gui_bg.active = gui_bg.hover = gui_bg.normal;

            gui_toggle = new GUIStyle(HighLogic.Skin.toggle);
            gui_toggle.normal.textColor = gui_header.normal.textColor;

            windowPos = new Rect(200f, 200f, 610f, 315f);

            initStyles = true;
        }
        public void Awake()
        {
            Utils.Log("UI: Awake");
        }

        private void OnGUI()
        {
            if (Event.current.type == EventType.Repaint || Event.current.isMouse)
            {
            }
            DrawBoiloffGUI();
        }


        private void DrawBoiloffGUI()
        {
            //Debug.Log("NFE: Start Capacitor UI Draw");
            Vessel activeVessel = FlightGlobals.ActiveVessel;

            if (activeVessel != null)
            {
                if (!initStyles)
                    InitStyles();

                if (showBoiloffWindow)
                {
                    if (boiler == null)
                      FindController();
                    // Debug.Log(windowPos.ToString());
                    GUI.skin = HighLogic.Skin;
                    gui_window.padding.top = 5;

                    windowPos = GUI.Window(windowID, windowPos, BoiloffWindow, new GUIContent(), gui_window);
                }
            }
            //Debug.Log("NFE: Stop Capacitor UI Draw");
        }


        // GUI function for the window
        private void BoiloffWindow(int windowId)
        {
            GUI.skin = HighLogic.Skin;
            GUILayout.BeginHorizontal();
            GUILayout.Label("Analytics boiloff controller", gui_header, GUILayout.MaxHeight(26f), GUILayout.MinHeight(26f), GUILayout.MinWidth(120f));
                GUILayout.FlexibleSpace();
                if (GUILayout.Button("X", GUILayout.MaxWidth(26f), GUILayout.MinWidth(26f), GUILayout.MaxHeight(26f), GUILayout.MinHeight(26f)))
                {
                    ToggleBoiloffWindow();
                }
            GUILayout.EndHorizontal();

            if (boiler != null && boiler.AnalyticMode)
            {
                double cryoCost = boiler.DetermineBoiloffConsumption();
                double gain = boiler.DetermineShipPowerProduction();
                double draw = boiler.DetermineShipPowerConsumption();
                scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.MinWidth(600f), GUILayout.MinHeight(271f));
                    GUILayout.BeginHorizontal();
                        //windowPos.height = 175f + 70f;
                        GUILayout.BeginVertical(gui_bg);
                        for (int i = 0; i <boiler.cryoTanks.Count; i++)
                        {
                            DrawTank(boiler.cryoTanks[i]);
                        }
                        GUILayout.Label(String.Format("Total Boiloff Consumption: {0:F2} Ec/s", cryoCost), gui_header);
                        GUILayout.EndVertical();
                        GUILayout.BeginVertical(gui_bg);
                        for (int i = 0; i <boiler.powerProducers.Count; i++)
                        {
                            DrawPowerProducer(boiler.powerProducers[i]);
                        }
                        GUILayout.Label(String.Format("Total Power Generation: {0:F2} Ec/s", gain), gui_header);
                        GUILayout.EndVertical();

                        GUILayout.BeginVertical(gui_bg);
                        for (int i = 0; i <boiler.powerConsumers.Count; i++)
                        {
                            DrawPowerConsumer(boiler.powerConsumers[i]);
                        }

                        GUILayout.Label(String.Format("Total Power Consumption: {0:F2} Ec/s", draw), gui_header);
                        GUILayout.EndVertical();
                   GUILayout.EndHorizontal();
                   GUILayout.Label(String.Format("Net Power Deficit: {0:F2} Ec/s", gain - draw - cryoCost), gui_header);
               GUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("Boiloff is being handled by parts, not the controller");
            }
            GUI.DragWindow();
        }

        private void DrawTank(ModuleCryoTank tank)
        {
          GUILayout.BeginHorizontal();
          GUILayout.Label(tank.part.partInfo.title, gui_header);
          GUILayout.Label(String.Format("Power Use: {0:F1} Ec/s", tank.GetCoolingCost()), gui_text);
          GUILayout.EndHorizontal();
        }
        private void DrawPowerProducer(ModuleCryoPowerProducer prod)
        {
          GUILayout.BeginHorizontal();
          GUILayout.Label(prod.ProducerType, gui_header);
          GUILayout.Label(String.Format("Producing: {0:F1} Ec/s", prod.GetPowerProduction()), gui_text);
          GUILayout.EndHorizontal();
        }
        private void DrawPowerConsumer(ModuleCryoPowerConsumer cons)
        {
          GUILayout.BeginHorizontal();
          GUILayout.Label(cons.ProducerType, gui_header);
          GUILayout.Label(String.Format("Consuming: {0:F1} Ec/s", cons.GetPowerConsumption()), gui_text);
          GUILayout.EndHorizontal();
        }

        void Update()
        {
          if (FlightGlobals.ActiveVessel != null)
            {
                if (activeVessel != null)
                {
                    if (partCount != activeVessel.parts.Count || activeVessel != FlightGlobals.ActiveVessel)
                    {
                        FindController();
                    }
                }
                else
                {
                    FindController();
                }

            }
            if (activeVessel != null)
            {
                if (partCount != activeVessel.parts.Count || activeVessel != FlightGlobals.ActiveVessel)
                {
                    FindController();

                }
            }
          if ((Input.GetKey(KeyCode.RightControl) || Input.GetKey(KeyCode.LeftControl)) &&
            (Input.GetKey(KeyCode.RightShift) || Input.GetKey(KeyCode.LeftShift)) &&
            Input.GetKeyDown(KeyCode.C) )
          {
            ToggleBoiloffWindow();
              // CTRL + Z
          }
        }
    }


}
