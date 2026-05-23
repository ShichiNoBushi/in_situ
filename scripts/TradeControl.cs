using Godot;
using System;
using System.Collections.Generic;

public partial class TradeControl : Node
{
	[Export] public PackedScene ResourceLabelScene;
	
	public const float TAKEOFF_FEE = 1f;
	
	public Trader activeTrader;
	public Infrastructure activeHub;
	
	public Dictionary<(int x, int y), List<Infrastructure>> tradeHubs;
	public Dictionary<(int x, int y), List<Dictionary<string, float>>> hubInventories;
	public int hubIndex;
	
	public Dictionary<string, float> traderOffer;
	public Dictionary<string, float> playerOffer;
	
	public VBoxContainer traderVBox;
	public VBoxContainer reserveVBox;
	public VBoxContainer resourceVBox;
	
	public Button previousTraderButton;
	public Button nextTraderButton;
	public Button previousHubButton;
	public Button nextHubButton;
	
	public OptionButton reserveMenu;
	public SpinBox reserveSpin;
	public Button reserveButton;
	
	public OptionButton returnMenu;
	public SpinBox returnSpin;
	public Button returnButton;
	
	public OptionButton traderTradeMenu;
	public OptionButton playerTradeMenu;
	
	public SpinBox traderTradeSpin;
	public SpinBox playerTradeSpin;
	
	public Button traderOfferButton;
	public Button traderRetractButton;
	public Button playerOfferButton;
	public Button playerRetractButton;
	
	public ProgressBar favorProgress;
	public Label offerValueLabel;
	
	public Button tradeButton;
	
	public VBoxContainer traderTradeVBox;
	public VBoxContainer playerTradeVBox;
	
	public Dictionary<string, Label> traderResLabels;
	public Dictionary<string, Label> reserveResLabels;
	public Dictionary<string, Label> resourceResLabels;
	
	public Dictionary<string, Control> traderOfferLabels;
	public Dictionary<string, Control> playerOfferLabels;
	
	public TradeSave lastSave;
	
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
		
		reserveMenu = GetNode<OptionButton>("ReserveMenu");
		reserveSpin = GetNode<SpinBox>("ReserveSpin");
		reserveButton = GetNode<Button>("ReserveButton");
		
		returnMenu = GetNode<OptionButton>("ReturnMenu");
		returnSpin = GetNode<SpinBox>("ReturnSpin");
		returnButton = GetNode<Button>("ReturnButton");
		
		traderTradeMenu = GetNode<OptionButton>("TraderTradeMenu");
		playerTradeMenu = GetNode<OptionButton>("PlayerTradeMenu");
		
		traderTradeSpin = GetNode<SpinBox>("TraderTradeSpin");
		playerTradeSpin = GetNode<SpinBox>("PlayerTradeSpin");
		
		traderOfferButton = GetNode<Button>("TraderOfferButton");
		traderRetractButton = GetNode<Button>("TraderRetractButton");
		playerOfferButton = GetNode<Button>("PlayerOfferButton");
		playerRetractButton = GetNode<Button>("PlayerRetractButton");
		
		reserveButton.Pressed += OnReservePress;
		returnButton.Pressed += OnReturnPress;
		
		traderOfferButton.Pressed += OnTraderOfferPress;
		traderRetractButton.Pressed += OnTraderRetractPress;
		playerOfferButton.Pressed += OnPlayerOfferPress;
		playerRetractButton.Pressed += OnPlayerRetractPress;
		
		favorProgress = GetNode<ProgressBar>("FavorProgress");
		offerValueLabel = GetNode<Label>("OfferValueLabel");
		
		tradeButton = GetNode<Button>("TradeButton");
		
		tradeButton.Pressed += OnTradePress;
		
		traderTradeVBox = GetNode<VBoxContainer>("TraderTradeScroll/TraderTradeVBox");
		playerTradeVBox = GetNode<VBoxContainer>("PlayerTradeScroll/PlayerTradeVBox");
		
		activeTrader = null;
		activeHub = null;
		
		tradeHubs = new();
		hubInventories = new();
		hubInventories[(0, 0)] = new();
		hubIndex = -1;
		
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
	
