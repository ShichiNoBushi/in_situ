using Godot;
using System;

public partial class TravelControl : Control
{
	Label currentLabel;
	public OptionButton regionMenu;
	Button travelButton;
	OptionButton directionMenu;
	Button exploreButton;
	Label featuresLabel;
	
	(int x, int y) northRegion;
	(int x, int y) southRegion;
	(int x, int y) westRegion;
	(int x, int y) eastRegion;
	(int x, int y) neRegion;
	(int x, int y) seRegion;
	(int x, int y) swRegion;
	(int x, int y) nwRegion;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("TravelControl: _Ready() called...");
		currentLabel = GetNode<Label>("Panel/CurrentLabel");
		regionMenu = GetNode<OptionButton>("Panel/RegionMenu");
		travelButton = GetNode<Button>("Panel/TravelButton");
		directionMenu = GetNode<OptionButton>("Panel/DirectionMenu");
		exploreButton = GetNode<Button>("Panel/ExploreButton");
		featuresLabel = GetNode<Label>("Panel/FeaturesScroll/FeaturesLabel");
		
		/*GD.Print("TravelControl: assigning coordinate references");
		GD.Print($"TravelControl: GameData.currentRegion null? {GameData.currentRegion == null}");
		Region current = GameData.currentRegion;
		northRegion = (current.coordX, current.coordY + 1);
		southRegion = (current.coordX, current.coordY - 1);
		westRegion = (current.coordX - 1, current.coordY);
		eastRegion = (current.coordX + 1, current.coordY);
		neRegion = (current.coordX + 1, current.coordY + 1);
		seRegion = (current.coordX + 1, current.coordY - 1);
		swRegion = (current.coordX - 1, current.coordY - 1);
		nwRegion = (current.coordX - 1, current.coordY + 1);*/
		
