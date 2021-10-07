using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP.Localization;

namespace SimpleBoiloff
{
  public class ModuleCryoTank : PartModule
  {

    // Module ID
    [KSPField(isPersistant = false)]
    public string moduleID = "CryoModule";

    /// Cost to cool off u/s per 1000 u
    [KSPField(isPersistant = false)]
    public float CoolingCost = 0.0f;

    // The current energy cost to cool the tank
    [KSPField(isPersistant = false)]
    public double currentCoolingCost = 0.0;

    // Minimum EC to leave on the vessel when consuming power
    [KSPField(isPersistant = false)]
    public float minResToLeave = 1.0f;

    // Last timestamp that boiloff occurred
    [KSPField(isPersistant = true)]
    public double LastUpdateTime = 0;

    // Whether active tank refrigeration is allowed on the part
    [KSPField(isPersistant = true)]
    public bool CoolingAllowed = true;

    // Whether active tank refrigeration is occurring on the part
    [KSPField(isPersistant = true)]
    public bool CoolingEnabled = true;

    // Whether the tank is CURRENTLY cooled or not
    [KSPField(isPersistant = true)]
    public bool BoiloffOccuring = false;

    // Special Heat-based-cooling parameters
    // Does longwave (energy emitted by planets) affect boiloff?
    [KSPField(isPersistant = false)]
    public bool LongwaveFluxAffectsBoiloff = false;

    // Does shortwave (energy emitted by solar sources) affect boiloff?
    [KSPField(isPersistant = false)]
    public bool ShortwaveFluxAffectsBoiloff = false;

    // Percent of solar energy reflected
    [KSPField(isPersistant = false)]
    public float Albedo = 0.5f;

    // The baseline solar flux (measured at Kerbin)
    [KSPField(isPersistant = false)]
    public float ShortwaveFluxBaseline = 0.7047f;

    // The baseline planetary flux (measured at Kerbin)
    [KSPField(isPersistant = false)]
    public float LongwaveFluxBaseline = 0.1231f;

    // The mimum scaling for boiloff
    [KSPField(isPersistant = false)]
    public double MinimumBoiloffScale = 0.1f;

    // The maximum scaling for boiloff
    [KSPField(isPersistant = false)]
    public double MaximumBoiloffScale = 5f;

    /// <summary>
    /// Indicates whether there are any boilable resources currently on the part
    /// </summary>
    public bool HasAnyBoiloffResource { get; private set; } = false;

    // PRIVATE
    private double finalCoolingCost = 0.0;
    private List<BoiloffFuel> fuels;

    private double fuelAmount = 0.0;
    private double maxFuelAmount = 0.0;

    private double boiloffRateSeconds = 0.0;

    // Thermal 
    private double solarFlux = 1.0;
    private double planetFlux = 1.0;
    private double fluxScale = 1.0;


    // UI FIELDS/ BUTTONS
    // Status string
    [KSPField(isPersistant = false, guiActive = true, guiName = "#LOC_CryoTanks_ModuleCryoTank_Field_BoiloffStatus")]
    public string BoiloffStatus = "N/A";

    [KSPField(isPersistant = false, guiActive = false, guiName = "#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus")]
    public string CoolingStatus = "N/A";

    [KSPEvent(guiActive = false, guiName = "#LOC_CryoTanks_ModuleCryoTank_Event_Enable", active = true)]
    public void Enable()
    {
      CoolingEnabled = true;
    }
    [KSPEvent(guiActive = false, guiName = "#LOC_CryoTanks_ModuleCryoTank_Event_Disable", active = false)]
    public void Disable()
    {
      CoolingEnabled = false;
    }

    // DEBUG FIELDS

    [KSPField(isPersistant = true)]
    public bool DebugMode = false;

    [KSPField(isPersistant = false, guiActive = false, guiName = "Albedo")]
    public string D_Albedo = "N/A";

    [KSPField(isPersistant = false, guiActive = false, guiName = "Emissivity")]
    public string D_Emiss = "N/A";

    [KSPField(isPersistant = false, guiActive = false, guiName = "SW_In(kW)")]
    public string D_InSolar = "N/A";