	public void LoadSave(TradeSave save)
	{
		if (save.activeTrader >= 0)
		{
			activeTrader = GameData.traders[save.activeTrader];
		}
		else
		{
			activeTrader = null;
		}
		
		Dictionary<(int x, int y), List<HubInvSave>> saves = new();
		
		foreach (var hubInv in save.hubInventories)
		{
			(int x, int y) coord = (hubInv.coordX, hubInv.coordY);
			
			if (!saves.ContainsKey(coord))
			{
				saves[coord] = new();
			}
			
			saves[coord].Add(hubInv);
		}
		
		hubInventories = new();
		tradeHubs = new();
		
		foreach (var hubInv in saves)
		{
			hubInventories[hubInv.Key] = new(hubInv.Value.Count);
			
			foreach (var inv in hubInv.Value)
			{
				hubInventories[hubInv.Key][inv.index] = inv.inventory;
			}
			
			tradeHubs[hubInv.Key] = new();
			
			foreach (var infra in GameData.regionMap[hubInv.Key].infrastructure)
			{
				if (infra.type == "trade")
				{
					tradeHubs[hubInv.Key].Add(infra);
				}
			}
		}
		
		if (save.activeHubI >= 0)
		{
			(int x, int y) coord = (save.activeHubX, save.activeHubY);
			hubIndex = save.activeHubI;
			activeHub = tradeHubs[coord][save.activeHubI];
		}
		else
		{
			activeHub = null;
		}
		
		traderOffer = new(save.traderOffer);
		playerOffer = new(save.playerOffer);
		
		lastSave = save;
		
		UpdateAllLabels();
		
		tradeButton.Disabled = !(traderOffer.Count > 0 || playerOffer.Count > 0);
	}
	
	public void OnReservePress()
	{
		if (activeHub == null || hubIndex == -1 || reserveSpin.Value == 0f)
		{
			return;
		}
		
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		if (hubIndex >= hubInventories[coord].Count)
		{
			return;
		}
		
		Dictionary<string, float> thisInventory = hubInventories[coord][hubIndex];
		
		string resID = (string)reserveMenu.GetSelectedMetadata();
		float amount = Math.Min((float)reserveSpin.Value, (GameData.currentRegion.resources.ContainsKey(resID) ? GameData.currentRegion.resources[resID] : 0f));
		
		if (GameData.currentRegion.resources.ContainsKey(resID))
		{
			GameData.currentRegion.resources[resID] -= amount;
			if (GameData.currentRegion.resources[resID] <= 0f)
			{
				GameData.currentRegion.resources.Remove(resID);
				GameData.resourceControl.UpdateResourcePanels();
			}
		}
		
		if (thisInventory.ContainsKey(resID))
		{
			thisInventory[resID] += amount;
		}
		else
		{
			thisInventory[resID] = amount;
			
			Control rLabel = CreateResourceLabel(resID, amount);
			Label aLabel = rLabel.GetNode<Label>("HBoxContainer/AmountLabel");
			
			reserveResLabels[resID] = aLabel;
			
			reserveVBox.AddChild(rLabel);
		}
	}
	
	public void OnReturnPress()
	{
		if (activeHub == null || hubIndex == -1 || returnSpin.Value == 0f)
		{
			return;
		}
		
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		if (hubIndex >= hubInventories[coord].Count)
		{
			return;
		}
		
		Dictionary<string, float> thisInventory = hubInventories[coord][hubIndex];
		
		string resID = (string)returnMenu.GetSelectedMetadata();
		float amount = Math.Min((float)returnSpin.Value, (thisInventory.ContainsKey(resID) ? thisInventory[resID] : 0f));
		
		if (thisInventory.ContainsKey(resID))
		{
			thisInventory[resID] -= amount;
			if (thisInventory[resID] <= 0f)
			{
				thisInventory.Remove(resID);
			}
		}
		if (GameData.currentRegion.resources.ContainsKey(resID))
		{
			GameData.currentRegion.resources[resID] += amount;
		}
		else
		{
			GameData.currentRegion.resources[resID] = amount;
			GameData.resourceControl.UpdateResourcePanels();
		}
	}
	
	public void OnTraderOfferPress()
	{
		if (activeTrader == null)
		{
			return;
		}
		
		string resID = (string)traderTradeMenu.GetSelectedMetadata();
		float amount = Math.Min((float)traderTradeSpin.Value, (activeTrader.inventory.ContainsKey(resID) ? activeTrader.inventory[resID] : 0f));
		
		if (amount <= 0f)
		{
			return;
		}
		
		if (activeTrader.inventory.ContainsKey(resID))
		{
			activeTrader.inventory[resID] -= amount;
		}
		else
		{
			GD.Print($"TradeControl: active trader inventory does not contain {GameData.RESOURCES[resID].name}");
			return;
		}
		
		if (traderOffer.ContainsKey(resID))
		{
			traderOffer[resID] += amount;
		}
		else
		{
			traderOffer[resID] = amount;
			
			Control rLabel = CreateResourceLabel(resID, amount);
			
			traderOfferLabels[resID] = rLabel;
			
			traderTradeVBox.AddChild(rLabel);
		}
		
		tradeButton.Disabled = false;
		
		UpdateTraderResourceValues();
		UpdateTraderOfferValues();
		UpdateFavorProgress();
	}
	
