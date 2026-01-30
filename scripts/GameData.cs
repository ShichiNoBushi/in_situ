using Godot;
using System;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;

//The main control of the game containing the persistent data.
public partial class GameData : Node
{
	//RNG for random effects.
	public static RandomNumberGenerator rng = new();
	
	//Data references for various imported databases.
	public static Dictionary<string, ResourceData> RESOURCES = new();
	public static Dictionary<string, HarvestData> HARVEST = new();
	public static Dictionary<string, MachineData> MACHINES = new();
	public static Dictionary<string, InfrastructureData> INFRASTRUCTURE = new();
	public static Dictionary<string, RecipeData> RECIPES = new();
	public static Dictionary<string, RegionData> REGIONS = new();
	public static Dictionary<string, QuestData> QUESTS = new();
	
	//Dictionaries to reference user facing strings to IDs in the above dictionaries.
	public static Dictionary<string, string> resNameToKey = new();
	public static Dictionary<string, string> harvActionToKey = new();
	public static Dictionary<string, string> machNameToKey = new();
	public static Dictionary<string, string> recNameToKey = new();
	public static Dictionary<string, string> regNameToKey = new();
	public static Dictionary<string, string> qstNameToKey = new();
	public static Dictionary<string, (int x, int y)> coordStringToTuple = new();
	
	//Android representing player character
	public static Android android;
	
	//Region map referenced by keys of 2D tuples as coordinates.
	public static Dictionary<(int x, int y), Region> regionMap = new();
	
	//The region the player is currently in.
	public static Region currentRegion;
	
	//The currently tracked quest.
	public static QuestData trackedQuest;
	
	//Quick references to interfaces within the game.
	public static MapControl mapControl;
	public static TravelControl travelControl;
	public static ResourceControl resourceControl;
	public static RichTextLabel maxPhaseLabel;
	public static MachinesControl machinesControl;
	public static HarvestControl harvestControl;
	public static BuildControl buildControl;
	public static LogisticsControl logisticsControl;
	public static AndroidControl androidControl;
	public static QuestControl questControl;
	
	//Label to display the current tracked objective.
	public static Label objectiveLabel;
	
	//Button to quit the game.
	public static Button quitButton;
	public static ConfirmationDialog quitConfirm;
	
	public static CheckBox unlockMachinesCheck;
	public static CheckBox unlockRecipesCheck;
	public static CheckBox disableWearCheck;
	public static CheckBox disableStorageCheck;
	
	//A variable to test if a feature is functioning (possibly no longer necessary).
	private static bool questUpdateFunctioning;
	
	//Variables set in development for test purposes.
	public static bool unlockAllMachines;
	public static bool unlockAllRecipes;
	public static bool disableWear;
	public static bool disableStorage;
	
	public override void _Ready()
	{
		GD.Print("GameData._Ready() called from ", GetPath());
		
		//Initialize the RNG.
		rng = new();
		rng.Randomize();
		
		//Load all the JSON data.
		LoadAll();
		GD.Print("Game data loaded automatically.");
		
		//Create quick references between names and IDs.
		BuildNameMaps();
		
		//Create the PC android.
		android = new();
		
		//Generate the map.
		regionMap = new();
		
		try
		{
			GenerateStartingRegion();
		}
		catch (Exception e)
		{
			GD.PrintErr($"GameData: Error generating starting region - {e.Message}");
		}
		//Create starting resources and machines according to JSON labels.
		GiveStartingResources();
		GiveStartingMachines();
		
		//Assign the player's initial location as the center of the map.
		currentRegion = regionMap[(0, 0)];
		
		coordStringToTuple[CoordToString((0, 0))] = (0, 0);
		
		//Assign control variables.
		mapControl = GetNode<MapControl>("../TabContainer/Base/MapControl");
		travelControl = GetNode<TravelControl>("../TabContainer/Base/TravelPanel");
		resourceControl = GetNode<ResourceControl>("../TabContainer/Base/ResourceScroll/VBoxContainer");
		maxPhaseLabel = GetNode<RichTextLabel>("../TabContainer/Base/StoragePanel/MaxPhaseLabel");
		machinesControl = GetNode<MachinesControl>("../TabContainer/Base/MachinesTab");
		harvestControl = GetNode<HarvestControl>("../TabContainer/Base/HarvestPanel");
		buildControl = GetNode<BuildControl>("../TabContainer/Base/BuildPanel");
		logisticsControl = GetNode<LogisticsControl>("../TabContainer/Logistics");
		androidControl = GetNode<AndroidControl>("../TabContainer/Android");
		questControl = GetNode<QuestControl>("../TabContainer/Quests");
		
		objectiveLabel = GetNode<Label>("../TabContainer/Base/QuestPanel/ObjectiveScroll/ObjectiveLabel");
		
		quitButton = GetNode<Button>("../TabContainer/Options/QuitButton");
		quitConfirm = GetNode<ConfirmationDialog>("../TabContainer/Options/QuitConfirm");
		quitButton.Pressed += QuitGame;
		quitConfirm.Confirmed += QuitConfirmed;
		
		unlockMachinesCheck = GetNode<CheckBox>("../TabContainer/Options/UnlockMachinesCheck");
		unlockRecipesCheck = GetNode<CheckBox>("../TabContainer/Options/UnlockRecipesCheck");
		disableWearCheck = GetNode<CheckBox>("../TabContainer/Options/DisableWearCheck");
		disableStorageCheck = GetNode<CheckBox>("../TabContainer/Options/DisableStorageCheck");
		unlockMachinesCheck.Toggled += ToggleUnlockMachines;
		unlockRecipesCheck.Toggled += ToggleUnlockRecipes;
		disableWearCheck.Toggled += ToggleWear;
		disableStorageCheck.Toggled += ToggleStorage;
		
		//Possibly unnecessary test variable.
		questUpdateFunctioning = true;
		
		//Set these to true to unlock all machines or recipes.
		unlockAllMachines = false;
		unlockAllRecipes = false;
		disableWear = false;
		disableStorage = false;
		
		UpdateQuestTracking();
		
		//logisticsControl.CallDeferred(nameof(LogisticsControl.PopulateResourceMenu));
	}
	
	public override void _Process(double delta)
	{
		//Process active machines' recipes and check quest completion status.
		//ProcessMachines(delta);
		foreach (var reg in regionMap.Values)
		{
			reg.Tick(delta);
		}
		
		DisplayMaxStorage();
		UpdateQuestTracking();
		CheckQuests();
	}
	
	public static void LoadAll()
	{
		//Load JSON from data files.
		string resourcePath = "res://data/resources.json";
		string harvestPath = "res://data/harvest.json";
		string machinePath = "res://data/machines.json";
		string infrastructurePath = "res://data/infrastructure.json";
		string recipePath = "res://data/recipes.json";
		string regionsPath = "res://data/regions.json";
		string questPath = "res://data/quests.json";
		
		//Assign data to reference variables.
		RESOURCES = LoadJson<Dictionary<string, ResourceData>>(resourcePath);
		HARVEST = LoadJson<Dictionary<string, HarvestData>>(harvestPath);
		MACHINES = LoadJson<Dictionary<string, MachineData>>(machinePath);
		INFRASTRUCTURE = LoadJson<Dictionary<string, InfrastructureData>>(infrastructurePath);
		RECIPES = LoadJson<Dictionary<string, RecipeData>>(recipePath);
		REGIONS = LoadJson<Dictionary<string, RegionData>>(regionsPath);
		QUESTS = LoadJson<Dictionary<string, QuestData>>(questPath);
	}
	
	//Quits game.
	public void QuitGame()
	{
		quitConfirm.PopupCentered();
	}
	
	public void QuitConfirmed()
	{
		GetTree().Quit();
	}
	
	public void ToggleUnlockMachines(bool isChecked)
	{
		unlockAllMachines = isChecked;
		buildControl.UpdateBuildMenu();
	}
	
	public void ToggleUnlockRecipes(bool isChecked)
	{
		unlockAllRecipes = isChecked;
		machinesControl.UpdateMachinePanels();
	}
	