    [KSPField(isPersistant = false, guiActive = false, guiName = "LW_In(kW)")]
    public string D_InPlanet = "N/A";

    [KSPField(isPersistant = false, guiActive = false, guiName = "NetRad_In(kW)")]
    public string D_NetRad = "N/A";

    // ACTIONS
    [KSPAction(guiName = "#LOC_CryoTanks_ModuleCryoTank_Action_EnableAction")]
    public void EnableAction(KSPActionParam param) { Enable(); }

    [KSPAction(guiName = "#LOC_CryoTanks_ModuleCryoTank_Action_DisableAction")]
    public void DisableAction(KSPActionParam param) { Disable(); }

    [KSPAction(guiName = "#LOC_CryoTanks_ModuleCryoTank_Action_ToggleAction")]
    public void ToggleAction(KSPActionParam param)
    {
      CoolingEnabled = !CoolingEnabled;
    }

    // REWRITE ME
    public override string GetInfo()
    {
      string msg;
      string fuelDisplayName;
      if (IsConfiguredAsCoolable())
      {
        string sub = "";
        float baseCooling = CoolingCost;
        foreach (BoiloffFuel fuel in fuels)
        {
          fuelDisplayName = PartResourceLibrary.Instance.GetDefinition(fuel.fuelName).displayName;
          sub += Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoBoiloff", fuelDisplayName, (fuel.boiloffRate).ToString("F2"), (baseCooling + fuel.coolingCost).ToString("F2"));
          if (fuel.outputs.Count > 0)
          {
            foreach (ResourceRatio output in fuel.outputs)
            {
              string outputDisplayName = PartResourceLibrary.Instance.GetDefinition(output.ResourceName).displayName;
              sub += Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoBoiloffOutput", outputDisplayName, (fuel.boiloffRate * output.Ratio).ToString("F2"));
            }
          }
        }
        msg = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoCooled", sub);
      }
      else
      {
        msg = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoUncooled");
        foreach (BoiloffFuel fuel in fuels)
        {
          fuelDisplayName = PartResourceLibrary.Instance.GetDefinition(fuel.fuelName).displayName;
          msg += Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoBoiloff", fuelDisplayName, (fuel.boiloffRate).ToString("F2"));
          if (fuel.outputs.Count > 0)
          {
            foreach (ResourceRatio output in fuel.outputs)
            {
              string outputDisplayName = PartResourceLibrary.Instance.GetDefinition(output.ResourceName).displayName;
              msg += Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoBoiloffOutput", outputDisplayName, (fuel.boiloffRate * output.Ratio).ToString("F2"));
            }
          }
        }
      }
      return msg;
    }


    public void Start()
    {
      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        ReloadDatabaseNodes();
        SetDebugMode(DebugMode);
      }

      if (HighLogic.LoadedSceneIsFlight)
      {
        /// Check to see if there's any boiloff resources on this part
        HasAnyBoiloffResource = false;
        foreach (BoiloffFuel fuel in fuels)
        {
          if (BoiloffUtils.IsPartResourcePresent(fuel.fuelName, part))
          {
            HasAnyBoiloffResource = true;
            fuel.Initialize();
          }
          else
          {
            fuel.fuelPresent = false;
          }
        }
        /// if no resources turn off the UI
        if (!HasAnyBoiloffResource)
        {
          Events["Disable"].guiActive = false;
          Events["Enable"].guiActive = false;
          Fields["BoiloffStatus"].guiActive = false;
          return;
        }

        maxFuelAmount = GetTotalMaxResouceAmount();

        planetFlux = LongwaveFluxBaseline;
        solarFlux = ShortwaveFluxBaseline;

        if (GetTotalCoolingCost() > 0.0)
        {
          finalCoolingCost = maxFuelAmount / 1000.0 * GetTotalCoolingCost();
          Events["Disable"].guiActive = true;
          Events["Enable"].guiActive = true;
          Events["Enable"].guiActiveEditor = true;
          Events["Disable"].guiActiveEditor = true;
        }
        // Catchup
        DoCatchup();
      }
    }

