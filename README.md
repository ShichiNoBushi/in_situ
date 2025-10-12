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
    * Iron: Common resource for construction.
    * Energy: Energy to run machines

* Machines
    * Miner: Automatically collects iron. Produces 1 unit per second per machine and costs 0.5 energy per second per machine. Production is reduced if not enough energy is available.
    * SRG: Stirling Radioisotope Generator. Produces 1 unit of energy per second.

## Future Improvements

* More raw resources like stone, copper, and ice
* Advanced resources produced from other resources like gears, rods, and plates
* Machines to process resources
* Byproducts from processing resources with variable ratios
* Refinement processes to improve resource production efficiency
* Expanded GUI
* Quest-based technology system
* Introductory tutorial