	public void OnTraderRetractPress()
	{
		if (activeTrader == null)
		{
			return;
		}
		
		if (traderTradeMenu.GetSelectedId() < 0 || (string)traderTradeMenu.GetSelectedMetadata() == "N/A")
		{
			return;
		}
		
		string resID = (string)traderTradeMenu.GetSelectedMetadata();
		
		if (!traderOffer.ContainsKey(resID))
		{
			return;
		}
		
		float amount = traderOffer[resID];
		
		if (activeTrader.inventory.ContainsKey(resID))
		{
			activeTrader.inventory[resID] += amount;
		}
		else
		{
			activeTrader.inventory[resID] = amount;
		}
		
		traderOffer.Remove(resID);
		traderOfferLabels[resID].QueueFree();
		
		tradeButton.Disabled = traderOffer.Count == 0 && playerOffer.Count == 0;
		
		UpdateTraderResourceValues();
		UpdateTraderOfferValues();
		UpdateFavorProgress();
	}
	
	public void OnPlayerOfferPress()
	{
		string resID = (string)playerTradeMenu.GetSelectedMetadata();
		float totalAvailable = GameData.currentRegion.resources.ContainsKey(resID) ? GameData.currentRegion.resources[resID] : 0f;
		
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		Dictionary<string, float> thisInventory = null;
		
		if (hubIndex >= 0 && hubIndex < hubInventories[coord].Count)
		{
			thisInventory = hubInventories[coord][hubIndex]; 
			totalAvailable += thisInventory.ContainsKey(resID) ? thisInventory[resID] : 0f;
		}
		
		float amount = Math.Min((float)playerTradeSpin.Value, totalAvailable);
		float remaining = amount;
		
		if (amount <= 0f)
		{
			return;
		}
		
		float hubAvailable = 0f;
		
		if (thisInventory != null)
		{
			hubAvailable = thisInventory.ContainsKey(resID) ? thisInventory[resID] : 0f;
		}
		
		if (hubAvailable > 0f)
		{
			if (hubAvailable >= remaining)
			{
				thisInventory[resID] -= remaining;
				remaining = 0f;
				
				if (thisInventory[resID] <= 0f)
				{
					thisInventory.Remove(resID);
				}
			}
			else
			{
				remaining -= thisInventory[resID];
				thisInventory.Remove(resID);
			}
		}
		
		if (remaining > 0f && GameData.currentRegion.resources.ContainsKey(resID))
		{
			GameData.currentRegion.resources[resID] -= remaining;
			if (GameData.currentRegion.resources[resID] <= 0f)
			{
				GameData.currentRegion.resources.Remove(resID);
				GameData.resourceControl.UpdateResourcePanels();
			}
		}
		else
		{
			GD.Print($"TradeControl: region storage does not contain {GameData.RESOURCES[resID].name}");
			return;
		}
		
		if (playerOffer.ContainsKey(resID))
		{
			playerOffer[resID] += amount;
		}
		else
		{
			playerOffer[resID] = amount;
			
			Control rLabel = CreateResourceLabel(resID, amount);
			
			playerOfferLabels[resID] = rLabel;
			
			playerTradeVBox.AddChild(rLabel);
		}
		
		tradeButton.Disabled = false;
		
		UpdateReserveResourceValues();
		UpdateRegionResourceValues();
		UpdatePlayerOfferValues();
		UpdateFavorProgress();
	}
	
	public void OnPlayerRetractPress()
	{
		if (playerTradeMenu.GetSelectedId() < 0 || (string)playerTradeMenu.GetSelectedMetadata() == "N/A")
		{
			return;
		}
		
		string resID = (string)playerTradeMenu.GetSelectedMetadata();
		
		if (!playerOffer.ContainsKey(resID))
		{
			return;
		}
		
		float amount = playerOffer[resID];
		
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		Dictionary<string, float> thisInventory = null;
		
		if (hubIndex >= 0 && hubIndex < hubInventories[coord].Count)
		{
			thisInventory = hubInventories[coord][hubIndex];
		}
		
		if (thisInventory != null)
		{
			if (thisInventory.ContainsKey(resID))
			{
				thisInventory[resID] += amount;
			}
			else
			{
				thisInventory[resID] = amount;
			}
		}
		else if (GameData.currentRegion.resources.ContainsKey(resID))
		{
			GameData.currentRegion.resources[resID] += amount;
		}
		else
		{
			GameData.currentRegion.resources[resID] = amount;
			GameData.resourceControl.UpdateResourcePanels();
		}
		
		playerOffer.Remove(resID);
		playerOfferLabels[resID].QueueFree();
		
		tradeButton.Disabled = traderOffer.Count == 0 && playerOffer.Count == 0;
		
		UpdateReserveResourceValues();
		UpdateRegionResourceValues();
		UpdatePlayerOfferValues();
		UpdateFavorProgress();
	}
	
