using Godot;
using System;
using System.Collections.Generic;

public partial class ResourceControl : VBoxContainer
{
	[Export] public PackedScene ResourcePanelScene;
	[Export] public PackedScene ResourceLabelScene;
	
	private Dictionary<string, Label> rlabels = new();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		Dictionary<string, int> typesCount = new();
		Dictionary<string, Control> typePanels = new();
		
		foreach (var res in GameData.RESOURCES)
		{
			string type = res.Value.type;
			Control tPanel;
			
			if (typesCount.ContainsKey(type))
			{
				typesCount[type]++;
				tPanel = typePanels[type];
			}
			else
			{
				typesCount[type] = 1;
				tPanel = ResourcePanelScene.Instantiate<Control>();
				Label tLabel = tPanel.GetNode<Label>("Panel/VBoxContainer/TypeLabel");
				tLabel.Text = type;
				typePanels[type] = tPanel;
				
				AddChild(tPanel);
			}
			
			//GD.Print($"ResourceControl: Loaded resource {res.Value.name} with type {res.Value.type}");
			
			Control rLabel = ResourceLabelScene.Instantiate<Control>();
			Label nLabel = rLabel.GetNode<Label>("HBoxContainer/NameLabel");
			Label aLabel = rLabel.GetNode<Label>("HBoxContainer/AmountLabel");
			
			nLabel.Text = res.Value.name;
			aLabel.Text = GameData.resources[res.Key].ToString();
			
			rlabels[res.Key] = aLabel;
			
			VBoxContainer vbox = tPanel.GetNode<VBoxContainer>("Panel/VBoxContainer");
			vbox.AddChild(rLabel);
		}
		
		int labelHeight = 30;
		
		foreach (var tPanel in typePanels)
		{
			GD.Print($"ResourceControl: {typesCount[tPanel.Key]} resources of type {tPanel.Key}");
			Panel panel = tPanel.Value.GetNode<Panel>("Panel");
			int count = typesCount[tPanel.Key];
			int height = labelHeight * (count + 1);
			panel.CustomMinimumSize = new Vector2(panel.CustomMinimumSize.X, height);
			tPanel.Value.CustomMinimumSize = new Vector2(tPanel.Value.CustomMinimumSize.X, height + 8);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		foreach(var res in GameData.resources)
		{
			rlabels[res.Key].Text = GameData.FormatUnit(res.Value, res.Key);
		}
	}
}
