using Godot;
using System;
using System.Text;

public partial class ResourceTest : Label
{
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		StringBuilder sb = new StringBuilder();
		var RESOURCES = GameData.RESOURCES;
		
		foreach (var res in RESOURCES)
		{
			sb.AppendLine(res.Value.name);
		}
		
		Text = sb.ToString();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
