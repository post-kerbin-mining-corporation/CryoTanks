using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


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

        // PRIVATE
        private double fuelAmount = 0.0;
        private double coolingCost = 0.0;
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
        [KSPAction("Enable Charging")]
        public void EnableAction(KSPActionParam param) { Enable(); }

        [KSPAction("Disable Charging")]
        public void DisableAction(KSPActionParam param) { Disable(); }

        [KSPAction("Toggle Charging")]
        public void ToggleAction(KSPActionParam param)
        {
            CoolingEnabled = !CoolingEnabled;
        }

        public override string GetInfo()
        {
          string msg = String.Format("Loss Rate: {0:F4}% {1}/hr", BoiloffRate, FuelName);
          if (CoolingCost > 0.0f)
            msg += String.Format("\nCooling Cost: {0:F2} Ec/s", CoolingCost);
          return msg;
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
              fuelAmount = GetResourceAmount(FuelName);

              boiloffRateSeconds = BoiloffRate/100.0/3600.0;
              if (CoolingCost > 0.0)
              {
                coolingCost = fuelAmount/1000.0 * CoolingCost;
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
            double elapsedTime = part.vessel.missionTime - LastUpdateTime;
        
            double toBoil = Math.Pow(1.0 - boiloffRateSeconds, elapsedTime);
            part.RequestResource(FuelName, (1.0 - toBoil) * fuelAmount );
          }
        }

        public void Update()
        {
          if (HighLogic.LoadedSceneIsFlight)
          {
        
            // Show the insulation status field if there is a cooling cost
            if (CoolingCost > 0f)
            {
              foreach (BaseField fld in base.Fields)
                {
                    if (fld.guiName == "Insulation")
                        fld.guiActive = true;
                }
                if (Events["Enable"].active == Enabled || Events["Disable"].active != Enabled)
                {
                    Events["Disable"].active = Enabled;
                    Events["Enable"].active = !Enabled;
               }
            }
            if (fuelAmount == 0.0)
            {
                foreach (BaseField fld in base.Fields)
                {
                    if (fld.guiName == "Boiloff")
                        fld.guiActive = false;
                }
                
            }

          }
        }
        protected void FixedUpdate()
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
                fuelAmount = GetResourceAmount(FuelName);
                // If we have no fuel, no need to do any calculations
                if (fuelAmount == 0.0)
                {
                  BoiloffStatus = "No Fuel";
                  CoolingStatus = "No Fuel";
                  return;
                }

                // If the cooling cost is zero, we must boil off
                if (CoolingCost == 0f)
                {
                  DoBoiloff();
                  BoiloffStatus = String.Format("Losing {0:F3} u/s", boiled);
                }
                // else check for available power
                else
                {
                    if (CoolingEnabled)
                    {
                      double req = part.RequestResource("ElectricCharge", coolingCost * TimeWarp.fixedDeltaTime);
                      if (req < (double)(coolingCost * TimeWarp.fixedDeltaTime))
                      {
                        DoBoiloff();
                        BoiloffStatus = String.Format("Losing {0:F3} u/s", boiled);
                        CoolingStatus = "ElectricCharge deprived!";
                      } else
                      {
                        BoiloffStatus = String.Format("Insulated");
                        CoolingStatus = String.Format("Using {0:F2} Ec/s", coolingCost);
                      }
                    } 
                    else
                    {
                        DoBoiloff();
                        BoiloffStatus = String.Format("Losing {0:F3} u/s", boiled);
                        CoolingStatus = "Disabled";
                    }
                  }
              LastUpdateTime = part.vessel.missionTime;
            }
        }
        protected void DoBoiloff()
        {
            // 0.025/100/3600
      			double toBoil = Math.Pow(1.0-boiloffRateSeconds, TimeWarp.fixedDeltaTime);

      			boiled = part.RequestResource(FuelName, (1.0-toBoil) * fuelAmount );
        }	

        private double boiled = 0d;

        protected double GetResourceAmount(string nm)
       {
           return this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(nm).id).amount;
       }

    }
}
