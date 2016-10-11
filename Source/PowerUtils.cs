using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SimpleBoiloff
{
    public static class PowerUtils
    {
        // Power Consumption evaluators, return power in double rate/s
        //STOCK
        public static double GetModuleGeneratorConsumption(PartModule pm)
        {
            ModuleGenerator gen = (ModuleGenerator)pm;
            if (gen == null || !gen.generatorIsActive)
                return 0d;
            for (int i = 0; i < gen.resHandler.inputResources.Count; i++)
                if (gen.resHandler.inputResources[i].name == "ElectricCharge")
                    return (double)gen.efficiency * gen.resHandler.inputResources[i].rate;

            return 0d;
        }
        public static double GetModuleResourceConverterConsumption(PartModule pm)
        {
            return 0d;
        }
        public static double GetModuleResourceHarvesterConsumption(PartModule pm)
        {
            return 0d;
        }
        public static double GetModuleActiveRadiatorConsumption(PartModule pm)
        {
            ModuleActiveRadiator rad = (ModuleActiveRadiator)pm;
            if (rad == null || !rad.isEnabled)
                return 0d;
            for (int i = 0; i < rad.resHandler.inputResources.Count; i++)
              if (rad.resHandler.inputResources[i].name == "ElectricCharge")
                  return rad.resHandler.inputResources[i].rate;
            return 0d;
        }

        // Power generation evaluators, return power in double rate/s
        // STOCK
        public static double GetModuleGeneratorProduction(PartModule pm)
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
        public static double GetModuleDeployableSolarPanelProduction(PartModule pm)
        {
          double results = 0d;
          double.TryParse( pm.Fields.GetValue("flowRate").ToString(), out results);
            return results;
        }
        // TODO: implement me!
        public static double GetModuleResourceConverterProduction(PartModule pm)
        {
          return 0d;
        }
        // NFT
        public static double GetFissionGeneratorProduction(PartModule pm)
        {
            double results = 0d;
          double.TryParse( pm.Fields.GetValue("CurrentGeneration").ToString(), out results);
          return results;
        }
        public static double GetModuleRadioisotopeGeneratorProduction(PartModule pm)
        {
            double results = 0d;
            double.TryParse(pm.Fields.GetValue("ActualPower").ToString(), out results);
            return results;
        }
        public static double GetModuleCurvedSolarPanelProduction(PartModule pm)
        {
            double results = 0d;
            double.TryParse(pm.Fields.GetValue("energyFlow").ToString(), out results);
            return results;
        }
    }
}
