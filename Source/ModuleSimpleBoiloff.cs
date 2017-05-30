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

        [KSPField(isPersistant = false)]
        public double FuelTotal;

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
        public double coolingCost = 0.0;

        // PRIVATE
        private bool hasResource = false;
        private double fuelAmount = 0.0;
        private double maxFuelAmount = 0.0;

        private double boiloffRateSeconds = 0.0;

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

        public override string GetInfo()
        {

          string msg;
            string fuelDisplayName = PartResourceLibrary.Instance.GetDefinition(FuelName).displayName
            if (CoolingCost > 0.0f)
            {
                msg =  Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoCooled", BoiloffRate.ToString("F2"), fuelDisplayName, CoolingCost.ToString("F2"), fuelDisplayName);
            } else
            {
              msg =  Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoUncooled", BoiloffRate.ToString("F2"), fuelDisplayName);
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

            if (HighLogic.LoadedSceneIsFlight)
            {
                hasResource = isResourcePresent(FuelName);
                if (!hasResource)
                {
                    Events["Disable"].guiActive = false;
                    Events["Enable"].guiActive = false;
                    Fields["BoiloffStatus"].guiActive = false;
                    return;
                }
              maxFuelAmount = GetMaxResourceAmount(FuelName);

              boiloffRateSeconds = BoiloffRate/100.0/3600.0;
              if (CoolingCost > 0.0)
              {
                coolingCost = maxFuelAmount/1000.0 * CoolingCost;
                Events["Disable"].guiActive = true;
                Events["Enable"].guiActive = true;
              }
              // Catchup
              DoCatchup();
            }
        }

        public void DoCatchup()
        {
          if (part.vessel.missionTime > 0.0)
          {
              if (part.RequestResource("ElectricCharge", coolingCost * TimeWarp.fixedDeltaTime) < coolingCost * TimeWarp.fixedDeltaTime)
              {
                  double elapsedTime = part.vessel.missionTime - LastUpdateTime;

                  double toBoil = Math.Pow(1.0 - boiloffRateSeconds, elapsedTime);
                  part.RequestResource(FuelName, (1.0 - toBoil) * fuelAmount,ResourceFlowMode.NO_FLOW);
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
              hasResource = isResourcePresent(FuelName);
              if (CoolingCost > 0f && hasResource)
              {
                  Fields["CoolingStatus"].guiActive = true;
                  double max = GetMaxResourceAmount(FuelName);
                  CoolingStatus =  Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Editor", (CoolingCost * (float)(max / 1000.0)).ToString("F2"));
              }
              if (CoolingCost > 0f && !hasResource)

                Fields["CoolingStatus"].guiActive = false;

          }

        }
        protected void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight && hasResource)
            {
                fuelAmount = GetResourceAmount(FuelName);
                // If we have no fuel, no need to do any calculations
                if (fuelAmount == 0.0)
                {
                    BoiloffStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_BoiloffStatus_NoFuel");
                    CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_NoFuel");
                    return;
                }

                // If the cooling cost is zero, we must boil off
                if (coolingCost == 0f)
                {
                    BoiloffOccuring = true;
                    BoiloffStatus = FormatRate(boiloffRateSeconds* fuelAmount);
                }
                // else check for available power
                else
                {
                    if (!CoolingEnabled)
                    {
                        BoiloffOccuring = true;
                        BoiloffStatus = FormatRate(boiloffRateSeconds * fuelAmount);
                        CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Disabled");
                    }
                  }
                  ConsumeCharge();
                if (BoiloffOccuring)
                {
                    DoBoiloff(1d);
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
        public void TryConsumeCharge()
        {
            if (CoolingEnabled && coolingCost > 0f)
            {
                double chargeRequest = coolingCost * TimeWarp.fixedDeltaTime;
                double req = part.RequestResource("ElectricCharge", chargeRequest);
                double tolerance = 0.0001;
                if (req >= chargeRequest - tolerance)
                {
                    SetBoiloffState(false);
                } else
                {
                    SetBoiloffState(true);
                }
            }
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


        protected void DoBoiloff(double scale)
        {
            // 0.025/100/3600
      		double toBoil = Math.Pow(1.0-boiloffRateSeconds, TimeWarp.fixedDeltaTime)*scale;
      		boiled = part.RequestResource(FuelName, (1.0-toBoil) * fuelAmount,ResourceFlowMode.NO_FLOW );
        }

        private double boiled = 0d;

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
