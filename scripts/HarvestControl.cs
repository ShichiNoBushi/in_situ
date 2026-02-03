using Godot;
using System;

public partial class HarvestControl : Control
{
	private bool harvesting;
	private float harvestTimer;
	
	private HarvestData harvestAction;
	
	OptionButton harvestMenu;
	Button harvestButton;
	ProgressBar harvestProgress;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		harvesting = false;
		harvestTimer = 0f;
		
		harvestMenu = GetNode<OptionButton>("Panel/HarvestMenu");
		harvestButton = GetNode<Button>("Panel/HarvestButton");
		harvestProgress = GetNode<ProgressBar>("Panel/HarvestProgress");
		
		foreach (var node in GameData.currentRegion.nodes)
		{
			foreach (var harv in GameData.HARVEST)
			{
				if (harv.Value.resource == node)
				{
					harvestMenu.AddItem(harv.Value.action);
					int id = harvestMenu.ItemCount - 1;
					harvestMenu.SetItemMetadata(id, harv.Key);
					break;
				}
			}
		}
		
		int idx = harvestMenu.GetSelectedId();
		String harvestKey = GameData.harvActionToKey[harvestMenu.GetItemText(idx)];
		harvestAction = GameData.HARVEST[harvestKey];
		
		harvestButton.Pressed += StartHarvest;
		harvestMenu.ItemSelected += SelectHarvest;
		
		harvestProgress.Value = 0f;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if (harvesting)
		{
			harvestTimer += (float)delta;
			harvestProgress.Value = Math.Clamp((harvestTimer / harvestAction.time) * 100f, 0f, 100f);
			
			if (harvestTimer >= harvestAction.time)
			{
				FinishHarvest();
			}
		}
	}
	
	public void UpdateHarvest()
	{
		harvestMenu.Clear();
		
		foreach (var node in GameData.currentRegion.nodes)
		{
			foreach (var harv in GameData.HARVEST)
			{
				if (harv.Value.resource == node)
				{
					harvestMenu.AddItem(harv.Value.action);
					int idx = harvestMenu.ItemCount - 1;
					harvestMenu.SetItemMetadata(idx, harv.Key);
					break;
				}
			}
		}
		
		if (harvestMenu.ItemCount == 0)
		{
			harvestMenu.AddItem("No Deposits");
			harvestMenu.Disabled = true;
		}
		else
		{
			harvestMenu.Disabled = false;
		}
	}
	
	public void StartHarvest()
	{
		if (!harvesting)
		{
			harvestButton.Disabled = true;
			harvestMenu.Disabled = true;
			
			harvesting = true;
		}
	}
	
	public void FinishHarvest()
	{
		harvesting = false;
		harvestTimer = 0f;
		
		String res = harvestAction.resource;
		
		GameData.currentRegion.resources[res] += harvestAction.amount;
		
		harvestProgress.Value = 0;
		harvestButton.Disabled = false;
		harvestMenu.Disabled = false;
	}
	
	public void SelectHarvest(long index)
	{
		//String harvestKey = GameData.harvActionToKey[harvestMenu.GetItemText((int)index)];
		string harvestKey = (string)harvestMenu.GetItemMetadata((int)index);
		harvestAction = GameData.HARVEST[harvestKey];
	}
}
