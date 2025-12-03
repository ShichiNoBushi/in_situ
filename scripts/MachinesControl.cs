using Godot;
using System;
using System.Collections.Generic;

public partial class MachinesControl : VBoxContainer
{
	[Export] public PackedScene MachinePanelScene;
	
	private Dictionary<(int x, int y), List<Machine>> regionPanels;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("MachinesControl._Ready() called...");
		
		regionPanels = new();
		
		GD.Print($"Machine count: {GameData.currentRegion.machines.Count}");
		foreach (var mach in GameData.currentRegion.machines)
		{
			AddMachinePanel(mach);
		}
	}
	
	public void AddStartingMachines()
	{
		regionPanels[(0, 0)] = new();
		foreach (var mach in GameData.regionMap[(0, 0)].machines)
		{
			AddMachinePanel(mach);
		}
		SaveRegionPanels();
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
	
	public void SaveRegionPanels()
	{
		String machList = "";
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		regionPanels[coord] = new();
		
		foreach (var child in GetChildren())
		{
			GD.Print($"MachinesControl: Saving panel {child}");
			if (child is MachinePanel mp)
			{
				Machine mach = mp.machine;
				String name = GameData.MACHINES[mach.id].name;
				machList += $"{name} ";
				regionPanels[coord].Add(mp.machine);
			}
		}
		GD.Print($"MachinesControl: Machine Panels at {coord} saved ({machList})");
	}
	
	public void UpdateRegionMachines()
	{
		String machList = "";
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		foreach (var child in GetChildren())
		{
			child.QueueFree();
		}
		
		if (!regionPanels.ContainsKey(coord))
		{
			regionPanels[coord] = new();
			GD.Print("MachinesControl: New region explored");
		}
		else
		{
			foreach (var mach in GameData.regionMap[coord].machines)
			{
				AddMachinePanel(mach);
			}
			/*
			foreach (var panel in regionPanels[coord])
			{
				GD.Print($"MachinesControl: Adding panel {panel}");
				Machine mach = panel.machine;
				String name = GameData.MACHINES[mach.id].name;
				machList += $"{name} ";
				AddChild(panel);
			}*/
			GD.Print($"MachinesControl: Populating machine panels at {coord} ({machList})");
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
