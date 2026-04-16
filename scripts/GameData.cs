using Godot;
using System;
using System.Text.Json;
using System.IO;
using System.Collections.Generic;
using System.Linq;

//The main control of the game containing the persistent data.
public partial class GameData : Node
{
	[Export] public PackedScene ResourceLabelSceneRef;
	public static PackedScene ResourceLabelScene;
	
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
	public static Dictionary<string, TraderData> TRADERS = new();
	
	//Dictionaries to reference user facing strings to IDs in the above dictionaries.
	public static Dictionary<string, string> resNameToKey = new();
	public static Dictionary<string, string> harvActionToKey = new();
	public static Dictionary<string, string> machNameToKey = new();
	public static Dictionary<string, string> infraNameToKey = new();
	public static Dictionary<string, string> recNameToKey = new();
	public static Dictionary<string, string> regNameToKey = new();
	public static Dictionary<string, string> qstNameToKey = new();
	public static Dictionary<string, (int x, int y)> coordStringToTuple = new();
	
	//Android representing player character
	public static Android android;
	
	//Region map referenced by keys of 2D tuples as coordinates.
	public static Dictionary<(int x, int y), Region> regionMap = new();
	
	public static Dictionary<string, List<LogisticsNetwork>> networks = new();
	
	public static Dictionary<int, Trader> traders = new();
	//public static List<Trader> landedTraders = new();
	
	//The region the player is currently in.
	public static Region currentRegion;
	
	//The currently tracked quest.
	public static QuestData trackedQuest;
	
	public static Dictionary<string, CommodityStock> galMarket;
	
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
	public static TradeControl tradeControl;
	public static QuestControl questControl;
	
	//Label to display the current tracked objective.
	public static Label objectiveLabel;
	
	public static VBoxContainer marketVBox;
	public static ItemList tradersList;
	public static RichTextLabel traderInfoLabel;
	public static Button callButton;
	
	public static Button newButton;
	public static Button saveButton;
	public static Button loadButton;
	public static ConfirmationDialog newConfirm;
	public static FileDialog saveDialog;
	public static FileDialog loadDialog;
	
	//Button to quit the game.
	public static Button quitButton;
	public static ConfirmationDialog quitConfirm;
	
	//delete after test
	public static AcceptDialog testDialog;
	
	public static CheckBox unlockMachinesCheck;
	public static CheckBox unlockRecipesCheck;
	public static CheckBox disableWearCheck;
	public static CheckBox disableStorageCheck;
	public static CheckBox disableSizeCheck;
	
	//A variable to test if a feature is functioning (possibly no longer necessary).
	private static bool questUpdateFunctioning;
	
	//Variables set in development for test purposes.
	public static bool unlockAllMachines;
	public static bool unlockAllRecipes;
	public static bool disableWear;
	public static bool disableStorage;
	public static bool disableSize;
	
	public override void _Ready()
	{
		GD.Print("GameData._Ready() called from ", GetPath());
		
		ResourceLabelScene = ResourceLabelSceneRef;
		
		//Initialize the RNG.
		rng = new();
		rng.Randomize();
		
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
		tradeControl = GetNode<TradeControl>("../TabContainer/Trade");
		questControl = GetNode<QuestControl>("../TabContainer/Quests");
		
		marketVBox = GetNode<VBoxContainer>("../TabContainer/Communication/MarketScroll/MarketVBox");
		tradersList = GetNode<ItemList>("../TabContainer/Communication/TradersList");
		traderInfoLabel = GetNode<RichTextLabel>("../TabContainer/Communication/TraderInfoLabel");
		callButton = GetNode<Button>("../TabContainer/Communication/CallButton");
		tradersList.ItemSelected += DisplayTraderInfo;
		callButton.Pressed += OnCallPressed;
		
		objectiveLabel = GetNode<Label>("../TabContainer/Base/QuestPanel/ObjectiveScroll/ObjectiveLabel");
		
		GD.Print("-- GameData: Assigning Option tab buttons... --");
		newButton = GetNode<Button>("../TabContainer/Options/NewButton");
		saveButton = GetNode<Button>("../TabContainer/Options/SaveButton");
		loadButton = GetNode<Button>("../TabContainer/Options/LoadButton");
		newConfirm = GetNode<ConfirmationDialog>("../TabContainer/Options/NewConfirm");
		saveDialog = GetNode<FileDialog>("../TabContainer/Options/SaveDialog");
		loadDialog = GetNode<FileDialog>("../TabContainer/Options/LoadDialog");
		newButton.Pressed += OnNewPressed;
		saveButton.Pressed += OnSavePressed;
		loadButton.Pressed += OnLoadPressed;
		newConfirm.Confirmed += NewConfirmed;
		saveDialog.FileSelected += SaveConfirmed;
		loadDialog.FileSelected += LoadConfirmed;
		
		quitButton = GetNode<Button>("../TabContainer/Options/QuitButton");
		quitConfirm = GetNode<ConfirmationDialog>("../TabContainer/Options/QuitConfirm");
		quitButton.Pressed += QuitGame;
		quitConfirm.Confirmed += QuitConfirmed;
		
		//delete after test
		testDialog = GetNode<AcceptDialog>("../TabContainer/Options/TestDialog");
		
		unlockMachinesCheck = GetNode<CheckBox>("../TabContainer/Options/UnlockMachinesCheck");
		unlockRecipesCheck = GetNode<CheckBox>("../TabContainer/Options/UnlockRecipesCheck");
		disableWearCheck = GetNode<CheckBox>("../TabContainer/Options/DisableWearCheck");
		disableStorageCheck = GetNode<CheckBox>("../TabContainer/Options/DisableStorageCheck");
		disableSizeCheck = GetNode<CheckBox>("../TabContainer/Options/DisableSizeCheck");
		unlockMachinesCheck.Toggled += ToggleUnlockMachines;
		unlockRecipesCheck.Toggled += ToggleUnlockRecipes;
		disableWearCheck.Toggled += ToggleWear;
		disableStorageCheck.Toggled += ToggleStorage;
		disableSizeCheck.Toggled += ToggleSize;
		
		//Possibly unnecessary test variable.
		questUpdateFunctioning = true;
		
		//Set these to true to unlock all machines or recipes.
		unlockAllMachines = false;
		unlockAllRecipes = false;
		disableWear = false;
		disableStorage = false;
		disableSize = false;
		
		//Generate the map.
		regionMap = new();
		
		galMarket = new();
		
		CallDeferred(nameof(NewGameDeferred));
		
		/*try
		{
			NewGame();
		}
		catch (Exception e)
		{
			GD.PrintErr($"GameData: Error starting new game - {e}");
		}*/
		
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
		
		try
		{
			foreach (var comm in galMarket.Values)
			{
				comm.Tick(delta);
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"GameData: Error ticking galactic market - {e.Message}");
		}
		
		try
		{
			DisplayMaxStorage();
		}
		catch (Exception e)
		{
			GD.PrintErr($"GameData: Error updating max storage - {e.Message}");
		}
		
		try
		{
			UpdateQuestTracking();
		}
		catch (Exception e)
		{
			GD.PrintErr($"GameData: Quest tracking failed {e.Message}");
		}
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
		string traderPath = "res://data/traders.json";
		
		//Assign data to reference variables.
		RESOURCES = LoadJson<Dictionary<string, ResourceData>>(resourcePath);
		HARVEST = LoadJson<Dictionary<string, HarvestData>>(harvestPath);
		MACHINES = LoadJson<Dictionary<string, MachineData>>(machinePath);
		INFRASTRUCTURE = LoadJson<Dictionary<string, InfrastructureData>>(infrastructurePath);
		RECIPES = LoadJson<Dictionary<string, RecipeData>>(recipePath);
		REGIONS = LoadJson<Dictionary<string, RegionData>>(regionsPath);
		QUESTS = LoadJson<Dictionary<string, QuestData>>(questPath);
		TRADERS = LoadJson<Dictionary<string, TraderData>>(traderPath);
	}
	
	public void DisplayTraderInfo(long idx)
	{
		int tradID = (int)tradersList.GetItemMetadata((int)idx);
		Trader trad = traders[tradID];
		
		String info = $"Trader: {trad.name}\nType: {trad.id}\n\nFavor: {trad.favor}\nProsperity: {trad.prosperity}";
		traderInfoLabel.Text = info;
	}
	
	public void OnCallPressed()
	{
		if (!currentRegion.ContainsCommunication() || !currentRegion.ContainsTrade() || !currentRegion.ContainsDock())
		{
			GD.Print($"GameData: cannot call trader; missing antenna, trade hub, or landing platform");
			return;
		}
		
		int pickTrader = rng.RandiRange(0, traders.Count);
		Trader trad;
		
		GD.Print($"GameData: number {pickTrader} picked out of {traders.Count} traders");
		
		if (traders.Count != 0 && pickTrader < traders.Count)
		{
			List<int> tradIDs = traders.Keys.ToList();
			trad = traders[tradIDs[pickTrader]];
			
			GD.Print($"GameData: Trader \"{trad.name}\" picked");
		}
		else
		{
			int idx = -1;
			string tradID = "[N/A]";
			try
			{
				idx = rng.RandiRange(0, TRADERS.Count - 1);
				tradID = TRADERS.Keys.ToList()[idx];
				trad = new Trader(tradID);
				traders[trad.idNum] = trad;
			}
			catch (Exception e)
			{
				GD.PrintErr($"GameData: error selecting new trader - idx: {idx}, tradID {tradID} {e}");
				return;
			}
			
			tradersList.AddItem(trad.name);
			int tradIdx = tradersList.ItemCount - 1;
			tradersList.SetItemMetadata(tradIdx, trad.idNum);
			
			GD.Print($"GameData: New trader called {trad.name}");
		}
		
		if (currentRegion.landedTraders.Count < currentRegion.DocksAvailable())
		{
			trad.GenerateInventory();
			trad.GenerateSnapshot();
			
			currentRegion.landedTraders.Add(trad);
			
			if (tradeControl.activeTrader == null)
			{
				tradeControl.activeTrader = trad;
				tradeControl.UpdateTraderResourceLabels();
			}
		}
		
		UpdateMarket();
	}
	
