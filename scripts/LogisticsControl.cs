using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

public partial class LogisticsControl : Control
{
	public ItemList logisticsList;
	public Label logisticsLabel;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		logisticsList = GetNode<ItemList>("LogisticsList");
		logisticsLabel = GetNode<Label>("LogisticsLabel");
		
		logisticsList.ItemSelected += SelectLogistics;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void SelectLogistics(long index)
	{
		var meta = GetInfraMeta((int)index);
		Region reg = meta.region;
		
		if (reg == null || meta.index < 0)
		{
			logisticsLabel.Text = "No selection";
			return;
		}
		
		Infrastructure logistics = reg.infrastructure[meta.index];
		InfrastructureData data = GameData.INFRASTRUCTURE[logistics.id];
		
		string text = $"{data.name}\n\nType: {data.type}\nThrough: {data.through}\nEnergy Cost: {data.energyCost}";
		
		if (data.type == "conveyer")
		{
			Infrastructure link = logistics.link;
			Region neighbor = link.location;
			text += $"\nLink: ({neighbor.coordX}, {neighbor.coordY})";
		}
		
		logisticsLabel.Text = text;
	}
	
	public void SetInfraMeta(int idxList, Region reg, int idxInfra)
	{
		var meta = new Dictionary
		{
			{"x", reg.coordX},
			{"y", reg.coordY},
			{"index", idxInfra}
		};
		logisticsList.SetItemMetadata(idxList, meta);
	}
	
	public (Region region, int index) GetInfraMeta(int idxList)
	{
		if (idxList < 0 || idxList >= logisticsList.ItemCount)
		{
			return (null, -1);
		}
		
		var metaVar = logisticsList.GetItemMetadata(idxList);
		
		if (metaVar.VariantType == Variant.Type.Nil)
		{
			return (null, -1);
		}
		
		var meta = metaVar.AsGodotDictionary();
		int x = meta.ContainsKey("x") ? meta["x"].AsInt32() : 0;
		int y = meta.ContainsKey("y") ? meta["y"].AsInt32() : 0;
		var coord = (x, y);
		
		if (!GameData.regionMap.ContainsKey(coord))
		{
			return (null, -1);
		}
		
		Region reg = GameData.regionMap[coord];
		int idxInfra = meta.ContainsKey("index") ? (int)meta["index"] : -1;
		return (reg, idxInfra);
	}
	
	public void UpdateRegionLogistics()
	{
		logisticsList.Clear();
		
		Region reg = GameData.currentRegion;
		
		GD.Print($"LogisticsControl: updating Logistics List at ({reg.coordX}, {reg.coordY})");
		
		foreach (var infra in reg.infrastructure)
		{
			string name = GameData.INFRASTRUCTURE[infra.id].name;
			logisticsList.AddItem(name);
			int idxList = logisticsList.ItemCount - 1;
			int idxInfra = reg.infrastructure.Count - 1;
			SetInfraMeta(idxList, reg, idxInfra);
			GD.Print($"LogisticsControl: adding item {name}");
		}
	}
}