    /// <summary>
    /// Sets the UI debug fields to active
    /// </summary>
    /// <param name="debug"></param>
    void SetDebugMode(bool debug)
    {

      Fields["D_NetRad"].guiActive = debug;
      Fields["D_InSolar"].guiActive = debug;
      Fields["D_InPlanet"].guiActive = debug;
      Fields["D_Albedo"].guiActive = debug;
      Fields["D_Emiss"].guiActive = debug;

    }
    void ReloadDatabaseNodes()
    {
      if (fuels == null || fuels.Count == 0)
      {
        Utils.Log(String.Format("Reloading ConfigNodes for {0}", part.partInfo.name));

        ConfigNode cfg;
        foreach (UrlDir.UrlConfig pNode in GameDatabase.Instance.GetConfigs("PART"))
        {
          if (pNode.name.Replace("_", ".") == part.partInfo.name)
          {
            List<ConfigNode> cryoNodes = pNode.config.GetNodes("MODULE").ToList().FindAll(n => n.GetValue("name") == moduleName);
            if (cryoNodes.Count > 1)
            {
              try
              {
                ConfigNode node = cryoNodes.Single(n => n.GetValue("moduleID") == moduleID);
                OnLoad(node);
              }
              catch (InvalidOperationException)
              {
                // Thrown if predicate is not fulfilled, ie moduleName is not unqiue
                Utils.Log(String.Format("Critical configuration error: Multiple ModuleCryoTank nodes found with identical or no moduleName"));
              }
              catch (ArgumentNullException)
              {
                // Thrown if ModuleCryoTank is not found (a Large Problem (tm))
                Utils.Log(String.Format("Critical configuration error: No ModuleCryoTank nodes found in part"));
              }
            }
            else
            {
              OnLoad(cryoNodes[0]);
            }
          }
        }
      }
    }

    /// <summary>
    /// Loads data from config node
    /// </summary>
    /// <param name="node"></param>
    public override void OnLoad(ConfigNode node)
    {
      base.OnLoad(node);
      
      ConfigNode[] varNodes = node.GetNodes("BOILOFFCONFIG");

      if (fuels == null)
        fuels = new List<BoiloffFuel>();
      if (varNodes.Length > 0 )
      {
        
        for (int i = 0; i < varNodes.Length; i++)
        {
          fuels.Add(new BoiloffFuel(varNodes[i], this.part));
        }
      }
      Utils.Log($"Now have {fuels.Count} fuels");
    }

    /// <summary>
    /// Execute a boiloff catchup operation, which looks at elapsed time to see how long we've been away and subtracts
    /// resources appropriately
    /// </summary>
    public void DoCatchup()
    {
      if (part.vessel.missionTime > 0.0)
      {
        if (BoiloffOccuring)
        {
          double elapsedTime = Planetarium.GetUniversalTime() - LastUpdateTime;
          if (elapsedTime > 0d)
          {
            Utils.Log($"Catching up {elapsedTime} s of time on load");

            for (int i = 0; i < fuels.Count; i++)
            {
              fuels[i].Boiloff(elapsedTime, 1.0);
            }
          }
        }
      }
    }