	public void ToggleWear(bool isChecked)
	{
		disableWear = isChecked;
	}
	
	public void ToggleStorage(bool isChecked)
	{
		disableStorage = isChecked;
	}
	
	public static T LoadJson<T>(string filepath)
	{
		//Check if file properly exists.
		if (!Godot.FileAccess.FileExists(filepath))
		{
			GD.PrintErr($"Missing data file {filepath}");
			return default;
		}
		//Attempt to load JSON file.
		try
		{
			using Godot.FileAccess fa = Godot.FileAccess.Open(filepath, Godot.FileAccess.ModeFlags.Read);
			string json = fa.GetAsText();
			return JsonSerializer.Deserialize<T>(json);
		}
		catch (Exception e)
		{
			GD.PrintErr($"Failed to load {filepath}: {e.Message}");
			return default;
		}
	}
	
	public static void BuildNameMaps()
	{
		//Create references from user facing names to reference IDs.
		foreach (var res in RESOURCES)
		{
			resNameToKey[res.Value.name] = res.Key;
		}
		foreach (var harv in HARVEST)
		{
			harvActionToKey[harv.Value.action] = harv.Key;
		}
		foreach (var mach in MACHINES)
		{
			machNameToKey[mach.Value.name] = mach.Key;
		}
		foreach (var rec in RECIPES)
		{
			recNameToKey[rec.Value.name] = rec.Key;
		}
		foreach (var reg in REGIONS)
		{
			regNameToKey[reg.Value.name] = reg.Key;
		}
		foreach (var qst in QUESTS)
		{
			qstNameToKey[qst.Value.name] = qst.Key;
		}
	}
	
	public static void GenerateStartingRegion()
	{
		//Generate the starting region at (0, 0).
		GD.Print("GameData: Generating starting region...");
		(int x, int y) origin = (0, 0);
		regionMap[origin] = new Region(REGIONS["landing zone"], origin);
		GD.Print("GameData: Starting region generated");
	}
	
	public static void GiveStartingResources()
	{
		GD.Print("GameData: Giving Starting Resources...");
		
		//Initialize resource quantities based on starting value in JSON data in the starting location.
		foreach(var res in RESOURCES)
		{
			regionMap[(0, 0)].resources[res.Key] = res.Value.startingAmount;
		}
	}
	
	public static void GiveStartingMachines()
	{
		GD.Print("GameData: Giving Starting Machines...");
		
		//Give the starting machines depending on JSON available value at starting location.
		foreach (var mach in MACHINES)
		{
			for (int i = 0; i < mach.Value.startingAmount; i++)
			{
				regionMap[(0, 0)].machines.Add(new Machine(mach.Key, regionMap[(0, 0)]));
				GD.Print($"GameData: Adding machine {mach.Value.name}");
			}
		}
		
		foreach (var infra in INFRASTRUCTURE)
		{
			for (int i = 0; i < infra.Value.startingAmount; i++)
			{
				regionMap[(0, 0)].infrastructure.Add(new Infrastructure(infra.Key, regionMap[(0, 0)]));
				GD.Print($"GameData: Adding infrastructure {infra.Value.name}");
			}
		}
		
		regionMap[(0, 0)].UpdateStorage();
	}
	
	public static void DisplayMaxStorage()
	{
		string text = $"[table={currentRegion.maxStorage.Keys.Count * 2}]";
		foreach (var phase in currentRegion.maxStorage)
		{
			string phaseKey = phase.Key;
			string unit = phase.Value.unit;
			float capacity = currentRegion.maxStorage[phaseKey].amount;
			text += $"[cell]{phaseKey[0]}:[/cell][cell][right]{GameData.FormatUnit2(capacity, unit)}[/right][/cell]";
		}
		text += "[/table]";
		
		maxPhaseLabel.Text = text;
	}
	
	public static void UpdateQuestTracking()
	{
		if (!questUpdateFunctioning)
		{
			return;
		}
		
		//Check if questControl and trackedQuest are functioning and a quest is being tracked.
		if (questControl != null && trackedQuest != null && trackedQuest.name != "No name")
		{
			//Reference quest's name and requirements.
			String text = trackedQuest.name;
			
			QuestRequirement requirements = trackedQuest.requirement;
			
			if (requirements.resources.Count > 0)
			{
				//Display required resources if any.
				text += "\n\nResources:";
				
				foreach (var res in requirements.resources)
				{
					//Calculate available resources total among all regions and display along with required amounts.
					ResourceData resource = RESOURCES[res.Key];
					float available = 0f;
					
					foreach (var reg in regionMap)
					{
						available += reg.Value.resources[res.Key];
					}
					
					text += $"\n{resource.name}: {FormatUnit(available, res.Key)} / {FormatUnit(res.Value, res.Key)}";
				}
			}
			
			if (requirements.machines.Count > 0)
			{
				//Display required machines if any.
				text += "\n\nMachines:";
				
				foreach (var mach in requirements.machines)
				{
					//Count all constructed machines among all regions and display along with required amount.
					MachineData machine = MACHINES[mach.Key];
					int machineCount = 0;
					
					foreach (var reg in regionMap)
					{
						foreach (Machine mach2 in reg.Value.machines)
						{
							if (mach2.id == mach.Key)
							{
								machineCount++;
							}
						}
					}
					
					text += $"\n{machine.name}: {machineCount} / {mach.Value}";
				}
			}
			
			if (requirements.quests.Count > 0)
			{
				//Display required quests completed if any.
				text += "\n\nQuests:";
				
				foreach (var qst in requirements.quests)
				{
					//Display quests' current status whether active, completed, or somehow both.
					QuestData quest = QUESTS[qst];
					String questState;
					
					bool active = questControl.activeQuests.ContainsKey(qst);
					bool complete = questControl.completeQuests.ContainsKey(qst);
					
					if (active && complete)
					{
						questState = "Error: A & C";
					}
					else if (active)
					{
						questState = "Active";
					}
					else if (complete)
					{
						questState = "Complete";
					}
					else
					{
						questState = "Incomplete";
					}
					
					text += $"\n{quest.name}: {questState}";
				}
			}
			
			objectiveLabel.Text = text;
		}
		else
		{
			objectiveLabel.Text = "No quest tracked.";
		}
	}
	
	public static string FormatUnit(float amount, string resource)
	{
		//Format unit quantity based on unit it is measured in and largest significant size of unit.
		string unit = GameData.RESOURCES.ContainsKey(resource)
			? GameData.RESOURCES[resource].unit
			: "u";
		
		string prefix;
		float display;
		
		if (amount >= 900000)
		{
			prefix = "M"; //mega
			display = amount / 1000000f;
		}
		else if (amount >= 900)
		{
			prefix = "k"; //kilo
			display = amount / 1000f;
		}
		else if (amount == 0 || amount >= 0.9)
		{
			//Displays "0.00 u" if amount is exactly 0.
			prefix = "";
			display = amount;
		}
		else if (amount >= 0.0009)
		{
			prefix = "m"; //milli
			display = amount * 1000f;
		}
		else
		{
			//Displays "Negligible" if amount is greater than 0 but trace amounts.
			return "Negligible";
		}
		
		//Return formatted string with adjusted amount and units.
		return $"{display:0.00} {prefix}{unit}";
	}
	
	public static string FormatUnit2(float amount, string u)
	{
		//Format unit quantity based on unit it is measured in and largest significant size of unit.
		string unit = u;
		
		string prefix;
		float display;
		
		if (amount >= 900000)
		{
			prefix = "M"; //mega
			display = amount / 1000000f;
		}
		else if (amount >= 900)
		{
			prefix = "k"; //kilo
			display = amount / 1000f;
		}
		else if (amount == 0 || amount >= 0.9)
		{
			//Displays "0.00 u" if amount is exactly 0.
			prefix = "";
			display = amount;
		}
		else if (amount >= 0.0009)
		{
			prefix = "m"; //milli
			display = amount * 1000f;
		}
		else
		{
			//Displays "Negligible" if amount is greater than 0 but trace amounts.
			return "Negligible";
		}
		
		//Return formatted string with adjusted amount and units.
		return $"{display:0.00} {prefix}{unit}";
	}
	
