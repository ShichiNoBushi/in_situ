using Godot;
using System;
using System.Collections.Generic;

public partial class MapControl : Control
{
	[Export] public PackedScene MapCellScene;
	public GridContainer mapGrid;
	public Dictionary<(int x, int y), MapCell> cells = new();
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print($"MapControl: _Ready() called... {GetPath()}");
		mapGrid = GetNode<GridContainer>("ScrollContainer/MarginContainer/CenterContainer/MapGrid");
		cells = new();
		GenerateMap();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void GenerateMap()
	{
		GD.Print("MapControl: Generating map...");
		try
		{
			cells.Clear();
			
			foreach(Node child in mapGrid.GetChildren())
			{
				child.QueueFree();
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"MapControl: Error clearing cells - {e.Message}");
		}
		
		int minX = int.MaxValue;
		int maxX = int.MinValue;
		int minY = int.MaxValue;
		int maxY = int.MinValue;
		
		try
		{
			foreach (var coord in GameData.regionMap.Keys)
			{
				if (coord.x < minX)
				{
					minX = coord.x;
				}
				if (coord.x > maxX)
				{
					maxX = coord.x;
				}
				if (coord.y < minY)
				{
					minY = coord.y;
				}
				if (coord.y > maxY)
				{
					maxY = coord.y;
				}
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"MapControl: Error setting min/max coordinates - {e.Message}");
		}
		
		int width = maxX - minX + 1;
		int height = maxY - minY + 1;
		
		mapGrid.Columns = width;
		
		GD.Print($"MapControl: MinX {minX}, MaxX {maxX}, MinY {minY}, MaxY {maxY}, Width {width}, Height {height}");
		
		try
		{
			for (int y = maxY; y >= minY; y--)
			{
				for (int x = minX; x <= maxX; x++)
				{
					(int x, int y) coord = (x, y);
					
					GD.Print($"MapControl: Checking region at {coord}");
					
					try
					{
						var cell = MapCellScene.Instantiate<MapCell>();
						mapGrid.AddChild(cell);
						
						if (GameData.regionMap.ContainsKey(coord))
						{
							GD.Print($"MapControl: Marking cell for explored region {coord}");
							cell.Initialize(coord, true);
						}
						else
						{
							GD.Print($"MapControl: Marking cell for unexplored region {coord}");
							cell.Initialize(coord, false);
						}
						
						GD.Print($"MapControl: Adding cell to dictionary {coord}");
						cells[coord] = cell;
					}
					catch (Exception e)
					{
						GD.PrintErr($"MapControl: Error generating region {e.Message}");
					}
				}
			}
		}
		catch (Exception e)
		{
			GD.PrintErr($"MapControl: Error adding map cells - {e.Message}");
		}
	}
}
