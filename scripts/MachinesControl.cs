using Godot;
using System;

public partial class MachinesControl : VBoxContainer
{
	[Export] public PackedScene MachinePanelScene;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
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
		var panel = MachinePanelScene.Instantiate<MachinePanel>();
		panel.Initialize(mach);
		AddChild(panel);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