	public static string CoordToString((int x, int y) coord)
	{
		//Convert an XY coordinate to a String.
		return $"({coord.x}, {coord.y})";
	}
	
	private void ProcessMachines(double delta)
	{
		foreach (var reg in regionMap)
		{
			//Process each active machine in the region.
			foreach (Machine mach in reg.Value.machines)
			{
				//If machine is turned on, is not excessively damaged, and selected recipe is valid.
				if (mach.active && (disableWear || mach.wear < mach.maxWear) && GameData.RECIPES.ContainsKey(mach.currentRecipe))
				{
					//Create a ratio value based on if recipe can be crafted.
					float ratio = CanCraft(mach.currentRecipe, mach.location, delta);
					
					if (ratio > 0)
					{
						//Create references to recipe and input resources.
						RecipeData recipe = GameData.RECIPES[mach.currentRecipe];
						Dictionary<string, float> inputs = recipe.inputs;
						
						if (recipe.local == "wind")
						{
							ratio *= mach.location.wind;
						}
						else if (recipe.local == "solar")
						{
							ratio *= mach.location.solar;
						}
						
						foreach (var res in inputs)
						{
							//Remove resource from region's storage used in the recipe according to the ratio.
							reg.Value.resources[res.Key] = Math.Max(0f, reg.Value.resources[res.Key] - res.Value * (float)delta * ratio);
						}
						
						//Create reference to recipe's output.
						Dictionary<string, float> outputs = recipe.outputs;
						
						foreach (var res in outputs)
						{
							//Produce crafted resource into region's storage according to the ratio.
							reg.Value.resources[res.Key] += res.Value * (float)delta * ratio;
						}
					}
					
					//Damage the machine according to the ratio.
					if (!disableWear)
					{
						mach.Damage(0.001f * ratio);
					}
				}
			}
			
			foreach (var infra in reg.Value.infrastructure)
			{
				infra.Tick(delta);
			}
		}
	}
	
	private float CanCraft(string name, Region reg, double delta)
	{
		//Check if the recipe can be crafted using resources in the region.
		//Return 1 if completely craftable, 0 if uncraftable, and a value between if partially craftable.
		if (!GameData.RECIPES.ContainsKey(name))
		{
			return 0f;
		}
		
		RecipeData recipe = GameData.RECIPES[name];
		Dictionary<string, float> inputs = recipe.inputs;
		
		//If recipe has no required resources.
		if (inputs.Count == 0)
		{
			return 1f;
		}
		
		List<float> ratios = new();
		
		foreach (var res in inputs)
		{
			if (res.Value <= 0)
			{
				//Skip if value is somehow 0 or less.
				continue;
			}
			
			//Determine available resources if a value exists and amount required for recipe adjusted for time passed.
			float available = reg.resources.ContainsKey(res.Key)
				? reg.resources[res.Key]
				: 0f;
			float required = res.Value * (float)delta;
			
			//Add ratio if amount available is insufficient.
			if (required != 0 && available < required)
			{
				ratios.Add(available / required);
			}
		}
		
		//Return full ratio if all required resources satisfied.
		if (ratios.Count == 0)
		{
			return 1f;
		}
		
		//Determin the smallest ratio among the list and return 0 if 0 or less.
		float minRatio = ratios.Min();
		if (minRatio <= 0f)
		{
			return 0f;
		}
		
		//Return the ratio between 0 and 1.
		return Math.Clamp(minRatio, 0f, 1f);
	}
	
	public static void CheckQuests()
	{
		//Check active quests for completion.
		List<string> toComplete = new();
		
		foreach (var quest in questControl.activeQuests)
		{
			//Add quest to list to complete if requirements are fulfilled.
			if (IsQuestFulfilled(quest.Value))
			{
				toComplete.Add(quest.Key);
				GD.Print($"GameData: {quest.Value.name} quest completed");
			}
		}
		
		foreach (var questKey in toComplete)
		{
			CompleteQuest(questKey);
		}
	}
	
	private static bool IsQuestFulfilled(QuestData quest)
	{
		//Check if quest's requirements are fullfilled.
		QuestRequirement requirements = quest.requirement;
		Dictionary<string, float> resRequirements = requirements.resources;
		Dictionary<string, int> machRequirements = requirements.machines;
		List<string> qstRequirements = requirements.quests;
		
		//Tolerance for imprecise float values.
		const float EPS = 0.0005f;
		
		//Determine if all the resource requirements among all regions has been met.
		bool resFulfilled = resRequirements.All(req => 
		{
			float total = 0f;
			foreach (var reg in regionMap.Values)
			{
				if (reg.resources.ContainsKey(req.Key))
				{
					total += reg.resources[req.Key];
					if (total + EPS >= req.Value)
					{
						return true;
					}
				}
			}
			return total + EPS >= req.Value;
		});
		//Determine if all the machine requirements among all regions has been met.
		bool machFulfilled = machRequirements.All(req =>
		{
			int total = 0;
			foreach (var reg in regionMap.Values)
			{
				if (GameData.MACHINES.ContainsKey(req.Key))
				{
					total += reg.machines.Count(m => m.id == req.Key);
				}
				else if (GameData.INFRASTRUCTURE.ContainsKey(req.Key))
				{
					total += reg.infrastructure.Count(m => m.id == req.Key);
				}
				if (total >= req.Value)
				{
					return true;
				}
			}
			return total >= req.Value;
		});
		
		//Determine if all required quests are completed.
		bool qstFulfilled = qstRequirements.All(qst => questControl.completeQuests.ContainsKey(qst));
		
		//Return true of all requirments are satisfied.
		return resFulfilled && machFulfilled && qstFulfilled;
	}
	
	public static void CompleteQuest(string questKey)
	{
		//Designate a quest as completed
		if (questControl.completeQuests.ContainsKey(questKey))
		{
			//Return if quest is somehow already completed.
			return;
		}
		
		//Remove quest from active quests and add it to completed quests lists.
		QuestData quest = QUESTS[questKey];
		questControl.activeQuests.Remove(questKey);
		questControl.completeQuests[questKey] = quest;
		
		GD.Print($"Completing quest {quest.name}");
		
		QuestUnlock unlocks = quest.unlocks;
		
		foreach (var q in unlocks.quests)
		{
			//Set new quest as active if not already in the active quests list.
			if (!questControl.activeQuests.ContainsKey(q))
			{
				GD.Print($"Unlock quest {QUESTS[q].name}");
				questControl.activeQuests[q] = QUESTS[q];
			}
		}
		
		foreach (var rec in unlocks.recipes)
		{
			//Unlock recipe.
			GD.Print($"Unlock recipe {RECIPES[rec].name}");
			RECIPES[rec].available = true;
		}
		
		foreach (var mach in unlocks.machines)
		{
			//Unlock machine.
			GD.Print($"Unlock machine {MACHINES[mach].name}");
			if (GameData.MACHINES.ContainsKey(mach))
			{
				MACHINES[mach].available = true;
			}
			else if (GameData.INFRASTRUCTURE.ContainsKey(mach))
			{
				INFRASTRUCTURE[mach].available = true;
			}
		}
		
		GD.Print("GameData: Updating menus...");
		
		//Update displayed active and completed quests.
		questControl.UpdateQuestLists();
		
		//Check if any newly active quests are already fulfilled.
		GD.Print("GameData: checking unlocked quests...");
		CheckQuests();
		
		//Update menus for new machines and recipes.
		GD.Print("GameData: Calling buildControl.UpdateBuildMenu()");
		buildControl.UpdateBuildMenu();
		GD.Print("GameData: Calling machinesControl.UpdateMachinePanels()");
		machinesControl.UpdateMachinePanels();
		
		GD.Print($"-- GameData: Finished quest completion events for quest {quest.name} --");
	}
	
