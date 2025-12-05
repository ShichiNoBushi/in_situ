using Godot;
using System;

public partial class MapCell : Control
{
	public int coordX;
	public int coordY;
	
	public Label cellLabel;
	//public ColorRect colorRect;
	public Panel colorPanel;
	
	private StyleBoxFlat baseStyle;
	private StyleBoxFlat style;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		cellLabel = GetNode<Label>("Label");
		//colorRect = GetNode<ColorRect>("ColorRect");
		colorPanel = GetNode<Panel>("ColorPanel");
		
		baseStyle = (StyleBoxFlat)colorPanel.GetThemeStylebox("panel");
		style = (StyleBoxFlat)baseStyle.Duplicate();
		
		colorPanel.AddThemeStyleboxOverride("panel", style);
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void Initialize((int x, int y) coord, bool explored)
	{
		GD.Print($"MapCell: Initializing at {coord}, Explored {explored}");
		coordX = coord.x;
		coordY = coord.y;
		
		Region region;
		String biome = "";
		if (GameData.regionMap.ContainsKey(coord))
		{
			region = GameData.regionMap[coord];
			biome = region.regData.name;
		}
		
		if (explored)
		{
			cellLabel.Text = $"({coordX}, {coordY})\n{biome}";
		}
		else
		{
			cellLabel.Text = "???";
		}
	}
	
	public void SetColor(Color bg)
	{
		style.BgColor = bg;
	}
}
