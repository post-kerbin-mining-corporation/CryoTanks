using System;
using UnityEngine;
using KSP.Localization;

namespace SimpleBoiloff
{
  public enum LogType
  {
    UI,
    Settings,
    Modules,
    Any
  }
  public static class Utils
  {
    public static string logTag = "CryoTanks";


    /// <summary>
    /// Log a message with the mod name tag prefixed
    /// </summary>
    /// <param name="str">message string </param>
    public static void Log(string str, LogType logType)
    {
      bool doLog = false;
      if (logType == LogType.Any) doLog = true;

      if (doLog)
        Debug.Log(String.Format("[{0}]{1}", logTag, str));
    }

    public static void Log(string str)
    {
      Debug.Log(String.Format("[{0}]{1}", logTag, str));
    }

    public static void LogWarning(string toLog)
    {
      Debug.LogWarning(String.Format("[{0}]{1}", logTag, toLog));
    }
    public static void LogError(string toLog)
    {
      Debug.LogError(String.Format("[{0}]{1}", logTag, toLog));
    }
  }

  public static class BoiloffUtils
  {
    /// <summary>
    /// Determines if a resource is present on a part
    /// </summary>
    /// <param name="resourceName"></param>
    /// <param name="p"></param>
    /// <returns></returns>
    public static bool IsPartResourcePresent(string resourceName, Part p)
    {
      int id = PartResourceLibrary.Instance.GetDefinition(resourceName).id;
      PartResource res = p.Resources.Get(id);
      if (res == null)
        return false;
      return true;
    }

    /// <summary>
    /// Gets the current amount of a resource by name on a part
    /// </summary>
    /// <param name="string">The name of the resource</param>
    /// <return>The amount</return>
    public static double GetResourceAmount(string nm, Part p)
    {
      PartResource res = p.Resources.Get(PartResourceLibrary.Instance.GetDefinition(nm).id);
      return res.amount;
    }

    /// <summary>
    /// Gets the maximum amount of a resource by name on a part
    /// </summary>
    /// <param name="string">The name of the resource</param>
    /// <return>The maximum amount</return>
    public static double GetMaxResourceAmount(string nm, Part p)
    {
      PartResource res = p.Resources.Get(PartResourceLibrary.Instance.GetDefinition(nm).id);
      return res.maxAmount;
    }

    /// <summary>
    /// Produces a friendly string describing boiloff rate
    /// </summary>
    /// <param name="rate">The rate in seconds to format</param>
    /// <return>String describing rate</return>
    public static string FormatRate(double rate)
    {
      double adjRate = rate;
      string interval = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_TimeInterval_Second_Abbrev");
      if (adjRate < 0.01)
      {
        adjRate = adjRate * 60.0;
        interval = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_TimeInterval_Minute_Abbrev");
      }
      if (adjRate < 0.01)
      {
        adjRate = adjRate * 60.0;
        interval = Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_TimeInterval_Hour_Abbrev");
      }
      return Localizer.Format("#LOC_CryoTanks_ModuleCryoTank_Field_BoiloffStatus_Boiloff", adjRate.ToString("F2"), interval.ToString());
    }
  }
}
