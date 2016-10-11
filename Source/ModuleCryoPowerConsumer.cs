using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace SimpleBoiloff
{
    public enum PowerConsumerType {
      ModuleActiveRadiator,
      ModuleResourceHarvester,
      ModuleGenerator,
      ModuleResourceConverter,
    }

    public class ModuleCryoPowerConsumer
    {

      PowerConsumerType consumerType;
      // Generic reference to PartModule
      PartModule pm

      // Hard references to stock modules
      ModuleGenerator gen;
      ModuleResourceConverter converter;
      ModuleResourceHarvester harvester;
      ModuleActiveRadiator radiator;


      public ModuleCryoPowerConsumer(PowerConsumerType tp, PartModule mod)
      {
        consumerType = tp;
        pm = mod;
        switch (consumerType)
        {
          case PowerConsumerType.ModuleActiveRadiator:
            radiator = (ModuleActiveRadiator)pm;
            break;
          case PowerConsumerType.ModuleResourceConverter:
            converter = (ModuleResourceConverter)pm;
            break;
          case PowerConsumerType.ModuleGenerator:
            gen = (ModuleGenerator)pm;
            break;
          case PowerConsumerType.ModuleResourceHarvester:
            harvester = (ModuleResourceHarvester)pm;
            break;
        }
      }
      public double GetPowerConsumption()
      {
        switch (consumerType)
        {
          case PowerConsumerType.ModuleActiveRadiator:
            return 0d;
            break;
          case PowerConsumerType.ModuleResourceConverter:
            return 0d;
            break;
          case PowerConsumerType.ModuleGenerator:
            return 0d;
            break;
          case PowerConsumerType.ModuleResourceHarvester:
            return 0d;
            break;
        }
        return 0d;
      }

      double GetModuleGeneratorConsumption()
      {
          if (gen == null || !gen.generatorIsActive)
              return 0d;
          for (int i = 0; i < gen.resHandler.inputResources.Count; i++)
              if (gen.resHandler.inputResources[i].name == "ElectricCharge")
                  return (double)gen.efficiency * gen.resHandler.inputResources[i].rate;

          return 0d;
      }
      double GetModuleResourceConverterConsumption()
      {
          return 0d;
      }
      double GetModuleResourceHarvesterConsumption(P)
      {
          return 0d;
      }
      double GetModuleActiveRadiatorConsumption()
      {
          if (radiator == null || !radiator.isEnabled)
              return 0d;
          for (int i = 0; i < radiator.resHandler.inputResources.Count; i++)
            if (radiator.resHandler.inputResources[i].name == "ElectricCharge")
                return radiator.resHandler.inputResources[i].rate;
          return 0d;
      }
    }
}
