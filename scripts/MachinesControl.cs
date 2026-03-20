using Godot;
using System;
using System.Collections.Generic;

public partial class MachinesControl : TabContainer
{
	[Export] public PackedScene MachinePanelScene;
	[Export] public PackedScene InfrastructurePanelScene;
	
	public VBoxContainer machinesVBox;
	public VBoxContainer infrastructureVBox;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("MachinesControl._Ready() called...");
		
		if (GameData.currentRegion != null)
		{
			GD.Print($"MachinesControl: {GameData.currentRegion.machines.Count}");
		}
		else
		{
			GD.Print("MachinesControl: currentRegion is null");
		}
		
		machinesVBox = GetNode<VBoxContainer>("Machines/MachinesVBox");
		infrastructureVBox = GetNode<VBoxContainer>("Infrastructure/InfrastructureVBox");
		
		if (machinesVBox == null)
		{
			GD.Print("MachinesControl: machinesVBox improperly instantiated");
		}
		if (infrastructureVBox == null)
		{
			GD.Print("MachinesControl: infrastructureVBox improperly instantiated");
		}
		
		//AddStartingMachines();
		//AddStartingInfrastructure();
		/*foreach (var mach in GameData.currentRegion.machines)
		{
			AddMachinePanel(mach);
		}*/
	}
	
	public void AddStartingMachines()
	{
		foreach (var mach in GameData.regionMap[(0, 0)].machines)
		{
			AddMachinePanel(mach);
		}
	}
	
	public void AddStartingInfrastructure()
	{
		foreach (var infra in GameData.regionMap[(0, 0)].infrastructure)
		{
			AddInfrastructurePanel(infra);
		}
	}
	
	public void AddMachinePanel(Machine mach)
	{
		GD.Print($"MachinesControl: Adding panel for {GameData.MACHINES[mach.id].name}");
		try
		{
			var panel = MachinePanelScene.Instantiate<MachinePanel>();
			machinesVBox.AddChild(panel);
			panel.machine = mach;
			panel.CallDeferred(nameof(MachinePanel.Initialize));
			GD.Print("MachinesControl: Adding child to container...");
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error initializing MachinePanel for {mach.id}: {e}");
		}
	}
	
	public void AddInfrastructurePanel(Infrastructure infra)
	{
		GD.Print($"MachinesControl: Adding panel for {GameData.INFRASTRUCTURE[infra.id].name}");
		try
		{
			var panel = InfrastructurePanelScene.Instantiate<InfrastructurePanel>();
			infrastructureVBox.AddChild(panel);
			panel.infrastructure = infra;
			panel.CallDeferred(nameof(InfrastructurePanel.Initialize));
			GD.Print("MachinesControl: Adding child to container...");
		}
		catch (Exception e)
		{
			GD.PrintErr($"Error initializing InfrastructurePanel for {infra.id}: {e}");
		}
	}
	
	public void UpdateMachinePanels()
	{
		GD.Print("MachinesControl: Updating panel recipe menus...");
		GD.Print($"MachinesControl: machinesVBox null? {machinesVBox == null}");
		foreach (var child in machinesVBox.GetChildren())
		{
			if (child is MachinePanel mp)
			{
				mp.UpdateRecipeMenu();
				mp.UpdateDismantle();
			}
		}
	}
	
	public void UpdateRegionMachines()
	{
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		foreach (var child in machinesVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		foreach (var mach in GameData.currentRegion.machines)
		{
			AddMachinePanel(mach);
		}
		
		GD.Print($"MachinesControl: Populating machine panels at {coord}");
	}
	
	public void UpdateRegionInfrastructure()
	{
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		foreach (var child in infrastructureVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		foreach (var infra in GameData.currentRegion.infrastructure)
		{
			AddInfrastructurePanel(infra);
		}
		
		GD.Print($"MachinesControl: Populating infrastructure panels at {coord}");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
