using Godot;
using System;
using Godot.Collections;

public partial class BuildControl : Control
{
	private bool building;
	private float buildTimer;
	
	private string buildKey;
	private string buildType;
	private BuildData selectedBuildable;
	
	OptionButton buildMenu;
	Button buildButton;
	RichTextLabel resourceLabel;
	ProgressBar buildProgress;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("BuildControl: _Ready() called...");
		
		building = false;
		buildTimer = 0f;
		
		GD.Print("BuildControl: Assigning node references...");
		buildMenu = GetNode<OptionButton>("Panel/BuildMenu");
		buildButton = GetNode<Button>("Panel/BuildButton");
		resourceLabel = GetNode<RichTextLabel>("Panel/CostScroll/ResourceLabel");
		buildProgress = GetNode<ProgressBar>("Panel/BuildProgress");
		
		resourceLabel.TabStops = new float[] {0f, 250f, 300f};
		
		UpdateBuildMenu();
		GD.Print("BuildControl: UpdateBuildMenu() successfully completed");
		
		int idx = buildMenu.GetSelected();
		GD.Print($"BuildControl: index set to {idx}");
		
		if (buildMenu.ItemCount > 0 && idx >= 0)
		{
			var meta = GetBuildableMeta(idx);
			buildKey = meta.key;
			buildType = meta.type;
			SelectBuildable(idx);
		}
		else
		{
			buildKey = "";
			buildType = "";
			selectedBuildable = null;
		}
		GD.Print("BuildControl: Displaying initial resources...");
		DisplayResources();
		
		buildButton.Pressed += StartBuild;
		buildMenu.ItemSelected += SelectBuildable;
		
		buildProgress.Value = 0f;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (building)
		{
			float completeTime = 1f;
			
			buildTimer += (float)delta;
			buildProgress.Value = Math.Clamp((buildTimer / completeTime) * 100f, 0f, 100f);
			
			if (buildTimer >= completeTime)
			{
				FinishBuild();
			}
		}
		
