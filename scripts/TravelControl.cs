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
	
	(int x, int y) northRegion;
	(int x, int y) southRegion;
	(int x, int y) westRegion;
	(int x, int y) eastRegion;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		currentLabel = GetNode<Label>("Panel/CurrentLabel");
		regionMenu = GetNode<OptionButton>("Panel/RegionMenu");
		travelButton = GetNode<Button>("Panel/TravelButton");
		directionMenu = GetNode<OptionButton>("Panel/DirectionMenu");
		exploreButton = GetNode<Button>("Panel/ExploreButton");
		featuresLabel = GetNode<Label>("Panel/FeaturesScroll/FeaturesLabel");
		
		Region current = GameData.currentRegion;
		northRegion = (current.coordX, current.coordY + 1);
		southRegion = (current.coordX, current.coordY - 1);
		westRegion = (current.coordX - 1, current.coordY);
		eastRegion = (current.coordX + 1, current.coordY);
		
		regionMenu.ItemSelected += OnRegionSelect;
		travelButton.Pressed += TravelRegion;
		directionMenu.ItemSelected += OnDirectionSelect;
		exploreButton.Pressed += ExploreRegion;
		
		travelButton.Disabled = !GameData.currentRegion.IsAdjacent(GameData.regionMap[(0, 0)]);
		exploreButton.Disabled = GameData.regionMap.ContainsKey(northRegion);
		
		DisplayFeatures();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void DisplayFeatures()
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
	
	private void OnRegionSelect(long index)
	{
		(int x, int y) coord = GameData.coordStringToTuple[regionMenu.GetItemText((int)index)];
		Region destination = GameData.regionMap[coord];
		
		travelButton.Disabled = !GameData.currentRegion.IsAdjacent(destination);
	}
	
	private void TravelRegion()
	{
		int idx = regionMenu.GetSelected();
		(int x, int y) coord = GameData.coordStringToTuple[regionMenu.GetItemText(idx)];
		
		GameData.TravelTo(coord);
	}
	
	private void OnDirectionSelect(long index)
	{
		switch (index)
		{
			case 0:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(northRegion);
				break;
			case 1:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(southRegion);
				break;
			case 2:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(westRegion);
				break;
			case 3:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(eastRegion);
				break;
			default:
				GD.Print($"TravelControl: Error explore menu index {index}");
				break;
		}
	}
	
	private void ExploreRegion()
	{
		(int x, int y) exploreCoord;
		int idx = directionMenu.GetSelected();
		switch (directionMenu.GetItemText(idx))
		{
			case "North":
				exploreCoord = northRegion;
				break;
			case "South":
				exploreCoord = southRegion;
				break;
			case "West":
				exploreCoord = westRegion;
				break;
			case "East":
				exploreCoord = eastRegion;
				break;
			default:
				GD.Print($"TravelControl: Exploring invalid coordinate, Index: {idx}");
				return;
		}
		
		if (GameData.regionMap.ContainsKey(exploreCoord))
		{
			GD.Print($"TravelControl: Region {exploreCoord} already explored");
			return;
		}
		
		GameData.ExploreRegion(exploreCoord);
	}
	
	public void UpdateRegions()
	{
		GD.Print("TravelControl: Updating regions menu...");
		regionMenu.Clear();
		
		foreach (var coord in GameData.regionMap.Keys)
		{
			GD.Print($"TravelControl: Adding coordinate {GameData.CoordToString(coord)}");
			regionMenu.AddItem(GameData.CoordToString(coord));
		}
		
		Region current = GameData.currentRegion;
		northRegion = (current.coordX, current.coordY + 1);
		southRegion = (current.coordX, current.coordY - 1);
		westRegion = (current.coordX - 1, current.coordY);
		eastRegion = (current.coordX + 1, current.coordY);
		
		switch (directionMenu.GetSelected())
		{
			case 0:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(northRegion);
				break;
			case 1:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(southRegion);
				break;
			case 2:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(westRegion);
				break;
			case 3:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(eastRegion);
				break;
			default:
				GD.Print($"TravelControl: Error explore menu index {directionMenu.GetSelected()}");
				break;
		}
	}
}
