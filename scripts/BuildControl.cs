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
	Label resourceLabel;
	ProgressBar buildProgress;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		building = false;
		buildTimer = 0f;
		
		machineMenu = GetNode<OptionButton>("Panel/MachineMenu");
		buildButton = GetNode<Button>("Panel/BuildButton");
		resourceLabel = GetNode<Label>("Panel/CostScroll/ResourceLabel");
		buildProgress = GetNode<ProgressBar>("Panel/BuildProgress");
		
		UpdateBuildMenu();
		
		int idx = machineMenu.GetSelectedId();
		machKey = GameData.machNameToKey[machineMenu.GetItemText(idx)];
		selectedMachine = GameData.MACHINES[machKey];
		
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
			if (mach.Value.available)
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
			SelectMachine((long)0);
			GD.Print("BuildControl: Mahchine selected");
		}
	}
	
	private bool EnoughResources()
	{
		foreach(var res in selectedMachine.cost)
		{
			if (GameData.resources[res.Key] < res.Value)
			{
				return false;
			}
		}
		
		return true;
	}
	
	private void StartBuild()
	{
		//if (!building && EnoughResources())
		if (!building)
		{
			buildButton.Disabled = true;
			machineMenu.Disabled = true;
			
			/*foreach(var res in selectedMachine.cost)
			{
				GameData.resources[res.Key] -= res.Value;
			}*/
			
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
		machKey = GameData.machNameToKey[machineMenu.GetItemText((int)index)];
		GD.Print($"BuildControl: Selecting machine {GameData.MACHINES[machKey]} at index {index}");
		selectedMachine = GameData.MACHINES[machKey];
	}
	
	private void DisplayResources()
	{
		if(GameData.MACHINES.ContainsKey(machKey))
		{
			String resourceCost = "Cost:";
			
			foreach (var res in selectedMachine.cost)
			{
				var resData = GameData.RESOURCES[res.Key];
				resourceCost += $"\n{resData.abbreviation} {GameData.resources[res.Key]} / {res.Value}";
			}
			
			resourceLabel.Text = resourceCost;
		}
		else
		{
			resourceLabel.Text = "No machine selected.";
		}
	}
}