	public void OnNewPressed()
	{
		newConfirm.PopupCentered();
	}
	
	public void NewConfirmed()
	{
		NewGame();
	}
	
	public void NewGameDeferred()
	{
		try
		{
			NewGame();
		}
		catch (Exception e)
		{
			GD.PrintErr($"GameData: Error starting new game - {e}");
		}
	}
	
	public void NewGame()
	{
		//Load all the JSON data.
		LoadAll();
		GD.Print("Game data loaded automatically.");
		
		//Create quick references between names and IDs.
		BuildNameMaps();
		
		//Reset the PC android.
		android = new();
		androidControl.UpdateAndroid();
		traders = new();
		//landedTraders = new();
		
		//Reset the map.
		regionMap.Clear();
		
		galMarket.Clear();
		
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
		//machinesControl.AddStartingMachines();
		//machinesControl.AddStartingInfrastructure();
		
		//Assign the player's initial location as the center of the map.
		currentRegion = regionMap[(0, 0)];
		travelControl.AssignCoordinateReferences();
		
		coordStringToTuple.Clear();
		coordStringToTuple[CoordToString((0, 0))] = (0, 0);
		
		questControl.GiveStartingQuests();
		GenerateGalMarket();
		
		trackedQuest = null;
		LogisticOrder.logisticsSequence = -1;
		
		SortResources(currentRegion.resources);
		resourceControl.UpdateResourcePanels();
		machinesControl.UpdateRegionMachines();
		machinesControl.UpdateRegionInfrastructure();
		machinesControl.UpdateMachinePanels();
		buildControl.UpdateBuildMenu();
		buildControl.UpdateNeighborMenu();
		harvestControl.UpdateHarvest();
		logisticsControl.PopulateResourceMenu();
		logisticsControl.UpdateRegionLogistics();
		mapControl.GenerateMap();
		mapControl.UpdateAllColors();
		UpdateQuestTracking();
		questControl.UpdateQuestLists();
		DisplayMaxStorage();
		
		travelControl.ResetMenuSelect();
		harvestControl.ResetMenuSelect();
		buildControl.ResetMenuSelect();
		logisticsControl.logResourceMenu.Select(0);
		logisticsControl.logResourceSpin.Value = 0;
		androidControl.resourceMenu.Select(0);
		androidControl.transferSpin.Value = 0;
	}
	
	public void SaveGame(string filename = "user://saves/save.json")
	{
		try
		{
			GameSave save = new GameSave();
			
			string json = JsonSerializer.Serialize(save, new JsonSerializerOptions{
				WriteIndented = true
			});
			
			using var file = Godot.FileAccess.Open(filename, Godot.FileAccess.ModeFlags.Write);
			if (file == null)
			{
				GD.PrintErr($"GameData: Save error - FileAccess.Open returned null {filename}");
				return;
			}
			
			file.StoreString(json);
			
			GD.Print($"GameData: Game saved successfully to {filename}");
		}
		catch (Exception e)
		{
			GD.PrintErr($"GameData: Save failed - {e}");
		}
	}
	
	public void LoadGame(string filename = "user://saves/save.json")
	{
		//string path = filename.StartsWith("res://") || filename.StartsWith("user://") ? filename : "user:/" + filename;
		
		if (!Godot.FileAccess.FileExists(filename))
		{
			GD.PrintErr("GameData: Save file does not exist");
			return;
		}
		
		try
		{
			using var file = Godot.FileAccess.Open(filename, Godot.FileAccess.ModeFlags.Read);
			string json = file.GetAsText();
			
			GameSave save = JsonSerializer.Deserialize<GameSave>(json);
			
			ApplySave(save);
			
			GD.Print($"GameData: Game loaded successfully from {filename}");
		}
		catch (Exception e)
		{
			GD.PrintErr($"GameData: Load failed - {e.Message}");
		}
	}
	
	public void ApplySave(GameSave save)
	{
		android = new Android(save.android);
		androidControl.UpdateAndroid();
		
		traders.Clear();
		foreach (var trad in save.traders)
		{
			Trader newTrad = new Trader(trad);
			traders[newTrad.idNum] = newTrad;
		}
		
		regionMap.Clear();
		coordStringToTuple.Clear();
		foreach (var reg in save.regionMap)
		{
			regionMap[(reg.coordX, reg.coordY)] = new Region(reg);
			coordStringToTuple[$"({reg.coordX}, {reg.coordY})"] = (reg.coordX, reg.coordY);
		}
		
		currentRegion = regionMap[(save.currentX, save.currentY)];
		
		foreach (var reg in regionMap.Values)
		{
			reg.BuildFromSave();
		}
		
		foreach (var reg in regionMap.Values)
		{
			reg.LinkFromSave();
		}
		
		galMarket.Clear();
		foreach (var commSave in save.galMarket)
		{
			try
			{
				galMarket[commSave.resource] = new(commSave);
			}
			catch (Exception e)
			{
				GD.PrintErr($"GameData: Error loading market commodity - {commSave.resource} {e.Message}");
				if (GameData.RESOURCES.ContainsKey(commSave.resource))
				{
					galMarket[commSave.resource] = new(commSave.resource);
				}
			}
		}
		
		foreach (var rec in GameData.RECIPES)
		{
			rec.Value.available = save.unlocks.recipes.Contains(rec.Key);
		}
		
		foreach (var mach in GameData.MACHINES)
		{
			mach.Value.available = save.unlocks.machines.Contains(mach.Key);
		}
		
		foreach (var infra in GameData.INFRASTRUCTURE)
		{
			infra.Value.available = save.unlocks.infrastructure.Contains(infra.Key);
		}
		
		tradeControl.LoadSave(save.tradeSave);
		
		trackedQuest = (!string.IsNullOrEmpty(save.trackedQuest)) && QUESTS.ContainsKey(save.trackedQuest)
			? GameData.QUESTS[save.trackedQuest]
			: null;
		
		questControl.LoadFromSave(save.activeQuests, save.completeQuests);
		
		LogisticOrder.logisticsSequence = save.logisticsSequence;
		
		SortResources(currentRegion.resources);
		resourceControl.UpdateResourcePanels();
		machinesControl.UpdateRegionMachines();
		machinesControl.UpdateRegionInfrastructure();
		machinesControl.UpdateMachinePanels();
		buildControl.UpdateBuildMenu();
		logisticsControl.UpdateRegionLogistics();
		travelControl.UpdateRegions();
		mapControl.GenerateMap();
		mapControl.UpdateAllColors();
		UpdateQuestTracking();
	}
	
	public void OnSavePressed()
	{
		saveDialog.PopupCentered();
	}
	
	public void OnLoadPressed()
	{
		loadDialog.PopupCentered();
	}
	
	public void SaveConfirmed(string path)
	{
		SaveGame(path);
		
		testDialog.DialogText = "Save: " + path;
		testDialog.PopupCentered();
	}
	
