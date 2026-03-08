using Godot;
using System;
using System.Collections.Generic;

public partial class TradeControl : Node
{
	[Export] public PackedScene ResourceLabelScene;
	
	public Trader activeTrader;
	public Infrastructure activeHub;
	
	public Dictionary<string, float> traderOffer;
	public Dictionary<string, float> playerOffer;
	
	public VBoxContainer traderVBox;
	public VBoxContainer reserveVBox;
	public VBoxContainer resourceVBox;
	
	public Button previousTraderButton;
	public Button nextTraderButton;
	public Button previousHubButton;
	public Button nextHubButton;
	
	public OptionButton traderTradeMenu;
	public OptionButton playerTradeMenu;
	
	public SpinBox traderTradeSpin;
	public SpinBox playerTradeSpin;
	
	public Button traderOfferButton;
	public Button playerOfferButton;
	
	public ProgressBar favorProgress;
	
	public Button tradeButton;
	
	public VBoxContainer traderTradeVBox;
	public VBoxContainer playerTradeVBox;
	
	public Dictionary<string, Label> traderResLabels;
	public Dictionary<string, Label> reserveResLabels;
	public Dictionary<string, Label> resourceResLabels;
	
	public Dictionary<string, Label> traderOfferLabels;
	public Dictionary<string, Label> playerOfferLabels;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		traderVBox = GetNode<VBoxContainer>("TraderScroll/TraderVBox");
		reserveVBox = GetNode<VBoxContainer>("ReserveScroll/ReserveVBox");
		resourceVBox = GetNode<VBoxContainer>("ResourceScroll/ResourceVBox");
		
		previousTraderButton = GetNode<Button>("PreviousTraderButton");
		nextTraderButton = GetNode<Button>("NextTraderButton");
		previousHubButton = GetNode<Button>("PreviousHubButton");
		nextHubButton = GetNode<Button>("NextHubButton");
		
		traderTradeMenu = GetNode<OptionButton>("TraderTradeMenu");
		playerTradeMenu = GetNode<OptionButton>("PlayerTradeMenu");
		
		traderTradeSpin = GetNode<SpinBox>("TraderTradeSpin");
		playerTradeSpin = GetNode<SpinBox>("PlayerTradeSpin");
		
		traderOfferButton = GetNode<Button>("TraderOfferButton");
		playerOfferButton = GetNode<Button>("PlayerOfferButton");
		
		favorProgress = GetNode<ProgressBar>("FavorProgress");
		
		tradeButton = GetNode<Button>("TradeButton");
		
		traderTradeVBox = GetNode<VBoxContainer>("TraderTradeScroll/TraderTradeVBox");
		playerTradeVBox = GetNode<VBoxContainer>("PlayerTradeScroll/PlayerTradeVBox");
		
		activeTrader = null;
		activeHub = null;
		
		traderOffer = new();
		playerOffer = new();
		
		traderResLabels = new();
		reserveResLabels = new();
		resourceResLabels = new();
		
		traderOfferLabels = new();
		playerOfferLabels = new();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		UpdateTraderResourceValues();
		UpdateRegionResourceValues();
		UpdateTraderOfferValues();
		UpdatePlayerOfferValues();
	}
	
	public void UpdateTraderResourceLabels()
	{
		traderResLabels.Clear();
		
		foreach (var child in traderVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		if (activeTrader == null)
		{
			return;
		}
		
		foreach (var res in activeTrader.inventory)
		{
			Control rLabel = ResourceLabelScene.Instantiate<Control>();
			Label nLabel = rLabel.GetNode<Label>("HBoxContainer/NameLabel");
			Label aLabel = rLabel.GetNode<Label>("HBoxContainer/AmountLabel");
			
			nLabel.Text = GameData.RESOURCES[res.Key].name;
			aLabel.Text = GameData.FormatUnit(res.Value, res.Key);
			
			traderResLabels[res.Key] = aLabel;
			
			traderVBox.AddChild(rLabel);
		}
	}
	
	public void UpdateRegionResourceLabels()
	{
		resourceResLabels.Clear();
		
		foreach (var child in resourceVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		foreach (var res in GameData.currentRegion.resources)
		{
			Control rLabel = ResourceLabelScene.Instantiate<Control>();
			Label nLabel = rLabel.GetNode<Label>("HBoxContainer/NameLabel");
			Label aLabel = rLabel.GetNode<Label>("HBoxContainer/AmountLabel");
			
			nLabel.Text = GameData.RESOURCES[res.Key].name;
			aLabel.Text = GameData.FormatUnit(res.Value, res.Key);
			
			resourceResLabels[res.Key] = aLabel;
			
			resourceVBox.AddChild(rLabel);
		}
	}
	
	public void UpdateTraderOfferLabels()
	{
		traderOfferLabels.Clear();
		
		foreach (var child in traderTradeVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		if (activeTrader == null)
		{
			return;
		}
		
		foreach (var res in traderOffer)
		{
			Control rLabel = ResourceLabelScene.Instantiate<Control>();
			Label nLabel = rLabel.GetNode<Label>("HBoxContainer/NameLabel");
			Label aLabel = rLabel.GetNode<Label>("HBoxContainer/AmountLabel");
			
			nLabel.Text = GameData.RESOURCES[res.Key].name;
			aLabel.Text = GameData.FormatUnit(res.Value, res.Key);
			
			traderOfferLabels[res.Key] = aLabel;
			
			traderTradeVBox.AddChild(rLabel);
		}
	}
	
	public void UpdatePlayerOfferLabels()
	{
		playerOfferLabels.Clear();
		
		foreach (var child in playerTradeVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		foreach (var res in playerOffer)
		{
			Control rLabel = ResourceLabelScene.Instantiate<Control>();
			Label nLabel = rLabel.GetNode<Label>("HBoxContainer/NameLabel");
			Label aLabel = rLabel.GetNode<Label>("HBoxContainer/AmountLabel");
			
			nLabel.Text = GameData.RESOURCES[res.Key].name;
			aLabel.Text = GameData.FormatUnit(res.Value, res.Key);
			
			playerOfferLabels[res.Key] = aLabel;
			
			playerTradeVBox.AddChild(rLabel);
		}
	}
	
	public void UpdateTraderResourceValues()
	{
		if (activeTrader == null)
		{
			return;
		}
		
		foreach (var res in activeTrader.inventory)
		{
			traderResLabels[res.Key].Text = GameData.FormatUnit(res.Value, res.Key);
		}
	}
	
	public void UpdateRegionResourceValues()
	{
		foreach (var res in GameData.currentRegion.resources)
		{
			resourceResLabels[res.Key].Text = GameData.FormatUnit(res.Value, res.Key);
		}
	}
	
	public void UpdateTraderOfferValues()
	{
		if (activeTrader == null)
		{
			return;
		}
		
		foreach (var res in traderOffer)
		{
			traderOfferLabels[res.Key].Text = GameData.FormatUnit(res.Value, res.Key);
		}
	}
	
	public void UpdatePlayerOfferValues()
	{
		foreach (var res in playerOffer)
		{
			playerOfferLabels[res.Key].Text = GameData.FormatUnit(res.Value, res.Key);
		}
	}
}
