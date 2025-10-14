# In Situ

## Description
*In Situ* is a resource collection and automation game like Factorio, Satisfactory, and Dyson Sphere program. The player controls a robot assigned to develop a new colony into a self-sufficient industrial factory. "In situ resource utilization" is a practice of using local resources to sustain and develop a remote colony or mission independently and in principle is cheaper in resources than taking everything they need with them. The player starts off with minimal critical machines to process local resources and construct other machines that are more efficient and more productive. As the game progresses, they player will be able to access a larger variety of resources and technologies and develop their colony to support colonists or become a trade hub.

## Quick Start
Make sure Python 3 is installed on the computer running this program. Then in your command line navigate to the root directory and enter "python3 main.py" into the command line followed by arguments listed under Usage.

## Usage

The game currently has a simple GUI displaying available resources and machines.

### Buttons
* "Mine Iron": Manually collect iron. Produces 5 units.
* "Build Miner": Constructs a miner that automatically collects iron. Costs 20 iron to construct. Produces 1 unit per second per machine and costs 0.5 Energy per second per machine. Production is reduced if not enough energy is available.

## Features

* Resources
    * Elements
        * Iron: Common resource for construction.
        * Copper: Common resource for electronics
        * Plutonium: Rare earth metal (unavailable)
    * Minerals
        * Stone: Common mineral from earth
        * Iron Ore: Common ore that refines to iron
        * Copper Ore: Common ore that refines to copper
    * Components
        * Iron Wire: Iron extruded into thin wire
        * Copper Wire: Copper extruded into thin wire
        * Iron Gear (Small): Small gear made of iron
        * Iron Plate: Thin plate made of iron
        * Copper Plate: Thin plate made of copper
    * Energy: Energy to run machines

* Machines
    * Miner: Automatically collects minerals. Produces 1 unit per second per machine and costs 0.5 energy per second per machine. Production is reduced if not enough energy is available.
    * Smelter Extruder: Refines ore and extrudes into wire
    * Component Printer: Crafts small components from wire and filament
    * SRG: Stirling Radioisotope Generator. Produces 1 unit of energy per second.
    * Solar Array: Produces energy from solar radiation

## Future Improvements

* More raw resources like ice
* Extract resources from atmosphere like CO2
* Advanced resources produced from other resources like gears, rods, and plates
* Machines to process resources
* Explorable regions
* Byproducts from processing resources with variable ratios
* Refinement processes to improve resource production efficiency
* Expanded GUI
* Quest-based technology system
* Introductory tutorial
