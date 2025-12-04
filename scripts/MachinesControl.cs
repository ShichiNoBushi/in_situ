using Godot;
using System;
using System.Collections.Generic;

public partial class MachinesControl : VBoxContainer
{
	[Export] public PackedScene MachinePanelScene;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("MachinesControl._Ready() called...");
		GD.Print($"Machine count: {GameData.currentRegion.machines.Count}");
		foreach (var mach in GameData.currentRegion.machines)
		{
			AddMachinePanel(mach);
		}
	}
	
	public void AddStartingMachines()
	{
		foreach (var mach in GameData.regionMap[(0, 0)].machines)
		{
			AddMachinePanel(mach);
		}
	}
	
	public void AddMachinePanel(Machine mach)
	{
		GD.Print($"Machines Control: Adding panel for {GameData.MACHINES[mach.id].name}");
		try
		{
			var panel = MachinePanelScene.Instantiate<MachinePanel>();
			AddChild(panel);
			panel.machine = mach;
			panel.CallDeferred(nameof(MachinePanel.Initialize));
			//panel.Initialize(mach);
			GD.Print("Machines Control: Adding child to container...");
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error initializing MachinePanel for {mach.id}: {e}");
		}
	}
	
	public void UpdateMachinePanels()
	{
		GD.Print("MachinesControl: Updating panel recipe menus...");
		foreach (var child in GetChildren())
		{
			if (child is MachinePanel mp)
			{
				mp.UpdateRecipeMenu();
			}
		}
	}
	
	public void UpdateRegionMachines()
	{
		String machList = "";
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}
		
		foreach (var mach in GameData.regionMap[coord].machines)
		{
			AddMachinePanel(mach);
		}
		
		GD.Print($"MachinesControl: Populating machine panels at {coord} ({machList})");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
