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
## Contributing

I certainly accept pull requests. Please target all such thing to the `dev` branch though!

## Translations

For translation instructions please see [Localization Instructions](https://github.com/ChrisAdderley/CryoTanks/blob/master/GameData/CryoTanks/Localization/Localization.md)

* **Spanish:** KSP forum user fitiales
* **Russian:** KSP forum user vladmir_v, Sooll3
* **German:** KSP forum user Three_Pounds
* **French:** KSP forum user Aodh4n

## Config Documentation

### Basic
Adding boiloff support to a fuel or fuel tank is simple. Specify the following MODULE block:
```
MODULE
{
  name =  ModuleCryoTank
  // in Ec per 1000 units per second
  CoolingCost = 0.08
  CoolingEnabled = True
  BOILOFFCONFIG
  {
    FuelName = LqdHydrogen
    // in % per hr
    BoiloffRate = 0.05
  }
}
```
Any number of BOILOFFCONFIGs can be used to boil off multiple fuels, though the CoolingCost is shared per tank.

### Resource Generation

It is possible to set things so that boiloff creates another resource instead of venting into the ether. Do do this, specify an `OUTPUT_RESOURCE` in the `BOILOFFCONFIG`. You can set ratio, flow mode and resource name. With this option set, an amount of `ResourceName` will be produced according to the `Ratio` field, with the specified `FlowMode`.
```
BOILOFFCONFIG
{
  FuelName = Hydrogen
  // in % per hr
  BoiloffRate = 0.05
  OUTPUT_RESOURCE
  {
    ResourceName = Hydrogen
    Ratio = 1.0
    FlowMode = ALL_VESSEL
  }
}
```

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
