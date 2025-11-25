using Godot;
using System;

public partial class TravelControl : Control
{
	Label currentLabel;
	OptionButton regionMenu;
	Button travelButton;
	OptionButton directionMenu;
	Button exploreButton;
	Label featuresLabel;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		currentLabel = GetNode<Label>("Panel/CurrentLabel");
		regionMenu = GetNode<OptionButton>("Panel/RegionMenu");
		travelButton = GetNode<Button>("Panel/TravelButton");
		directionMenu = GetNode<OptionButton>("Panel/DirectionMenu");
		exploreButton = GetNode<Button>("Panel/ExploreButton");
		featuresLabel = GetNode<Label>("Panel/FeaturesScroll/FeaturesLabel");
		
		DisplayFeatures();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	private void DisplayFeatures()
	{
		Region region = GameData.currentRegion;
		RegionData data = region.regData;
		
		currentLabel.Text = $"({region.coordX}, {region.coordY})";
		
		String features = "Features:\n\n";
		
		features += $"Biome: {data.name}\n\n";
		
		features += $"Elevation: {data.elevation}\nTemperature: {data.temperature}\nPressure: {data.pressure}\nRoughness: {data.roughness}\n\n";
		
		features += "Resourse Deposits:";
		if (region.nodes.Count > 0)
		{
			foreach (var n in region.nodes)
			{
				features += $"\n{GameData.RESOURCES[n].name}";
			}
		}
		else
		{
			features += "\nNo deposits";
		}
		
		featuresLabel.Text = features;
	}
}