	public void LoadConfirmed(string path)
	{
		LoadGame(path);
		
		testDialog.DialogText = "Load: " + path;
		testDialog.PopupCentered();
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
	
	public void ToggleSize(bool isChecked)
	{
		disableSize = isChecked;
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
		resNameToKey.Clear();
		foreach (var res in RESOURCES)
		{
			resNameToKey[res.Value.name] = res.Key;
		}
		harvActionToKey.Clear();
		foreach (var harv in HARVEST)
		{
			harvActionToKey[harv.Value.action] = harv.Key;
		}
		machNameToKey.Clear();
		foreach (var mach in MACHINES)
		{
			machNameToKey[mach.Value.name] = mach.Key;
		}
		infraNameToKey.Clear();
		foreach (var infra in INFRASTRUCTURE)
		{
			infraNameToKey[infra.Value.name] = infra.Key;
		}
		recNameToKey.Clear();
		foreach (var rec in RECIPES)
		{
			recNameToKey[rec.Value.name] = rec.Key;
		}
		regNameToKey.Clear();
		foreach (var reg in REGIONS)
		{
			regNameToKey[reg.Value.name] = reg.Key;
		}
		qstNameToKey.Clear();
		foreach (var qst in QUESTS)
		{
			qstNameToKey[qst.Value.name] = qst.Key;
		}
	}
	
	public static void GenerateGalMarket()
	{
		GD.Print("GameData: Generating Galactic Market...");
		
		galMarket.Clear();
		
		foreach (var res in GameData.RESOURCES.Keys)
		{
			galMarket[res] = new(res);
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
			if (res.Value.startingAmount > 0)
			{
				regionMap[(0, 0)].resources[res.Key] = res.Value.startingAmount;
			}
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
		/*if (!questUpdateFunctioning)
		{
			return;
		}*/
		
		//Check if questControl and trackedQuest are functioning and a quest is being tracked.
		if (questControl != null && trackedQuest != null && trackedQuest.name != "No name")
		{
			//Reference quest's name and requirements.
			string text = trackedQuest.name;
			
			if (IsQuestFulfilled(trackedQuest))
			{
				text += "\n\nCOMPLETED!";
			}
			
			QuestRequirement requirements = trackedQuest.requirement;
			
			try
			{
				if (requirements.resources.Count > 0)
				{
					//Display required resources if any.
					text += "\n\nResources:";
					
					foreach (var res in requirements.resources)
					{
						ResourceData resource = null;
						//Calculate available resources total among all regions and display along with required amounts.
						if (RESOURCES.ContainsKey(res.Key))
						{
							resource = RESOURCES[res.Key];
						}
						else
						{
							objectiveLabel.Text = $"Tracking failed\n{res.Key} not in RESOURCES dictionary";
							GD.PrintErr($"GameData: Quest tracking failed - {res.Key} not in RESOURCES dictionary");
							trackedQuest = null;
							return;
						}
						float available = 0f;
						
						foreach (var reg in regionMap)
						{
							if (reg.Value.resources.ContainsKey(res.Key))
							{
								available += reg.Value.resources[res.Key];
							}
						}
						
						text += $"\n{resource.name}: {FormatUnit(available, res.Key)} / {FormatUnit(res.Value, res.Key)}";
					}
				}
			}
			catch (Exception e)
			{
				text += $"\nError counting resources\n{e.Message}";
			}
			
			try
			{
				if (requirements.machines.Count > 0)
				{
					//Display required machines if any.
					text += "\n\nMachines:";
					
					foreach (var mach in requirements.machines)
					{
						int machineCount = 0;
						//Count all constructed machines among all regions and display along with required amount.
						if (MACHINES.ContainsKey(mach.Key))
						{
							MachineData machine = MACHINES[mach.Key];
							
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
						else if (INFRASTRUCTURE.ContainsKey(mach.Key))
						{
							InfrastructureData infrastructure = INFRASTRUCTURE[mach.Key];
							
							foreach (var reg in regionMap)
							{
								foreach (Infrastructure mach2 in reg.Value.infrastructure)
								{
								if (mach2.id == mach.Key)
									{
										machineCount++;
									}
								}
							}
							
							text += $"\n{infrastructure.name}: {machineCount} / {mach.Value}";
						}
						else
						{
							text += $"\nMachine key not valid: {mach.Key}";
						}
					}
				}
			}
			catch (Exception e)
			{
				text += $"\nError counting machines\n{e.Message}";
			}
			
			try
			{
				if (requirements.quests.Count > 0)
				{
					//Display required quests completed if any.
					text += "\n\nQuests:";
					
					foreach (var qst in requirements.quests)
					{
						//Display quests' current status whether active, completed, or somehow both.
						QuestData quest = QUESTS[qst];
						string questState;
						
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
			}
			catch (Exception e)
			{
				text += $"\nError counting quests\n{e.Message}";
			}
			
			objectiveLabel.Text = text;
		}
		else
		{
			string text = "No quest tracked";
			if (questControl == null)
			{
				text += "\nquestControl null";
			}
			if (trackedQuest == null)
			{
				text += "\ntrackedQuest null";
			}
			if (trackedQuest != null && trackedQuest.name == "No name")
			{
				text += "\ntrackedQuest name \"No name\"";
			}
			objectiveLabel.Text = text;
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
			if (GameData.MACHINES.ContainsKey(mach))
			{
				GD.Print($"Unlock machine {MACHINES[mach].name}");
				MACHINES[mach].available = true;
			}
			else if (GameData.INFRASTRUCTURE.ContainsKey(mach))
			{
				GD.Print($"Unlock infrastructure {INFRASTRUCTURE[mach].name}");
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
		
		tradeControl.RetractAllOffers();
		
		//Set current region to coordinate if it exists in the map.
		if (regionMap.ContainsKey(coord))
		{
			currentRegion = regionMap[coord];
		}
		
		//Update displays.
		SortResources(currentRegion.resources);
		resourceControl.UpdateResourcePanels();
		travelControl.UpdateRegions();
		travelControl.DisplayFeatures();
		
		//Update the list of machines to ones constructed in the new region.
		try
		{
			machinesControl.UpdateRegionMachines();
			machinesControl.UpdateRegionInfrastructure();
		}
		catch (Exception e)
		{
			GD.PrintErr($"GameData: Error updating machines {e.Message}");
		}
		
		logisticsControl.UpdateRegionLogistics();
		tradeControl.UpdateRegionTrade();
		
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
		if (selectedBiome == "nowhere" && cummulative >= total)
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
		
		SortRegions(regionMap);
		
		string regionsList = "";
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
	
	public static void SortResources(Dictionary<string, float> resources)
	{
		Dictionary<string, float> clone = new Dictionary<string, float>(resources);
		List<string> resKeys = resources.Keys.ToList();
		
		resKeys.Sort(CompareResources);
		
		resources.Clear();
		
		foreach (var res in resKeys)
		{
			resources[res] = clone[res];
		}
	}
	
	public static void SortRegions(Dictionary<(int x, int y), Region> map)
	{
		Dictionary<(int x, int y), Region> clone = new Dictionary<(int x, int y), Region>(map);
		List<(int x, int y)> coordKeys = map.Keys.ToList();
		
		coordKeys.Sort(CompareCoords);
		
		map.Clear();
		
		foreach (var coord in coordKeys)
		{
			map[coord] = clone[coord];
		}
	}
	
	public static int CompareResources(string x, string y)
	{
		ResourceData resX = GameData.RESOURCES[x];
		ResourceData resY = GameData.RESOURCES[y];
		
		for (int i = 0; i < resX.sort.Count; i++)
		{
			if (resY.sort.Count <= i)
			{
				return resX.sort[i];
			}
			
			if (resX.sort[i] != resY.sort[i])
			{
				return resX.sort[i] - resY.sort[i];
			}
		}
		
		if (resX.sort.Count < resY.sort.Count)
		{
			return -(resY.sort[resX.sort.Count]);
		}
		
		return 0;
	}
	
	public static int CompareMachines(Machine x, Machine y)
	{
		MachineData machX = GameData.MACHINES[x.id];
		MachineData machY = GameData.MACHINES[y.id];
		
		for (int i = 0; i < machX.sort.Count; i++)
		{
			if (machY.sort.Count < machX.sort.Count)
			{
				return machX.sort[i];
			}
			
			if (machX.sort[i] != machY.sort[i])
			{
				return machX.sort[i] - machY.sort[i];
			}
		}
		
		if (machX.sort.Count < machY.sort.Count)
		{
			return -(machY.sort[machX.sort.Count]);
		}
		
		return 0;
	}
	
	public static int CompareInfrastructure(Infrastructure x, Infrastructure y)
	{
		InfrastructureData infraX = GameData.INFRASTRUCTURE[x.id];
		InfrastructureData infraY = GameData.INFRASTRUCTURE[y.id];
		
		for (int i = 0; i < infraX.sort.Count; i++)
		{
			if (infraY.sort.Count < infraX.sort.Count)
			{
				return infraX.sort[i];
			}
			
			if (infraX.sort[i] != infraY.sort[i])
			{
				return infraX.sort[i] - infraY.sort[i];
			}
		}
		
		if (infraX.sort.Count < infraY.sort.Count)
		{
			return -(infraY.sort[infraX.sort.Count]);
		}
		
		return 0;
	}
	
	public static int CompareCoords((int x, int y) x, (int x, int y) y)
	{
		if (x.y == y.y)
		{
			return x.x - y.x;
		}
		else
		{
			return -(x.y - y.y);
		}
	}
	
	public static float RandNormal(float mean, float stddev)
	{
		float u1 = Mathf.Max(rng.Randf(), 1e-7f);
		float u2 = rng.Randf();
		
		return mean + stddev * Mathf.Sqrt(-2f * Mathf.Log(u1)) * Mathf.Cos(2f * Mathf.Pi * u2);
	}
	
	public static void UpdateMarket()
	{
		foreach (var child in marketVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		foreach (var comm in galMarket)
		{
			Control rLabel = ResourceLabelScene.Instantiate<Control>();
			Label nLabel = rLabel.GetNode<Label>("HBoxContainer/NameLabel");
			Label aLabel = rLabel.GetNode<Label>("HBoxContainer/AmountLabel");
			
			nLabel.Text = RESOURCES[comm.Key].name;
			aLabel.Text = $"{comm.Value.GetMarketValue():0.00}";
			
			marketVBox.AddChild(rLabel);
		}
	}
}

public class GameSave
{
	public AndroidSave android {get; set;}
	
	public List<TraderSave> traders {get; set;}
	
	public List<RegionSave> regionMap {get; set;}
	public int currentX {get; set;}
	public int currentY {get; set;}
	
	public List<CommoditySave> galMarket {get; set;}
	
	public UnlockSave unlocks {get; set;}
	
	public TradeSave tradeSave {get; set;}
	
	public string trackedQuest {get; set;}
	public List<string> activeQuests {get; set;}
	public List<string> completeQuests {get; set;}
	
	public long logisticsSequence {get; set;}
	
	public GameSave()
	{
		android = new(GameData.android);
		
		traders = new();
		foreach (var trad in GameData.traders.Values)
		{
			traders.Add(new TraderSave(trad));
		}
		
		regionMap = new();
		foreach (var reg in GameData.regionMap.Values)
		{
			regionMap.Add(new RegionSave(reg));
		}
		
		currentX = GameData.currentRegion.coordX;
		currentY = GameData.currentRegion.coordY;
		
		galMarket = new();
		foreach (var comm in GameData.galMarket.Values)
		{
			galMarket.Add(new(comm));
		}
		
		unlocks = new();
		
		foreach (var rec in GameData.RECIPES)
		{
			if (rec.Value.available)
			{
				unlocks.recipes.Add(rec.Key);
			}
		}
		
		foreach (var mach in GameData.MACHINES)
		{
			if (mach.Value.available)
			{
				unlocks.machines.Add(mach.Key);
			}
		}
		
		foreach (var infra in GameData.INFRASTRUCTURE)
		{
			if (infra.Value.available)
			{
				unlocks.infrastructure.Add(infra.Key);
			}
		}
		
		tradeSave = new(GameData.tradeControl);
		
		if (GameData.trackedQuest != null && GameData.qstNameToKey.ContainsKey(GameData.trackedQuest.name))
		{
			trackedQuest = GameData.qstNameToKey[GameData.trackedQuest.name];
		}
		else
		{
			trackedQuest = "";
		}
		
		activeQuests = GameData.questControl.activeQuests.Keys.ToList();
		completeQuests = GameData.questControl.completeQuests.Keys.ToList();
		
		logisticsSequence = LogisticOrder.logisticsSequence;
	}
}

public class UnlockSave
{
	public List<string> recipes {get; set;}
	public List<string> machines {get; set;}
	public List<string> infrastructure {get; set;}
	
	public UnlockSave()
	{
		recipes = new();
		machines = new();
		infrastructure = new();
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
	
	public Dictionary<string, float> scrap {get; set;}
	public float value {get; set;}
	public float liquidity {get; set;}
	public List<int> sort {get; set;}
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
		value = 0f;
		liquidity = 5000f;
		sort = new();
		description = "No description";
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
	public string type {get; set;}
	public Dictionary<string, float> cost {get; set;}
	public float durability {get; set;}
	public float size {get; set;}
	
	[System.Text.Json.Serialization.JsonPropertyName("starting amount")]
	public int startingAmount {get; set;}
	
	public bool available {get; set;}
	public List<int> sort {get; set;}
	public string description {get; set;}
	
	protected BuildData()
	{
		name = "No Name";
		type = "untyped";
		cost = new Dictionary<string, float>();
		durability = 1f;
		size = 1f;
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
	public List<string> serves {get; set;}
	public float through {get; set;}
	
	[System.Text.Json.Serialization.JsonPropertyName("energy cost")]
	public float energyCost {get; set;}
	
	public InfrastructureData() : base()
	{
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
	public string name {get; set;}
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

public class TraderData
{
	public Dictionary<string, float> preferences {get; set;}
	public Dictionary<string, float> catalog {get; set;}
	public float frequency {get; set;}
	public float greed {get; set;}
	public float landingFee {get; set;}
	
	public TraderData()
	{
		preferences = new();
		catalog = new();
		frequency = 0f;
		greed = 1f;
		landingFee = 0f;
	}
}

//A geographical region within the game world.
public class Region
{
	public RegionData regData;
	public int coordX;
	public int coordY;
	
	public float space;
	
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
	public Dictionary<string, LogisticsNetwork> localNetworks;
	public List<Trader> landedTraders;
	
	public RegionSave lastSave {get; private set;}
	
	public Region(RegionData data, (int x, int y) coord)
	{
		//Generate a new region using region data and the coordinate of the region.
		GD.Print($"GameData: Generating region at ({coord.x}, {coord.y})");
		regData = data;
		coordX = coord.x;
		coordY = coord.y;
		
		space = 100 / regData.roughness;
		
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
		localNetworks = new();
		landedTraders = new();
		
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
			//resources[res.Key] = 0f;
			
			if (!maxStorage.ContainsKey(res.Value.phase))
			{
				maxStorage[res.Value.phase] = (0f, res.Value.unit);
			}
		}
		
		UpdateStorage();
		
		lastSave = null;
	}
	
	public Region(RegionSave save)
	{
		regData = GameData.REGIONS[save.id];
		coordX = save.coordX;
		coordY = save.coordY;
		
		space = 100 / regData.roughness;
		
		windMax = (regData.elevation * 0.2f) + regData.pressure + regData.roughness;
		windK = regData.roughness * 0.1f;
		windSigma = regData.roughness * 0.5f;
		wind = windMax / 2;
		windState = save.windState;
		
		solarMax = (regData.elevation * 0.2f) + (1.0f - regData.pressure) + (1.0f - regData.roughness);
		solarK = regData.roughness * 0.1f;
		solarSigma = regData.roughness * 0.5f;
		solar = solarMax / 2;
		solarState = save.solarState;
		
		resources = new(save.resources);
		maxStorage = new();
		foreach (var res in GameData.RESOURCES)
		{
			if (!maxStorage.ContainsKey(res.Value.phase))
			{
				maxStorage[res.Value.phase] = (0f, res.Value.unit);
			}
		}
		
		machines = new();
		/*foreach (var mach in save.machines)
		{
			machines.Add(new Machine(mach));
		}*/
		
		infrastructure = new();
		/*foreach (var infra in save.infrastructure)
		{
			infrastructure.Add(new Infrastructure(infra));
		}*/
		
		nodes = new(save.nodes);
		
		localNetworks = new();
		
		landedTraders = new();
		foreach (var trad in save.landedTraders)
		{
			landedTraders.Add(GameData.traders[trad]);
		}
		
		UpdateStorage();
		
		lastSave = save;
	}
	
	public void BuildFromSave()
	{
		foreach (var mach in lastSave.machines)
		{
			machines.Add(new Machine(mach));
		}
		
		foreach (var infra in lastSave.infrastructure)
		{
			infrastructure.Add(new Infrastructure(infra));
		}
		
		UpdateStorage();
	}
	
	public void LinkFromSave()
	{
		foreach (var infra in infrastructure)
		{
			if (infra.type == "conveyer")
			{
				infra.LinkFromSave();
			}
		}
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
	
	public void AddNetwork(LogisticsNetwork network)
	{
		string phase = network.phaseServed;
		
		if (!localNetworks.ContainsKey(phase))
		{
			localNetworks[phase] = network;
			return;
		}
		
		if (localNetworks[phase].IsConnectedTo(network))
		{
			localNetworks[phase].MergeNetworks(network);
		}
	}
	
	public void RemoveNetwork(LogisticsNetwork network)
	{
		string phase = network.phaseServed;
		
		if (localNetworks.ContainsKey(phase) && ReferenceEquals(localNetworks[phase], network))
		{
			localNetworks.Remove(phase);
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
		return Math.Max(0f, maxStorage[phase].amount - TotalStored(phase));
	}
	
	public float SpaceOccupied()
	{
		float total = 0f;
		foreach (var mach in machines)
		{
			total += mach.size;
		}
		foreach (var infra in infrastructure)
		{
			total += infra.size;
		}
		
		return total;
	}
	
	public float SpaceAvailable()
	{
		return Math.Max(0f, space - SpaceOccupied());
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
	
	public int DocksAvailable()
	{
		int docks = 0;
		foreach (var infra in infrastructure)
		{
			if (infra.type == "dock")
			{
				docks++;
			}
		}
		
		return docks;
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
	
	public bool ContainsCommunication()
	{
		foreach (var infra in infrastructure)
		{
			if (infra.type == "communication")
			{
				return true;
			}
		}
		
		return false;
	}
	
	public bool ContainsTrade()
	{
		foreach (var infra in infrastructure)
		{
			if (infra.type == "trade")
			{
				return true;
			}
		}
		
		return false;
	}
	
	public bool ContainsDock()
	{
		foreach (var infra in infrastructure)
		{
			if (infra.type == "dock")
			{
				return true;
			}
		}
		
		return false;
	}
}

public class RegionSave
{
	public int coordX {get; set;}
	public int coordY {get; set;}
	public string id {get; set;}
	public float windState {get; set;}
	public float solarState {get; set;}
	
	public Dictionary<string, float> resources {get; set;}
	public List<MachineSave> machines {get; set;}
	public List<InfrastructureSave> infrastructure {get; set;}
	public List<string> nodes {get; set;}
	public List<int> landedTraders {get; set;}
	
	public RegionSave()
	{
		coordX = 0;
		coordY = 0;
		id = "";
		windState = 0f;
		solarState = 0f;
		
		resources = new();
		machines = new();
		infrastructure = new();
		nodes = new();
		landedTraders = new();
	}
	
	public RegionSave(Region reg)
	{
		coordX = reg.coordX;
		coordY = reg.coordY;
		
		RegionData regData = reg.regData;
		id = GameData.regNameToKey[regData.name];
		
		windState = reg.windState;
		solarState = reg.solarState;
		
		resources = new(reg.resources);
		
		machines = new();
		foreach (var mach in reg.machines)
		{
			machines.Add(new MachineSave(mach));
		}
		
		infrastructure = new();
		foreach (var infra in reg.infrastructure)
		{
			infrastructure.Add(new InfrastructureSave(infra));
		}
		
		nodes = new(reg.nodes);
		
		landedTraders = new();
		foreach (var trad in reg.landedTraders)
		{
			landedTraders.Add(trad.idNum);
		}
	}
}

public class Buildable
{
	//A parent class for buildable elements such as machines.
	public string id {get; protected set;}
	public string name {get; protected set;}
	public bool active {get; protected set;}
	public float wear {get; protected set;}
	public float durability {get; protected set;}
	public float size {get; protected set;}
	public List<int> sort {get; protected set;}
	public float diagnosedWear {get; protected set;}
	public float maxWear {get; protected set;}
	public Dictionary<string, float> repairComponents {get; protected set;} = new();
	public Region location {get; protected set;}
	
	protected Buildable(string buildableID, Region loc)
	{
		id = buildableID;
		name = "No name";
		location = loc;
		active = false;
		
		wear = 0f;
		durability = 1f;
		size = 1f;
		sort = new();
		
		if (GameData.MACHINES.ContainsKey(id))
		{
			name = GameData.MACHINES[id].name;
			durability = GameData.MACHINES[id].durability;
			size = GameData.MACHINES[id].size;
			sort = GameData.MACHINES[id].sort;
		}
		else if (GameData.INFRASTRUCTURE.ContainsKey(id))
		{
			name = GameData.INFRASTRUCTURE[id].name;
			durability = GameData.INFRASTRUCTURE[id].durability;
			size = GameData.INFRASTRUCTURE[id].size;
			sort = GameData.INFRASTRUCTURE[id].sort;
		}
		
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
		float modDmg = dmg / durability;
		wear = Math.Min(wear + modDmg, maxWear);
	}
	
	public void Repair()
	{
		//Repair the machine, consume repair components, and create recyclable scrap.
		float total = 0f;
		
		foreach (var compKey in repairComponents.Keys.ToList())
		{
			float needed = repairComponents[compKey];
			//Determine how much of the required resource is available at the machine's location.
			float available = location.resources.ContainsKey(compKey) ? location.resources[compKey] : 0f;
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
				if (location.resources.ContainsKey(compKey))
				{
					location.resources.Remove(compKey);
				}
				//location.resources[compKey] = 0;
				total += available;
				scrap = available;
			}
			
			foreach (var sc in GameData.RESOURCES[compKey].scrap)
			{
				//For each resource used to craft the replaced component, produce the associated scrap component.
				float scrapAmount = scrap * sc.Value;
				
				if (location.resources.ContainsKey(sc.Key))
				{
					location.resources[sc.Key] += scrapAmount;
				}
				else if (scrapAmount > 0f)
				{
					location.resources[sc.Key] = scrapAmount;
				}
			}
		}
		
		//Reduce wear and diagnosed wear by the total amount repaired.
		wear -= total;
		diagnosedWear -= total;
		
		GameData.SortResources(location.resources);
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
		if (!(GameData.MACHINES.ContainsKey(id) || GameData.INFRASTRUCTURE.ContainsKey(id)))
		{
			return;
		}
		
		if (GameData.MACHINES.ContainsKey(id) && !GameData.MACHINES[id].available || GameData.INFRASTRUCTURE.ContainsKey(id) && !GameData.INFRASTRUCTURE[id].available)
		{
			return;
		}
		
		if (diagnosedWear < wear)
		{
			Diagnose();
		}
		
		bool updateResources = false;
		Dictionary<string, float> resources = location.resources;
		
		if (GameData.MACHINES.ContainsKey(id))
		{
			foreach (var res in GameData.MACHINES[id].cost)
			{
				GD.Print($"GameData: Checking if working components {GameData.RESOURCES[res.Key].name}");
				if (repairComponents.TryGetValue(res.Key, out float comp))
				{
					if (resources.ContainsKey(res.Key))
					{
						resources[res.Key] += res.Value - comp;
					}
					else if (res.Value - comp > 0f)
					{
						resources[res.Key] = res.Value - comp;
						updateResources = true;
					}
				}
				else
				{
					if (resources.ContainsKey(res.Key))
					{
						resources[res.Key] += res.Value;
					}
					else if (res.Value > 0f)
					{
						resources[res.Key] = res.Value;
						updateResources = true;
					}
				}
			}
		}
		else if (GameData.INFRASTRUCTURE.ContainsKey(id))
		{
			foreach (var res in GameData.INFRASTRUCTURE[id].cost)
			{
				GD.Print($"GameData: Checking if working components {GameData.RESOURCES[res.Key].name}");
				if (repairComponents.TryGetValue(res.Key, out float comp))
				{
					if (resources.ContainsKey(res.Key))
					{
						resources[res.Key] += res.Value - comp;
					}
					else if (res.Value - comp > 0f)
					{
						resources[res.Key] = res.Value - comp;
						updateResources = true;
					}
				}
				else
				{
					if (resources.ContainsKey(res.Key))
					{
						resources[res.Key] += res.Value;
					}
					else if (res.Value > 0f)
					{
						resources[res.Key] = res.Value;
						updateResources = true;
					}
				}
			}
		}
		
		foreach (var comp in repairComponents)
		{
			foreach (var scrap in GameData.RESOURCES[comp.Key].scrap)
			{
				if (resources.ContainsKey(scrap.Key))
				{
					resources[scrap.Key] += scrap.Value * comp.Value;
				}
				else if (scrap.Value * comp.Value > 0f)
				{
					resources[scrap.Key] = scrap.Value * comp.Value;
					updateResources = true;
				}
			}
		}
		
		if (updateResources && location == GameData.currentRegion)
		{
			GameData.SortResources(location.resources);
			GameData.resourceControl.UpdateResourcePanels();
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
	public MachineSave lastSave {get; private set;}
	
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
		
		lastSave = null;
	}
	
	public Machine(MachineSave save) : base(save.id, GameData.regionMap[(save.coordX, save.coordY)])
	{
		MachineData data = GameData.MACHINES[id];
		active = save.active;
		
		wear = save.wear;
		
		diagnosedWear = save.diagnosedWear;
		repairComponents = new(save.repairComponents);
		maxWear = 0f;
		foreach (var res in data.cost.Values)
		{
			maxWear += res;
		}
		
		//Assign recipes based on which ones are assigned to this machine.
		recipes = GameData.RECIPES.Where(r => r.Value.machines.Contains(id)).OrderBy(r => r.Key).Select(r => r.Key).ToList();
		
		if (!string.IsNullOrEmpty(save.currentRecipe) && GameData.RECIPES.ContainsKey(save.currentRecipe))
		{
			currentRecipe = save.currentRecipe;
		}
		else if (recipes.Count > 0)
		{
			currentRecipe = recipes[0];
		}
		else
		{
			currentRecipe = "";
		}
		
		outputBuffer = new(save.outputBuffer);
		
		lastSave = save;
	}
	
	public void SetRecipe(string rid)
	{
		//Select the current recipe to process.
		currentRecipe = rid;
	}
	
	public override void Tick(double delta)
	{
		bool updateResources = false;
		
		if (active && (GameData.disableStorage || outputBuffer.Count == 0) && (GameData.disableWear || wear < maxWear) && GameData.RECIPES.ContainsKey(currentRecipe))
		{
			//Create a ratio value based on if recipe can be crafted.
			float ratio = CanCraft(delta);
			
			if (ratio > 0f)
			{
				//Create references to recipe and input resources.
				RecipeData recipe = GameData.RECIPES[currentRecipe];
				Dictionary<string, float> inputs = recipe.inputs;
				List<string> removed = new();
				
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
					if (location.resources[res.Key] <= 0f)
					{
						removed.Add(res.Key);
						updateResources = true;
					}
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
						
						if (location.resources.ContainsKey(res.Key))
						{
							location.resources[res.Key] += stored;
						}
						else if (stored > 0f)
						{
							location.resources[res.Key] = stored;
							updateResources = true;
						}
						
						float remainder = outputVolume - stored;
						
						if (remainder > 0f)
						{
							if (outputBuffer.ContainsKey(res.Key))
							{
								outputBuffer[res.Key] += remainder;
							}
							else if (remainder > 0f)
							{
								outputBuffer[res.Key] = remainder;
							}
						}
					}
					else
					{
						if (location.resources.ContainsKey(res.Key))
						{
							location.resources[res.Key] += outputVolume;
						}
						else
						{
							location.resources[res.Key] = outputVolume;
							updateResources = true;
						}
					}
				}
				
				foreach (var res in removed)
				{
					if (location.resources[res] <= 0f)
					{
						location.resources.Remove(res);
						updateResources = true;
					}
				}
			}
			
			//Damage the machine according to the ratio.
			if (!GameData.disableWear)
			{
				Damage(1f * ratio);
			}
		}
		
		List<string> removeBuffer = new();
		foreach (var res in outputBuffer)
		{
			string resPhase = GameData.RESOURCES[res.Key].phase;
			if (!GameData.disableStorage && location.TotalAvailable(resPhase) > 0f)
			{
				float stored = Math.Min(res.Value, location.TotalAvailable(resPhase));
				
				if (location.resources.ContainsKey(res.Key))
				{
					location.resources[res.Key] += stored;
				}
				else if (stored > 0f)
				{
					location.resources[res.Key] = stored;
					updateResources = true;
				}
				
				outputBuffer[res.Key] -= stored;
				
				if (outputBuffer[res.Key] <= 0f)
				{
					removeBuffer.Add(res.Key);
				}
			}
			else if (GameData.disableStorage)
			{
				if (location.resources.ContainsKey(res.Key))
				{
					location.resources[res.Key] += res.Value;
				}
				else if (res.Value > 0f)
				{
					location.resources[res.Key] = res.Value;
					updateResources = true;
				}
				removeBuffer.Add(res.Key);
			}
		}
		
		foreach (var res in removeBuffer)
		{
			outputBuffer.Remove(res);
		}
		
		if (updateResources && location == GameData.currentRegion)
		{
			GameData.SortResources(location.resources);
			GameData.resourceControl.UpdateResourcePanels();
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

public class MachineSave
{
	public string id {get; set;}
	public bool active {get; set;}
	public float wear {get; set;}
	public float diagnosedWear {get; set;}
	
	public Dictionary<string, float> repairComponents {get; set;}
	public int coordX {get; set;}
	public int coordY {get; set;}
	
	public string currentRecipe {get; set;}
	public Dictionary<string, float> outputBuffer {get; set;}
	
	public MachineSave()
	{
		id = "";
		active = false;
		wear = 0f;
		diagnosedWear = 0f;
		
		repairComponents = new();
		coordX = 0;
		coordY = 0;
		
		currentRecipe = "";
		outputBuffer = new();
	}
	
	public MachineSave(Machine mach)
	{
		id = mach.id;
		active = mach.active;
		wear = mach.wear;
		diagnosedWear = mach.diagnosedWear;
		
		repairComponents = new(mach.repairComponents);
		coordX = mach.location.coordX;
		coordY = mach.location.coordY;
		
		currentRecipe = mach.currentRecipe;
		outputBuffer = new(mach.outputBuffer);
	}
}

public class Infrastructure : Buildable
{
	public string type {get; private set;}
	public List<string> serves {get; private set;}
	public float through {get; private set;}
	public float energyCost {get; private set;}
	public Infrastructure link {get; private set;}
	public Dictionary<string, LogisticsNetwork> localNetworks {get; private set;}
	public Region target {get; private set;}
	public List<LogisticOrder> input {get; private set;}
	public List<LogisticOrder> output {get; private set;}
	public long lastServed {get; private set;}
	
	public InfrastructureSave lastSave {get; private set;}
	
	public Infrastructure(string infraID, Region loc) : base(infraID, loc)
	{
		InfrastructureData data = GameData.INFRASTRUCTURE[infraID];
		type = data.type;
		serves = data.serves;
		through = data.through;
		energyCost = data.energyCost;
		link = null;
		localNetworks = new();
		target = null;
		input = new();
		output = new();
		lastServed = -1;
		
		lastSave = null;
		
		if (type == "hub" || type == "conveyer")
		{
			foreach (var phase in serves)
			{
				if (location.localNetworks.ContainsKey(phase))
				{
					AddNetwork(location.localNetworks[phase]);
					localNetworks[phase].RegisterInfrastructure(this);
				}
				else
				{
					(int x, int y) coord = (location.coordX, location.coordY);
					LogisticsNetwork network = new(coord, phase);
					AddNetwork(network);
					location.AddNetwork(network);
					
					if (!GameData.networks.ContainsKey(phase))
					{
						GameData.networks[phase] = new();
					}
					
					GameData.networks[phase].Add(network);
					
					network.RegisterInfrastructure(this);
				}
			}
		}
	}
	
	public Infrastructure(InfrastructureSave save) : base(save.id, GameData.regionMap[(save.coordX, save.coordY)])
	{
		InfrastructureData data = GameData.INFRASTRUCTURE[id];
		active = save.active;
		
		wear = save.wear;
		
		diagnosedWear = save.diagnosedWear;
		repairComponents = new(save.repairComponents);
		maxWear = 0f;
		
		foreach (var res in data.cost.Values)
		{
			maxWear += res;
		}
		
		type = data.type;
		serves = data.serves;
		through = data.through;
		energyCost = data.energyCost;
		
		link = null;
		localNetworks = new();
		target = null;
		
		input = new();
		foreach (var order in save.input)
		{
			input.Add(new LogisticOrder(order));
		}
		
		output = new();
		foreach (var order in save.output)
		{
			output.Add(new LogisticOrder(order));
		}
		
		lastServed = save.lastServed;
		
		lastSave = save;
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
		bool updateResources = false;
		
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
				else if (ord.amount > 0f)
				{
					location.resources[ord.resource] = ord.amount;
					updateResources = true;
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
				
				if (ord.hasDestination && location.localNetworks.ContainsKey(ord.phase) && ord.destinationCoord == (location.coordX, location.coordY))
				{
					con = this;
					
					conveyerIndex = conveyerIndex == 0 ? conveyers.Count - 1 : conveyerIndex - 1;
				}
				else if (ord.hasDestination && location.localNetworks.ContainsKey(ord.phase))
				{
					LogisticsNetwork network = location.localNetworks[ord.phase];
					
					List<Infrastructure> path = network.FindRoute(this, ord.destinationCoord, new HashSet<Infrastructure>());
					
					int idx = 0;
					
					while (!ReferenceEquals(this, path[idx]))
					{
						idx++;
					}
					
					con = path[idx];
					
					conveyerIndex = conveyerIndex == 0 ? conveyers.Count - 1 : conveyerIndex - 1;
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
		
		if (updateResources && location == GameData.currentRegion)
		{
			GameData.SortResources(location.resources);
			GameData.resourceControl.UpdateResourcePanels();
		}
	}
	
	private void TickConveyer(float thruMod)
	{
		bool updateResources = false;
		
		CompactOrders();
		
		float inpMod = thruMod;
		int safety = 0;
		
		if (link == null)
		{
			if(!AttemptRelink())
			{
				foreach (var ord in input)
				{
					if (location.resources.ContainsKey(ord.resource))
					{
						location.resources[ord.resource] += ord.amount;
					}
					else if (ord.amount > 0f)
					{
						location.resources[ord.resource] = ord.amount;
						updateResources = true;
					}
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
					if ((infra.type == "hub" || infra.type == "conveyer") && infra.CanServe(ord))
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
					if (location.resources.ContainsKey(ord.resource))
					{
						location.resources[ord.resource] += ord.amount;
					}
					else if (ord.amount > 0f)
					{
						location.resources[ord.resource] = ord.amount;
						updateResources = true;
					}
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
				
				if (ord.hasDestination && location.localNetworks.ContainsKey(ord.phase) && ord.destinationCoord == (location.coordX, location.coordY))
				{
					LogisticsNetwork network = location.localNetworks[ord.phase];
					
					List<Infrastructure> hubs = network.hubsByNode[ord.destinationCoord];
					
					infra = hubs[0];
					
					infraIndex = infraIndex == 0 ? convAndHubs.Count - 1 : infraIndex - 1;
				}
				else if (ord.hasDestination && location.localNetworks.ContainsKey(ord.phase))
				{
					LogisticsNetwork network = location.localNetworks[ord.phase];
					
					List<Infrastructure> path = network.FindRoute(this, ord.destinationCoord, new HashSet<Infrastructure>());
					
					int idx = 0;
					
					while (!ReferenceEquals(this, path[idx]))
					{
						idx++;
					}
					
					infra = path[idx];
					
					infraIndex = infraIndex == 0 ? convAndHubs.Count - 1 : infraIndex - 1;
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
		
		if (updateResources && location == GameData.currentRegion)
		{
			GameData.SortResources(location.resources);
			GameData.resourceControl.UpdateResourcePanels();
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
	
	public void LinkFromSave()
	{
		if (type == "conveyer" && GameData.regionMap.ContainsKey((lastSave.targetX, lastSave.targetY)))
		{
			target = GameData.regionMap[(lastSave.targetX, lastSave.targetY)];
			
			if (lastSave.linkIdx >= 0 && lastSave.linkIdx < target.infrastructure.Count && target.infrastructure[lastSave.linkIdx].type == "conveyer" && target.infrastructure[lastSave.linkIdx].name == name)
			{
				link = target.infrastructure[lastSave.linkIdx];
			}
			else
			{
				for (int i = 0; i < target.infrastructure.Count; i++)
				{
					Infrastructure infra = target.infrastructure[i];
					if (infra.link == this || infra.link == null)
					{
						link = infra;
						break;
					}
				}
			}
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
	
	public void AddNetwork(LogisticsNetwork network)
	{
		string phase = network.phaseServed;
		
		if (!localNetworks.ContainsKey(phase))
		{
			localNetworks[phase] = network;
			return;
		}
		
		if (localNetworks[phase].IsConnectedTo(network))
		{
			localNetworks[phase].MergeNetworks(network);
		}
	}
	
	public void RemoveNetwork(LogisticsNetwork network)
	{
		string phase = network.phaseServed;
		
		if (localNetworks.ContainsKey(phase) && ReferenceEquals(localNetworks[phase], network))
		{
			localNetworks.Remove(phase);
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

public class InfrastructureSave
{
	public string id {get; set;}
	public bool active {get; set;}
	public float wear {get; set;}
	public float diagnosedWear {get; set;}
	
	public Dictionary<string, float> repairComponents {get; set;}
	public int coordX {get; set;}
	public int coordY {get; set;}
	
	public int linkIdx {get; set;}
	public int targetX {get; set;}
	public int targetY {get; set;}
	
	public List<LogisticSave> input {get; set;}
	public List<LogisticSave> output {get; set;}
	public long lastServed {get; set;}
	
	public InfrastructureSave()
	{
		id = "";
		active = false;
		wear = 0f;
		diagnosedWear = 0f;
		
		repairComponents = new();
		coordX = 0;
		coordY = 0;
		
		linkIdx = -1;
		targetX = 0;
		targetY = 0;
		
		input = new();
		output = new();
		lastServed = 0;
	}
	
	public InfrastructureSave(Infrastructure infra)
	{
		id = infra.id;
		active = infra.active;
		wear = infra.wear;
		diagnosedWear = infra.diagnosedWear;
		
		repairComponents = new(infra.repairComponents);
		coordX = infra.location.coordX;
		coordY = infra.location.coordY;
		
		if (infra.type == "conveyer")
		{
			linkIdx = -1;
			if (infra.link != null)
			{
				Region linkLocation = infra.link.location;
				
				for (int i = 0; i < linkLocation.infrastructure.Count; i++)
				{
					if (linkLocation.infrastructure[i] == infra.link)
					{
						linkIdx = i;
						break;
					}
				}
				
				targetX = linkLocation.coordX;
				targetY = linkLocation.coordY;
				
				if (linkIdx == -1)
				{
					GD.PrintErr($"InfrastructureSave: conveyer link not found at ({linkLocation.coordX}, {linkLocation.coordY})");
				}
			}
			else
			{
				if (infra.target != null)
				{
					targetX = infra.target.coordX;
					targetY = infra.target.coordY;
				}
				else
				{
					targetX = 0;
					targetY = 0;
				}
				
				GD.PrintErr($"InfrastructureSave: conveyer {infra.name} at ({infra.location.coordX}, {infra.location.coordY}) link is null");
			}
		}
		else
		{
			linkIdx = -1;
			targetX = 0;
			targetY = 0;
		}
		
		input = new();
		foreach (var order in infra.input)
		{
			input.Add(new LogisticSave(order));
		}
		
		output = new();
		foreach (var order in infra.output)
		{
			output.Add(new LogisticSave(order));
		}
		
		lastServed = infra.lastServed;
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
	
	public LogisticSave lastSave;
	
	public static long logisticsSequence = 0;
	
	public LogisticOrder(string res, float amt, bool has = false, int coordX = 0, int coordY = 0, int ttl = 32)
	{
		resource = res;
		phase = GameData.RESOURCES[res].phase;
		amount = amt;
		hasDestination = has;
		destinationCoord = (coordX, coordY);
		hopLimit = ttl;
		
		lastSave = null;
	}
	
	public LogisticOrder(LogisticSave save)
	{
		resource = save.resource;
		phase = GameData.RESOURCES[resource].phase;
		amount = save.amount;
		hasDestination = save.hasDestination;
		destinationCoord = (save.destinX, save.destinY);
		hopLimit = save.hopLimit;
		
		lastSave = save;
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

public class LogisticSave
{
	public string resource {get; set;}
	public float amount {get; set;}
	public bool hasDestination {get; set;}
	public int destinX {get; set;}
	public int destinY {get; set;}
	public int hopLimit {get; set;}
	
	public LogisticSave()
	{
		resource = "";
		amount = 0f;
		hasDestination = false;
		destinX = 0;
		destinY = 0;
		hopLimit = 0;
	}
	
	public LogisticSave(LogisticOrder order)
	{
		resource = order.resource;
		amount = order.amount;
		hasDestination = order.hasDestination;
		destinX = order.destinationCoord.x;
		destinY = order.destinationCoord.y;
		hopLimit = order.hopLimit;
	}
}

public class LogisticsNetwork
{
	private HashSet<(int x, int y)> nodeSet;
	private HashSet<Infrastructure> hubSet;
	private HashSet<Infrastructure> conveyerSet;
	public Dictionary<(int x, int y), List<Infrastructure>> hubsByNode {get; set;}
	public Dictionary<Infrastructure, List<Infrastructure>> adjacency {get; set;}
	public string phaseServed {get; set;}
	
	public LogisticsNetwork()
	{
		nodeSet = new();
		hubSet = new();
		conveyerSet = new();
		hubsByNode = new();
		adjacency = new();
		phaseServed = "";
	}
	
	public LogisticsNetwork((int x, int y) start, string phase)
	{
		nodeSet = new();
		hubSet = new();
		conveyerSet = new();
		hubsByNode = new();
		adjacency = new();
		phaseServed = phase;
		
		BuildFromNode(start);
	}
	
	public List<(int x, int y)> Nodes => nodeSet.ToList();
	public List<Infrastructure> Hubs => hubSet.ToList();
	public List<Infrastructure> Conveyers => conveyerSet.ToList();
	
	public void BuildFromNode((int x, int y) start)
	{
		if (!GameData.regionMap.ContainsKey(start))
		{
			return;
		}
		
		Queue<(int x, int y)> frontier = new();
		HashSet<(int x, int y)> visited = new();
		frontier.Enqueue(start);
		
		while (frontier.Count > 0)
		{
			(int x, int y) node = frontier.Dequeue();
			
			if (!visited.Add(node))
			{
				continue;
			}
			
			AddNode(node);
			Region reg = GameData.regionMap[node];
			
			foreach (var infra in reg.infrastructure)
			{
				if (infra.type != "conveyer" || !IsNetworkCompatible(infra))
				{
					continue;
				}
				
				if (infra.link != null)
				{
					(int x, int y) target = (infra.link.location.coordX, infra.link.location.coordY);
					
					if (!visited.Contains(target))
					{
						frontier.Enqueue(target);
					}
				}
			}
		}
	}
	
	public void AddNode((int x, int y) node)
	{
		if (!GameData.regionMap.ContainsKey(node))
		{
			return;
		}
		
		nodeSet.Add(node);
		Region reg = GameData.regionMap[node];
		
		if (!hubsByNode.ContainsKey(node))
		{
			hubsByNode[node] = new();
		}
		
		foreach (var infra in reg.infrastructure)
		{
			if (!IsNetworkCompatible(infra))
			{
				continue;
			}
			
			RegisterInfrastructure(infra);
			
			if (infra.type == "hub")
			{
				foreach (var infra2 in reg.infrastructure)
				{
					if (infra2.type == "conveyer" && IsNetworkCompatible(infra2))
					{
						RegisterInfrastructure(infra2);
						AddEdge(infra, infra2);
					}
				}
			}
			else if (infra.type == "conveyer")
			{
				if (infra.link != null)
				{
					RegisterInfrastructure(infra.link);
					AddEdge(infra, infra.link);
				}
				
				foreach (var infra2 in reg.infrastructure)
				{
					if ((infra2.type == "hub" || infra2.type == "conveyer") && IsNetworkCompatible(infra2))
					{
						RegisterInfrastructure(infra2);
						AddEdge(infra, infra2);
					}
				}
			}
		}
	}
	
	public void RegisterInfrastructure(Infrastructure infra)
	{
		if (infra == null)
		{
			return;
		}
		
		if (infra.type == "hub")
		{
			hubSet.Add(infra);
			
			(int x, int y) coord = (infra.location.coordX, infra.location.coordY);
			
			if (!hubsByNode.ContainsKey(coord))
			{
				hubsByNode[coord] = new();
			}
			
			if (!hubsByNode[coord].Contains(infra))
			{
				hubsByNode[coord].Add(infra);
			}
		}
		else if (infra.type == "conveyer")
		{
			conveyerSet.Add(infra);
		}
		
		if (!adjacency.ContainsKey(infra))
		{
			adjacency[infra] = new();
		}
	}
	
	public void AddEdge(Infrastructure a, Infrastructure b)
	{
		if (a == null || b == null || ReferenceEquals(a, b))
		{
			return;
		}
		
		RegisterInfrastructure(a);
		RegisterInfrastructure(b);
		
		if (!adjacency[a].Contains(b))
		{
			adjacency[a].Add(b);
		}
		if (!adjacency[b].Contains(a))
		{
			adjacency[b].Add(a);
		}
	}
	
	public bool IsNetworkCompatible(Infrastructure infra)
	{
		return infra != null && (infra.type == "hub" || infra.type == "conveyer") && infra.serves.Contains(phaseServed);
	}
	
	public bool ContainsNode((int x, int y) coord)
	{
		return nodeSet.Contains(coord);
	}
	
	public bool ContainsInfrastructure(Infrastructure infra)
	{
		return hubSet.Contains(infra) || conveyerSet.Contains(infra);
	}
	
	public bool CanServe(string phase)
	{
		return phaseServed == phase;
	}
	
	public bool IsConnectedTo (LogisticsNetwork other)
	{
		if (other == null)
		{
			return false;
		}
		
		return other.Nodes.Any(ContainsNode) || other.Hubs.Any(ContainsInfrastructure) || other.Conveyers.Any(ContainsInfrastructure);
	}
	
	public List<Infrastructure> FindRoute(Infrastructure current, (int x, int y) end, HashSet<Infrastructure> visited)
	{
		if (nodeSet.Count == 0 || !(hubSet.Contains(current) || conveyerSet.Contains(current)) || visited.Contains(current))
		{
			return null;
		}
		
		visited.Add(current);
		
		if (current.type == "conveyer" && current.link != null && (current.link.location.coordX, current.link.location.coordY) == end)
		{
			List<Infrastructure> path = new();
			path.Add(current);
			return path;
		}
		
		List<Infrastructure> bestPath = null;
		
		foreach (var infra in adjacency[current])
		{
			if (infra.type == "conveyer" && !ReferenceEquals(infra, current.link))
			{
				List<Infrastructure> candidate = FindRoute(infra, end, new HashSet<Infrastructure>(visited));
				
				if (candidate != null)
				{
					List<Infrastructure> path = new();
					path.Add(current);
					path.AddRange(candidate);
					
					if (bestPath == null || path.Count < bestPath.Count)
					{
						bestPath = path;
					}
				}
			}
		}
		
		return bestPath;
	}
	
	public bool MergeNetworks(LogisticsNetwork other)
	{
		if (other == null || !CanServe(other.phaseServed) || !IsConnectedTo(other))
		{
			return false;
		}
		
		nodeSet.UnionWith(other.nodeSet);
		hubSet.UnionWith(other.hubSet);
		conveyerSet.UnionWith(other.conveyerSet);
		
		foreach (var hubs in other.hubsByNode)
		{
			if (!hubsByNode.ContainsKey(hubs.Key))
			{
				hubsByNode[hubs.Key] = new();
			}
			
			foreach (var hub in hubs.Value)
			{
				if (!hubsByNode[hubs.Key].Contains(hub))
				{
					hubsByNode[hubs.Key].Add(hub);
				}
			}
		}
		
		foreach (var adj in other.adjacency)
		{
			if (!adjacency.ContainsKey(adj.Key))
			{
				adjacency[adj.Key] = new();
			}
			
			foreach (var neighbor in adj.Value)
			{
				if (!adjacency[adj.Key].Contains(neighbor))
				{
					adjacency[adj.Key].Add(neighbor);
				}
			}
		}
		
		return true;
	}
}

//Android character to represent the player
public class Android
{
	public float maxInventory; //maximum amount of resources android can carry
	public Dictionary<string, float> inventory; //resources carried keyed by resource ID
	
	public AndroidSave lastSave;
	
	public Android()
	{
		maxInventory = 5000f;
		inventory = new();
		
		lastSave = null;
	}
	
	public Android(AndroidSave save)
	{
		maxInventory = 5000f;
		inventory = new(save.inventory);
		
		lastSave = save;
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

public class AndroidSave
{
	public Dictionary<string, float> inventory {get; set;}
	
	public AndroidSave()
	{
		inventory = new();
	}
	
	public AndroidSave(Android andro)
	{
		inventory = new(andro.inventory);
	}
}

public class Trader
{
	public string id {get; private set;}
	public TraderData data {get; private set;}
	public int idNum {get; private set;}
	public string name {get; private set;}
	public float favor {get; private set;}
	public float prosperity {get; private set;}
	public float greed {get; private set;}
	
	public Dictionary<string, float> inventory {get; private set;}
	public Dictionary<string, float> marketSnapshot {get; private set;}
	
	public TraderSave lastSave;
	
	public static List<string> CATALOG = new() {
		"hydrogen",
		"carbon",
		"oxygen",
		"iron ore",
		"copper ore",
		"boron ore",
		"phosphate ore",
		"water",
		"salt",
		"methane",
		"ethylene",
		"concrete",
		"low-density polyethylene",
		"high-density polyethylene",
		"electric motor",
		"induction coil",
		"electromagnetic relay",
		"resistor",
		"capacitor",
		"transistor",
		"diode",
		"light emitting diode",
		"solar cell",
		"computer board"
	};
	
	public Trader(string traderID)
	{
		id = traderID;
		data = GameData.TRADERS[traderID];
		
		do
		{
			idNum = GameData.rng.RandiRange(0, 999999);
		}
		while (GameData.traders.Keys.Contains(idNum));
		
		name = $"Trader-{idNum}";
		favor = 0.5f;
		prosperity = 1f;
		greed = data.greed;
		
		inventory = new();
		marketSnapshot = new();
		
		lastSave = null;
	}
	
	public Trader(TraderSave save)
	{
		id = save.id;
		data = GameData.TRADERS[save.id];
		
		idNum = save.idNum;
		
		name = save.name;
		favor = save.favor;
		prosperity = save.prosperity;
		greed = save.greed;
		
		inventory = new(save.inventory);
		marketSnapshot = new(save.marketSnapshot);
		
		lastSave = save;
	}
	
	public void GenerateInventory()
	{
		inventory.Clear();
		
		float inventoryValue = 1000f * prosperity;
		int safety = 0;
		
		Dictionary<string, float> catalog = GameData.TRADERS[id].catalog;
		
		while (inventoryValue > 0f && safety++ < 1000)
		{
			//int idx = (int)GameData.rng.RandiRange(0, CATALOG.Count - 1);
			
			//List<string> cat = GameData.TRADERS[id].catalog.Keys.ToList();
			
			float total = 0f;
			string largestResource = "";
			float largestValue = 0f;
			
			foreach (var res in catalog)
			{
				total += res.Value;
				
				if (res.Value > largestValue)
				{
					largestResource = res.Key;
					largestValue = res.Value;
				}
			}
			
			string resID = "";
			float roll = GameData.rng.Randf() * total;
			float cummulative = 0f;
			
			foreach (var res in catalog)
			{
				cummulative += res.Value;
				
				if (cummulative >= roll)
				{
					resID = res.Key;
					break;
				}
			}
			
			if (resID == "" && cummulative >= total)
			{
				resID = largestResource;
			}
			
			if (resID == "")
			{
				GD.PrintErr($"Trader: error rolling for resource - Total: {total}, Roll: {roll}, Cummulative: {cummulative}");
				continue;
			}
			
			if (!GameData.RESOURCES.ContainsKey(resID))
			{
				GD.PrintErr($"Trader: invalid resource ID {resID}");
				continue;
			}
			
			float unitValue = GameData.RESOURCES[resID].value;
			if (unitValue <= 0f)
			{
				continue;
			}
			
			float qty = GameData.rng.RandfRange(0f, 100f);
			float cost = qty * unitValue;
			
			if (cost > inventoryValue)
			{
				qty = inventoryValue / unitValue;
				cost = qty * unitValue;
				
				if (qty < 0.0001f)
				{
					break;
				}
			}
			
			if (inventory.ContainsKey(resID))
			{
				inventory[resID] += qty;
			}
			else
			{
				inventory[resID] = qty;
			}
			
			inventoryValue -= cost;
		}
		
		GameData.SortResources(inventory);
	}
	
	public void GenerateSnapshot()
	{
		marketSnapshot.Clear();
		
		foreach (var comm in GameData.galMarket)
		{
			marketSnapshot[comm.Key] = comm.Value.GetMarketValue();
		}
	}
	
	public float CalculateFavor(Dictionary<string, float> offer)
	{
		float totalValue = 0f;
		
		foreach (var res in offer)
		{
			if (!marketSnapshot.ContainsKey(res.Key))
			{
				continue;
			}
			
			float resValue = marketSnapshot[res.Key];
			
			if (data.preferences.ContainsKey(res.Key))
			{
				totalValue += resValue * data.preferences[res.Key] * res.Value;
			}
			else
			{
				totalValue += resValue * res.Value * 0.5f;
			}
		}
		
		return totalValue;
	}
	
	public void AdjustFavor(float fav)
	{
		favor += fav;
	}
	
	public void AdjustProsperity(float pros)
	{
		prosperity += pros;
	}
}

public class TraderSave
{
	public string id {get; private set;}
	public int idNum {get; private set;}
	public string name {get; private  set;}
	public float favor {get; private set;}
	public float prosperity {get; private set;}
	public float greed {get; private set;}
	
	public Dictionary<string, float> inventory {get; private set;}
	public Dictionary<string, float> marketSnapshot {get; private set;}
	
	public TraderSave()
	{
		id = "no ID";
		idNum = -1;
		name = "no name";
		favor = 0f;
		prosperity = 0f;
		greed = 0f;
		
		inventory = new();
		marketSnapshot = new();
	}
	
	public TraderSave(Trader t)
	{
		id = t.id;
		idNum = t.idNum;
		name = t.name;
		favor = t.favor;
		prosperity = t.prosperity;
		greed = t.greed;
		
		inventory = new(t.inventory);
		marketSnapshot = new(t.marketSnapshot);
	}
}

public class CommodityStock
{
	public string resource {get; private set;}
	public float baseValue {get; private set;}
	public float liquidity {get; private set;}
	public float shift {get; private set;}
	public float bull {get; private set;}
	public float bear {get; private set;}
	public float volatility {get; private set;}
	public float reversion {get; private set;}
	
	public float bullDecay {get; private set;}
	public float bearDecay {get; private set;}
	
	public float minMultiplier {get; private set;}
	public float maxMultiplier {get; private set;}
	
	public CommodityStock(string resID)
	{
		resource = resID;
		baseValue = GameData.RESOURCES[resID].value;
		liquidity = GameData.RESOURCES[resID].liquidity;
		
		shift = 1f;
		bull = 0f;
		bear = 0f;
		volatility = 1f;
		reversion = 1f;
		
		bullDecay = 0.05f;
		bearDecay = 0.05f;
		
		minMultiplier = 0.3f;
		maxMultiplier = 3.0f;
	}
	
	public CommodityStock(CommoditySave save)
	{
		resource = save.resource;
		baseValue = GameData.RESOURCES[resource].value;
		liquidity = GameData.RESOURCES[resource].liquidity;
		
		shift = save.shift;
		bull = save.bull;
		bear = save.bear;
		volatility = 1f;
		reversion = 1f;
		
		bullDecay = 0.05f;
		bearDecay = 0.05f;
		
		minMultiplier = 0.3f;
		maxMultiplier = 3.0f;
	}
	
	public void Tick(double delta)
	{
		float dt = (float)delta;
		
		shift += -reversion * (shift - 1f) * dt / liquidity + volatility * Mathf.Sqrt(dt) * GameData.RandNormal(0f, 1f);
		
		bull = Math.Max(bull - dt * bullDecay, 0f);
		bear = Math.Max(bear - dt * bearDecay, 0f);
	}
	
	public float GetMarketValue()
	{
		float multiplier = Math.Clamp(shift * (1 + bull - bear), minMultiplier, maxMultiplier);
		return baseValue * multiplier;
	}
	
	public void ApplyBull(float amount)
	{
		bull += amount / liquidity;
	}
	
	public void ApplyBear(float amount)
	{
		bear += amount / liquidity;
	}
}

public class CommoditySave
{
	public string resource {get; set;}
	public float shift {get; set;}
	public float bull {get; set;}
	public float bear {get; set;}
	
	public CommoditySave()
	{
		resource = "";
		shift = 0f;
		bull = 0f;
		bear = 0f;
	}
	
	public CommoditySave(CommodityStock comm)
	{
		resource = comm.resource;
		shift = comm.shift;
		bull = comm.bull;
		bear = comm.bear;
	}
}