	public static void TravelTo((int x, int y) coord)
	{
		//Travel player to designated coordinate.
		GD.Print($"GameData: Traveling to ({coord.x}, {coord.y})");
		
		//Set current region to coordinate if it exists in the map.
		if (regionMap.ContainsKey(coord))
		{
			currentRegion = regionMap[coord];
		}
		
		//Update displays.
		travelControl.UpdateRegions();
		travelControl.DisplayFeatures();
		
		//Update the list of machines to ones constructed in the new region.
		try
		{
			machinesControl.UpdateRegionMachines();
		}
		catch (Exception e)
		{
			GD.PrintErr($"GameData: Error updating machines {e.Message}");
		}
		
		logisticsControl.UpdateRegionLogistics();
		
		//Update colored regions on the map.
		mapControl.UpdateAllColors();
		//Update the harvest controls for local deposits.
		harvestControl.UpdateHarvest();
		buildControl.UpdateNeighborMenu();
	}
	
	public static void ExploreRegion((int x, int y) coord)
	{
		//Explore an adjacent region and add it to the map.
		if (regionMap.ContainsKey(coord))
		{
			//Ignore if region already explored.
			GD.Print($"GameData: Region {coord} already explored");
			return;
		}
		
		//Define adjacent and diagnonal coordinates.
		(int x, int y) north = (coord.x, coord.y + 1);
		(int x, int y) south = (coord.x, coord.y - 1);
		(int x, int y) west = (coord.x - 1, coord.y);
		(int x, int y) east = (coord.x + 1, coord.y);
		(int x, int y) nw = (coord.x - 1, coord.y + 1);
		(int x, int y) ne = (coord.x + 1, coord.y + 1);
		(int x, int y) se = (coord.x + 1, coord.y - 1);
		(int x, int y) sw = (coord.x - 1, coord.y - 1);
		
		List<Region> adjacent = new();
		List<Region> diagonal = new();
		
		//Add explored regions to list of adjacent regions.
		if (regionMap.ContainsKey(north))
		{
			adjacent.Add(regionMap[north]);
		}
		if (regionMap.ContainsKey(south))
		{
			adjacent.Add(regionMap[south]);
		}
		if (regionMap.ContainsKey(west))
		{
			adjacent.Add(regionMap[west]);
		}
		if (regionMap.ContainsKey(east))
		{
			adjacent.Add(regionMap[east]);
		}
		//Add explored regions to list of diagonal regions.
		if (regionMap.ContainsKey(nw))
		{
			diagonal.Add(regionMap[nw]);
		}
		if (regionMap.ContainsKey(ne))
		{
			diagonal.Add(regionMap[ne]);
		}
		if (regionMap.ContainsKey(se))
		{
			diagonal.Add(regionMap[se]);
		}
		if (regionMap.ContainsKey(sw))
		{
			diagonal.Add(regionMap[sw]);
		}
		
		//Create list of possible biomes with weighted probabilities.
		Dictionary<string, float> weightedBiomes = new();
		string selectedBiome = "nowhere";
		string largestBiome = "nowhere";
		float largestValue = 0f;
		
		//Add adjacent regions' biomes at full value.
		foreach (var reg in adjacent)
		{
			foreach (var neighbor in reg.regData.neighbors)
			{
				if (weightedBiomes.ContainsKey(neighbor.Key))
				{
					weightedBiomes[neighbor.Key] += neighbor.Value;
				}
				else
				{
					weightedBiomes[neighbor.Key] = neighbor.Value;
				}
				
				if (weightedBiomes[neighbor.Key] > largestValue)
				{
					largestBiome = neighbor.Key;
					largestValue = weightedBiomes[neighbor.Key];
				}
			}
		}
		//Add diagonal regions' biomes at half value.
		foreach (var reg in diagonal)
		{
			foreach (var neighbor in reg.regData.neighbors)
			{
				if (weightedBiomes.ContainsKey(neighbor.Key))
				{
					weightedBiomes[neighbor.Key] += neighbor.Value / 2;
				}
				else
				{
					weightedBiomes[neighbor.Key] = neighbor.Value / 2;
				}
				
				if (weightedBiomes[neighbor.Key] > largestValue)
				{
					largestBiome = neighbor.Key;
					largestValue = weightedBiomes[neighbor.Key];
				}
			}
		}
		
		//Explored region's biome is randomly determined and influenced by nearby regions' biomes.
		//Calculate the total weight of all possible biomes.
		float total = 0f;
		foreach (var w in weightedBiomes.Values)
		{
			total += w;
		}
		
		//Randomly generate a number between 0 and the total weight.
		float roll = rng.Randf() * total;
		
		//Track a total cummulative value of checked weighted values.
		float cummulative = 0f;
		foreach (var w in weightedBiomes)
		{
			//Check if the random number falls in the range between the previous weight (or 0) and the current one's.
			cummulative += w.Value;
			if (cummulative >= roll)
			{
				//Select the biome that the random number is within its range.
				selectedBiome = w.Key;
				break;
			}
		}
		
		//If somehow the cummulative exceeds the total weight, default to the biome with the largest probability.
		if (cummulative >= total)
		{
			selectedBiome = largestBiome;
		}
		
		//Return if a biome wasn't selected.
		if (selectedBiome == "nowhere")
		{
			GD.Print($"GameData: Error generating region; Total: {total}, Roll: {roll}, Cummulative: {cummulative}");
			return;
		}
		
		//Generate a new region according to the selected biome's data.
		Region explored = new Region(REGIONS[selectedBiome], coord);
		GD.Print($"GameData: Adding new region {explored.regData.name} at {coord}");
		//Add new region to the map.
		regionMap[coord] = explored;
		coordStringToTuple[CoordToString(coord)] = coord;
		GD.Print("GameData: Successfully added new region");
		String regionsList = "";
		foreach (var c in regionMap.Keys)
		{
			regionsList += $"{CoordToString(c)} ";
		}
		GD.Print($"GameData: Explored regions {regionsList}");
		
		//Update interface with new region.
		mapControl.GenerateMap();
		mapControl.UpdateAllColors();
		travelControl.UpdateRegions();
		buildControl.UpdateNeighborMenu();
	}
	
	public static float RandNormal(float mean, float stddev)
	{
		float u1 = Mathf.Max(rng.Randf(), 1e-7f);
		float u2 = rng.Randf();
		
		return mean + stddev * Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.Pi * u2);
	}
}

public class ResourceData
{
	public string name {get; set;}
	public string abbreviation {get; set;}
	public string type {get; set;}
	public List<string> subtypes {get; set;}
	public string phase {get; set;}
	public string unit {get; set;}
	
	[System.Text.Json.Serialization.JsonPropertyName("starting amount")]
	public float startingAmount {get; set;}
	
	public float value {get; set;}
	public Dictionary<string, float> scrap {get; set;}
	public string description {get; set;}
	
	public ResourceData()
	{
		name = "No Name";
		abbreviation = "N/A";
		type = "untyped";
		subtypes = new List<string>();
		phase = "intangible";
		unit = "u";
		scrap = new Dictionary<string, float>();
	}
}

public class HarvestData
{
	public string resource {get; set;}
	public float amount {get; set;}
	public float time {get; set;}
	public string action {get; set;}
	
	public HarvestData()
	{
		resource = "nothing";
		amount = 0.0f;
		time = 0.0f;
		action = "No Action";
	}
}

public class BuildData
{
	public string name {get; set;}
	public Dictionary<string, float> cost {get; set;}
	
	[System.Text.Json.Serialization.JsonPropertyName("starting amount")]
	public int startingAmount {get; set;}
	
	public bool available {get; set;}
	public string description {get; set;}
	
	protected BuildData()
	{
		name = "No Name";
		cost = new Dictionary<string, float>();
		startingAmount = 0;
		available = false;
		description = "No description";
	}
}

public class MachineData : BuildData
{
	public MachineData() : base()
	{
		
	}
}