    public void Update()
    {
      if (HighLogic.LoadedSceneIsFlight && HasAnyBoiloffResource)
      {
        /// Show the insulation status field if there is a cooling cost
        if (IsCoolable())
        {
          Fields["CoolingStatus"].guiActive = true;
          if (Events["Enable"].active == CoolingEnabled || Events["Disable"].active != CoolingEnabled)
          {
            Events["Disable"].active = CoolingEnabled;
            Events["Enable"].active = !CoolingEnabled;
          }
        }
        else
        {
          Fields["CoolingStatus"].guiActive = false;
          Events["Disable"].active = false;
          Events["Enable"].active = false;
        }

        /// if there is no more fuel, hide the boiloff status
        if (fuelAmount == 0.0)
        {
          Fields["BoiloffStatus"].guiActive = false;
        }
      }
      else if (HighLogic.LoadedSceneIsEditor)
      {
        /// Check for the presence of any resource
        HasAnyBoiloffResource = false;
        foreach (BoiloffFuel fuel in fuels)
        {
          if (BoiloffUtils.IsPartResourcePresent(fuel.fuelName, part))
          {
            HasAnyBoiloffResource = true;
            fuel.Initialize();
          }
          else
          {
            fuel.fuelPresent = false;
          }
        }

        if (IsCoolable() && HasAnyBoiloffResource)
        {
          Fields["CoolingStatus"].guiActive = true;
          Fields["CoolingStatus"].guiActiveEditor = true;

          double max = GetTotalMaxResouceAmount();

          CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Editor", (GetTotalCoolingCost() * (float)(max / 1000.0)).ToString("F2"));

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
          Fields["CoolingStatus"].guiActive = false;
          Events["Disable"].guiActiveEditor = false;
          Events["Enable"].guiActiveEditor = false;
        }
      }

    }
    protected void FixedUpdate()
    {
      if (HighLogic.LoadedSceneIsFlight && HasAnyBoiloffResource)
      {

        fluxScale = CalculateRadiativeEffects();
        fuelAmount = GetTotalResouceAmount();

        // If we have no fuel, no need to do any calculations
        if (fuelAmount == 0.0)
        {
          BoiloffStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_BoiloffStatus_NoFuel");
          CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_NoFuel");
          currentCoolingCost = 0.0;
          return;
        }

        // If the tank is not coolable we must boil off
        if (!IsCoolable())
        {
          BoiloffOccuring = true;
          BoiloffStatus = BoiloffUtils.FormatRate(GetTotalBoiloffRate() * fuelAmount * fluxScale);
          currentCoolingCost = 0.0;
        }
        // else check for available power
        else
        {
          if (!CoolingEnabled)
          {
            BoiloffOccuring = true;
            BoiloffStatus = BoiloffUtils.FormatRate(GetTotalBoiloffRate() * fuelAmount * fluxScale);
            CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Disabled");
            currentCoolingCost = 0.0;
          }
          else
          {
            BoiloffOccuring = ConsumeCharge();
            currentCoolingCost = finalCoolingCost;
          }
        }

        if (BoiloffOccuring)
        {
          DoBoiloff();
        }
        if (part.vessel.missionTime > 0.0)
        {

        }
      }
      if (HighLogic.LoadedSceneIsFlight && DebugMode)
      {
        D_Albedo = String.Format("{0:F4}", Albedo);
        D_Emiss = String.Format("{0:F4}", part.emissiveConstant);
        D_InPlanet = String.Format("{0:F4}", planetFlux);
        D_InSolar = String.Format("{0:F4}", solarFlux * Albedo);
        D_NetRad = String.Format("{0:F4}", fluxScale);
      }
      if (HighLogic.LoadedSceneIsEditor && HasAnyBoiloffResource)
      {
        currentCoolingCost = GetTotalCoolingCost() * GetTotalMaxResouceAmount() / 1000d;
      }
    }

    /// <summary>
    /// Calcualates boiloff scaling by radiation
    /// </summary>
    /// <return>Scaling factor for boiloff depending on radiation</return>
    protected double CalculateRadiativeEffects()
    {
      if (part.ptd != null)
      {
        if (LongwaveFluxAffectsBoiloff)
          planetFlux = part.ptd.bodyFlux;

        if (ShortwaveFluxAffectsBoiloff)
          solarFlux = part.ptd.sunFlux / part.emissiveConstant;
      }

      if (ShortwaveFluxAffectsBoiloff || LongwaveFluxAffectsBoiloff)
      {
        fluxScale = Math.Max((planetFlux / LongwaveFluxBaseline + (solarFlux * Albedo) / ShortwaveFluxBaseline) / 2.0f, MinimumBoiloffScale);
      }
      else
      {
        fluxScale = 1.0;
      }
      return fluxScale;
    }

    /// <summary>
    /// Sets the boiloff state
    /// </summary>
    /// <param name="state">Whether boiloff is occuring or not</param>
    /// <return>The total cost of the boiloff</return>
    public double SetBoiloffState(bool isBoiling)
    {
      if (CoolingEnabled && IsCoolable())
      {
        if (isBoiling)
        {
          BoiloffOccuring = true;
          BoiloffStatus = BoiloffUtils.FormatRate(GetTotalBoiloffRate() * fuelAmount* fluxScale);
          CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Uncooled");
        }
        else
        {

          BoiloffOccuring = false;
          BoiloffStatus = String.Format("Insulated");
          CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Cooling", finalCoolingCost.ToString("F2"));

        }
        return (double)finalCoolingCost;
      }
      return 0d;
    }
    public override void OnSave(ConfigNode node)
    {
      LastUpdateTime = Planetarium.GetUniversalTime();
      base.OnSave(node);
    }

