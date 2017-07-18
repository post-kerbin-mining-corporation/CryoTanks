using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace SimpleBoiloff
{
    public class ModuleCryoTank: PartModule
    {
        // Name of the fuel to boil off
        [KSPField(isPersistant = false)]
        public string FuelName;

        // Rate of boiling off in %/hr
        [KSPField(isPersistant = false)]
        public float BoiloffRate = 0.025f;

        // Cost to cool off u/s per 1000 u
        [KSPField(isPersistant = false)]
        public float CoolingCost = 0.0f;

        // Last timestamp that boiloff occurred
        [KSPField(isPersistant = true)]
        public double LastUpdateTime = 0;

        // Whether active tank refrigeration is occurring
        [KSPField(isPersistant = true)]
        public bool CoolingEnabled = true;

        [KSPField(isPersistant = true)]
        public bool BoiloffOccuring = false;

        public bool HasResource { get { return HasResource; } }

        [KSPField(isPersistant = false)]
        public double currentCoolingCost = 0.0;
        private double coolingCost = 0.0;

        // PRIVATE
        private List<BoiloffFuel> fuels;
        private bool hasResource = false;
        private double fuelAmount = 0.0;
        private double maxFuelAmount = 0.0;

        private double boiloffRateSeconds = 0.0;

        // Represents a fuel that boils off
        [System.Serializable]
        public class BoiloffFuel
        {
          public string fuelName;
          public float boiloffRate;

          public PartResource resource;

          bool fuelPresent = false;
          float boiloffRateSeconds = 0d;
          int id = -1;
          Part part;

          public BoiloffFuel(ConfigNode node, Part p)
          {
              part = p;
              node.TryGetValue("FuelName", ref fuelName);
              node.TryGetValue("BoiloffRate", ref boiloffRate);
          }

          public void Initialize()
          {
              if (id == -1)
                id = PartResourceLibrary.Instance.GetDefinition(nm).id;
              resource = part.Resources.Get(id);
              boiloffRateSeconds = boiloffRate/100f/3600f;
              fuelPresent = true;
          }

          public void Boiloff(double seconds)
          {
            double toBoil = Math.Pow(1.0 - boiloffRateSeconds, seconds);
            part.RequestResource(fuelName, (1.0 - toBoil) * resource.amount, ResourceFlowMode.NO_FLOW);
          }


        }

        // UI FIELDS/ BUTTONS
        // Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Boiloff")]
        public string BoiloffStatus = "N/A";

        [KSPField(isPersistant = false, guiActive = false, guiName = "Insulation")]
        public string CoolingStatus = "N/A";

        [KSPEvent(guiActive = false, guiName = "Enable Cooling", active = true)]
        public void Enable()
        {
            CoolingEnabled = true;
        }
        [KSPEvent(guiActive = false, guiName = "Disable Cooling", active = false)]
        public void Disable()
        {
            CoolingEnabled = false;
        }

        // ACTIONS
        [KSPAction("Enable Cooling")]
        public void EnableAction(KSPActionParam param) { Enable(); }

        [KSPAction("Disable Cooling")]
        public void DisableAction(KSPActionParam param) { Disable(); }

        [KSPAction("Toggle Cooling")]
        public void ToggleAction(KSPActionParam param)
        {
            CoolingEnabled = !CoolingEnabled;
        }

        // REWRITE ME
        public override string GetInfo()
        {

          string msg;
          string fuelDisplayName
            if (CoolingCost > 0.0f)
            {
              msg = String.Format("Cryogenic fuels evaporate over time if uncooled\n\n Cooling: {0} Ec/s per 1000 units\n\n", CoolingCost.ToString("F2"));
              foreach(BoiloffFuel fuel in fuels)
              {
                fuelDisplayName = PartResourceLibrary.Instance.GetDefinition(fuels[i].fuelName).displayName;
                msg += String.Format("\n- {0}: {1}%/hr ", fuels[i].BoiloffRate.ToString("F2"), fuelDisplayName);
              }

            } else
            {
              msg = String.Format("Cryogenic fuels evaporate over time\n\n");
              foreach(BoiloffFuel fuel in fuels)
              {
                fuelDisplayName = PartResourceLibrary.Instance.GetDefinition(fuels[i].fuelName).displayName;
                msg += String.Format("\n- {0}: {1}%/hr ", fuels[i].BoiloffRate.ToString("F2"), fuelDisplayName);
              }
              ///msg =  Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoUncooled", BoiloffRate.ToString("F2"), fuelDisplayName);
            }
          return msg;
        }


        public void Start()
        {
            Fields["BoiloffStatus"].guiName = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_BoiloffStatus");
            Fields["CoolingStatus"].guiName = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus");

            Events["Enable"].guiName = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Event_Enable");
            Events["Disable"].guiName = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Event_Disable");

            Actions["EnableAction"].guiName =  Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Action_EnableAction");
            Actions["DisableAction"].guiName =  Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Action_DisableAction");
            Actions["ToggleAction"].guiName =  Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Action_ToggleAction");

            if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
            {
              if (fuels == null || fuels.Count == 0)
              {
                  ConfigNode node = GameDatabase.Instance.GetConfigs("PART").
                      Single(c => part.partInfo.name == c.name).config.
                      GetNodes("MODULE").Single(n => n.GetValue("name") == moduleName);
                  OnLoad(node);
              }
            }

            if (HighLogic.LoadedSceneIsFlight)
            {
                hasResource = false;
                foreach(BoiloffFuel fuel in fuels)
                {
                  if (isResourcePresent(fuel.fuelName))
                  {
                    hasResource = true;
                    fuel.Initialize();
                  }
                }
                if (!hasResource)
                {
                    Events["Disable"].guiActive = false;
                    Events["Enable"].guiActive = false;
                    Fields["BoiloffStatus"].guiActive = false;
                    return;
                }
                maxFuelAmount = GetTotalMaxResouceAmount();
              if (CoolingCost > 0.0)
              {
                coolingCost = maxFuelAmount/1000.0 * CoolingCost;
                Events["Disable"].guiActive = true;
                Events["Enable"].guiActive = true;
                Events["Enable"].guiActiveEditor = true;
                Events["Disable"].guiActiveEditor = true;
              }
              // Catchup
              DoCatchup();
            }
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            ConfigNode[] varNodes = node.GetNodes("BOILOFFCONFIG");
            lengthConfigs = new List<BoiloffFuel>();
            for (int i=0; i < varNodes.Length; i++)
            {
              fuels.Add(new BoiloffFuel(varNodes[i]));
            }
        }

        public void DoCatchup()
        {
          if (part.vessel.missionTime > 0.0)
          {
              if (part.RequestResource("ElectricCharge", coolingCost * TimeWarp.fixedDeltaTime) < coolingCost * TimeWarp.fixedDeltaTime)
              {
                  double elapsedTime = part.vessel.missionTime - LastUpdateTime;
                  for (int i = 0; i < fuels.Count ; i++)
                    fuels[i].Boiloff(elapsedTime);
              }
          }
        }

        public void Update()
        {
          if (HighLogic.LoadedSceneIsFlight && hasResource)
          {
            // Show the insulation status field if there is a cooling cost
            if (CoolingCost > 0f)
            {
              Fields["CoolingStatus"].guiActive = true;
              if (Events["Enable"].active == CoolingEnabled || Events["Disable"].active != CoolingEnabled)
                {
                    Events["Disable"].active = CoolingEnabled;
                    Events["Enable"].active = !CoolingEnabled;
               }
            }
            if (fuelAmount == 0.0)
            {

                Fields["BoiloffStatus"].guiActive = false;
            }
          }
          if (HighLogic.LoadedSceneIsEditor)
          {
              hasResource = false;
              foreach(BoiloffFuel fuel in fuels)
              {
                if (isResourcePresent(fuel.fuelName))
                {
                  hasResource = true;
                  fuel.Initialize();
                }
              }
              if (CoolingCost > 0f && hasResource)
              {
                  Fields["CoolingStatus"].guiActive = true;

                  double max = GetTotalMaxResouceAmount();

                  CoolingStatus =  Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Editor", (CoolingCost * (float)(max / 1000.0)).ToString("F2"));
              }
              if (CoolingCost > 0f && !hasResource)

                Fields["CoolingStatus"].guiActive = false;
              if (CoolingCost > 0f)
              {
                  Events["Disable"].guiActiveEditor = true;
                  Events["Enable"].guiActiveEditor = true;
                  if (Events["Enable"].active == CoolingEnabled || Events["Disable"].active != CoolingEnabled)
                  {
                      Events["Disable"].active = CoolingEnabled;
                      Events["Enable"].active = !CoolingEnabled;
                  }
              }
              else
              {
                  Events["Disable"].guiActiveEditor = false;
                  Events["Enable"].guiActiveEditor = false;
              }
          }

        }
        protected void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && hasResource)
            {
                fuelAmount = GetTotalResouceAmount();

                // If we have no fuel, no need to do any calculations
                if (fuelAmount == 0.0)
                {
                    BoiloffStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_BoiloffStatus_NoFuel");
                    CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_NoFuel");
                    currentCoolingCost = 0.0;
                    return;
                }

                // If the cooling cost is zero, we must boil off
                if (coolingCost == 0f)
                {
                    BoiloffOccuring = true;
                    BoiloffStatus = FormatRate(boiloffRateSeconds* fuelAmount);
                    currentCoolingCost = 0.0;
                }
                // else check for available power
                else
                {
                    if (!CoolingEnabled)
                    {
                        BoiloffOccuring = true;
                        BoiloffStatus = FormatRate(GetTotalBoiloffRate() * fuelAmount);
                        CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Disabled");
                        currentCoolingCost = 0.0;
                    }
                    else
                    {
                        ConsumeCharge();
                        currentCoolingCost = coolingCost;
                    }

                  }

                if (BoiloffOccuring)
                {
                    DoBoiloff();
                }
                if (part.vessel.missionTime > 0.0)
                {
                    LastUpdateTime = part.vessel.missionTime;
                }
            }
        }
        // Returns the cooling cost if the system is enabled
        public double GetCoolingCost()
        {
          if (CoolingEnabled)
          {
            return coolingCost;
          }
          return 0d;
        }

        public double SetBoiloffState(bool state)
        {
          if (CoolingEnabled && coolingCost > 0f)
          {
            if (state)
            {
              BoiloffOccuring = true;
              BoiloffStatus = FormatRate(boiloffRateSeconds * fuelAmount);
              CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Uncooled");
            } else
            {

              BoiloffOccuring = false;
              BoiloffStatus = String.Format("Insulated");
              CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Cooling", coolingCost.ToString("F2"));

            }
            return (double)coolingCost;
        }
        return 0d;


        }

        public void ConsumeCharge()
        {
          if (CoolingEnabled && coolingCost > 0f)
          {
            double chargeRequest = coolingCost * TimeWarp.fixedDeltaTime;

            double req = part.RequestResource("ElectricCharge", chargeRequest);
            //Debug.Log(req.ToString() + " rec, wanted "+ chargeRequest.ToString());
            // Fully cooled
            double tolerance = 0.0001;
            if (req >= chargeRequest - tolerance)
            {
                BoiloffOccuring = false;
                BoiloffStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_BoiloffStatus_Insulated");
                CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Cooling", coolingCost.ToString("F2"));
            }
            else
            {
                BoiloffOccuring = true;
                BoiloffStatus = FormatRate(boiloffRateSeconds * fuelAmount);
                CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Uncooled");
            }
          }
        }


        protected void DoBoiloff()
        {
            for (int i = 0; i < fuels.Count ; i++)
              fuels[i].Boiloff(TimeWarp.fixedDeltaTime);
        }

        protected string FormatRate(double rate)
        {
            double adjRate = rate;
            string interval = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_TimeInterval_Second_Abbrev");
            if (adjRate < 0.01)
            {
                adjRate = adjRate*60.0;
                interval = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_TimeInterval_Minute_Abbrev");
            }
            if (adjRate < 0.01)
            {
                adjRate = adjRate * 60.0;
                interval = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_TimeInterval_Hour_Abbrev");
            }
            return Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_BoiloffStatus_Boiloff", adjRate.ToString("F2"), interval.ToString());
        }
        public bool isResourcePresent(string nm)
        {
            int id = PartResourceLibrary.Instance.GetDefinition(nm).id;
            PartResource res = this.part.Resources.Get(id);
            if (res == null)
                return false;
            return true;
        }
        protected double GetTotalResouceAmount()
        {
            double max = 0d;
            for (int i = 0; i < fuels.Count ; i++)
              max += fuels[i].resource.amount;
            return max;
        }
        protected double GetTotalMaxResouceAmount()
        {
            double max = 0d;
            for (int i = 0; i < fuels.Count ; i++)
              max += fuels[i].resource.maxAmount;
            return max;
        }
        protected double GetTotalBoiloffRate()
        {
            double max = 0d;
            for (int i = 0; i < fuels.Count ; i++)
              max += fuels[i].boiloffRateSeconds;
            return max;
        }

        protected double GetResourceAmount(string nm)
        {
            PartResource res = this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(nm).id);
            return res.amount;
        }
        protected double GetMaxResourceAmount(string nm)
        {
            int id = PartResourceLibrary.Instance.GetDefinition(nm).id;
            PartResource res = this.part.Resources.Get(id);
            return res.maxAmount;
        }

    }
}