public class InfrastructureData : BuildData
{
	public string type {get; set;}
	public List<string> serves {get; set;}
	public float through {get; set;}
	
	[System.Text.Json.Serialization.JsonPropertyName("energy cost")]
	public float energyCost {get; set;}
	
	public InfrastructureData() : base()
	{
		type = "untyped";
		serves = new();
		through = 0f;
		energyCost = 0f;
	}
}

public class RecipeData
{
	public string name {get; set;}
	public Dictionary<string, float> inputs {get; set;}
	public Dictionary<string, float> outputs {get; set;}
	public bool available {get; set;}
	public List<string> machines {get; set;}
	public string local {get; set;}
	public string description {get; set;}
	
	public RecipeData()
	{
		name = "No Name";
		inputs = new Dictionary<string, float>();
		outputs = new Dictionary<string, float>();
		available = false;
		machines = new List<string>();
		description = "No description.";
	}
}

public class QuestData
{
	public string name {get; set;}
	public bool start {get; set;}
	public QuestRequirement requirement {get; set;}
	public QuestUnlock unlocks {get; set;}
	public string text {get; set;}
	public string hint {get; set;}
	
	public QuestData()
	{
		name = "No Name";
		start = false;
		requirement = new QuestRequirement();
		unlocks = new QuestUnlock();
		text = "No text.";
		hint = "No hint.";
	}
}

public class QuestRequirement
{
	public Dictionary<string, float> resources {get; set;}
	public Dictionary<string, int> machines {get; set;}
	public List<string> quests {get; set;}
	
	public QuestRequirement()
	{
		resources = new Dictionary<string, float>();
		machines = new Dictionary<string, int>();
		quests = new List<string>();
	}
}

public class QuestUnlock
{
	public List<string> quests {get; set;}
	public List<string> recipes {get; set;}
	public List<string> machines {get; set;}
	
	public QuestUnlock()
	{
		quests = new List<string>();
		recipes = new List<string>();
		machines = new List<string>();
	}
}

public class RegionData
{
	public String name {get; set;}
	public float elevation {get; set;}
	public float temperature {get; set;}
	public float pressure {get; set;}
	public float roughness {get; set;}
	
	public Dictionary<String, float> resources {get; set;}
	public Dictionary<String, float> neighbors {get; set;}
	public Dictionary<String, float> features {get; set;}
	public Dictionary<String, float> hazards {get; set;}
	
	public RegionData()
	{
		name = "No name";
		elevation = 0f;
		temperature = 0f;
		pressure = 0f;
		roughness = 0f;
		
		resources = new();
		neighbors = new();
		features = new();
		hazards = new();
	}
}

//A geographical region within the game world.
public class Region
{
	public RegionData regData;
	public int coordX;
	public int coordY;
	
	public float wind;
	public float windMax;
	public float windK;
	public float windSigma;
	public float windState;
	
	public float solar;
	public float solarMax;
	public float solarK;
	public float solarSigma;
	public float solarState;
	
	public Dictionary<string, float> resources;
	public Dictionary<string, (float amount, string unit)> maxStorage;
	public List<Machine> machines;
	public List<Infrastructure> infrastructure;
	public List<string> nodes;
	
	public Region(RegionData data, (int x, int y) coord)
	{
		//Generate a new region using region data and the coordinate of the region.
		GD.Print($"GameData: Generating region at ({coord.x}, {coord.y})");
		regData = data;
		coordX = coord.x;
		coordY = coord.y;
		
		windMax = (regData.elevation * 0.2f) + regData.pressure + regData.roughness;
		windK = regData.roughness * 0.1f;
		windSigma = regData.roughness * 0.5f;
		wind = windMax / 2;
		windState = 0f;
		
		solarMax = (regData.elevation * 0.2f) + (1.0f - regData.pressure) + (1.0f - regData.roughness);
		solarK = regData.roughness * 0.1f;
		solarSigma = regData.roughness * 0.5f;
		solar = solarMax / 2;
		solarState = 0f;
		
		resources = new();
		maxStorage = new();
		machines = new();
		infrastructure = new();
		nodes = new();
		
		//Create local resource deposits randomly determined by what's typically available in the region.
		foreach (var res in regData.resources)
		{
			if (res.Value >= 1f || GameData.rng.Randf() < res.Value)
			{
				GD.Print($"Adding resource node {GameData.RESOURCES[res.Key].name}");
				nodes.Add(res.Key);
			}
		}
		
		//Create an empty storage of resources in the region.
		foreach (var res in GameData.RESOURCES)
		{
			resources[res.Key] = 0f;
			
			if (!maxStorage.ContainsKey(res.Value.phase))
			{
				maxStorage[res.Value.phase] = (0f, res.Value.unit);
			}
		}
		
		UpdateStorage();
	}
	
	public void UpdateStorage()
	{
		foreach (var phase in maxStorage.Keys)
		{
			maxStorage[phase] = (0f, maxStorage[phase].unit);
		}
		
		foreach (var infra in infrastructure)
		{
			if (infra.type == "storage")
			{
				foreach (var phase in infra.serves)
				{
					maxStorage[phase] = (maxStorage[phase].amount + infra.through, maxStorage[phase].unit);
				}
			}
		}
	}
	
	public float TotalStored(string phase)
	{
		float total = 0f;
		foreach (var res in resources)
		{
			if (GameData.RESOURCES[res.Key].phase == phase)
			{
				total += res.Value;
			}
		}
		
		return total;
	}
	
	public float TotalAvailable(string phase)
	{
		return maxStorage[phase].amount - TotalStored(phase);
	}
	
	public void Tick(double delta)
	{
		foreach (var mach in machines)
		{
			mach.Tick(delta);
		}
		foreach (var infra in infrastructure)
		{
			infra.Tick(delta);
		}
		
		ShiftWeather((float)delta);
	}
	
	public void ShiftWeather(float delta)
	{
		windState += -windK * windState * delta + windSigma * Mathf.Sqrt(delta) * GameData.RandNormal(0f, 1f);
		windState = Math.Clamp(windState, -1f, 1f);
		wind = windMax * (1f + 0.4f * windState);
		
		solarState += -solarK * solarState * delta + solarSigma * Mathf.Sqrt(delta) * GameData.RandNormal(0f, 1f);
		solarState = Math.Clamp(solarState, -1f, 1f);
		solar = solarMax * (1f + 0.4f * solarState);
	}
	
	public bool IsAdjacent(Region reg)
	{
		//Check if another region is directly adjacent to this region.
		int vectX = Math.Abs(coordX - reg.coordX);
		int vectY = Math.Abs(coordY - reg.coordY);
		
		return vectX == 1 && vectY == 0 || vectX == 0 && vectY == 1;
	}
	
	public bool IsDiagonal(Region reg)
	{
		//Check if another region is in a coordinate diagonal to this region.
		int vectX = Math.Abs(coordX - reg.coordX);
		int vectY = Math.Abs(coordY - reg.coordY);
		
		return vectX == 1 && vectY == 1;
	}
}

public class Buildable
{
	//A parent class for buildable elements such as machines.
	public string id {get; protected set;}
	public bool active {get; protected set;}
	public float wear {get; protected set;}
	public float diagnosedWear {get; protected set;}
	public float maxWear {get; protected set;}
	public Dictionary<string, float> repairComponents {get; protected set;} = new();
	public Region location {get; protected set;}
	
	protected Buildable(string buildableID, Region loc)
	{
		id = buildableID;
		location = loc;
		active = false;
		
		wear = 0f;
		diagnosedWear = 0f;
		maxWear = 0f;
	}
	
	public void ToggleActive(bool on)
	{
		//Turn machine on/off.
		active = on;
	}
	
	public virtual void Tick(double delta)
	{
		
	}
	
	public void Damage(float dmg)
	{
		//Damage the machine and increase wear.
		wear = Math.Min(wear + dmg, maxWear);
	}
	
