using Godot;
using System;

public partial class BuildControl : Control
{
	private bool building;
	private float buildTimer;
	
	private String machKey;
	private MachineData selectedMachine;
	
	OptionButton machineMenu;
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
		machineMenu = GetNode<OptionButton>("Panel/MachineMenu");
		buildButton = GetNode<Button>("Panel/BuildButton");
		resourceLabel = GetNode<RichTextLabel>("Panel/CostScroll/ResourceLabel");
		buildProgress = GetNode<ProgressBar>("Panel/BuildProgress");
		
		resourceLabel.TabStops = new float[] {0f, 250f, 300f};
		
		UpdateBuildMenu();
		GD.Print("BuildControl: UpdateBuildMenu() successfully completed");
		
		int idx = machineMenu.GetSelected();
		GD.Print($"BuildControl: index set to {idx}");
		
		String selected = machineMenu.GetItemText(idx);
		if (GameData.machNameToKey.ContainsKey(selected))
		{
			machKey = GameData.machNameToKey[selected];
			selectedMachine = GameData.MACHINES[machKey];
		}
		else
		{
			machKey = "";
			selectedMachine = null;
		}
		
		GD.Print("BuildControl: Displaying initial resources...");
		DisplayResources();
		
		buildButton.Pressed += StartBuild;
		machineMenu.ItemSelected += SelectMachine;
		
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
	
	public void UpdateBuildMenu()
	{
		GD.Print("BuildControl: Updating build menu...");
		machineMenu.Clear();
		
		bool found = false;
		
		foreach (var mach in GameData.MACHINES)
		{
			if (GameData.unlockAllMachines || mach.Value.available)
			{
				GD.Print($"BuildControl: Adding machine {mach.Value.name}");
				machineMenu.AddItem(mach.Value.name);
				found = true;
			}
		}
		
		machineMenu.Disabled = !found;
		
		if (!found)
		{
			GD.Print("BuildControl: No machines available");
			machineMenu.AddItem("No available machines");
		}
		else
		{
			machineMenu.Select(0);
			GD.Print("BuildControl: Selecting machine id 0");
			if (machineMenu.ItemCount > 0)
			{
				try
				{
					SelectMachine(machineMenu.GetSelected());
				}
				catch (Exception e)
				{
					GD.PrintErr($"BuildControl: Error selecting item {machineMenu.GetSelected()}: {e.Message}");
				}
			}
			GD.Print("BuildControl: Machine selected");
		}
		
		/*if (machineMenu.ItemCount > 0)
		{
			SelectMachine(machineMenu.GetSelected());
		}*/
	}
	
	private bool EnoughResources()
	{
		GD.Print("BuildControl: Checking resources...");
		foreach(var res in selectedMachine.cost)
		{
			if (!GameData.resources.ContainsKey(res.Key))
			{
				GD.PrintErr($"BuildControl: Resource {res.Key} does not exist in GameData.resources");
				return false;
			}
			
			float available = GameData.resources[res.Key];
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
		
		if (selectedMachine == null)
		{
			GD.Print("BuildControl: selected machine null value");
			return;
		}
		
		if (!building && EnoughResources())
		{
			GD.Print($"BuildControl: Building maching {selectedMachine.name}");
			buildButton.Disabled = true;
			machineMenu.Disabled = true;
			
			foreach(var res in selectedMachine.cost)
			{
				GameData.resources[res.Key] -= res.Value;
			}
			
			building = true;
		}
	}
	
	private void FinishBuild()
	{
		building = false;
		buildTimer = 0f;
		
		Machine newMachine = new Machine(machKey);
		GameData.machines.Add(newMachine);
		GameData.machinesControl.AddMachinePanel(newMachine);
		
		buildProgress.Value = 0;
		buildButton.Disabled = false;
		machineMenu.Disabled = false;
	}
	
	private void SelectMachine(long index)
	{
		int menuIdx = (int)index;
		
		if (menuIdx < 0 || menuIdx >= machineMenu.ItemCount)
		{
			GD.PrintErr("BuildControl: Invalid item ID in SelectMachine()");
			selectedMachine = null;
			return;
		}
		
		String machName = machineMenu.GetItemText((int)index);
		
		if (!GameData.machNameToKey.ContainsKey(machName))
		{
			GD.Print($"BuildControl: Machine name {machName} not in machNameToKey");
			selectedMachine = null;
			return;
		}
		
		machKey = GameData.machNameToKey[machName];
		selectedMachine = GameData.MACHINES[machKey];
		
		GD.Print($"BuildControl: Selected machine {machName}");
	}
	
	private void DisplayResources()
	{
		if(GameData.MACHINES.ContainsKey(machKey))
		{
			String resourceCost = "Cost:";
			
			foreach (var res in selectedMachine.cost)
			{
				var resData = GameData.RESOURCES[res.Key];
				//resourceCost += $"\n{resData.abbreviation} {GameData.resources[res.Key]} / {res.Value}";
				resourceCost += $"\n[code]{resData.abbreviation, -15} {GameData.FormatUnit(GameData.resources[res.Key], res.Key), 8} / {GameData.FormatUnit(res.Value, res.Key), 8}[/code]";
				//resourceCost += $"\n{resData.abbreviation}\t{GameData.FormatUnit(GameData.resources[res.Key], res.Key)} /\t{GameData.FormatUnit(res.Value, res.Key)}";
			}
			
			resourceLabel.Text = resourceCost;
		}
		else
		{
			resourceLabel.Text = "No machine selected.";
		}
	}
}
