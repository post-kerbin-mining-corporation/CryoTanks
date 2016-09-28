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
        List<ModuleCryoTank> cryoTanks = new List<ModuleCryoTank>();
        Dictionary<string,PartModule> powerProducers = new Dictionary<string,PartModule>();
        Dictionary<string, PartModule> powerConsumers = new Dictionary<string, PartModule>();

        Vessel vessel;
        bool dataReady = false;
        int partCount = -1;

        protected override void  OnStart()
        {
 	        base.OnStart();
            vessel = GetComponent<Vessel>();
            partCount = vessel.parts.Count;

            GetVesselElectricalData();
        }
        

        void FixedUpdate()
        {
          if (HighLogic.LoadedSceneIsFlight && dataReady)
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
          double usedPower = 0d;
          for (int i = 0; i< cryoTanks.Count;i++)
          {
              if (usedPower <= availablePower)
              {
                usedPower += cryoTanks[i].SetBoiloffState(true);
              } else
              {
                usedPower += cryoTanks[i].SetBoiloffState(false);
              }
          }
        }

        protected double DetermineBoiloffConsumption()
        {
          double totalConsumption = 0d;
          for (int i = 0; i < cryoTanks.Count;i++)
          {
            totalConsumption += cryoTanks[i].GetCoolingCost();
          }
          return totalConsumption;
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
          for (int i = 0; i < gen.resHandler.outputResources.Count; i++)
              if (gen.resHandler.outputResources[i].name == "ElectricCharge")
                  return (double)gen.efficiency * gen.resHandler.outputResources[i].rate;

          return 0d;

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

              ModuleCryoTank tank =  part.GetComponent<ModuleCryoTank>();
              if (tank != null)
                  cryoTanks.Add(tank);
              for (int j = part.Modules.Count - 1; j >= 0; --j)
              {
                  
                  PartModule m = part.Modules[j];
                  // Add all power producers
                  // Stock
                  TryAddModule("ModuleGenerator", m, ref powerProducers, false);
                  TryAddModule("ModuleDeployableSolarPanel", m, ref powerProducers);
                  TryAddModule("ModuleResourceConverter", m, ref powerProducers, false);
                  // NFT
                  TryAddModule("FissionGenerator", m, ref powerProducers);
                  TryAddModule("ModuleRadioisotopeGenerator", m, ref powerProducers);
                  TryAddModule("ModuleCurvedSolarPanel", m, ref powerProducers);

                  // Add all power consumers
                  // Stock
                  TryAddModule("ModuleGenerator", m, ref powerConsumers, true);
                  TryAddModule("ModuleResourceConverter", m, ref powerConsumers, true);
                  TryAddModule("ModuleResourceHarvester", m, ref powerConsumers, true);
              }
            }
          Debug.Log(String.Format("CryoTanks: {0} cryo tanks detected", cryoTanks.Count));
          dataReady = true;
        }
        protected void TryAddModule(string moduleName, PartModule pm, ref Dictionary<string,PartModule> dict)
        {
          if (pm.moduleName == moduleName)
          {
            dict.Add(moduleName, pm);
            Debug.Log(String.Format("CryoTanks: {0} detected and added",moduleName));
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
                  for (int i = 0; i < gen.resHandler.inputResources.Count; i++)
                      if (gen.resHandler.inputResources[i].name == "ElectricCharge")
                      {
                          valid = true;
                      }
              } else
              {
                  for (int i = 0; i < gen.resHandler.outputResources.Count; i++)
                      if (gen.resHandler.outputResources[i].name == "ElectricCharge")
                      {
                          valid = true;
                      }
              }
            }
          }
          if (valid)
          {
              dict.Add(moduleName, pm);
              Debug.Log(String.Format("CryoTanks: {0} detected and added", moduleName));
          }
        }

  }
}