	public void OnTradePress()
	{
		if (activeTrader == null)
		{
			GD.Print("TradeControl: Active Trader is null value");
			return;
		}
		
		GD.Print("TradeControl: Beginning trade negotiations...");
		
		float playerValue = activeTrader.CalculateFavor(playerOffer);
		float traderValue = activeTrader.CalculateFavor(traderOffer) * activeTrader.greed + TAKEOFF_FEE;
		float totalValue = traderValue > 0f ? playerValue / traderValue: 2f;
		
		float rand1 = GameData.rng.Randf();
		float rand2 = GameData.rng.Randf();
		
		if (rand1 <= totalValue && rand2 <= totalValue)
		{
			GD.Print($"TradeControl: Trade confirmed - random numbers {rand1:0.00} and {rand2:0.00} compared to value {totalValue:0.00}");
			TradeOffers();
		}
		else
		{
			GD.Print($"TradeControl: Trade failed - random numbers {rand1:0.00} and {rand2:0.00} compared to value {totalValue:0.00}");
			tradeButton.Disabled = true;
		}
	}
	
	public void OnReserveSelect(long index)
	{
		string resKey = (string)reserveMenu.GetItemMetadata((int)index);
		float value = GameData.currentRegion.resources.ContainsKey(resKey) ? GameData.currentRegion.resources[resKey] : 0f;
		reserveSpin.MaxValue = value;
	}
	
	public void OnReturnSelect(long index)
	{
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		if (hubIndex < 0 || hubIndex >= hubInventories[coord].Count)
		{
			returnSpin.MaxValue = 0f;
			return;
		}
		
		Dictionary<string, float> thisInventory = hubInventories[coord][hubIndex];
		
		string resKey = (string)returnMenu.GetItemMetadata((int)index);
		float value = thisInventory[resKey];
		returnSpin.MaxValue = value;
	}
	
	public void OnTTradeSelect(long index)
	{
		string resKey = (string)traderTradeMenu.GetItemMetadata((int)index);
		float value = activeTrader.inventory[resKey];
		traderTradeSpin.MaxValue = value;
	}
	
	public void OnPTradeSelect(long index)
	{
		string resKey = (string)playerTradeMenu.GetItemMetadata((int)index);
		float value = GameData.currentRegion.resources.ContainsKey(resKey) ? GameData.currentRegion.resources[resKey] : 0f;
		
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		if (hubIndex >= 0 && hubIndex < hubInventories[coord].Count)
		{
			Dictionary<string, float> thisInventory = hubInventories[coord][hubIndex];
			value += thisInventory.ContainsKey(resKey) ? thisInventory[resKey] : 0f;
		}
		
		playerTradeSpin.MaxValue = value;
	}
	
	public void TradeOffers()
	{
		float bonusProsperity = 0f;
		
		bool updateResRegion = false;
		foreach (var res in traderOffer)
		{
			if (GameData.currentRegion.resources.ContainsKey(res.Key))
			{
				GameData.currentRegion.resources[res.Key] += res.Value;
			}
			else
			{
				GameData.currentRegion.resources[res.Key] = res.Value;
				updateResRegion = true;
			}
			
			bonusProsperity -= GameData.RESOURCES[res.Key].value * res.Value;
			
			GameData.galMarket[res.Key].ApplyBear(res.Value);
		}
		
		if (updateResRegion)
		{
			GameData.SortResources(GameData.currentRegion.resources);
			GameData.resourceControl.UpdateResourcePanels();
		}
		
		foreach (var res in playerOffer)
		{
			if (activeTrader.inventory.ContainsKey(res.Key))
			{
				activeTrader.inventory[res.Key] += res.Value;
			}
			else
			{
				activeTrader.inventory[res.Key] = res.Value;
			}
			
			bonusProsperity += GameData.RESOURCES[res.Key].value * res.Value;
			
			GameData.galMarket[res.Key].ApplyBull(res.Value);
		}
		
		float playerValue = activeTrader.CalculateFavor(playerOffer);
		float traderValue = activeTrader.CalculateFavor(traderOffer) * activeTrader.greed + TAKEOFF_FEE;
		float favorRatio = traderValue > 0f ? playerValue / traderValue : 2f;
		float bonusFavor = Mathf.Clamp((favorRatio - 1f) * 0.25f, -0.25f, 0.5f);
		
		activeTrader.AdjustFavor(bonusFavor);
		activeTrader.AdjustProsperity(bonusProsperity);
		
		GameData.currentRegion.landedTraders.Remove(activeTrader);
		
		if (GameData.currentRegion.landedTraders.Count > 0)
		{
			activeTrader = GameData.currentRegion.landedTraders[0];
		}
		else
		{
			activeTrader = null;
		}
		
		traderOffer.Clear();
		playerOffer.Clear();
		
		UpdateAllLabels();
		UpdateFavorProgress();
		
		tradeButton.Disabled = true;
	}
	
