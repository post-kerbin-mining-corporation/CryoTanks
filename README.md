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

** Disabling Boiloff**

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

### 0.4.5
* Added Russian translation from vladmir_v
* Fixed a typo in the version file
* Adjusted the position of the nodes for the compact variants of 3.75m tanks
* Fixed low cost of 5m hydrogen tanks
* Fixed slightly off cost of 10m hydrogen tank

### Previous
* Not tracked in this readme
