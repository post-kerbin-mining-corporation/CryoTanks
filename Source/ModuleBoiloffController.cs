using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace SimpleBoiloff
{
  public class BoiloffController : VesselModule
  {

        float timeWarpLimit = 1000f;
        public List<ModuleCryoTank> cryoTanks = new List<ModuleCryoTank>();
        public List<ModuleCryoPowerConsumer> powerConsumers = new List<ModuleCryoPowerConsumer>();
        public List<ModuleCryoPowerProducer> powerProducers = new List<ModuleCryoPowerProducer>();


        bool analyticMode = false;
        bool dataReady = false;
        int partCount = -1;

        public bool AnalyticMode {get {return analyticMode;}}

        protected override void  OnStart()
        {
 	        base.OnStart();
            partCount = vessel.parts.Count;

            GetVesselElectricalData();
        }


        void FixedUpdate()
        {
          if (HighLogic.LoadedSceneIsFlight && dataReady)
          {
            if (TimeWarp.CurrentRate < timeWarpLimit)
            {
              analyticMode = false;
                DoLowWarpSimulation();

            } else
            {
              analyticMode = true;
              DoHighWarpSimulation();
            }

          }
        }
        protected void DoLowWarpSimulation()
        {
          for (int i = 0; i< cryoTanks.Count;i++)
          {
              cryoTanks[i].ConsumeCharge();
          }
        }
        protected void DoHighWarpSimulation()
        {
          double production = DetermineShipPowerProduction();
          double consumption = DetermineShipPowerConsumption();
          double boiloffConsumption = DetermineBoiloffConsumption();
          AllocatePower(production-consumption, boiloffConsumption);
        }

        protected void AllocatePower(double availablePower, double boiloffConsumption)
        {

          float powerDeficit = Mathf.Clamp((float)(availablePower - boiloffConsumption),-9999999f, 0f);

         // Debug.Log(String.Format("Power Deficit: {0}", powerDeficit));
          double usedPower = 0d;

          for (int i = 0; i< cryoTanks.Count;i++)
          {
              if (usedPower >= availablePower)
              {
                cryoTanks[i].TryConsumeCharge();
                //usedPower += cryoTanks[i].SetBoiloffState(true);
              } else
              {
                usedPower += cryoTanks[i].SetBoiloffState(false);
              }
          }
        }

        public double DetermineBoiloffConsumption()
        {
          double totalConsumption = 0d;
          for (int i = 0; i < cryoTanks.Count;i++)
          {
            totalConsumption += cryoTanks[i].GetCoolingCost();
          }
          //Debug.Log(String.Format("CryoTanks: total ship boiloff consumption: {0} Ec/s", totalConsumption));
          return totalConsumption;
        }

        // TODO: implement me!
        public double DetermineShipPowerConsumption()
        {
          double currentPowerRate = 0d;
          foreach (ModuleCryoPowerConsumer p in powerConsumers)
          {
            currentPowerRate += p.GetPowerConsumption();
          }
          //Debug.Log(String.Format("CryoTanks: total ship power consumption: {0} Ec/s", currentPowerRate));
          return currentPowerRate;
        }

        public double DetermineShipPowerProduction()
        {
          double currentPowerRate = 0d;
          foreach (ModuleCryoPowerProducer p in powerProducers)
          {
            currentPowerRate += p.GetPowerProduction();
          }
          //Debug.Log(String.Format("CryoTanks: total ship power production: {0} Ec/s", currentPowerRate));
          return currentPowerRate;
        }

        protected void GetVesselElectricalData()
        {
          cryoTanks.Clear();
           powerProducers.Clear();
          for (int i = partCount - 1; i >= 0; --i)
          {
              Part part = vessel.Parts[i];

              ModuleCryoTank tank =  part.GetComponent<ModuleCryoTank>();
              if (tank != null)
              {
                  if (tank.isResourcePresent(tank.FuelName))
                    cryoTanks.Add(tank);
              }

              for (int j = part.Modules.Count - 1; j >= 0; --j)
              {
                  PartModule m = part.Modules[j];
                  // Try to create accessor modules
                  bool success = TrySetupProducer(m);
                  if (!success)
                    TrySetupConsumer(m);
              }
            }
          //Debug.Log(String.Format("CryoTanks: {0} cryo tanks detected", cryoTanks.Count));
          dataReady = true;
        }
        protected bool TrySetupProducer(PartModule pm)
        {
          PowerProducerType prodType;
          if (TryParse<PowerProducerType>(pm.moduleName, out prodType))
          {
            // Verify
            bool isProducer = VerifyInputs(pm, true);
            if (isProducer)
            {
              ModuleCryoPowerProducer prod =  new ModuleCryoPowerProducer(prodType, pm);
              powerProducers.Add(prod);
              return true;
            }
          }
          return false;


        }
        protected bool TrySetupConsumer(PartModule pm)
        {
          PowerConsumerType prodType;
          if (TryParse<PowerConsumerType>(pm.moduleName, out prodType))
          {
            // Verify
            bool isConsumer = VerifyInputs(pm, false);
            if (isConsumer)
            {
              ModuleCryoPowerConsumer con =  new ModuleCryoPowerConsumer(prodType, pm);
              powerConsumers.Add(con);
              return true;
            }
          }
          return false;


        }
        /// Checks to see whether a ModuleGenerator/ModuleResourceConverter/ModuleResourceHarvester is a producer or consumer
        protected bool VerifyInputs(PartModule pm, bool isProducer)
        {
          if (pm.moduleName == "ModuleResourceConverter" || pm.moduleName == "ModuleResourceHarvester")
          {
            BaseConverter conv = (BaseConverter)pm;
            if (isProducer)
            {
              for (int i = 0;i < conv.outputList.Count;i++)
                if (conv.inputList[i].ResourceName == "ElectricCharge")
                    return true;
              return false;
            } else
            {
                for (int i = 0; i < conv.inputList.Count; i++)
                    if (conv.inputList[i].ResourceName == "ElectricCharge")
                        return true;
              return false;
            }
          }
          if (pm.moduleName == "ModuleGenerator")
          {
            ModuleGenerator gen = (ModuleGenerator)pm;
            if (isProducer)
            {
              for (int i = 0; i < gen.resHandler.outputResources.Count; i++)
                  if (gen.resHandler.outputResources[i].name == "ElectricCharge")
                  {
                      return true;
                  }
              return false;
            } else
            {
              for (int i = 0; i < gen.resHandler.inputResources.Count; i++)
                  if (gen.resHandler.inputResources[i].name == "ElectricCharge")
                  {
                      return true;
                  }
              return false;
            }
          }
          return true;
        }


        public static bool TryParse<TEnum>(string value, out TEnum result)
      where TEnum : struct, IConvertible
        {
            var retValue = value == null ?
                        false :
                        Enum.IsDefined(typeof(TEnum), value);
            result = retValue ?
                        (TEnum)Enum.Parse(typeof(TEnum), value) :
                        default(TEnum);
            return retValue;
        }

  }
}
