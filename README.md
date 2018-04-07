# CryoTanks

Support mod pack for Kerbal Atomics and CryoEngines, dealing with cryogenic fuels

## Features

### Fuel Switching

This mod contains patches that enables fuel switching on most parts, stock and modded, that contain LiquidFuel/Oxidizer. The parts will be able to contain the following fuel settings with correct dry mass
* LiquidFuel/Oxidizer
* LqdHydrogen/Oxidizer
* LiquidFuel
* Oxidizer
* LqdHydrogen

### Hydrogen Boiloff

The mod contains a plugin that causes LiquidHydrogen to evaporate from improperly cooled tanks. The boiloff rate is quite low so there is only a need to deal with it if you are storing fuel on orbit or doing long interplanetary transfers.

### Disabling Boiloff

Either remove the `SimpleBoiloff.dll` file from `GameData/CryoTanks/Plugins/` or remove the following code block from `GameData/CryoTanks/Patches/CryoTanksFuelSwitcher.cfg`:
```
MODULE
{
  name =  ModuleCryoTank
  FuelName = LqdHydrogen
  // in % per hr
  BoiloffRate = 0.05
}
```

### Orbital Zero-boiloff (ZBO) Fuel Tanks

This mod adds several ZBO tanks that use electricity to halt boiloff. These are provided in 1.25m to 5m size classes as well as several radial mount models.

## Changelog

### 0.5.0
* KSP 1.4.2
* Updated MiniAVC to 1.2.0.1
* Redid all tank textures
* 3 new tank models
 * Short 3.75m
 * Short 2.5m
 * Short 5m
* Fixed attach nodes of 3.75m compact tank variants
* Rebalanced capacities, masses and costs of all tanks
* Decreased cooling cost of ZBO tanks to 0.5 Ec/1000u

### 0.4.9
* Deconflicted a WBI fuel switcher

### 0.4.8
* Cryo Tanks no longer suck up all EC

### 0.4.7
* Fixed an issue where parts with '_' in their name would not be patched properly

### 0.4.6
* All tanks can now be cooled. Lifting tanks cast ~10% more to cool and have cooling disabled by default
* Repaired normals on tanks
* Fixed science costs of many tanks
* Refactored plugin to support multiple cryogenic fuels per tank
* Updates to MFT Compatibility

### 0.4.5
* Added Russian translation from vladmir_v
* Fixed a typo in the version file
* Adjusted the position of the nodes for the compact variants of 3.75m tanks
* Fixed medium 3.75m tank's Compact variant collider being correctly specified
* Fixed low cost of 5m hydrogen tanks
* Fixed slightly off cost of 10m hydrogen tank

### Previous
* Not tracked in this readme
