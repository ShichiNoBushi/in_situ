# In Situ

## Description
*In Situ* is a resource collection and automation game like Factorio, Satisfactory, and Dyson Sphere program. The player controls a robot assigned to develop a new colony into a self-sufficient industrial factory. "In situ resource utilization" is a practice of using local resources to sustain and develop a remote colony or mission independently and in principle is cheaper in resources than taking everything they need with them. The player starts off with minimal critical machines to process local resources and construct other machines that are more efficient and more productive. As the game progresses, they player will be able to access a larger variety of resources and technologies and develop their colony to support colonists or become a trade hub.

## Quick Start
Open project in Godot and run. (Exported executables for Mac and Windows to come.)

## Usage

The game currently has a simple GUI displaying available resources and machines.

### Interface
* "Base Tab"
	* Region Map: displays explored regions with (x, y) coordinates.
		* +y is North, -y is South, -x is West, +x is East
		* Blue region is the currently selected region according to the Travel panel.
		* Red region is the player's current location (overridden by Blue).
		* Dark Gray regions are unexplored (???).
		* Light Gray regions are default color.
	
	* Travel Panel: Displays information on regions, explores new regions, and travels between them.
		* "Current Region": The coordinates of the player's current location.
		* Region option menu: Select a region from a list of explored regions.
		* "Travel" button: Travel to the selected region if it is adjacent to the current location (x or y differenc of 1).
		* Direction option menu: Select a cardinal direction to explore an adjacent unexplored region.
		* "Explore" button: Explore an adjacent region in the selected direction.
		* "Features" label: Displays information for the selected region including topology and available resource deposits.
		
	* Active Quest Panel: Display information on the currently tracked quest including requirements.
	
	* Harvest Panel: collect local resources.
		* Resource option menu: select resource to collect.
		* "Harvest" button: begin harvesting selected resource.
		* Harvest progress bar: shows time until completing harvest action and collecting resource.
	
	* Build Panel: construct machines and infrastructure.
		* Machine option menu: select a machine from list of unlocked machines.
		* "Build" button: build the selected machine consuming resources based on machine's cost.
		* "Cost" label: display list of resources necessary to build machine including amounts available and needed.
		
	* Machine Panels: each panel is an interface for a constructed machine.
		* "Active" switch: turns machine on/off.
		* "Production" tab: controls processing of recipes in machine.
			* Recipe option menu: select recipe usable in machine that has been unlocked.
			* "Input"/"Output" label: display resources needed for the recipe with amounts available and required and amount of resources produced.
		* "Maintenance" tab: repairing the machine.
			* Wear progress bar: shows percentage of accumulated wear out of max before machine is nonfunctional.
			* "Required Materials" label: displays resources diagnosed necessary for repairs.
			* "Diagnostics" button: creates list of random resources required to repair current wear. (nonfunctional)
			* "Repair" button: uses necessary resources to repair as much as can be afforded. (nonfunctional)

* "Quest Tab": displays information for quests to unlock machines and recipes.
	* Active quests list: list of currently active quests.
	* Completed quests list: list of completed quests.
	* Quest information label: displays information on a quest selected from active or completed lists including narrative and quest requirements.
	* "Track Quest" button: sets currently selected active quest to be tracked on Base Tab.

* "Options Tab": miscelaneous options
	* "Quit" button: exits game after confirmation.
	* Cheat/Dev command entry box: type a code for a cheat or dev command.
	* "Enter" button: executes command (Enter/Return key also functions).
	* Feedback label: displays feedback for results of entering command.

## Features

* Resources
	* Elements
		* Hydrogen: H2
		* Oxygen: O2
		* Iron: Common resource for construction.
		* Copper: Common resource for electronics
		* Plutonium: Rare earth metal (unavailable)
	* Compounds
		* Gravel: Stone aggregate
		* Stone Dust: Pulverized stone
		* Sand: Pulverized quartz
		* Water: Liquid water (H20)
		* Ice: Frozen water
		* Clay Slurry: Mixture of clay and water
		* Carbon Dioxide: CO2
		* Methane: CH4
	* Minerals
		* Stone: Common mineral from earth
		* Iron Ore: Common ore that refines to iron
		* Copper Ore: Common ore that refines to copper
	* Components
		* Iron Wire: Iron extruded into thin wire
		* Copper Wire: Copper extruded into thin wire
		* Glass Filament: Glass in thin threads
		* Stone Brick: Uniform block of stone
		* Iron Gear (Small): Small gear made of iron
		* Iron Plate: Thin plate made of iron
		* Iron Rod: Long round rod of iron
		* Iron Blade: Sharpened sheet of iron
		* Copper Plate: Thin plate made of copper
		* Glass Pane: Flat sheet of glass
		* Magnet: Magnetized rod of iron
		* Electric Motor: Motor that converts electricity to mechanical energy or vice versa
		* Induction Coil: Produces heat from electricity
	* Scrap
		* Iron Scrap: Damaged iron
		* Copper Scrap: Damaged copper
		* Plutonium Scrap: Damaged Plutonium
		* Glass Shard: Broken glass
	* Energy: Energy to run machines

* Machines
	* Miner: Automatically collects minerals. Produces 1 unit per second per machine and costs 0.5 energy per second per machine. Production is reduced if not enough energy is available.
	* Atmospheric Compressor: Collects gases from the atmosphere
	* Smelter Extruder: Refines ore and extrudes into wire
	* Component Printer: Crafts small components from wire and filament
	* Parts Assembler: Crafts moving parts with fine precision
	* SRG: Stirling Radioisotope Generator. Produces 1 unit of energy per second.
	* Magnetizer: Magnetizes iron rods into permanent magnets
	* Crusher: Grinds ores and stone into finer aggregates
	* Mixer: Combines fluids and aggregates
	* Stone Carver: Shapes stone into components
	* Induction Smelter: Smelts raw ore into refined metal
	* Roller Press: Crafts metal into flat sheets
	* Component Mold: Crafts solid components from metal
	* Extruder: Crafts metal into wire or other thin or round components
	* Electrolyzer: Seperates compounds via electrolysis
	* Chemical Reactor: Converts chemicals into other chemicals
	* Wind Turbine: Uses wind energy to produce electricity
	* Solar Array: Produces energy from solar radiation

* Quest System: Quests provided by assistant AI instructs the player in processing resources and constructing machines and progressively unlocks recipes and machines to advance technology
* Wear: Machines accumulate wear as they operate
	* Diagnostics identifies resources needed to repair machine based on cost to build machine
	* Repair reduces wear and consumes required resources
	* Repaired components are converted into scrap based on component materials which can be recycled
	* Machines no longer function when they reach 100% wear
* Regions: locations with individual geography, resource stockpiles, and constructed machines
* Cheat/Dev Commands: enter codes to grant resources or unlock features
	* give resource [name] [amount]: gives amount of named resource
	* unlock recipe [name]: unlockes named recipe for all machines
	* unlock machine [name]: unlocks named machine to build
	* unlock quest [name]: sets quest as active if not already active or completed
	* complete quest [name]: completes quest unless already completed

## Future Improvements

* More raw resources like other ores
* Advanced resources produced from other resources like gears, rods, and plates
* Transporting resources between regions (manually and automatically)
* Byproducts from processing resources with variable ratios
* Refinement processes to improve resource production efficiency
* Wear system for player's android body as a form of health and Game Over condition
* Expanded GUI
