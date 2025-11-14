using Godot;
using System;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;

public partial class GameData : Node
{
	public static Dictionary<string, ResourceData> RESOURCES = new();
	public static Dictionary<string, HarvestData> HARVEST = new();
	public static Dictionary<string, MachineData> MACHINES = new();
	public static Dictionary<string, RecipeData> RECIPES = new();
	public static Dictionary<string, QuestData> QUESTS = new();
	
	public static Dictionary<string, string> resNameToKey = new();
	public static Dictionary<string, string> harvActionToKey = new();
	public static Dictionary<string, string> machNameToKey = new();
	public static Dictionary<string, string> recNameToKey = new();
	public static Dictionary<string, string> qstNameToKey = new();
	
	public static Dictionary<string, float> resources = new();
	public static List<Machine> machines = new();
	
	public static ResourceControl resourceControl;
	public static MachinesControl machinesControl;
	public static HarvestControl harvestControl;
	
	public override void _Ready()
	{
		GD.Print("GameData._Ready() called from ", GetPath());
		LoadAll();
		GD.Print("Game data loaded automatically.");
		
		BuildNameMaps();
		
		GiveStartingResources();
		GiveStartingMachines();
		
		resourceControl = GetNode<ResourceControl>("../TabContainer/BaseTab/ResourceScroll/VBoxContainer");
		machinesControl = GetNode<MachinesControl>("../TabContainer/BaseTab/MachineScroll/VBoxContainer");
		harvestControl = GetNode<HarvestControl>("../TabContainer/BaseTab/HarvestPanel");
		//var machinesControl = GetNode<MachinesControl>("../TabContainer/Base/MachineScroll/VBoxContainer");
		//machinesControl.AddStartingMachines();
	}
	
	public override void _Process(double delta)
	{
		ProcessMachines(delta);
	}
	
	public static void LoadAll()
	{
		string resourcePath = ProjectSettings.GlobalizePath("res://data/resources.json");
		string harvestPath = ProjectSettings.GlobalizePath("res://data/harvest.json");
		string machinePath = ProjectSettings.GlobalizePath("res://data/machines.json");
		string recipePath = ProjectSettings.GlobalizePath("res://data/recipes.json");
		string questPath = ProjectSettings.GlobalizePath("res://data/quests.json");
		
		RESOURCES = LoadJson<Dictionary<string, ResourceData>>(resourcePath);
		HARVEST = LoadJson<Dictionary<string, HarvestData>>(harvestPath);
		MACHINES = LoadJson<Dictionary<string, MachineData>>(machinePath);
		RECIPES = LoadJson<Dictionary<string, RecipeData>>(recipePath);
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
		foreach (var qst in QUESTS)
		{
			qstNameToKey[qst.Value.name] = qst.Key;
		}
	}
	
	public static void GiveStartingResources()
	{
		GD.Print("GameData: Giving Starting Resources...");
		
		foreach(var res in RESOURCES)
		{
			resources[res.Key] = res.Value.startingAmount;
		}
	}
	
	public static void GiveStartingMachines()
	{
		GD.Print("GameData: Giving Starting Machines...");
		
		foreach (var mach in MACHINES)
		{
			for (int i = 0; i < mach.Value.startingAmount; i++)
			{
				machines.Add(new Machine(mach.Key));
				GD.Print($"GameData: Adding machine {mach.Value.name}");
			}
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
		
		return $"{display:0.##} {prefix}{unit}";
	}
	
	private void ProcessMachines(double delta)
	{
		foreach (Machine mach in machines)
		{
			if (mach.active && GameData.RECIPES.ContainsKey(mach.currentRecipe))
			{
				float ratio = CanCraft(mach.currentRecipe, delta);
				
				if (ratio > 0)
				{
					RecipeData recipe = GameData.RECIPES[mach.currentRecipe];
					Dictionary<String, float> inputs = recipe.inputs;
					
					foreach (var res in inputs)
					{
						GameData.resources[res.Key] = Math.Max(0f, GameData.resources[res.Key] - res.Value * (float)delta * ratio);
					}
					
					Dictionary<String, float> outputs = recipe.outputs;
					
					foreach (var res in outputs)
					{
						GameData.resources[res.Key] += res.Value * (float)delta * ratio;
					}
				}
			}
		}
	}
	
	private float CanCraft(String name, double delta)
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
			
			float available = GameData.resources.ContainsKey(res.Key)
				? GameData.resources[res.Key]
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

public class Machine
{
	public string id {get; private set;}
	public bool active {get; private set;}
	public List<string> recipes {get; private set;} = new();
	public string currentRecipe {get; private set;}
	
	public Machine(string machineID)
	{
		id = machineID;
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
