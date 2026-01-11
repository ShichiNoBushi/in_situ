using Godot;
using System;
using System.Collections.Generic;
using Godot.Collections;

public partial class LogisticsControl : Control
{
	public ItemList logisticsList;
	public Label logisticsLabel;
	public OptionButton logResourceMenu;
	public Label logResourceLabel;
	public SpinBox logResourceSpin;
	public Button logOrderButton;
	public RichTextLabel logOrderLabel;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("LogisticsControl.Ready() called...");
		
		logisticsList = GetNode<ItemList>("LogisticsList");
		logisticsLabel = GetNode<Label>("LogisticsLabel");
		logResourceMenu = GetNode<OptionButton>("LogResourceMenu");
		logResourceLabel = GetNode<Label>("LogResourceLabel");
		logResourceSpin = GetNode<SpinBox>("LogResourceSpin");
		logOrderButton = GetNode<Button>("LogOrderButton");
		logOrderLabel = GetNode<RichTextLabel>("LogisticsScroll/LogOrderLabel");
		
		logisticsList.ItemSelected += SelectLogistics;
		logOrderButton.Pressed += SendOrder;
		
		logOrderLabel.BbcodeEnabled = true;
		
		PopulateResourceMenu();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (logResourceMenu.ItemCount == 0) return;
		
		int idx = logResourceMenu.GetSelected();
		if (idx < 0) return;
		
		if (logResourceMenu.IsItemSeparator(idx))
		{
			logResourceLabel.Text = "";
			return;
		}
		
		string resID = (string)logResourceMenu.GetItemMetadata(idx);
		string formatted = GameData.FormatUnit(GameData.currentRegion.resources[resID], resID);
		logResourceLabel.Text = formatted;
		
		logResourceSpin.MaxValue = GameData.currentRegion.resources[resID];
		
