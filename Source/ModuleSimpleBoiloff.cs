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

    // Cost to cool off u/s per 1000 u
    [KSPField(isPersistant = false)]
    public string moduleID = "CryoModule";

    // Cost to cool off u/s per 1000 u
    [KSPField(isPersistant = false)]
    public float CoolingCost = 0.0f;

    // The current energy cost to cool the tank
    [KSPField(isPersistant = false)]
    public double currentCoolingCost = 0.0;

    // Minimum EC to leave when boiling off
    [KSPField(isPersistant = false)]
    public float minResToLeave = 1.0f;

    // Last timestamp that boiloff occurred
    [KSPField(isPersistant = true)]
    public double LastUpdateTime = 0;

    // Whether active tank refrigeration is allowed
    [KSPField(isPersistant = true)]
    public bool CoolingAllowed = true;

    // Whether active tank refrigeration is occurring
    [KSPField(isPersistant = true)]
    public bool CoolingEnabled = true;

    // Whether the tank is cooled or not
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

    public bool HasResource { get { return HasResource; } }

    // PRIVATE
    private double finalCoolingCost = 0.0;
    private List<BoiloffFuel> fuels;
    private bool hasResource = false;
    private double fuelAmount = 0.0;
    private double maxFuelAmount = 0.0;

    private double boiloffRateSeconds = 0.0;
    private double solarFlux = 1.0;
    private double planetFlux = 1.0;
    private double fluxScale = 1.0;

    // Represents a fuel that boils off
    [System.Serializable]
    public class BoiloffFuel
    {
      // Name of the fuel to boil
      public string fuelName;
      // Rate at which it boils
      public float boiloffRate;
      public float coolingCost;

      public PartResource resource;
      public List<ResourceRatio> outputs;
      public float boiloffRateSeconds = 0f;

      public bool fuelPresent = false;
      int id = -1;
      Part part;

      public BoiloffFuel(ConfigNode node, Part p)
      {
        part = p;
        node.TryGetValue("FuelName", ref fuelName);
        node.TryGetValue("BoiloffRate", ref boiloffRate);
        node.TryGetValue("CoolingCost", ref coolingCost);

        outputs = new List<ResourceRatio>();
        ConfigNode[] outNodes = node.GetNodes("OUTPUT_RESOURCE");
        for (int i = 0; i < outNodes.Length; i++)
        {
          ResourceRatio r = new ResourceRatio();
          r.Load(outNodes[i]);
          outputs.Add(r);
        }
      }

      public void Initialize()
      {
        //if (id == -1)
         id = PartResourceLibrary.Instance.GetDefinition(fuelName).id;
        resource = part.Resources.Get(id);
        boiloffRateSeconds = boiloffRate/100f/3600f;
        fuelPresent = true;
      }

      public double FuelAmountMax()
      {
        if (fuelPresent)
          return resource.maxAmount;
        return 0d;
      }

      public double FuelAmount()
      {
        if (fuelPresent)
          return resource.amount;
        return 0d;
      }

      public float FuelCoolingCost()
      {

        if (fuelPresent)
          return coolingCost;
        return 0f;
      }

      public void Boiloff(double seconds, double scale)
      {
        if (fuelPresent)
        {
          double toBoil = resource.amount * (1.0 - Math.Pow(1.0 - boiloffRateSeconds, seconds)) * scale;
          resource.amount = Math.Max(resource.amount - toBoil, 0d);

          if (outputs.Count > 0)
          {
            for (int i = 0; i < outputs.Count; i++)
            {
              part.RequestResource(outputs[i].ResourceName, -toBoil*outputs[i].Ratio, outputs[i].FlowMode);
            }
          }
        }
      }
    }

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
    [KSPAction("Enable Cooling")]
    public void EnableAction(KSPActionParam param) { Enable(); }

    [KSPAction("Disable Cooling")]
    public void DisableAction(KSPActionParam param) { Disable(); }

    [KSPAction("Toggle Cooling")]
    public void ToggleAction(KSPActionParam param)
    {
      CoolingEnabled = !CoolingEnabled;
    }

    // REWRITE ME
    public override string GetInfo()
    {
      string msg;
      string fuelDisplayName;
      if (IsCoolable())
      {
        string sub = "";
        float baseCooling = CoolingCost;
        foreach(BoiloffFuel fuel in fuels)
        {
          fuelDisplayName = PartResourceLibrary.Instance.GetDefinition(fuel.fuelName).displayName;
          sub += Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoBoiloff", fuelDisplayName, (fuel.boiloffRate).ToString("F2"), (baseCooling+fuel.coolingCost).ToString("F2"));
          if (fuel.outputs.Count > 0)
          {
            foreach (ResourceRatio output in fuel.outputs)
            {
              string outputDisplayName = PartResourceLibrary.Instance.GetDefinition(output.ResourceName).displayName;
              sub += Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoBoiloffOutput", outputDisplayName, (fuel.boiloffRate*output.Ratio).ToString("F2"));
            }
          }
        }
        msg = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoCooled", sub);
      }
      else
      {
        msg = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoUncooled");
        foreach(BoiloffFuel fuel in fuels)
        {
          fuelDisplayName = PartResourceLibrary.Instance.GetDefinition(fuel.fuelName).displayName;
          msg += Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_PartInfoBoiloff",  fuelDisplayName, (fuel.boiloffRate).ToString("F2"));
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


    public override void OnStart(StartState state)
    {
      Fields["BoiloffStatus"].guiName = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_BoiloffStatus");
      Fields["CoolingStatus"].guiName = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus");

      Events["Enable"].guiName = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Event_Enable");
      Events["Disable"].guiName = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Event_Disable");

      Actions["EnableAction"].guiName =  Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Action_EnableAction");
      Actions["DisableAction"].guiName =  Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Action_DisableAction");
      Actions["ToggleAction"].guiName =  Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Action_ToggleAction");

      if (HighLogic.LoadedSceneIsFlight || HighLogic.LoadedSceneIsEditor)
      {
        ReloadDatabaseNodes();
        if (DebugMode)
        {
          Fields["D_NetRad"].guiActive = true;
          Fields["D_InSolar"].guiActive = true;
          Fields["D_InPlanet"].guiActive = true;
          Fields["D_Albedo"].guiActive = true;
          Fields["D_Emiss"].guiActive = true;
        }
      }

      if (HighLogic.LoadedSceneIsFlight)
      {
        hasResource = false;
        foreach(BoiloffFuel fuel in fuels)
        {
          if (isResourcePresent(fuel.fuelName))
          {
            hasResource = true;
            fuel.Initialize();
          }
          else
          {
            fuel.fuelPresent = false;
          }
        }
        if (!hasResource)
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
          finalCoolingCost = maxFuelAmount/1000.0 * GetTotalCoolingCost();
          Events["Disable"].guiActive = true;
          Events["Enable"].guiActive = true;
          Events["Enable"].guiActiveEditor = true;
          Events["Disable"].guiActiveEditor = true;
        }
        // Catchup
        DoCatchup();
      }
    }

    void ReloadDatabaseNodes()
    {
      if (fuels == null || fuels.Count == 0)
      {
        Debug.Log(String.Format("[ModuleCryoTank]: Reloading ConfigNodes for {0}", part.partInfo.name));

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
              } catch (InvalidOperationException)
              {
                // Thrown if predicate is not fulfilled, ie moduleName is not unqiue
                Debug.Log(String.Format("[ModuleCryoTank]: Critical configuration error: Multiple ModuleCryoTank nodes found with identical or no moduleName"));
              } catch (ArgumentNullException)
              {
                // Thrown if ModuleCryoTank is not found (a Large Problem (tm))
                Debug.Log(String.Format("[ModuleCryoTank]: Critical configuration error: No ModuleCryoTank nodes found in part"));
              }
            } else
            {
              OnLoad(cryoNodes[0]);
            }
          }
        }
      }
    }

    public override void OnLoad(ConfigNode node)
    {
      base.OnLoad(node);

      ConfigNode[] varNodes = node.GetNodes("BOILOFFCONFIG");
      fuels = new List<BoiloffFuel>();
      for (int i=0; i < varNodes.Length; i++)
      {
        fuels.Add(new BoiloffFuel(varNodes[i], this.part));
      }
    }

    public void DoCatchup()
    {
      if (part.vessel.missionTime > 0.0)
      {
        double currentEC = 0d;
        double maxAmount = 0d;
        vessel.GetConnectedResourceTotals(PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id, out currentEC, out maxAmount);
        // no consumption here anymore, since we know, that there won't be enough EC
        if((currentEC - minResToLeave) < (finalCoolingCost * TimeWarp.fixedDeltaTime))
        {
          double elapsedTime = part.vessel.missionTime - LastUpdateTime;
          for (int i = 0; i < fuels.Count ; i++)
            fuels[i].Boiloff(elapsedTime, 1.0);
        }
      }
    }

    public void Update()
    {
      if (HighLogic.LoadedSceneIsFlight && hasResource)
      {
        // Show the insulation status field if there is a cooling cost
        if (IsCoolable())
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
      else if (HighLogic.LoadedSceneIsEditor)
      {
        hasResource = false;
        foreach(BoiloffFuel fuel in fuels)
        {
          if (isResourcePresent(fuel.fuelName))
          {
            hasResource = true;
            fuel.Initialize();
          }
          else
          {
            fuel.fuelPresent = false;
          }
        }

        if (IsCoolable() && hasResource)
        {
          Fields["CoolingStatus"].guiActive = true;
          Fields["CoolingStatus"].guiActiveEditor = true;

          double max = GetTotalMaxResouceAmount();

          CoolingStatus =  Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Editor", (GetTotalCoolingCost() * (float)(max / 1000.0)).ToString("F2"));

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
      if (HighLogic.LoadedSceneIsFlight && hasResource)
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

        // If the cooling cost is zero, we must boil off
        if (!IsCoolable())
        {
          BoiloffOccuring = true;
          BoiloffStatus = FormatRate(GetTotalBoiloffRate() * fuelAmount * fluxScale);
          currentCoolingCost = 0.0;
        }
        // else check for available power
        else
        {
          if (!CoolingEnabled)
          {
            BoiloffOccuring = true;
            BoiloffStatus = FormatRate(GetTotalBoiloffRate() * fuelAmount * fluxScale);
            CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Disabled");
            currentCoolingCost = 0.0;
          }
          else
          {
            ConsumeCharge();
            currentCoolingCost = finalCoolingCost;
          }
        }

        if (BoiloffOccuring)
        {
            DoBoiloff();
        }
        if (part.vessel.missionTime > 0.0)
        {
            LastUpdateTime = part.vessel.missionTime;
        }
      }
      if (HighLogic.LoadedSceneIsFlight && DebugMode)
      {
        D_Albedo = String.Format("{0:F4}", Albedo);
        D_Emiss = String.Format("{0:F4}", part.emissiveConstant);
        D_InPlanet = String.Format("{0:F4}", planetFlux);
        D_InSolar = String.Format("{0:F4}", solarFlux*Albedo);
        D_NetRad = String.Format("{0:F4}", fluxScale);
      }
      if (HighLogic.LoadedSceneIsEditor && hasResource)
      {
        currentCoolingCost = GetTotalCoolingCost()*GetTotalMaxResouceAmount()/1000d;
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
    public double SetBoiloffState(bool state)
    {
      if (CoolingEnabled && IsCoolable())
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
          CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Cooling", finalCoolingCost.ToString("F2"));

        }
        return (double)finalCoolingCost;
      }
      return 0d;
    }

    public void ConsumeCharge()
    {
      if (CoolingEnabled && IsCoolable())
      {
        double chargeRequest = finalCoolingCost * TimeWarp.fixedDeltaTime;

        double currentEC = 0d;
        double maxEC = 0d;
        vessel.GetConnectedResourceTotals(PartResourceLibrary.Instance.GetDefinition("ElectricCharge").id, out currentEC, out maxEC);

        // only use EC if there is more then minResToLeave left
        double req = 0;
        if (currentEC > chargeRequest + minResToLeave)
        {
          req = part.RequestResource("ElectricCharge", chargeRequest);
        }

        double tolerance = 0.0001;
        if (req >= chargeRequest - tolerance)
        {
          BoiloffOccuring = false;
          BoiloffStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_BoiloffStatus_Insulated");
          CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Cooling", finalCoolingCost.ToString("F2"));
        }
        else
        {
          BoiloffOccuring = true;
          BoiloffStatus = FormatRate(boiloffRateSeconds * fuelAmount);
          CoolingStatus = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_CoolingStatus_Uncooled");
        }
      }
    }


    protected void DoBoiloff()
    {
      // Iterate through fuels and do boiloff
      for (int i = 0; i < fuels.Count ; i++)
        fuels[i].Boiloff(TimeWarp.fixedDeltaTime, fluxScale);
    }

    /// <summary>
    /// Produces a friendly string describing boiloff rate
    /// </summary>
    /// <param name="rate">The rate in seconds to format</param>
    /// <return>String describing rate</return>
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

    /// <summary>
    /// Determines if a specified resource is present on the part
    /// </summary>
    /// <param name="string">The name of the resource</param>
    /// <return>True if the resouce is present</return>
    public bool isResourcePresent(string nm)
    {
      int id = PartResourceLibrary.Instance.GetDefinition(nm).id;
      PartResource res = this.part.Resources.Get(id);
      if (res == null)
        return false;
      return true;
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
      for (int i = 0; i < fuels.Count ; i++)
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
      for (int i = 0; i < fuels.Count ; i++)
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

      for (int i = 0; i < fuels.Count ; i++)
        if (fuels[i].FuelCoolingCost() > 0.0f)
          return true;

      if (CoolingCost > 0.0f)
        return true;
      return false;
    }

    /// <summary>
    /// Gets the current amount of a resource by name
    /// </summary>
    /// <param name="string">The name of the resource</param>
    /// <return>The amount</return>
    protected double GetResourceAmount(string nm)
    {
      PartResource res = this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(nm).id);
      return res.amount;
    }

    /// <summary>
    /// Gets the maximum amount of a resource by name
    /// </summary>
    /// <param name="string">The name of the resource</param>
    /// <return>The maximum amount</return>
    protected double GetMaxResourceAmount(string nm)
    {
      PartResource res = this.part.Resources.Get(PartResourceLibrary.Instance.GetDefinition(nm).id);
      return res.maxAmount;
    }

  }
}
