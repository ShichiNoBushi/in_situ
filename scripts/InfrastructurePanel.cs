using Godot;
using System;
using System.Collections.Generic;

public partial class InfrastructurePanel : Control
{
	public Infrastructure infrastructure {get; set;}
	
	private Label nameLabel;
	private CheckButton activeButton;
	private ProgressBar wearProgress;
	private Label wearLabel;
	private Button diagnosticsButton;
	private Button repairButton;
	private Button dismantleButton;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("InfrastructurePanel: _Ready() called...");
		nameLabel = GetNode<Label>("Panel/VBoxMain/InfrastructureName");
		activeButton = GetNode<CheckButton>("Panel/VBoxMain/ActiveButton");
		wearProgress = GetNode<ProgressBar>("Panel/VBoxMain/InfrastructureTab/Maintenance/VBoxMaintenance/WearProgress");
		wearLabel = GetNode<Label>("Panel/VBoxMain/InfrastructureTab/Maintenance/VBoxMaintenance/WearScroll/WearLabel");
		diagnosticsButton = GetNode<Button>("Panel/VBoxMain/InfrastructureTab/Maintenance/VBoxMaintenance/HBoxContainer/DiagnosticsButton");
		repairButton = GetNode<Button>("Panel/VBoxMain/InfrastructureTab/Maintenance/VBoxMaintenance/HBoxContainer/RepairButton");
		dismantleButton = GetNode<Button>("Panel/VBoxMain/InfrastructureTab/Maintenance/VBoxMaintenance/HBoxContainer/DismantleButton");
		
		activeButton.Toggled += OnActiveToggled;
		diagnosticsButton.Pressed += DiagnoseInfrastructure;
		repairButton.Pressed += RepairInfrastructure;
		dismantleButton.Pressed += DismantleInfrastructure;
	}
	
	public void Initialize()
	{
		InfrastructureData data = GameData.INFRASTRUCTURE[infrastructure.id];
		nameLabel.Text = data.name;
		activeButton.SetPressedNoSignal(infrastructure.active);
		
		DisplayMaintenance();
		
		dismantleButton.Disabled = !data.available;
	}
	
	private void OnActiveToggled(bool pressed)
	{
		if (infrastructure == null) return;
		infrastructure.ToggleActive(pressed);
	}
	
	private void DiagnoseInfrastructure()
	{
		GD.Print($"InfrastructurePanel: Diagnosing infrastructure {GameData.INFRASTRUCTURE[infrastructure.id].name}");
		try
		{
			infrastructure.Diagnose();
		}
		catch (Exception e)
		{
			GD.PrintErr($"InfrastructurePanel: Error diagnosing infrastructure - {e.Message}");
		}
		DisplayMaintenance();
		GD.Print("InfrastructurePanel: Diagnostics complete");
	}
	
	private void RepairInfrastructure()
	{
		GD.Print($"InfrastructurePanel: Repairing infrastructure {GameData.INFRASTRUCTURE[infrastructure.id].name}");
		infrastructure.Repair();
	}
	
	private void DismantleInfrastructure()
	{
		Region loc = infrastructure.location;
		
		GD.Print($"InfrastructurePanel: Dismantling infrastructure {GameData.INFRASTRUCTURE[infrastructure.id].name}");
		
		try
		{
			infrastructure.DumpBuffers();
			infrastructure.Dismantle();
			
			if (infrastructure.type == "conveyer" && infrastructure.link != null)
			{
				GD.Print($"InfrastructurePanel: Dismantling linked conveyer {GameData.INFRASTRUCTURE[infrastructure.link.id].name}");
				infrastructure.link.DumpBuffers();
				Region loc2 = infrastructure.link.location;
				loc.infrastructure.Remove(infrastructure.link);
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"InfrastructurePanel: Error dismantling infrastructure - {e.Message}");
			return;
		}
		
		int removeCount = 0;
		
		if (loc != null && loc.infrastructure != null)
		{
			int before = loc.infrastructure.Count;
			loc.infrastructure.RemoveAll(i => ReferenceEquals(i, infrastructure));
			removeCount += before - loc.infrastructure.Count;
		}
		
		GD.Print($"InfrastructurePanel: Removed {removeCount} instances of {GameData.INFRASTRUCTURE[infrastructure.id].name}");
		
		if (removeCount == 0)
		{
			GD.Print($"InfrastructurePanel: Dismantle completed but no infrastructure removed from location's list");
		}
		
		GameData.logisticsControl.UpdateRegionLogistics();
		
		QueueFree();
		
		if (GameData.machinesControl != null)
		{
			GameData.machinesControl.UpdateMachinePanels();
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		DisplayMaintenance();
	}
	
	public void UpdateDismantle()
	{
		InfrastructureData data = GameData.INFRASTRUCTURE[infrastructure.id];
		dismantleButton.Disabled = !data.available;
	}
	
	private void DisplayMaintenance()
	{
		wearProgress.Value = Math.Clamp(infrastructure.wear / infrastructure.maxWear * 100f, 0f, 100f);
		
		string text = "Required Materials:";
		
		if (infrastructure.wear == 0f)
		{
			text += "\nUndamaged";
		}
		else if (infrastructure.diagnosedWear == 0f)
		{
			text += "\nUndiagnosed";
		}
		else
		{
			foreach (var res in infrastructure.repairComponents)
			{
				string available = GameData.FormatUnit(infrastructure.location.resources[res.Key], res.Key);
				string needed = GameData.FormatUnit(res.Value, res.Key);
				text += $"\n{GameData.RESOURCES[res.Key].name} {available} / {needed}";
			}
		}
		
		wearLabel.Text = text;
	}
}