    /// <summary>
    /// Consumes electric charge and return whether boiloff happens
    /// </summary>
    /// <returns></returns>
    public bool ConsumeCharge()
    {
      bool boiloff = false; 
      if (CoolingEnabled && IsCoolable())
      {
        double chargeRequest = finalCoolingCost * TimeWarp.fixedDeltaTime;

        vessel.GetConnectedResourceTotals(PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id, out double currentEC, out double maxEC);

        // only use EC if there is more then minResToLeave left
        double consumedEC = 0;
        if (currentEC > (chargeRequest + minResToLeave))
        {
          consumedEC = part.RequestResource("ElectricCharge", chargeRequest);
        }

        double tolerance = 0.0001;
        if (consumedEC >= chargeRequest - tolerance)
        {
          boiloff = false;
          BoiloffStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_BoiloffStatus_Insulated");
          CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Cooling", finalCoolingCost.ToString("F2"));
        }
        else
        {
          boiloff = true;
          BoiloffStatus = BoiloffUtils.FormatRate(GetTotalBoiloffRate() * fuelAmount * fluxScale);
          CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Uncooled");
        }
      }
      return boiloff;
    }


    /// <summary>
    /// Iteractes through the fuels and applies boiloff
    /// </summary>
    protected void DoBoiloff()
    {
      for (int i = 0; i < fuels.Count; i++)
        fuels[i].Boiloff(TimeWarp.fixedDeltaTime, fluxScale);
    }

   

    /// <summary>
    /// Get the total fuel amount of the tank (sum of all boilable fuels)
    /// </summary>
    /// <return>Total fuel</return>
    protected double GetTotalResouceAmount()
    {
      double max = 0d;
      for (int i = 0; i < fuels.Count; i++)
        max += fuels[i].FuelAmount();
      return max;
    }

    /// <summary>
    /// Gets the total fuel maximum of the part (sum of all boilable fuels)
    /// </summary>
    /// <return>Total max fuel</return>
    protected double GetTotalMaxResouceAmount()
    {
      double max = 0d;
      for (int i = 0; i < fuels.Count; i++)
        max += fuels[i].FuelAmountMax();
      return max;
    }

    /// <summary>
    /// Returns the total amount of fuel (units) boiling off per second
    /// </summary>
    /// <return>Total fuel boiloff per second </return>
    protected double GetTotalBoiloffRate()
    {
      double max = 0d;
      for (int i = 0; i < fuels.Count; i++)
        max += fuels[i].boiloffRateSeconds;
      return max;
    }

    /// <summary>
    /// Returns the total cooling cost per 1000 units of the tank (Base + added by fuels)
    /// </summary>
    /// <return>Cooling cost, per 1000 units</return>
    protected float GetTotalCoolingCost()
    {
      float total = CoolingCost;
      for (int i = 0; i < fuels.Count; i++)
        total += fuels[i].FuelCoolingCost();
      return total;
    }

    /// <summary>
    /// Determines if the tank is currently coolable (ie, can use power to stop boiloff)
    /// </summary>
    /// <return>True if the tank can be cooled</return>
    protected bool IsCoolable()
    {
      if (!CoolingAllowed)
        return false;

      for (int i = 0; i < fuels.Count; i++)
      {
        if (fuels[i].FuelCoolingCost() > 0.0f)
          return true;
      } 
      if (CoolingCost > 0.0f)
        return true;

      return false;
    }

    /// <summary>
    /// Determines if the tank is currently coolable (ie, can use power to stop boiloff)
    /// </summary>
    /// <return>True if the tank can be cooled</return>
    protected bool IsConfiguredAsCoolable()
    {
      if (!CoolingAllowed)
        return false;

      for (int i = 0; i < fuels.Count; i++)
      {
        if (fuels[i].FuelConfiguredCoolingCost() > 0.0f)
          return true;
      }
      if (CoolingCost > 0.0f)
        return true;

      return false;
    }
  }
}