	public void Repair()
	{
		//Repair the machine, consume repair components, and create recyclable scrap.
		float total = 0f;
		
		foreach (var compKey in repairComponents.Keys.ToList())
		{
			float needed = repairComponents[compKey];
			//Determine how much of the required resource is available at the machine's location.
			float available = location.resources[compKey];
			//Track an amount of scrap that would be produced from this resource.
			float scrap = 0f;
			
			if (available >= needed)
			{
				//If amount of resource is sufficient, consume the required amount and note that as scrap.
				location.resources[compKey] -= needed;
				total += needed;
				repairComponents.Remove(compKey);
				scrap = needed;
			}
			else
			{
				//If amount is insufficient, consume all that is available and reduce that amount from amount required taking not of scrap.
				repairComponents[compKey] = needed - available;
				location.resources[compKey] = 0;
				total += available;
				scrap = available;
			}
			
			foreach (var sc in GameData.RESOURCES[compKey].scrap)
			{
				//For each resource used to craft the replaced component, produce the associated scrap component.
				location.resources[sc.Key] += scrap * sc.Value;
			}
		}
		
		//Reduce wear and diagnosed wear by the total amount repaired.
		wear -= total;
		diagnosedWear -= total;
	}
	
	public void Diagnose()
	{
		//Diagnose the necessary resources to repair the machine.
		//Caluculate the amount of wear undiagnosed from how much has wear has already been diagnosed.
		float undiagnosed = wear - diagnosedWear;
		Dictionary<string, float> workingComponents = new();
		
		//Determine the amount of working components from what has been already diagnosed to need replacement.
		foreach (var res in GameData.MACHINES[id].cost)
		{
			GD.Print($"GameData: Checking if working components {GameData.RESOURCES[res.Key].name}");
			if (repairComponents.TryGetValue(res.Key, out float comp))
			{
				workingComponents[res.Key] = res.Value - comp;
			}
			else
			{
				workingComponents[res.Key] = res.Value;
			}
		}
		
		while (diagnosedWear < wear)
		{
			//Loop as long as diagnosed wear is less than the current amount of wear.
			//Calculate the total amounts among working components.
			float total = 0f;
			string selectedComp = "";
			string largestComp = "";
			float largestAmount = 0f;
			foreach (var comp in workingComponents)
			{
				total += comp.Value;
				
				if (comp.Value > largestAmount)
				{
					largestComp = comp.Key;
					largestAmount = comp.Value;
				}
			}
			
			//Generate a random number up to the total weight of working components.
			float roll = GameData.rng.Randf() * total;
			
			//Track a cummulative total of checked components.
			float cummulative = 0f;
			foreach (var comp in workingComponents)
			{
				//Determine if the random number is between of the last checked component (or 0) and the current component's.
				cummulative += comp.Value;
				if (cummulative >= roll)
				{
					//Select this component if the random number is within its range.
					selectedComp = comp.Key;
					break;
				}
			}
			
			//If the cummulative is somehow larger than the total, select the largest component by default.
			if (cummulative >= total)
			{
				selectedComp = largestComp;
			}
			
			GD.Print($"GameData: Selected component {GameData.RESOURCES[selectedComp].name}");
			
			if (undiagnosed <= 0.5f)
			{
				//If the amount of undiagnosed wear is small, assign a fixed amount of the selected component to repair.
				if (undiagnosed <= workingComponents[selectedComp])
				{
					//If the amount of undiagnosed wear is less than the amount of the amount of working components, diagnose the undiagnosed quantity to be repaired.
					//Increase the amount of wear diagnosed.
					diagnosedWear += undiagnosed;
					if (diagnosedWear > wear)
					{
						//If diagnosed wear exceeds total wear somehow, recalculated based on the difference and assure they are equal.
						undiagnosed -= diagnosedWear - wear;
						diagnosedWear = wear;
					}
					//If the currently diagnosed components for repair includes the selected component, increase the value, otherwise create a new entry for it.
					if (repairComponents.ContainsKey(selectedComp))
					{
						repairComponents[selectedComp] += undiagnosed;
					}
					else
					{
						repairComponents[selectedComp] = undiagnosed;
					}
				}
				else
				{
					//If undiagnosed wear is greater than how many of the component is still working.
					//Calculate the difference between undiagnosed wear and how many of the component is working.
					float difference = undiagnosed - workingComponents[selectedComp];
					//If the currently diagnosed components for repair includes the selected component, increase the value, otherwise create a new entry for it.
					if (repairComponents.ContainsKey(selectedComp))
					{
						repairComponents[selectedComp] += difference;
					}
					else
					{
						repairComponents[selectedComp] = difference;
					}
					//Designate the amount of working components as 0 and increase diagnosed wear by the difference.
					workingComponents[selectedComp] = 0f;
					diagnosedWear += difference;
				}
			}
			else
			{
				//If there is still a large amount of undiagnosed wear, roll a random number up to the amount of working components.
				roll = GameData.rng.Randf() * workingComponents[selectedComp];
				//Set it to the minimum between the rolled number and currently undiagnosed wear.
				roll = Math.Min(roll, undiagnosed);
				//Increase diagnosed wear by the modified roll.
				diagnosedWear += roll;
				if (diagnosedWear > wear)
				{
					//If diagnosed wear exceeds total wear, adjust for the difference and set the two values as equal.
					roll -= diagnosedWear - wear;
					diagnosedWear = wear;
				}
				//If the currently diagnosed components for repair includes the selected component, increase the value, otherwise create a new entry for it.
				if (repairComponents.ContainsKey(selectedComp))
				{
					repairComponents[selectedComp] += roll;
				}
				else
				{
					repairComponents[selectedComp] = roll;
				}
				//Adjust the currently working components.
				workingComponents[selectedComp] -= roll;
			}
			
			//Redefine the amount of undiagnosed wear and loop.
			undiagnosed = wear - diagnosedWear;
		}
	}
	
	public void Dismantle()
	{
		if (!GameData.MACHINES[id].available)
		{
			return;
		}
		
		if (diagnosedWear < wear)
		{
			Diagnose();
		}
		
		//Dictionary<string, float> workingComponents = new();
		
		Dictionary<string, float> resources = location.resources;
		
		foreach (var res in GameData.MACHINES[id].cost)
		{
			GD.Print($"GameData: Checking if working components {GameData.RESOURCES[res.Key].name}");
			if (repairComponents.TryGetValue(res.Key, out float comp))
			{
				resources[res.Key] += res.Value - comp;
			}
			else
			{
				resources[res.Key] += res.Value;
			}
		}
		
		foreach (var comp in repairComponents)
		{
			foreach (var scrap in GameData.RESOURCES[comp.Key].scrap)
			{
				resources[scrap.Key] += scrap.Value * comp.Value;
			}
		}
		
		location.UpdateStorage();
	}
}

public class Machine : Buildable
{
	//A machine that processes recipes to consume and produce resources.
	public List<string> recipes {get; private set;} = new();
	public string currentRecipe {get; private set;}
	public Dictionary<string, float> outputBuffer {get; private set;} = new();
	
	public Machine(string machineID, Region loc) : base(machineID, loc)
	{
		//Create a machine using the data ID at designated location.
		//Maximum wear is calculated as the total mass of components used to construct it.
		MachineData data = GameData.MACHINES[id];
		foreach (var mach in data.cost.Values)
		{
			maxWear += mach;
		}
		
		//Assign recipes based on which ones are assigned to this machine.
		recipes = GameData.RECIPES.Where(r => r.Value.machines.Contains(id)).OrderBy(r => r.Key).Select(r => r.Key).ToList();
		
		if (recipes.Count > 0)
		{
			currentRecipe = recipes[0];
		}
		else
		{
			currentRecipe = "";
		}
	}
	
	public void SetRecipe(string rid)
	{
		//Select the current recipe to process.
		currentRecipe = rid;
	}
	
