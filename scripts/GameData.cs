using Godot;
using System;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public partial class GameData : Node
{
	public static RandomNumberGenerator rng = new();
	
	public static Dictionary<string, ResourceData> RESOURCES = new();
	public static Dictionary<string, HarvestData> HARVEST = new();
	public static Dictionary<string, MachineData> MACHINES = new();
	public static Dictionary<string, RecipeData> RECIPES = new();
	public static Dictionary<string, RegionData> REGIONS = new();
	public static Dictionary<string, QuestData> QUESTS = new();
	
	public static Dictionary<string, string> resNameToKey = new();
	public static Dictionary<string, string> harvActionToKey = new();
	public static Dictionary<string, string> machNameToKey = new();
	public static Dictionary<string, string> recNameToKey = new();
	public static Dictionary<string, string> regNameToKey = new();
	public static Dictionary<string, string> qstNameToKey = new();
	public static Dictionary<string, (int x, int y)> coordStringToTuple = new();
	
	public static Dictionary<(int x, int y), Region> regionMap = new();
	//public static Dictionary<string, float> resources = new();
	//public static List<Machine> machines = new();
	
	public static Region currentRegion;
	
	public static QuestData trackedQuest;
	
	public static TravelControl travelControl;
	public static ResourceControl resourceControl;
	public static MachinesControl machinesControl;
	public static HarvestControl harvestControl;
	public static BuildControl buildControl;
	public static QuestControl questControl;
	
	public static Label objectiveLabel;
	
	private static bool questUpdateFunctioning;
	public static bool unlockAllMachines;
	public static bool unlockAllRecipes;
	
	public override void _Ready()
	{
		GD.Print("GameData._Ready() called from ", GetPath());
		rng = new();
		rng.Randomize();
		
		LoadAll();
		GD.Print("Game data loaded automatically.");
		
		BuildNameMaps();
		
		regionMap = new();
		
		try
		{
			GenerateStartingRegion();
		}
		catch (Exception e)
		{
			GD.PrintErr($"GameData: Error generating region {e.Message}");
		}
		GiveStartingResources();
		GiveStartingMachines();
		
		currentRegion = regionMap[(0, 0)];
		
		coordStringToTuple[CoordToString((0, 0))] = (0, 0);
		
		travelControl = GetNode<TravelControl>("../TabContainer/BaseTab/TravelPanel");
		resourceControl = GetNode<ResourceControl>("../TabContainer/BaseTab/ResourceScroll/VBoxContainer");
		machinesControl = GetNode<MachinesControl>("../TabContainer/BaseTab/MachineScroll/VBoxContainer");
		harvestControl = GetNode<HarvestControl>("../TabContainer/BaseTab/HarvestPanel");
		buildControl = GetNode<BuildControl>("../TabContainer/BaseTab/BuildPanel");
		questControl = GetNode<QuestControl>("../TabContainer/QuestsTab");
		
		objectiveLabel = GetNode<Label>("../TabContainer/BaseTab/QuestPanel/ObjectiveScroll/ObjectiveLabel");
		
		questUpdateFunctioning = true;
		unlockAllMachines = false;
		unlockAllRecipes = false;
		
		UpdateQuestTracking();
	}
	
	public override void _Process(double delta)
	{
		ProcessMachines(delta);
		UpdateQuestTracking();
		CheckQuests();
	}
	
	public static void LoadAll()
	{
		string resourcePath = ProjectSettings.GlobalizePath("res://data/resources.json");
		string harvestPath = ProjectSettings.GlobalizePath("res://data/harvest.json");
		string machinePath = ProjectSettings.GlobalizePath("res://data/machines.json");
		string recipePath = ProjectSettings.GlobalizePath("res://data/recipes.json");
		string regionsPath = ProjectSettings.GlobalizePath("res://data/regions.json");
		string questPath = ProjectSettings.GlobalizePath("res://data/quests.json");
		
		RESOURCES = LoadJson<Dictionary<string, ResourceData>>(resourcePath);
		HARVEST = LoadJson<Dictionary<string, HarvestData>>(harvestPath);
		MACHINES = LoadJson<Dictionary<string, MachineData>>(machinePath);
		RECIPES = LoadJson<Dictionary<string, RecipeData>>(recipePath);
		REGIONS = LoadJson<Dictionary<string, RegionData>>(regionsPath);
		QUESTS = LoadJson<Dictionary<string, QuestData>>(questPath);
	}
	
	public static T LoadJson<T>(string filepath)
	{
		if (!File.Exists(filepath))
		{
			GD.PrintErr($"Missing data file {filepath}");
			return default;
		}
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
		GD.Print("GameData: Generating starting region...");
		(int x, int y) origin = (0, 0);
		regionMap[origin] = new Region(REGIONS["landing zone"], origin);
		GD.Print("GameData: Starting region generated");
	}
	
	public static void GiveStartingResources()
	{
		GD.Print("GameData: Giving Starting Resources...");
		
		foreach(var res in RESOURCES)
		{
			regionMap[(0, 0)].resources[res.Key] = res.Value.startingAmount;
		}
	}
	
	public static void GiveStartingMachines()
	{
		GD.Print("GameData: Giving Starting Machines...");
		
		foreach (var mach in MACHINES)
		{
			for (int i = 0; i < mach.Value.startingAmount; i++)
			{
				regionMap[(0, 0)].machines.Add(new Machine(mach.Key, regionMap[(0, 0)]));
				GD.Print($"GameData: Adding machine {mach.Value.name}");
			}
		}
	}
	
	public static void UpdateQuestTracking()
	{
		if (!questUpdateFunctioning)
		{
			return;
		}
		
		if (questControl != null && trackedQuest != null && trackedQuest.name != "No name")
		{
			String text = trackedQuest.name;
			
			QuestRequirement requirements = trackedQuest.requirement;
			
			if (requirements.resources.Count > 0)
			{
				text += "\n\nResources:";
				
				foreach (var res in requirements.resources)
				{
					ResourceData resource = RESOURCES[res.Key];
					//float available = resources[res.Key];
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
				text += "\n\nMachines:";
				
				foreach (var mach in requirements.machines)
				{
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
				text += "\n\nQuests:";
				
				foreach (var qst in requirements.quests)
				{
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
	
	public static String FormatUnit(float amount, String resource)
	{
		String unit = GameData.RESOURCES.ContainsKey(resource)
			? GameData.RESOURCES[resource].unit
			: "u";
		
		string prefix;
		float display;
		
		if (amount >= 900000)
		{
			prefix = "M";
			display = amount / 1000000f;
		}
		else if (amount >= 900)
		{
			prefix = "k";
			display = amount / 1000f;
		}
		else if (amount == 0 || amount >= 0.9)
		{
			prefix = "";
			display = amount;
		}
		else if (amount >= 0.0009)
		{
			prefix = "m";
			display = amount * 1000f;
		}
		else
		{
			return "Negligible";
		}
		
		return $"{display:0.00} {prefix}{unit}";
	}
	
	public static String CoordToString((int x, int y) coord)
	{
		return $"({coord.x}, {coord.y})";
	}
	
	private void ProcessMachines(double delta)
	{
		foreach (var reg in regionMap)
		{
			foreach (Machine mach in reg.Value.machines)
			{
				if (mach.active && GameData.RECIPES.ContainsKey(mach.currentRecipe))
				{
					float ratio = CanCraft(mach.currentRecipe, mach.location, delta);
					
					if (ratio > 0)
					{
						RecipeData recipe = GameData.RECIPES[mach.currentRecipe];
						Dictionary<String, float> inputs = recipe.inputs;
						
						foreach (var res in inputs)
						{
							reg.Value.resources[res.Key] = Math.Max(0f, reg.Value.resources[res.Key] - res.Value * (float)delta * ratio);
						}
						
						Dictionary<String, float> outputs = recipe.outputs;
						
						foreach (var res in outputs)
						{
							reg.Value.resources[res.Key] += res.Value * (float)delta * ratio;
						}
					}
				}
			}
		}
	}
	
	private float CanCraft(String name, Region reg, double delta)
	{
		if (!GameData.RECIPES.ContainsKey(name))
		{
			return 0f;
		}
		
		RecipeData recipe = GameData.RECIPES[name];
		Dictionary<String, float> inputs = recipe.inputs;
		
		if (inputs.Count == 0)
		{
			return 1f;
		}
		
		List<float> ratios = new();
		
		foreach (var res in inputs)
		{
			if (res.Value <= 0)
			{
				continue;
			}
			
			float available = reg.resources.ContainsKey(res.Key)
				? reg.resources[res.Key]
				: 0f;
			float required = res.Value * (float)delta;
			
			if (required != 0 && available < required)
			{
				ratios.Add(available / required);
			}
		}
		
		if (ratios.Count == 0)
		{
			return 1f;
		}
		
		float minRatio = ratios.Min();
		if (minRatio <= 0f)
		{
			return 0f;
		}
		
		return Math.Clamp(minRatio, 0f, 1f);
	}
	
	public static void CheckQuests()
	{
		List<String> toComplete = new();
		
		foreach (var quest in questControl.activeQuests)
		{
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
		QuestRequirement requirements = quest.requirement;
		Dictionary<String, float> resRequirements = requirements.resources;
		Dictionary<String, int> machRequirements = requirements.machines;
		List<String> qstRequirements = requirements.quests;
		
		//bool resFulfilled = resRequirements.All(res => GameData.resources[res.Key] >= res.Value); //include combined regional resources
		bool resFulfilled = resRequirements.All(req => 
		{
			float total = 0f;
			foreach (var reg in regionMap.Values)
			{
				if (reg.resources.ContainsKey(req.Key))
				{
					total += reg.resources[req.Key];
					if (total >= req.Value)
					{
						return true;
					}
				}
			}
			return total >= req.Value;
		});
		//bool machFulfilled = machRequirements.All(mach => GameData.machines.Count(m => m.id == mach.Key) >= mach.Value); //include combined regional machines
		bool machFulfilled = machRequirements.All(req =>
		{
			int total = 0;
			foreach (var reg in regionMap.Values)
			{
				total += reg.machines.Count(m => m.id == req.Key);
				if (total >= req.Value)
				{
					return true;
				}
			}
			return total >= req.Value;
		});
		
		bool qstFulfilled = qstRequirements.All(qst => questControl.completeQuests.ContainsKey(qst));
		
		return resFulfilled && machFulfilled && qstFulfilled;
	}
	
	public static void CompleteQuest(String questKey)
	{
		if (questControl.completeQuests.ContainsKey(questKey))
		{
			return;
		}
		
		QuestData quest = QUESTS[questKey];
		questControl.activeQuests.Remove(questKey);
		questControl.completeQuests[questKey] = quest;
		
		GD.Print($"Completing quest {quest.name}");
		
		QuestUnlock unlocks = quest.unlocks;
		
		foreach (var q in unlocks.quests)
		{
			if (!questControl.activeQuests.ContainsKey(q))
			{
				GD.Print($"Unlock quest {QUESTS[q].name}");
				questControl.activeQuests[q] = QUESTS[q];
			}
		}
		
		foreach (var rec in unlocks.recipes)
		{
			GD.Print($"Unlock recipe {RECIPES[rec].name}");
			RECIPES[rec].available = true;
		}
		
		foreach (var mach in unlocks.machines)
		{
			GD.Print($"Unlock machine {MACHINES[mach].name}");
			MACHINES[mach].available = true;
		}
		
		GD.Print("GameData: Updating menus...");
		
		questControl.UpdateQuestLists();
		
		GD.Print("GameData: checking unlocked quests...");
		CheckQuests();
		
		GD.Print("GameData: Calling buildControl.UpdateBuildMenu()");
		buildControl.UpdateBuildMenu();
		GD.Print("GameData: Calling machinesControl.UpdateMachinePanels()");
		machinesControl.UpdateMachinePanels();
		
		GD.Print($"-- GameData: Finished quest completion events for quest {quest.name} --");
	}
	
	public static void TravelTo((int x, int y) coord)
	{
		if (regionMap.ContainsKey(coord))
		{
			currentRegion = regionMap[coord];
		}
		
		travelControl.UpdateRegions();
		travelControl.DisplayFeatures();
	}
	
	public static void ExploreRegion((int x, int y) coord)
	{
		if (regionMap.ContainsKey(coord))
		{
			GD.Print($"GameData: Region {coord} already explored");
			return;
		}
		
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
		
		Dictionary<String, float> weightedBiomes = new();
		String selectedBiome = "nowhere";
		String largestBiome = "nowhere";
		float largestValue = 0f;
		
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
		
		float total = 0f;
		foreach (var w in weightedBiomes.Values)
		{
			total += w;
		}
		
		float roll = rng.Randf() * total;
		
		float cummulative = 0f;
		foreach (var w in weightedBiomes)
		{
			cummulative += w.Value;
			if (cummulative >= roll)
			{
				selectedBiome = w.Key;
				break;
			}
		}
		
		if (cummulative >= total)
		{
			selectedBiome = largestBiome;
		}
		
		if (selectedBiome == "nowhere")
		{
			GD.Print($"GameData: Error generating region; Total: {total}, Roll: {roll}, Cummulative: {cummulative}");
			return;
		}
		
		Region explored = new Region(REGIONS[selectedBiome], coord);
		GD.Print($"GameData: Adding new region {explored.regData.name} at {coord}");
		regionMap[coord] = explored;
		coordStringToTuple[CoordToString(coord)] = coord;
		GD.Print("GameData: Successfully added new region");
		String regionsList = "";
		foreach (var c in regionMap.Keys)
		{
			regionsList += $"{CoordToString(c)} ";
		}
		GD.Print($"GameData: Explored regions {regionsList}");
		
		travelControl.UpdateRegions();
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
	public string description {get; set;}
	
	public ResourceData()
	{
		name = "No Name";
		abbreviation = "N/A";
		type = "untyped";
		subtypes = new List<string>();
		phase = "intangible";
		unit = "u";
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

public class MachineData
{
	public string name {get; set;}
	public Dictionary<string, float> cost {get; set;}
	
	[System.Text.Json.Serialization.JsonPropertyName("starting amount")]
	public int startingAmount {get; set;}
	
	public bool available {get; set;}
	public string description {get; set;}
	
	public MachineData()
	{
		name = "No Name";
		cost = new Dictionary<string, float>();
		startingAmount = 0;
		available = false;
		description = "No description.";
	}
}

public class RecipeData
{
	public string name {get; set;}
	public Dictionary<string, float> inputs {get; set;}
	public Dictionary<string, float> outputs {get; set;}
	public bool available {get; set;}
	public List<string> machines {get; set;}
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

public class Region
{
	public RegionData regData;
	public int coordX;
	public int coordY;
	
	public Dictionary<string, float> resources;
	public List<Machine> machines;
	public List<String> nodes;
	
	public Region(RegionData data, (int x, int y) coord)
	{
		GD.Print($"GameData: Generating region at ({coord.x}, {coord.y})");
		regData = data;
		coordX = coord.x;
		coordY = coord.y;
		
		resources = new();
		machines = new();
		nodes = new();
		
		foreach (var res in regData.resources)
		{
			if (res.Value >= 1f || GameData.rng.Randf() < res.Value)
			{
				GD.Print($"Adding resource node {GameData.RESOURCES[res.Key].name}");
				nodes.Add(res.Key);
			}
		}
		
		foreach (var res in GameData.RESOURCES)
		{
			resources[res.Key] = 0f;
		}
	}
	
	public bool IsAdjacent(Region reg)
	{
		int vectX = Math.Abs(coordX - reg.coordX);
		int vectY = Math.Abs(coordY - reg.coordY);
		
		return vectX == 1 && vectY == 0 || vectX == 0 && vectY == 1;
	}
	
	public bool IsDiagonal(Region reg)
	{
		int vectX = Math.Abs(coordX - reg.coordX);
		int vectY = Math.Abs(coordY - reg.coordY);
		
		return vectX == 1 && vectY == 1;
	}
}

public class Machine
{
	public string id {get; private set;}
	public bool active {get; private set;}
	public List<string> recipes {get; private set;} = new();
	public string currentRecipe {get; private set;}
	public Region location {get; private set;}
	
	public Machine(string machineID, Region loc)
	{
		id = machineID;
		location = loc;
		active = false;
		
		recipes = new();
		foreach (var rec in GameData.RECIPES)
		{
			if (rec.Value.machines.Contains(machineID))
			{
				recipes.Add(rec.Key);
			}
		}
		
		if (recipes.Count > 0)
		{
			currentRecipe = recipes[0];
		}
		else
		{
			currentRecipe = "";
		}
	}
	
	public void ToggleActive(bool on)
	{
		active = on;
	}
	
	public void SetRecipe(string rid)
	{
		currentRecipe = rid;
	}
}