	public void UpdateHubReserves()
	{
		reserveResLabels.Clear();
		
		foreach (var child in reserveVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		if (activeHub == null || hubIndex == -1)
		{
			return;
		}
		
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		if (hubIndex >= hubInventories[coord].Count)
		{
			return;
		}
		
		Dictionary<string, float> thisInventory = hubInventories[coord][hubIndex];
		
		foreach (var res in thisInventory)
		{
			Control rLabel = CreateResourceLabel(res.Key, res.Value);
			Label aLabel = rLabel.GetNode<Label>("HBoxContainer/AmountLabel");
			
			reserveResLabels[res.Key] = aLabel;
			
			reserveVBox.AddChild(rLabel);
		}
	}
	
	public Control CreateResourceLabel(string resID, float amount)
	{
		Control rLabel = ResourceLabelScene.Instantiate<Control>();
		Label nLabel = rLabel.GetNode<Label>("HBoxContainer/NameLabel");
		Label aLabel = rLabel.GetNode<Label>("HBoxContainer/AmountLabel");
		
		nLabel.Text = GameData.RESOURCES[resID].name;
		aLabel.Text = GameData.FormatUnit(amount, resID);
		
		return rLabel;
	}
	
	public void SetAmountLabelText(Control root, string resID, float amount)
	{
		if (root == null)
		{
			return;
		}
		
		Label aLabel = root.GetNode<Label>("HBoxContainer/AmountLabel");
		aLabel.Text = GameData.FormatUnit(amount, resID);
	}
	
	public void UpdateRegionTrade()
	{
		activeTrader = null;
		if (GameData.currentRegion.landedTraders.Count > 0)
		{
			activeTrader = GameData.currentRegion.landedTraders[0];
		}
		
		//tradeHubs.Clear();
		activeHub = null;
		hubIndex = -1;
		
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		if (hubInventories.ContainsKey(coord) && hubInventories[coord].Count > 0)
		{
			activeHub = tradeHubs[coord][0];
			hubIndex = 0;
		}
		/*foreach (var infra in GameData.currentRegion.infrastructure)
		{
			if (infra.type == "trade")
			{
				tradeHubs.Add(infra);
			}
		}*/
		
		/*if (tradeHubs.Count > 0)
		{
			activeHub = tradeHubs[0];
			hubIndex = 0;
		}*/
		
		UpdateAllLabels();
	}
	
	public void UpdateAllLabels()
	{
		UpdateTraderResourceLabels();
		UpdateReserveResourceLabels();
		UpdateRegionResourceLabels();
		UpdateTraderOfferLabels();
		UpdatePlayerOfferLabels();
	}
	
	public void RetractAllOffers()
	{
		if (activeTrader != null)
		{
			foreach (var res in traderOffer)
			{
				if (activeTrader.inventory.ContainsKey(res.Key))
				{
					activeTrader.inventory[res.Key] += res.Value;
				}
				else
				{
					activeTrader.inventory[res.Key] = res.Value;
				}
			}
		}
		
		traderOffer.Clear();
		
		bool updateResPanels = false;
		foreach (var res in playerOffer)
		{
			if (GameData.currentRegion.resources.ContainsKey(res.Key))
			{
				GameData.currentRegion.resources[res.Key] += res.Value;
			}
			else
			{
				GameData.currentRegion.resources[res.Key] = res.Value;
				updateResPanels = true;
			}
		}
		
		playerOffer.Clear();
		
		if (updateResPanels)
		{
			GameData.SortResources(GameData.currentRegion.resources);
			GameData.resourceControl.UpdateResourcePanels();
		}
		
		UpdateAllLabels();
	}
	
	public void UpdateTraderResourceLabels()
	{
		traderResLabels.Clear();
		
		foreach (var child in traderVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		string selectedMeta = (string)traderTradeMenu.GetSelectedMetadata();
		int selectedIdx = -1;
		traderTradeMenu.Clear();
		
		if (activeTrader == null || activeTrader.inventory.Count == 0)
		{
			traderTradeMenu.AddItem("No Trader Inventory");
			traderTradeMenu.SetItemMetadata(0, "N/A");
			traderTradeMenu.Select(0);
			traderTradeMenu.Disabled = true;
			traderTradeSpin.Editable = false;
			traderOfferButton.Disabled = true;
			traderRetractButton.Disabled = true;
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
			
			traderTradeMenu.AddItem(GameData.RESOURCES[res.Key].name);
			int idx = traderTradeMenu.ItemCount - 1;
			traderTradeMenu.SetItemMetadata(idx, res.Key);
			
			if (selectedIdx < 0 && selectedMeta == res.Key)
			{
				selectedIdx = idx;
				traderTradeMenu.Select(idx);
			}
		}
		
		if (selectedIdx < 0)
		{
			traderTradeMenu.Select(0);
		}
		
		traderTradeMenu.Disabled = false;
		traderTradeSpin.Editable = true;
		traderOfferButton.Disabled = false;
		traderRetractButton.Disabled = false;
	}
	
	public void UpdateReserveResourceLabels()
	{
		reserveResLabels.Clear();
		
		foreach (var child in reserveVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		string selectedMeta = (string)returnMenu.GetSelectedMetadata();
		int selectedIdx = -1;
		returnMenu.Clear();
		
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		Dictionary<string, float> thisInventory = (hubIndex >= 0 && hubIndex < hubInventories[coord].Count) ? hubInventories[coord][hubIndex] : null;
		
		if (hubIndex < 0 || hubIndex >= hubInventories[coord].Count || thisInventory == null || thisInventory.Count == 0)
		{
			returnMenu.AddItem("No Trade Reserves");
			returnMenu.SetItemMetadata(0, "N/A");
			returnMenu.Select(0);
			returnMenu.Disabled = true;
			returnSpin.Editable = false;
			returnButton.Disabled = true;
			return;
		}
		
		foreach (var res in thisInventory)
		{
			Control rLabel = ResourceLabelScene.Instantiate<Control>();
			Label nLabel = rLabel.GetNode<Label>("HBoxContainer/NameLabel");
			Label aLabel = rLabel.GetNode<Label>("HBoxContainer/AmountLabel");
			
			nLabel.Text = GameData.RESOURCES[res.Key].name;
			aLabel.Text = GameData.FormatUnit(res.Value, res.Key);
			
			reserveResLabels[res.Key] = aLabel;
			
			reserveVBox.AddChild(rLabel);
			
			returnMenu.AddItem(GameData.RESOURCES[res.Key].name);
			int idx = returnMenu.ItemCount - 1;
			returnMenu.SetItemMetadata(idx, res.Key);
			
			if (selectedIdx < 0 && selectedMeta == res.Key)
			{
				selectedIdx = idx;
				returnMenu.Select(idx);
			}
		}
		
		if (selectedIdx < 0)
		{
			returnMenu.Select(0);
		}
		
		returnMenu.Disabled = false;
		returnSpin.Editable = true;
		returnButton.Disabled = false;
	}
	
	public void UpdateRegionResourceLabels()
	{
		resourceResLabels.Clear();
		
		foreach (var child in resourceVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		/*string selectedMeta = (string)playerTradeMenu.GetSelectedMetadata();
		int selectedIdx = -1;
		playerTradeMenu.Clear();*/
		
		string selectedMeta = (string)reserveMenu.GetSelectedMetadata();
		int selectedIdx = -1;
		reserveMenu.Clear();
		
		if (GameData.currentRegion.resources.Count == 0)
		{
			//playerTradeMenu.AddItem("No Region Resources");
			reserveMenu.AddItem("No Region Resources");
			//playerTradeMenu.SetItemMetadata(0, "N/A");
			reserveMenu.SetItemMetadata(0, "N/A");
			//playerTradeMenu.Select(0);
			reserveMenu.Select(0);
			//playerTradeMenu.Disabled = true;
			//playerTradeSpin.Editable = false;
			//playerOfferButton.Disabled = true;
			//playerRetractButton.Disabled = true;
			reserveMenu.Disabled = true;
			reserveSpin.Editable = false;
			reserveButton.Disabled = true;
			return;
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
			
			/*playerTradeMenu.AddItem(GameData.RESOURCES[res.Key].name);
			int idx = playerTradeMenu.ItemCount - 1;
			playerTradeMenu.SetItemMetadata(idx, res.Key);
			
			if (selectedIdx < 0 && selectedMeta == res.Key)
			{
				selectedIdx = idx;
				playerTradeMenu.Select(idx);
			}*/
			
			reserveMenu.AddItem(GameData.RESOURCES[res.Key].name);
			int idx = reserveMenu.ItemCount - 1;
			reserveMenu.SetItemMetadata(idx, res.Key);
			
			if (selectedIdx < 0 && selectedMeta == res.Key)
			{
				selectedIdx = idx;
				reserveMenu.Select(idx);
			}
		}
		
		/*if (selectedIdx < 0)
		{
			playerTradeMenu.Select(0);
		}*/
		
		playerTradeMenu.Disabled = false;
		playerTradeSpin.Editable = true;
		playerOfferButton.Disabled = false;
		playerRetractButton.Disabled = false;
		reserveMenu.Disabled = false;
		reserveSpin.Editable = true;
		reserveButton.Disabled = false;
	}
	
	public void UpdatePlayerTradeMenu()
	{
		List<string> resKeys = new();
		
		foreach (var res in GameData.currentRegion.resources.Keys)
		{
			if (!resKeys.Contains(res))
			{
				resKeys.Add(res);
			}
		}
		
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		if (hubIndex >= 0 && hubIndex < hubInventories[coord].Count)
		{
			List<string> hubRes = new(hubInventories[coord][hubIndex].Keys);
			
			foreach (var res in hubRes)
			{
				if (!resKeys.Contains(res))
				{
					resKeys.Add(res);
				}
			}
		}
		
		playerTradeMenu.Clear();
		
		if (resKeys.Count == 0)
		{
			playerTradeMenu.AddItem("No Region Resources");
			playerTradeMenu.SetItemMetadata(0, "N/A");
			playerTradeMenu.Select(0);
			playerTradeMenu.Disabled = true;
			playerTradeSpin.Editable = false;
			playerOfferButton.Disabled = true;
			playerRetractButton.Disabled = true;
			return;
		}
		
		resKeys.Sort(GameData.CompareResources);
		
		foreach (var res in resKeys)
		{
			string resName = GameData.RESOURCES.ContainsKey(res) ? GameData.RESOURCES[res].name : "invalid res key";
			playerTradeMenu.AddItem(resName);
			int idx = playerTradeMenu.ItemCount - 1;
			playerTradeMenu.SetItemMetadata(idx, res);
		}
		
		playerTradeMenu.Disabled = false;
		playerTradeSpin.Editable = true;
		playerOfferButton.Disabled = false;
		playerRetractButton.Disabled = false;
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
			Control rLabel = CreateResourceLabel(res.Key, res.Value);
			
			traderOfferLabels[res.Key] = rLabel;
			
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
			Control rLabel = CreateResourceLabel(res.Key, res.Value);
			
			playerOfferLabels[res.Key] = rLabel;
			
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
		
		if (traderTradeMenu.GetSelectedId() < 0 || (string)traderTradeMenu.GetSelectedMetadata() == "N/A")
		{
			traderTradeSpin.MaxValue = 0;
			return;
		}
		
		string resID = (string)traderTradeMenu.GetSelectedMetadata();
		traderTradeSpin.MaxValue = activeTrader.inventory[resID];
	}
	
	public void UpdateReserveResourceValues()
	{
		if (hubIndex < 0)
		{
			return;
		}
		
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		if (hubIndex >= hubInventories[coord].Count)
		{
			return;
		}
		
		Dictionary<string, float> thisInventory = hubInventories[coord][hubIndex];
		
		foreach (var res in thisInventory)
		{
			reserveResLabels[res.Key].Text = GameData.FormatUnit(res.Value, res.Key);
		}
		
		if (returnMenu.GetSelectedId() < 0 || (string)returnMenu.GetSelectedMetadata() == "N/A")
		{
			returnSpin.MaxValue = 0;
			return;
		}
		
		string resID = (string)returnMenu.GetSelectedMetadata();
		returnSpin.MaxValue = thisInventory[resID];
	}
	
	public void UpdateRegionResourceValues()
	{
		foreach (var res in GameData.currentRegion.resources)
		{
			resourceResLabels[res.Key].Text = GameData.FormatUnit(res.Value, res.Key);
		}
		
		if (playerTradeMenu.GetSelectedId() < 0 || (string)playerTradeMenu.GetSelectedMetadata() == "N/A")
		{
			playerTradeSpin.MaxValue = 0;
			return;
		}
		
		string resID = (string)playerTradeMenu.GetSelectedMetadata();
		playerTradeSpin.MaxValue = GameData.currentRegion.resources[resID];
	}
	
	public void UpdateTraderOfferValues()
	{
		if (activeTrader == null)
		{
			return;
		}
		
		foreach (var res in traderOffer)
		{
			if (traderOfferLabels.TryGetValue(res.Key, out Control root))
			{
				SetAmountLabelText(root, res.Key, res.Value);
			}
		}
	}
	
	public void UpdatePlayerOfferValues()
	{
		foreach (var res in playerOffer)
		{
			if (playerOfferLabels.TryGetValue(res.Key, out Control root))
			{
				SetAmountLabelText(root, res.Key, res.Value);
			}
		}
	}
	
	public void UpdateFavorProgress()
	{
		float playerValue = activeTrader.CalculateFavor(playerOffer);
		float traderValue = activeTrader.CalculateFavor(traderOffer) * activeTrader.greed + TAKEOFF_FEE;
		
		favorProgress.Value = (playerValue > 0f && traderValue > 0f ? playerValue / traderValue : 0f) * 100f;
		offerValueLabel.Text = $"{playerValue:0.00} / {traderValue:0.00}";
	}
	
	public void AddTradeHub(Infrastructure infra)
	{
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		if (!tradeHubs.ContainsKey(coord))
		{
			tradeHubs[coord] = new();
		}
		
		tradeHubs[coord].Add(infra);
		
		if (!hubInventories.ContainsKey(coord))
		{
			hubInventories[coord] = new();
		}
		
		hubInventories[coord].Add(new Dictionary<string, float>());
		
		if (activeHub == null)
		{
			activeHub = infra;
			hubIndex = 0;
		}
	}
	
	public void RemoveTradeHub(Infrastructure infra)
	{
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		if (!hubInventories.ContainsKey(coord) || !tradeHubs.ContainsKey(coord) || !tradeHubs[coord].Contains(infra))
		{
			return;
		}
		
		int idx = tradeHubs[coord].IndexOf(infra);
		
		tradeHubs[coord].Remove(infra);
		
		Dictionary<string, float> thisInventory = hubInventories[coord][idx];
		
		foreach (var res in thisInventory)
		{
			if (GameData.currentRegion.resources.ContainsKey(res.Key))
			{
				GameData.currentRegion.resources[res.Key] += res.Value;
			}
			else
			{
				GameData.currentRegion.resources[res.Key] = res.Value;
				GameData.resourceControl.UpdateResourcePanels();
			}
		}
		
		hubInventories[coord].RemoveAt(idx);
		
		if (hubInventories[coord].Count == 0)
		{
			hubInventories.Remove(coord);
		}
	}
}

public class TradeSave
{
	public int activeTrader;
	public int activeHubX;
	public int activeHubY;
	public int activeHubI;
	
	public List<HubInvSave> hubInventories;
	
	public Dictionary<string, float> traderOffer;
	public Dictionary<string, float> playerOffer;
	
	public TradeSave()
	{
		activeTrader = -1;
		activeHubX = 0;
		activeHubY = 0;
		activeHubI = -1;
		
		hubInventories = new();
		
		traderOffer = new();
		playerOffer = new();
	}
	
	public TradeSave(TradeControl tc)
	{
		activeTrader = tc.activeTrader != null ? tc.activeTrader.idNum : -1;
		activeHubX = 0;
		activeHubY = 0;
		activeHubI = -1;
		
		hubInventories = new();
		
		foreach (var hubInv in tc.hubInventories)
		{
			for (int i = 0; i < hubInv.Value.Count; i++)
			{
				hubInventories.Add(new HubInvSave(hubInv.Key.x, hubInv.Key.y, i, hubInv.Value[i]));
			}
		}
		
		(int x, int y) coord = (GameData.currentRegion.coordX, GameData.currentRegion.coordY);
		
		if (tc.tradeHubs.ContainsKey(coord))
		{
			for (int i = 0; i < tc.tradeHubs[coord].Count; i++)
			{
				if (tc.activeHub == tc.tradeHubs[coord][i])
				{
					activeHubX = coord.x;
					activeHubY = coord.y;
					activeHubI = i;
					break;
				}
			}
		}
		
		traderOffer = new(tc.traderOffer);
		playerOffer = new(tc.playerOffer);
	}
}

public class HubInvSave
{
	public int coordX;
	public int coordY;
	public int index;
	
	public Dictionary<string, float> inventory;
	
	public HubInvSave()
	{
		coordX = 0;
		coordY = 0;
		index = -1;
		
		inventory = new();
	}
	
	public HubInvSave(int x, int y, int i, Dictionary<string, float> inv)
	{
		coordX = x;
		coordY = y;
		index = i;
		
		inventory = new(inv);
	}
}
