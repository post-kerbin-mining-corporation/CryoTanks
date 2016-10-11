using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;


namespace SimpleBoiloff
{
    public enum PowerProducerType {
      ModuleDeployableSolarPanel,
      ModuleGenerator,
      ModuleResourceConverter,
      ModuleCurvedSolarPanel,
      FissionGenerator,
      ModuleRadioisotopeGenerator
    }


    public class ModuleCryoPowerProducer
    {

      PowerProducerType producerType;
      // Generic reference to PartModule
      PartModule pm

      // Hard references to stock modules
      ModuleDeployableSolarPanel panel;
      ModuleGenerator gen;
      ModuleResourceConverter converter;

      public ModuleCryoPowerProducer(PowerProducerType tp, PartModule mod)
      {
        producerType = tp;
        pm = mod;
        switch (producerType)
        {
          case PowerProducerType.ModuleDeployableSolarPanel:
            panel =  (ModuleDeployableSolarPanel)pm;
            break;
          case PowerProducerType.ModuleGenerator:
            gen = (ModuleGenerator)pm;
            break;
          case PowerProducerType.ModuleResourceConverter:
            converter = (ModuleResourceConverter)pm
            break;
        }
      }

      public double GetPowerProduction()
      {
        switch (producerType)
        {
          case PowerProducerType.ModuleDeployableSolarPanel:
            return GetModuleDeployableSolarPanelProduction();
            break;
          case PowerProducerType.ModuleGenerator:
            return GetModuleGeneratorProduction();
            break;
          case PowerProducerType.ModuleResourceConverter:
            return GetModuleResourceConverterProduction()
            break;
          case PowerProducerType.ModuleCurvedSolarPanel:
            return GetModuleCurvedSolarPanelProduction()
            break;
          case PowerProducerType.FissionGenerator;
            return GetFissionGeneratorProduction();
            break;
          case PowerProducerType.ModuleRadioisotopeGenerator:
            return GetModuleRadioisotopeGeneratorProduction();
            break;
        }
      }

      double GetModuleDeployableSolarPanelProduction()
      {
        if (panel != null)
          return (double)panel.flowRate;
      }

      double GetModuleGeneratorProduction()
      {
        if (gen == null || !gen.generatorIsActive)
          return 0d;
        for (int i = 0; i < gen.resHandler.outputResources.Count; i++)
            if (gen.resHandler.outputResources[i].name == "ElectricCharge")
                return (double)gen.efficiency * gen.resHandler.outputResources[i].rate;
        return 0d;
      }

      double GetModuleResourceConverterProduction()
      {
          return 0d;
      }

      // NFT
      double GetFissionGeneratorProduction()
      {
          double results = 0d;
        double.TryParse( pm.Fields.GetValue("CurrentGeneration").ToString(), out results);
        return results;
      }
      double GetModuleRadioisotopeGeneratorProduction()
      {
          double results = 0d;
          double.TryParse(pm.Fields.GetValue("ActualPower").ToString(), out results);
          return results;
      }
      double GetModuleCurvedSolarPanelProduction()
      {
          double results = 0d;
          double.TryParse(pm.Fields.GetValue("energyFlow").ToString(), out results);
          return results;
      }

    }
}