	public override void Tick(double delta)
	{
		if (active && (GameData.disableStorage || outputBuffer.Keys.Count == 0) && (GameData.disableWear || wear < maxWear) && GameData.RECIPES.ContainsKey(currentRecipe))
		{
			//Create a ratio value based on if recipe can be crafted.
			float ratio = CanCraft(delta);
			
			if (ratio > 0f)
			{
				//Create references to recipe and input resources.
				RecipeData recipe = GameData.RECIPES[currentRecipe];
				Dictionary<string, float> inputs = recipe.inputs;
				
				if (recipe.local == "wind")
				{
					ratio *= location.wind;
				}
				else if (recipe.local == "solar")
				{
					ratio *= location.solar;
				}
				
				foreach (var res in inputs)
				{
					//Remove resource from region's storage used in the recipe according to the ratio.
					location.resources[res.Key] = Math.Max(0f, location.resources[res.Key] - res.Value * (float)delta * ratio);
				}
				
				//Create reference to recipe's output.
				Dictionary<string, float> outputs = recipe.outputs;
				
				foreach (var res in outputs)
				{
					//Produce crafted resource into region's storage according to the ratio.
					float outputVolume = res.Value * (float)delta * ratio;
					string resPhase = GameData.RESOURCES[res.Key].phase;
					if (!GameData.disableStorage)
					{
						float stored = Math.Min(outputVolume, location.TotalAvailable(resPhase));
						location.resources[res.Key] += stored;
						float remainder = outputVolume - stored;
						
						if (remainder > 0f)
						{
							if (outputBuffer.ContainsKey(res.Key))
							{
								outputBuffer[res.Key] += remainder;
							}
							else
							{
								outputBuffer[res.Key] = remainder;
							}
						}
					}
					else
					{
						location.resources[res.Key] += outputVolume;
					}
				}
			}
			
			//Damage the machine according to the ratio.
			if (!GameData.disableWear)
			{
				Damage(0.001f * ratio);
			}
		}
		
		List<string> removeBuffer = new();
		foreach (var res in outputBuffer)
		{
			string resPhase = GameData.RESOURCES[res.Key].phase;
			if (location.TotalAvailable(resPhase) > 0)
			{
				float stored = Math.Min(res.Value, location.TotalAvailable(resPhase));
				location.resources[res.Key] += stored;
				outputBuffer[res.Key] -= stored;
				
				if (outputBuffer[res.Key] <= 0f)
				{
					removeBuffer.Add(res.Key);
				}
			}
		}
		
		foreach (var res in removeBuffer)
		{
			outputBuffer.Remove(res);
		}
	}
	
	public float CanCraft(double delta)
	{
		if (!GameData.RECIPES.ContainsKey(currentRecipe))
		{
			return 0f;
		}
		
		RecipeData recipe = GameData.RECIPES[currentRecipe];
		Dictionary<string, float> inputs = recipe.inputs;
		
		//If recipe has no required resources.
		if (inputs.Count == 0)
		{
			return 1f;
		}
		
		List<float> ratios = new();
		
		foreach (var res in inputs)
		{
			if (res.Value <= 0)
			{
				//Skip if value is somehow 0 or less.
				continue;
			}
			
			//Determine available resources if a value exists and amount required for recipe adjusted for time passed.
			float available = location.resources.ContainsKey(res.Key)
				? location.resources[res.Key]
				: 0f;
			float required = res.Value * (float)delta;
			
			//Add ratio if amount available is insufficient.
			if (required != 0 && available < required)
			{
				ratios.Add(available / required);
			}
		}
		
		//Return full ratio if all required resources satisfied.
		if (ratios.Count == 0)
		{
			return 1f;
		}
		
		//Determin the smallest ratio among the list and return 0 if 0 or less.
		float minRatio = ratios.Min();
		if (minRatio <= 0f)
		{
			return 0f;
		}
		
		//Return the ratio between 0 and 1.
		return Math.Clamp(minRatio, 0f, 1f);
	}
}

public class Infrastructure : Buildable
{
	public string type {get; private set;}
	public List<string> serves {get; private set;}
	public float through {get; private set;}
	public float energyCost {get; private set;}
	public Infrastructure link {get; private set;}
	public Region target {get; private set;}
	public List<LogisticOrder> input {get; private set;}
	public List<LogisticOrder> output {get; private set;}
	public long lastServed {get; private set;}
	
	public Infrastructure(string infraID, Region loc) : base(infraID, loc)
	{
		InfrastructureData data = GameData.INFRASTRUCTURE[infraID];
		type = data.type;
		serves = data.serves;
		through = data.through;
		energyCost = data.energyCost;
		link = null;
		target = null;
		input = new();
		output = new();
		lastServed = -1;
	}
	
	public override void Tick(double delta)
	{
		/*if (!active)
		{
			return;
		}*/
		
		float thruMod = through * (float)delta;
		
		if (thruMod <= 0f)
		{
			return;
		}
		
		switch (type)
		{
			case "hub":
				TickHub(thruMod);
				return;
			case "conveyer":
				TickConveyer(thruMod);
				return;
			default:
				return;
		} 
	}
	
	private void TickHub(float thruMod)
	{
		CompactOrders();
		
		if (input.Count > 0)
		{
			foreach (var ord in input)
			{
				if (ord == null) continue;
				
				if (location.resources.ContainsKey(ord.resource))
				{
					location.resources[ord.resource] += ord.amount;
				}
			}
			input.Clear();
		}
		
		List<Infrastructure> conveyers = new();
		foreach (var infra in location.infrastructure)
		{
			if (infra != null && infra.type == "conveyer")
			{
				foreach (var ord in output)
				{
					if (infra.CanServe(ord))
					{
						conveyers.Add(infra);
						break;
					}
				}
			}
		}
		
		if (conveyers.Count == 0 || output.Count == 0)
		{
			foreach (var ord in output)
			{
				GiveInput(ord);
			}
			output.Clear();
			return;
		}
		
		conveyers.Sort((a, b) => a.lastServed.CompareTo(b.lastServed));
		
		int conveyerIndex = 0;
		int safety = 0;
		
		while (thruMod > 0f && output.Count > 0 && conveyers.Count > 0 && safety < 10000)
		{
			safety++;
			
			Infrastructure con = conveyers[conveyerIndex];
			int lastIndex = conveyerIndex;
			conveyerIndex = (conveyerIndex + 1) % conveyers.Count;
			
			LogisticOrder ord = null;
			
			int serveIndex = 0;
			while (serveIndex < output.Count)
			{
				ord = output[serveIndex];
				if (ord == null)
				{
					output.RemoveAt(0);
					continue;
				}
				
				if (con.CanServe(ord))
				{
					break;
				}
				
				serveIndex++;
			}
			
			if (serveIndex >= output.Count)
			{
				conveyers.RemoveAt(lastIndex);
				if (conveyerIndex >= conveyers.Count)
				{
					conveyerIndex = 0;
				}
				continue;
			}
			
			float sendAmt = Math.Min(thruMod, ord.amount);
			if (sendAmt <= 0f)
			{
				output.RemoveAt(0);
				continue;
			}
			
			LogisticOrder packet = ord.Split(sendAmt);
			if (packet == null)
			{
				break;
			}
			
			con.GiveInput(packet);
			thruMod -= sendAmt;
			
			long seq = ++LogisticOrder.logisticsSequence;
			con.lastServed = seq;
			lastServed = seq;
			
			if (ord.amount <= 0f)
			{
				output.RemoveAt(0);
			}
			
			conveyers.Sort((a, b) => a.lastServed.CompareTo(b.lastServed));
		}
	}
	