		DisplayOrders();
	}
	
	public void SelectLogistics(long index)
	{
		var meta = GetInfraMeta((int)index);
		Region reg = meta.region;
		
		if (reg == null || meta.index < 0)
		{
			GD.Print("LogisticsControl: No logistics selected");
			logisticsLabel.Text = "No selection";
			logResourceMenu.Disabled = true;
			logResourceSpin.Editable = false;
			logOrderButton.Disabled = true;
			return;
		}
		
		Infrastructure logistics = reg.infrastructure[meta.index];
		InfrastructureData data = GameData.INFRASTRUCTURE[logistics.id];
		
		GD.Print($"LogisticsControl: Selected index {index} name {data.name} type {data.type}");
		
		string text = $"{data.name}\n\nType: {data.type}\nThrough: {data.through}\nEnergy Cost: {data.energyCost}";
		
		if (data.type == "conveyer")
		{
			Infrastructure link = logistics.link;
			Region neighbor = link.location;
			text += $"\nLink: ({neighbor.coordX}, {neighbor.coordY})";
		}
		
		logisticsLabel.Text = text;
		
		logResourceMenu.Disabled = logistics.type != "hub";
		logResourceSpin.Editable = logistics.type == "hub";
		logOrderButton.Disabled = logistics.type != "hub";
	}
	
	public void SendOrder()
	{
		int idx = logisticsList.GetSelectedItems()[0];
		var meta = GetInfraMeta(idx);
		Region reg = meta.region;
		Infrastructure infra = reg.infrastructure[meta.index];
		
		if (infra.type != "hub")
		{
			GD.Print("LogisticsControl: Selected infrastructure is not hub type");
			return;
		}
		
		int resIdx = logResourceMenu.GetSelected();
		string resource = (string)logResourceMenu.GetItemMetadata(resIdx);
		
		float available = reg.resources[resource];
		float amount = (float)logResourceSpin.Value;
		
		if (amount <= 0f || amount > available)
		{
			GD.Print("LogisticsControl: Invalid amount");
			return;
		}
		
		GD.Print($"LogisticsOrder: Creating order of {GameData.FormatUnit(amount, resource)} of {GameData.RESOURCES[resource].name}");
		reg.resources[resource] -= amount;
		
		LogisticOrder order = new(resource, amount);
		infra.GiveOutput(order);
		
		logResourceSpin.Value = 0f;
		GD.Print($"LogisticsOrder: infrastructure has {infra.input.Count} input orders and {infra.output.Count} output orders");
	}
	
	public void PopulateResourceMenu()
	{
		GD.Print("LogisticsControl: Populating resource menu");
		GD.Print($"LogisticsControl: {GameData.RESOURCES.Count} different resources");
		
		System.Collections.Generic.Dictionary<string, List<string>> resTypes = new();
		try
		{
			GD.Print("LogisticsControl: Beginning loop...");
			foreach (var res in GameData.RESOURCES)
			{
				string type = res.Value.type;
				if (!resTypes.ContainsKey(type))
				{
					resTypes[type] = new();
				}
				
				resTypes[type].Add(res.Key);
				//logResourceMenu.AddItem(res.Value.name);
				//int idx = logResourceMenu.ItemCount - 1;
				//logResourceMenu.SetItemMetadata(idx, res.Key);
			}
		}
		catch(Exception e)
		{
			GD.PrintErr($"LogisticsControl: error populating resources menu - {e}");
		}
		
		foreach (var typeList in resTypes)
		{
			logResourceMenu.AddSeparator(typeList.Key);
			foreach (var res in typeList.Value)
			{
				logResourceMenu.AddItem(GameData.RESOURCES[res].name);
				int idx = logResourceMenu.ItemCount - 1;
				logResourceMenu.SetItemMetadata(idx, res);
			}
		}
		
		GD.Print($"LogisticsControl: added {logResourceMenu.ItemCount} items to menu");
	}
	
	public void DisplayOrders()
	{
		int idx = logisticsList.GetSelectedItems()[0];
		var meta = GetInfraMeta(idx);
		Region reg = meta.region;
		Infrastructure infra = reg.infrastructure[meta.index];
		
		if (infra == null || (infra.type != "hub" && infra.type != "conveyer"))
		{
			logOrderLabel.Text = "No orders";
			return;
		}
		
		if (infra.input.Count == 0 && infra.output.Count == 0)
		{
			logOrderLabel.Text = "No orders";
			return;
		}
		
		string text = "";
		
		if (infra.input.Count > 0)
		{
			text += "Input:\n[table=2]";
			
			foreach (var inp in infra.input)
			{
				text += $"[cell]{GameData.RESOURCES[inp.resource].name}[/cell][cell][right]{GameData.FormatUnit(inp.amount, inp.resource)}[/right][/cell]";
			}
			
			text += "[/table]";
			
			if (infra.output.Count > 0)
			{
				text += "\n\n";
			}
		}
		
		if (infra.output.Count > 0)
		{
			text += "Output:\n[table=2]";
			
			foreach (var outp in infra.output)
			{
				text += $"[cell]{GameData.RESOURCES[outp.resource].name}[/cell][cell][right]{GameData.FormatUnit(outp.amount, outp.resource)}[/right][/cell]";
			}
			
			text += "[/table]";
		}
		
		logOrderLabel.Text = text;
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
		logisticsLabel.Text = "No selection";
		logResourceMenu.Disabled = true;
		logResourceSpin.Editable = false;
		logOrderButton.Disabled = true;
		
		Region reg = GameData.currentRegion;
		
		GD.Print($"LogisticsControl: updating Logistics List at ({reg.coordX}, {reg.coordY})");
		
		for (int i = 0; i < reg.infrastructure.Count; i++)
		{
			Infrastructure infra = reg.infrastructure[i];
			string name = GameData.INFRASTRUCTURE[infra.id].name;
			logisticsList.AddItem(name);
			int idxList = logisticsList.ItemCount - 1;
			SetInfraMeta(idxList, reg, i);
			GD.Print($"LogisticsControl: adding item {name} at index {i}");
		}
	}
}