		DisplayResources();
	}
	
	// Assigns meta data to buildMenu item.
	private void SetBuildableMeta(int idx, string key, string type)
	{
		var meta = new Dictionary
		{
			{"key", key},
			{"type", type}
		};
		buildMenu.SetItemMetadata(idx, meta);
	}
	
	// Gets meta data from selected buildMenu item.
	private (string key, string type) GetBuildableMeta(int idx)
	{
		if (idx < 0 || idx >= buildMenu.ItemCount)
		{
			return ("", "");
		}
		
		var metaVar = buildMenu.GetItemMetadata(idx);
		
		if (metaVar.VariantType == Variant.Type.Nil)
		{
			return ("", "");
		}
		
		var meta = metaVar.AsGodotDictionary();
		string key = meta.ContainsKey("key") ? (string)meta["key"] : "";
		string type = meta.ContainsKey("type") ? (string)meta["type"] : "";
		return (key, type);
	}
	
	public void UpdateBuildMenu()
	{
		GD.Print("BuildControl: Updating build menu...");
		
		int oldSelectedIdx = buildMenu.GetSelected();
		string oldSelectedItem = (oldSelectedIdx >= 0 && oldSelectedIdx < buildMenu.ItemCount) ? buildMenu.GetItemText(oldSelectedIdx) : "";
		
		buildMenu.Clear();
		
		bool found = false;
		
		foreach (var mach in GameData.MACHINES)
		{
			if (GameData.unlockAllMachines || mach.Value.available)
			{
				GD.Print($"BuildControl: Adding machine {mach.Value.name}");
				buildMenu.AddItem(mach.Value.name);
				int idx = buildMenu.ItemCount - 1;
				SetBuildableMeta(idx, mach.Key, "mach");
				found = true;
			}
		}
		
		foreach (var infra in GameData.INFRASTRUCTURE)
		{
			if (GameData.unlockAllMachines || infra.Value.available)
			{
				GD.Print($"BuildControl: Adding infrastructure {infra.Value.name}");
				buildMenu.AddItem(infra.Value.name);
				int idx = buildMenu.ItemCount - 1;
				SetBuildableMeta(idx, infra.Key, "infra");
				found = true;
			}
		}
		
		buildMenu.Disabled = !found;
		
		if (!found)
		{
			GD.Print("BuildControl: No buildables available");
			buildMenu.AddItem("No available buildables");
			SetBuildableMeta(0, "", "");
		}
		else
		{
			int selectIdx = -1;
			
			if (!string.IsNullOrEmpty(oldSelectedItem) && oldSelectedItem != "No available buildables")
			{
				for (int i = 0; i < buildMenu.ItemCount; i++)
				{
					if (buildMenu.GetItemText(i) == oldSelectedItem)
					{
						selectIdx = i;
						break;
					}
				}
			}
			
			if (selectIdx < 0 && buildMenu.ItemCount > 0)
			{
				selectIdx = 0;
			}
			
			if (selectIdx >= 0)
			{
				buildMenu.Select(selectIdx);
			}
			
			GD.Print($"BuildControl: Selecting buildable id {selectIdx}");
			if (buildMenu.ItemCount > 0)
			{
				try
				{
					SelectBuildable(buildMenu.GetSelected());
				}
				catch (Exception e)
				{
					GD.PrintErr($"BuildControl: Error selecting item {buildMenu.GetSelected()}: {e.Message}");
				}
			}
			GD.Print("BuildControl: Buildable selected");
		}
	}
	
	private bool EnoughResources()
	{
		GD.Print("BuildControl: Checking resources...");
		foreach(var res in selectedBuildable.cost)
		{
			if (!GameData.currentRegion.resources.ContainsKey(res.Key))
			{
				GD.PrintErr($"BuildControl: Resource {res.Key} does not exist in GameData.currentRegion.resources");
				return false;
			}
			
			float available = GameData.currentRegion.resources[res.Key];
			float required = res.Value;
			
			GD.Print($"BuildControl: Checking cost {GameData.RESOURCES[res.Key].name} have {available} need {required}");
			
			if (available < required)
			{
				GD.Print($"BuildControl: Insufficient resources ({GameData.RESOURCES[res.Key].name})");
				return false;
			}
		}
		
		return true;
	}
	
	private void StartBuild()
	{
		GD.Print("BuildControl: Checking build...");
		
		if (selectedBuildable == null)
		{
			GD.Print("BuildControl: selected buildable null value");
			return;
		}
		
		if (!building && EnoughResources())
		{
			GD.Print($"BuildControl: Building maching {selectedBuildable.name}");
			buildButton.Disabled = true;
			buildMenu.Disabled = true;
			
			foreach(var res in selectedBuildable.cost)
			{
				GameData.currentRegion.resources[res.Key] -= res.Value;
			}
			
			building = true;
		}
	}
	
	private void FinishBuild()
	{
		building = false;
		buildTimer = 0f;
		
		if (buildType == "mach")
		{
			Machine newMachine = new Machine(buildKey, GameData.currentRegion);
			GameData.currentRegion.machines.Add(newMachine);
			GameData.machinesControl.AddMachinePanel(newMachine);
		}
		else if (buildType == "infra")
		{
			Infrastructure newInfrastructure = new Infrastructure(buildKey, GameData.currentRegion);
			GameData.currentRegion.infrastructure.Add(newInfrastructure);
			//Add infrastructure to Logistics Item List
		}
		
		buildProgress.Value = 0;
		buildButton.Disabled = false;
		buildMenu.Disabled = false;
	}
	
	private void SelectBuildable(long index)
	{
		int menuIdx = (int)index;
		
		if (menuIdx < 0 || menuIdx >= buildMenu.ItemCount)
		{
			GD.PrintErr("BuildControl: Invalid item ID in SelectBuildable()");
			selectedBuildable = null;
			buildKey = "";
			buildType = "";
			return;
		}
		
		string buildName = buildMenu.GetItemText(menuIdx);
		
		var meta = GetBuildableMeta(menuIdx);
		buildKey = meta.key;
		buildType = meta.type;
		
		if (buildType == "mach")
		{
			selectedBuildable = GameData.MACHINES.ContainsKey(buildKey) ? GameData.MACHINES[buildKey] : null;
		}
		else if (buildType == "infra")
		{
			selectedBuildable = GameData.INFRASTRUCTURE.ContainsKey(buildKey) ? GameData.INFRASTRUCTURE[buildKey] : null;
		}
		else
		{
			selectedBuildable = null;
		}
		
		GD.Print($"BuildControl: Selected buildable {buildName}");
	}
	
	private void DisplayResources()
	{
		if(selectedBuildable != null && ((buildType == "mach" && GameData.MACHINES.ContainsKey(buildKey)) || (buildType == "infra" && GameData.INFRASTRUCTURE.ContainsKey(buildKey))))
		{
			string resourceCost = "Cost:\n[table=5]";
			
			foreach (var res in selectedBuildable.cost)
			{
				var resData = GameData.RESOURCES[res.Key];
				//resourceCost += $"\n{resData.abbreviation} {GameData.currentRegion.resources[res.Key]} / {res.Value}";
				resourceCost += $"\n[cell]{resData.abbreviation}[/cell][cell]:[/cell][cell][right]{GameData.FormatUnit(GameData.currentRegion.resources[res.Key], res.Key)}[/right][/cell][cell]/[/cell][cell][right]{GameData.FormatUnit(res.Value, res.Key)}[/right][/cell]";
				//resourceCost += $"\n{resData.abbreviation}\t{GameData.FormatUnit(GameData.currentRegion.resources[res.Key], res.Key)} /\t{GameData.FormatUnit(res.Value, res.Key)}";
			}
			
			resourceCost += "\n[/table]";
			
			resourceLabel.Text = resourceCost;
		}
		else
		{
			resourceLabel.Text = "No buildable selected.";
		}
	}
}