	private void TickConveyer(float thruMod)
	{
		CompactOrders();
		
		float inpMod = thruMod;
		int safety = 0;
		
		if (link == null)
		{
			if(!AttemptRelink())
			{
				foreach (var ord in input)
				{
					location.resources[ord.resource] += ord.amount;
				}
				
				input.Clear();
				active = false;
			}
		}
		
		if (link != null)
		{
			while (inpMod > 0f && input.Count > 0 && safety < 10000)
			{
				safety++;
				
				LogisticOrder ord = input[0];
				
				if (ord == null)
				{
					input.RemoveAt(0);
					continue;
				}
				
				float transAmt = Math.Min(inpMod, ord.amount);
				
				if (transAmt <= 0f)
				{
					input.RemoveAt(0);
					continue;
				}
				
				LogisticOrder transPacket = ord.Split(transAmt);
				
				if (transPacket == null)
				{
					break;
				}
				
				link.GiveOutput(transPacket);
				
				inpMod -= transAmt;
			}
		}
		
		List<Infrastructure> convAndHubs = new();
		
		foreach (var infra in location.infrastructure)
		{
			if (infra != null && infra != this)
			{
				foreach (var ord in output)
				{
					if (infra.CanServe(ord))
					{
						convAndHubs.Add(infra);
						break;
					}
				}
			}
		}
		
		if (convAndHubs.Count == 0)
		{
			foreach (var ord in output)
			{
				if (ord.ConsumeHop() && link != null)
				{
					GiveInput(ord);
				}
				else
				{
					location.resources[ord.resource] += ord.amount;
				}
			}
			output.Clear();
			return;
		}
		
		convAndHubs.Sort((a, b) => a.lastServed.CompareTo(b.lastServed));
		
		float outpMod = thruMod;
		
		int infraIndex = 0;
		safety = 0;
		
		while (outpMod > 0f && output.Count > 0 && convAndHubs.Count > 0 && safety < 10000)
		{
			safety++;
			
			Infrastructure infra = convAndHubs[infraIndex];
			int lastIndex = infraIndex;
			infraIndex = (infraIndex + 1) % convAndHubs.Count;
			
			LogisticOrder ord = null;
			
			int serveIndex = 0;
			while (serveIndex < output.Count)
			{
				ord = output[serveIndex];
				if (ord == null)
				{
					output.RemoveAt(0);
					continue;
				}
				
				if (infra.CanServe(ord))
				{
					break;
				}
				
				serveIndex++;
			}
			
			if (serveIndex >= output.Count)
			{
				convAndHubs.RemoveAt(lastIndex);
				if (infraIndex >= convAndHubs.Count)
				{
					infraIndex = 0;
				}
				continue;
			}
			
			float sendAmt = Math.Min(outpMod, ord.amount);
			if (sendAmt <= 0f)
			{
				output.RemoveAt(0);
				continue;
			}
			
			LogisticOrder sendPacket = ord.Split(sendAmt);
			
			if (sendPacket == null)
			{
				break;
			}
			
			infra.GiveInput(sendPacket);
			outpMod -= sendAmt;
			
			long seq = ++LogisticOrder.logisticsSequence;
			infra.lastServed = seq;
			lastServed = seq;
			
			if (ord.amount <= 0f)
			{
				output.RemoveAt(0);
			}
			
			convAndHubs.Sort((a, b) => a.lastServed.CompareTo(b.lastServed));
		}
	}
	
	public void SetLink(Infrastructure lnk)
	{
		link = lnk;
		target = link.location;
	}
	
	private bool AttemptRelink()
	{
		if (target != null)
		{
			bool found = false;
			foreach(var infra in target.infrastructure)
			{
				if (infra.type == "conveyer" && (infra.link == this || infra.link == null) && (infra.target == location || infra.target == null))
				{
					link = infra;
					link.SetLink(this);
					found = true;
					break;
				}
			}
			return found;
		}
		else
		{
			return false;
		}
	}
	
	public void GiveInput(LogisticOrder ord)
	{
		input.Add(ord);
	}
	
	public void GiveOutput(LogisticOrder ord)
	{
		output.Add(ord);
	}
	
	private void CompactOrders()
	{
		for (int i = 0; i < input.Count - 1; i++)
		{
			if (input[i] == null)
			{
				input.RemoveAt(i);
				i--;
				continue;
			}
			
			while (i + 1 < input.Count && input[i + 1] != null && input[i].Matches(input[i + 1]))
			{
				input[i].Merge(input[i + 1]);
				input.RemoveAt(i + 1);
			}
		}
		
		for (int i = 0; i < output.Count - 1; i++)
		{
			if (output[i] == null)
			{
				output.RemoveAt(i);
				i--;
				continue;
			}
			
			while (i + 1 < output.Count && output[i + 1] != null && output[i].Matches(output[i + 1]))
			{
				output[i].Merge(output[i + 1]);
				output.RemoveAt(i + 1);
			}
		}
	}
	
	public bool CanServe(LogisticOrder ord)
	{
		return serves.Contains(ord.phase);
	}
	
	public void DumpBuffers()
	{
		foreach (var log in input)
		{
			location.resources[log.resource] += log.amount;
		}
		input.Clear();
		
		foreach (var log in output)
		{
			location.resources[log.resource] += log.amount;
		}
		output.Clear();
	}
}

public class LogisticOrder
{
	public string resource {get; private set;}
	public string phase {get; private set;}
	public float amount {get; private set;}
	public bool hasDestination {get; private set;}
	public (int x, int y) destinationCoord {get; private set;}
	public int hopLimit {get; private set;}
	public static long logisticsSequence = 0;
	
	public LogisticOrder(string res, float amt, bool has = false, int coordX = 0, int coordY = 0, int ttl = 32)
	{
		resource = res;
		phase = GameData.RESOURCES[res].phase;
		amount = amt;
		hasDestination = has;
		destinationCoord = (coordX, coordY);
		hopLimit = ttl;
	}
	
	public LogisticOrder Split()
	{
		float half = amount / 2f;
		amount -= half;
		
		return new(resource, half, hasDestination, destinationCoord.x, destinationCoord.y, hopLimit);
	}
	
	public LogisticOrder Split(float amt)
	{
		if (amt <= 0f || amt > amount)
		{
			return null;
		}
		
		amount -= amt;
		return new(resource, amt, hasDestination, destinationCoord.x, destinationCoord.y, hopLimit);
	}
	
	public bool Merge(LogisticOrder other)
	{
		if (resource != other.resource || hasDestination != other.hasDestination)
		{
			return false;
		}
		
		if (hasDestination && destinationCoord != other.destinationCoord)
		{
			return false;
		}
		
		amount += other.amount;
		return true;
	}
	
	public bool ConsumeHop()
	{
		return --hopLimit > 0;
	}
	
	public bool Matches(LogisticOrder other)
	{
		if (resource != other.resource) return false;
		if (hasDestination != other.hasDestination) return false;
		if (!hasDestination) return true;
		return destinationCoord == other.destinationCoord;
	}
}

//Android character to represent the player
public class Android
{
	public float maxInventory; //maximum amount of resources android can carry
	public Dictionary<string, float> inventory; //resources carried keyed by resource ID
	
	public Android()
	{
		maxInventory = 5000f;
		inventory = new();
	}
	
	//Return total amount of resources carried by android
	public float AmountCarried()
	{
		float amt = 0f;
		foreach (var res in inventory.Values)
		{
			amt += res;
		}
		
		return amt;
	}
	
	//Return amount of resource in android's inventory if any
	public float GetResource(string res)
	{
		if (inventory.ContainsKey(res))
		{
			return inventory[res];
		}
		else
		{
			return 0f;
		}
	}
	
	//Add resource to android's inventory up to maximum carried and return difference if above maximum
	public float GiveResource(string res, float amt)
	{
		if (amt <= 0f)
		{
			return amt;
		}
		
		float amount = Math.Min(amt, maxInventory - AmountCarried());
		
		if (inventory.ContainsKey(res))
		{
			inventory[res] += amount;
		}
		else
		{
			inventory[res] = amount;
		}
		
		return amt - amount;
	}
	
	//Attempt to take amount of resource from android's inventory and return amount taken from available
	public float TakeResource(string res, float amt)
	{
		if (amt <= 0f)
		{
			return 0f;
		}
		
		if (!inventory.ContainsKey(res))
		{
			return 0f;
		}
		
		float amount = Math.Min(amt, inventory[res]);
		
		inventory[res] -= amount;
		if (inventory[res] <= 0f)
		{
			inventory.Remove(res);
		}
		
		return amount;
	}
}
