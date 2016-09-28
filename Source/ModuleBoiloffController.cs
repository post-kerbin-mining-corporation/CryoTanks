using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace SimpleBoiloff
{
  public class BoiloffController : VesselModule
  {

        float timeWarpLimit = 100f;
        List<ModuleCryoTank> cryoTanks;
        Dictionary<string,PartModule> powerProducers;
        Dictionary<string,PartModule> powerConsumers;

        Vessel vessel;

        int partCount = -1;

        public void Start()
        {
            vessel = GetComponent<Vessel>();

            ullageSets = new List<UllageSet>();
            tanks = new List<Tanks.ModuleFuelTanks>();

            partCount = vessel.parts.Count;

            Reset();
        }

        void FixedUpdate()
        {
          if (HighLogic.LoadedSceneIsFlight)
          {
            if (TimeWarp.CurrentRate < timeWarpLimit)
            {
                DoLowWarpSimulation();

            } else
            {
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

          AllocatePower(production-consumption);

        }

        protected void AllocatePower(double availablePower)
        {
          usedPower = 0d;
          for (int i = 0; i< cryoTanks.Count;i++)
          {
              if (usedPower <= availablePower)
              {
                usedPower += cryoTanks[i].SetBoiloffState(false);
              } else
              {
                usedPower += cryoTanks[i].SetBoiloffState(true);
              }
          }
        }

        protected double DetermineBoiloffConsumption();
        {
          double totalConsumption = 0d;
          for (int i = 0; i< cryoTanks.Count;i++)
          {
            totalConsumption += (double)cryoTanks.GetCoolingCost();
          }
        }

        // TODO: implement me!
        protected double DetermineShipPowerConsumption()
        {
          double currentPowerRate = 0d;
          foreach (KeyValuePair<string,PartModule> kvp in powerConsumers)
          {
            currentPowerRate += GetPowerConsumption(kvp.Key, kvp.Value);
          }
          return currentPowerRate;
        }
        protected double GetPowerConsumption(string modType, PartModule pm)
        {
          double production = 0d;
          switch (modType)
          {
          }
          return production;
        }
        protected double DetermineShipPowerProduction()
        {
          double currentPowerRate = 0d;
          foreach (KeyValuePair<string,PartModule> kvp in powerProducers)
          {
            currentPowerRate += GetPowerProduction(kvp.Key, kvp.Value);
          }
          return currentPowerRate;
        }
        protected double GetPowerProduction(string modType, PartModule pm)
        {
          double production = 0d;
          switch (modType)
          {
            case "ModuleDeployableSolarPanel":
              production = GetModuleDeployableSolarPanelProduction(pm);
              break;
            case "ModuleCurvedSolarPanel":
                production = GetModuleDeployableSolarPanelProduction(pm);
                break;
            case "FissionGenerator":
              production = GetFissionGeneratorProduction(pm);
              break;
            case "ModuleRadioisotopeGenerator":
            production = GetModuleRadioisotopeGeneratorProduction(pm);
              break;
            case "ModuleGenerator":
              production = GetModuleGeneratorProduction(pm);
              break;
            case "ModuleResourceConverter":
              production = GetModuleResourceConverterProduction(pm);
              break;
            case default:
                break;
          }
          return production;
        }

        // Power generation evaluators, return power in double rate/s
        // STOCK
        protected double GetModuleGeneratorProduction(PartModule pm)
        {
          ModuleGenerator gen = (ModuleGenerator)pm;
          if (gen == null || !gen.generatorIsActive)
            return 0d;
          for (int i = 0;i < gen.inputList.Count;i++)
            if (gen.outputList[i].name == "ElectricCharge")
                return (double)gen.efficiency*inputList.rate;

        }
        //
        protected double GetModuleDeployableSolarPanelProduction(PartModule pm)
        {
          return (double)pm.Fields.GetValue("flowRate");
        }
        // TODO: implement me!
        protected double GetModuleResourceConverterProduction(PartModule pm)
        {
          return 0d;
        }
        // NFT
        protected double GetFissionGeneratorProduction(PartModule pm)
        {
          return (double)pm.Fields.GetValue("CurrentGeneration");
        }
        protected double GetModuleRadioisotopeGeneratorProduction(PartModule pm)
        {
          return (double)pm.Fields.GetValue("ActualPower");
        }
        protected double GetModuleCurvedSolarPanelProduction(PartModule pm)
        {
          return (double)pm.Fields.GetValue("energyFlow");
        }

        // Power consumption evaluators, return power in double rate/s
        // STOCK


        protected void GetVesselElectricalData()
        {
          cryoTanks.Clear();
          powerProducers.Clear();
          for (int i = partCount - 1; i >= 0; --i)
          {
              Part part = vessel.Parts[i];
              for (int j = part.Modules.Count - 1; j >= 0; --j)
              {
                  PartModule m = part.Modules[j];
                  // Add all power producers
                  // Stock
                  TryAddModule("ModuleGenerator", m, powerProducers, false);
                  TryAddModule("ModuleDeployableSolarPanel", m, powerProducers);
                  TryAddModule("ModuleResourceConverter", m, powerProducers, false);
                  // NFT
                  TryAddModule("FissionGenerator", m, powerProducers);
                  TryAddModule("ModuleRadioisotopeGenerator", m, powerProducers);
                  TryAddModule("ModuleCurvedSolarPanel", m, powerProducers);

                  // Add all power consumers
                  // Stock
                  TryAddModule("ModuleGenerator", m, powerConsumers, true);
                  TryAddModule("ModuleResourceConverter", m, powerConsumers, true);
                  TryAddModule("ModuleResourceHarvester", m, powerConsumers, true);

              }
            }
        }
        protected void TryAddModule(string moduleName, PartModule pm, ref Dictionary<string,PartModule> dict)
        {
          if (pm.moduleName == moduleName)
          {
            dict.Add(moduleName, pm);
          }
        }
        protected void TryAddModule(string moduleName, PartModule pm, ref Dictionary<string,PartModule> dict, bool inputs)
        {
          bool valid = false;
          if (pm.moduleName == moduleName)
          {
            // If i'm a resource converter...
            if (moduleName == "ModuleResourceConverter" || moduleName == "ModuleResourceHarvester")
            {
              BaseConverter conv = (BaseConverter)pm;
              if (inputs)
              {
                for (int i = 0;i < conv.inputList.Count;i++)
                  if (conv.inputList[i].ResourceName == "ElectricCharge")
                      valid = true;
              } else
              {
                for (int i = 0;i < conv.outputList.Count;i++)
                  if (conv.inputList[i].ResourceName == "ElectricCharge")
                      valid = true;
              }

            } else if (moduleName == "ModuleGenerator")
            {
              ModuleGenerator gen = (ModuleGenerator)pm;
              if (inputs)
              {
                for (int i = 0;i < gen.inputList.Count;i++)
                  if (gen.inputList[i].name == "ElectricCharge")
                      valid = true;
              } else
              {
                for (int i = 0;i < gen.outputList.Count;i++)
                  if (gen.inputList[i].name == "ElectricCharge")
                      valid = true;
              }
            }
          }
          if (valid)
            dict.Add(moduleName, pm);
        }

  }
}