		GD.Print($"TravelControl: regionMenu null? {regionMenu == null}");
		GD.Print($"TravelControl: travelButton null? {regionMenu == null}");
		GD.Print($"TravelControl: directionMenu null? {directionMenu == null}");
		GD.Print($"TravelControl: exploreButton null? {exploreButton == null}");
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
		DisplayFeatures();
	}
	
	public void AssignCoordinateReferences()
	{
		Region current = GameData.currentRegion;
		northRegion = (current.coordX, current.coordY + 1);
		southRegion = (current.coordX, current.coordY - 1);
		westRegion = (current.coordX - 1, current.coordY);
		eastRegion = (current.coordX + 1, current.coordY);
		neRegion = (current.coordX + 1, current.coordY + 1);
		seRegion = (current.coordX + 1, current.coordY - 1);
		swRegion = (current.coordX - 1, current.coordY - 1);
		nwRegion = (current.coordX - 1, current.coordY + 1);
	}
	
	public void DisplayFeatures()
	{
		int idx = regionMenu.GetSelected();
		//(int x, int y) coord = GameData.coordStringToTuple[regionMenu.GetItemText(idx)];
		Vector2I coordV2 = (Vector2I)regionMenu.GetItemMetadata(idx);
		(int x, int y) coord = (coordV2.X, coordV2.Y);
		Region current = GameData.currentRegion;
		Region selected = GameData.regionMap[coord];
		RegionData currentData = current.regData;
		RegionData selectedData = selected.regData;
		
		currentLabel.Text = $"({current.coordX}, {current.coordY})";
		
		String features = "Features:\n\n";
		
		features += $"Biome: {selectedData.name}\n\n";
		
		features += $"Elevation: {selectedData.elevation}\nTemperature: {selectedData.temperature}\nPressure: {selectedData.pressure}\nRoughness: {selectedData.roughness}\n\n";
		
		features += $"Wind: {selected.wind:0.00}\nSolar: {selected.solar:0.00}\n\n";
		
		features += $"Space: {selected.SpaceOccupied()} / {selected.space}\n\n";
		
		features += "Resourse Deposits:";
		if (selected.nodes.Count > 0)
		{
			foreach (var n in selected.nodes)
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
	
	public void ResetMenuSelect()
	{
		regionMenu.Select(0);
		directionMenu.Select(0);
	}
	
	private void OnRegionSelect(long index)
	{
		//(int x, int y) coord = GameData.coordStringToTuple[regionMenu.GetItemText((int)index)];
		Vector2I coordV2 = (Vector2I)regionMenu.GetItemMetadata((int)index);
		(int x, int y) coord = (coordV2.X, coordV2.Y);
		Region destination = GameData.regionMap[coord];
		
		DisplayFeatures();
		
		travelButton.Disabled = !(GameData.currentRegion.IsAdjacent(destination) || GameData.currentRegion.IsDiagonal(destination));
		GameData.mapControl.UpdateAllColors();
	}
	
	private void TravelRegion()
	{
		int idx = regionMenu.GetSelected();
		Vector2I coordV2 = (Vector2I)regionMenu.GetItemMetadata(idx);
		(int x, int y) coord = (coordV2.X, coordV2.Y);
		
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
			case 5:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(neRegion);
				break;
			case 6:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(seRegion);
				break;
			case 7:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(swRegion);
				break;
			case 8:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(nwRegion);
				break;
			default:
				GD.Print($"TravelControl: Error explore menu index {index}");
				break;
		}
	}
	
	public void SelectRegion((int x, int y) coord)
	{
		for (int i = 0; i < regionMenu.ItemCount; i++)
		{
			Vector2I itemCoord = (Vector2I)regionMenu.GetItemMetadata(i);
			
			if (itemCoord.X == coord.x && itemCoord.Y == coord.y)
			{
				regionMenu.Select(i);
				OnRegionSelect(i);
				return;
			}
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
			case "NE":
				exploreCoord = neRegion;
				break;
			case "SE":
				exploreCoord = seRegion;
				break;
			case "SW":
				exploreCoord = swRegion;
				break;
			case "NW":
				exploreCoord = nwRegion;
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
		
		int oldSelectedIdx = regionMenu.GetSelected();
		string oldSelectedItem = regionMenu.GetItemText(oldSelectedIdx);
		
		regionMenu.Clear();
		
		foreach (var coord in GameData.regionMap.Keys)
		{
			GD.Print($"TravelControl: Adding coordinate {GameData.CoordToString(coord)}");
			regionMenu.AddItem(GameData.CoordToString(coord));
			int idx = regionMenu.ItemCount - 1;
			Vector2I coordV2 = new Vector2I(coord.x, coord.y);
			regionMenu.SetItemMetadata(idx, coordV2);
		}
		
		int selectIdx = -1;
		
		if (!string.IsNullOrEmpty(oldSelectedItem))
		{
			for (int i = 0; i < regionMenu.ItemCount; i ++)
			{
				if (regionMenu.GetItemText(i) == oldSelectedItem)
				{
					selectIdx = i;
					break;
				}
			}
		}
		
		if (selectIdx < 0 && regionMenu.ItemCount > 0)
		{
			selectIdx = 0;
		}
		
		if (selectIdx >= 0)
		{
			regionMenu.Select(selectIdx);
		}
		
		Region current = GameData.currentRegion;
		northRegion = (current.coordX, current.coordY + 1);
		southRegion = (current.coordX, current.coordY - 1);
		westRegion = (current.coordX - 1, current.coordY);
		eastRegion = (current.coordX + 1, current.coordY);
		neRegion = (current.coordX + 1, current.coordY + 1);
		seRegion = (current.coordX + 1, current.coordY - 1);
		swRegion = (current.coordX - 1, current.coordY - 1);
		nwRegion = (current.coordX - 1, current.coordY + 1);
		
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
			case 5:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(neRegion);
				break;
			case 6:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(seRegion);
				break;
			case 7:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(swRegion);
				break;
			case 8:
				exploreButton.Disabled = GameData.regionMap.ContainsKey(nwRegion);
				break;
			default:
				GD.Print($"TravelControl: Error explore menu index {directionMenu.GetSelected()}");
				break;
		}
	}
}
