using Godot;
using System;

public partial class MachinesControl : VBoxContainer
{
	[Export] public PackedScene MachinePanelScene;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("MachinesControl._Ready() called...");
		GD.Print($"Machine count: {GameData.machines.Count}");
		foreach (var mach in GameData.machines)
		{
			AddMachinePanel(mach);
		}
	}
	
	public void AddStartingMachines()
	{
		foreach (var mach in GameData.machines)
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

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
