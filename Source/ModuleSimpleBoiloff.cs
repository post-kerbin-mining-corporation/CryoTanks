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

        // Cost to cool off u/s
        [KSPField(isPersistant = false)]
        public float CoolingCost = 0.0f;

        [KSPField(isPersistant = true)]
        public double LastUpdateTime = 0;

        // Status string
        [KSPField(isPersistant = false, guiActive = true, guiName = "Boiloff Status")]
        public string BoiloffStatus = "N/A";

        [KSPField(isPersistant = false, guiActive = false, guiName = "Insulation Status")]
        public string CoolingStatus = "N/A";

        private double fuelAmount = 0.0;
        private double boiloffRateSeconds = 0.0;

        public override string GetInfo()
        {
          string msg = String.Format("Loss Rate: {0:F4} {1}/s", BoiloffRate, FuelName);
          if (CoolingCost > 0.0f)
            msg += String.Format("\nCooling Cost: {0:F2} Ec/s", CoolingCost);
          return msg;
        }

        public override void OnStart(PartModule.StartState state)
        {
            if (HighLogic.LoadedSceneIsFlight)
            {
              boiloffRateSeconds = BoiloffRate/3600.0;
              // Catchup

              DoCatchup();
            }
        }

        public void DoCatchup()
        {
          if (part.vessel.missionTime > 0.0)
          {
            double elapsedTime = part.vessel.missionTime - LastUpdateTime;
            double toBoil = Math.Pow(boiloffRateSeconds,elapsedTime)*GetResourceAmount(FuelName);
            part.RequestResource(FuelName, toBoil);
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
                    if (fld.guiName == "Insulation Status")
                        fld.guiActive = true;
                }
            }
            if (fuelAmount == 0.0)
            {
                foreach (BaseField fld in base.Fields)
                {
                    if (fld.guiName == "BoiloffStatus")
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
                  BoiloffStatus = "No Fuel Remaining";
                  CoolingStatus = "No Cooling Required";
                  return;
                }

                // If the cooling cost is zero, we must boil off
                if (CoolingCost == 0f)
                {
                  DoBoiloff();
                  BoiloffStatus = String.Format("Losing {0:F2} u/s", BoiloffRate);
                }
                // else check for available power
                else
                {
                    double req = part.RequestResource("ElectricCharge", CoolingCost * TimeWarp.fixedDeltaTime);
                    if (req < (double)CoolingCost)
                    {
                      DoBoiloff();
                      BoiloffStatus = String.Format("Losing {0:F2} u/s", boiloffRateSeconds);
                      CoolingStatus = "ElectricCharge deprived!";
                    } else
                    {
                      BoiloffStatus = String.Format("Insulated", BoiloffRate);
                      CoolingStatus = String.Format("Using {0:F2} Ec/s", CoolingCost);
                    }
                  }
              LastUpdateTime = part.vessel.missionTime;
            }
        }
        protected void DoBoiloff()
        {
          double toBoil = boiloffRateSeconds*fuelAmount;
          part.RequestResource(FuelName, toBoil*TimeWarp.fixedDeltaTime,ResourceFlowMode.NO_FLOW);

        }

        protected double GetResourceAmount(string nm)
       {
           return this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(nm).id).amount;
       }

    }
}
