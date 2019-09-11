# Cryogenic Tanks

A mod pack for Kerbal Space Program, specifically supporting my other mods [Kerbal Atomics](https://github.com/ChrisAdderley/KerbalAtomics) and [Cryogenic Engines](https://github.com/ChrisAdderley/CryoEngines), dealing with cryogenic fuels, their storage and their properties

* [Features](#features)
* [Config Documentation](#config-documentation)
* [Contributing](#contributing)
* [Changelog](#changelog)

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

### Orbital Zero-boiloff (ZBO) Fuel Tanks

This mod adds several ZBO tanks that use electricity to halt boiloff. These are provided in 1.25m to 5m size classes as well as several radial mount models. They have the same mass properties, but take less power to cool

### Disabling Boiloff

Either remove the `SimpleBoiloff.dll` file from `GameData/CryoTanks/Plugins/` or remove the following code block from `GameData/CryoTanks/Patches/CryoTanksFuelSwitcher.cfg`:
```
MODULE
{
  name =  ModuleCryoTank
  ... stuff
}
```
### Methalox Fuel Type

A hidden feature of this mod is support for Methane/Oxidizer fuels. This adds LqdMethane and LqdMethane/Oxidizer fuel tanks to all the tanks affected by the mod. LqdMethane is less cryogenic and boils off slower, and additionally takes up less volume. Additionally, ISRU options are added to the appropriate parts.

This feature is activated by declaring a ModuleManager patch with `:FOR[CryoTanksMethalox]` anywhere in your installation, or creating such a folder in your GameData directory. Currently, the only mod that uses this is the `NearFutureLaunchVehiclesMethalox` optional patch.

## Config Documentation

### Basic
Adding boiloff support to a fuel or fuel tank is simple. Specify the following MODULE block:
```
MODULE
{
  name =  ModuleCryoTank

  // Should be unique among all ModuleCryoTank instances
  moduleID = basicBoiloff

  // Base power cost of cooling, in EC per 1000 units per second (a 1000 unit tank will take 80 EC/s here)
  CoolingCost = 0.08

  // Whether the tank starts with cooling enabled or not
  CoolingEnabled = True

  BOILOFFCONFIG
  {
    // The fuel name to boil off
    FuelName = LqdHydrogen
    // The rate of boiloff, in % per game hour
    BoiloffRate = 0.05
    // The fuel cooling rate in EC per 1000 units per second. This is optional and additive to the rate in the base module
    CoolingCost = 0.08
  }
}
```
Any number of `BOILOFFCONFIG`s can be used to boil off multiple fuels. Note that the CoolingCost can appear in multiple locations. This is an additive property - all instances of it will be added together.
In the following example, the total cost of cooling the tank will be `0.11 EC/s/1000 u` - `0.01` from the base cost, `0.05` from the `LqdHydrogen` block, and `0.02` from the `LqdOxygen` block. This allows you to have multiple fuel types with different cooling costs in a single part. Note that any of the blocks can be omitted - no need to have a base `CoolingCost` or specify any of the individual fuels.
```
MODULE
{
  name =  ModuleCryoTank

  // Must be unique among all ModuleCryoTank instances, if you have more than one
  moduleID = basicBoiloff

  // Base power cost of cooling, in EC per 1000 units per second (a 1000 unit tank will take 80 EC/s here)
  CoolingCost = 0.01

  // Whether the tank starts with cooling enabled or not
  CoolingEnabled = True

  BOILOFFCONFIG
  {
    // The fuel name to boil off
    FuelName = LqdHydrogen
    // The rate of boiloff, in % per game hour
    BoiloffRate = 0.05
    // The fuel cooling rate in EC per 1000 units per second. This is optional and additive to the rate in the base module
    CoolingCost = 0.08
  }
  BOILOFFCONFIG
  {
    // The fuel name to boil off
    FuelName = LqdOxygen
    // The rate of boiloff, in % per game hour
    BoiloffRate = 0.05
    // The fuel cooling rate in EC per 1000 units per second. This is optional and additive to the rate in the base module
    CoolingCost = 0.02
  }
}
```
It should be noted that while costs can be specified per-fuel, cooling is an all or nothing thing - you cannot enable and disable cooling separately for different fuels in a single part.

### Resource Generation

It is possible to set things so that boiloff creates another resource instead of venting into the ether. Do do this, specify an `OUTPUT_RESOURCE` in the `BOILOFFCONFIG`. You can set ratio, flow mode and resource name. With this option set, an amount of `ResourceName` will be produced according to the `Ratio` field, with the specified `FlowMode`.
```
BOILOFFCONFIG
{
  FuelName = LqdHydrogen
  // in % per hr
  BoiloffRate = 0.05
  OUTPUT_RESOURCE
  {
    ResourceName = Hydrogen
    Ratio = 0.5
    FlowMode = ALL_VESSEL
  }
}
```
In the above case, 0.05% per hour of `LqdHydrogen` will boil off, adding half the amount lost of `Hydrogen` (note the `Ratio = 0.5`).

### Heating Effects

It is also possible to configure such that there is a boiloff dependence on energy input from planets and the sun. This does not affect cooling cost, but allows more interesting boiloff mitigation strategies, like hiding behind planets and being in the outer solar system.

```
MODULE
{
  name =  ModuleCryoTank
  // in Ec per 1000 units per second
  CoolingCost = 0.08
  CoolingEnabled = True

  Albedo = 0.5
  LongwaveFluxAffectsBoiloff = True
  LongwaveFluxBaseline = 0.5
  ShortwaveFluxAffectsBoiloff = True
  ShortwaveFluxBaseline = 0.5

  MaximumBoiloffScale = 5
  MinimumBoiloffScale = 0.001
  BOILOFFCONFIG
  {
    FuelName = LqdHydrogen
    // in % per hr
    BoiloffRate = 0.05
  }
}
```
Setting `LongwaveFluxAffectsBoiloff` will cause emission from planets to affect boiloff. This depends on the part's `emissiveConstant`, so ensure it is configured correctly. High `emissiveConstant` will increase boiloff. Modifying `LongwaveFluxBaseline` allows the scale of this to be changed - it defaults to a value of 0.1231, which is the flux received by an object in Low Kerbin Orbit with an `emissiveConstant` of 0.2. If the baseline is unchanged, the boiloff in LKO should be approximately the same as if `LongwaveFluxAffectsBoiloff` was disabled.

Setting `ShortwaveFluxAffectsBoiloff` will cause emission from the sun to affect boiloff. This depends on the `Albedo` field, so ensure it is configured correctly. High `Albedo` will decrease boiloff. Modifying `ShortwaveFluxBaseline` allows the scale of this to be changed - it defaults to a value of 0.7047, which is the flux received by an object in Low Kerbin Orbit with an `Albedo` of 0.5. If the baseline is unchanged, the boiloff in LKO should be approximately the same as if `ShortwaveFluxAffectsBoiloff` was disabled.

The `DebugMode` flag can also be set in order to observe solar input and planetary input as UI fields ingame.

Finally, the `MaximumBoiloffScale` and `MinimumBoiloffScale` fields can be configured to cap the modifier to boiloff from these modifications.


## Contributing

I certainly accept pull requests. Please target all such thing to the `dev` branch though!

## Translations

For translation instructions please see [Localization Instructions](https://github.com/ChrisAdderley/CryoTanks/blob/master/GameData/CryoTanks/Localization/Localization.md)

* **Spanish:** KSP forum user fitiales
* **Russian:** KSP forum user vladmir_v, Sooll3
* **German:** KSP forum user Three_Pounds
* **French:** KSP forum user Aodh4n
* **Chinese:** Github user 6DYZBX
