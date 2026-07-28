using Godot;
using System;
using System.Collections.Generic;

public partial class TradeControl : Node
{
	[Export] public PackedScene ResourceLabelScene;
	
	public const float TAKEOFF_FEE = 1f;
	
	public Trader activeTrader;
	
	public Dictionary<string, float> traderOffer;
	public Dictionary<string, float> playerOffer;
	
	public Label traderNameLabel;
	
	public VBoxContainer traderVBox;
	public VBoxContainer reserveVBox;
	public VBoxContainer resourceVBox;
	
	public Button previousTraderButton;
	public Button nextTraderButton;
	
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
	
	public Panel favorPanel;
	public ProgressBar favorProgress;
	public Label offerValueLabel;
	
	public Panel creditsPanel;
	public Label creditsLabel;
	
	public Button tradeButton;
	public Button dismissButton;
	
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
		traderNameLabel = GetNode<Label>("TraderNameLabel");
		
		traderVBox = GetNode<VBoxContainer>("TraderScroll/TraderVBox");
		reserveVBox = GetNode<VBoxContainer>("ReserveScroll/ReserveVBox");
		resourceVBox = GetNode<VBoxContainer>("ResourceScroll/ResourceVBox");
		
		previousTraderButton = GetNode<Button>("PreviousTraderButton");
		nextTraderButton = GetNode<Button>("NextTraderButton");
		
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
		
		reserveMenu.ItemSelected += OnReserveSelect;
		returnMenu.ItemSelected += OnReturnSelect;
		
		reserveButton.Pressed += OnReservePress;
		returnButton.Pressed += OnReturnPress;
		
		previousTraderButton.Pressed += OnPreviousTraderPress;
		nextTraderButton.Pressed += OnNextTraderPress;
		
		traderOfferButton.Pressed += OnTraderOfferPress;
		traderRetractButton.Pressed += OnTraderRetractPress;
		playerOfferButton.Pressed += OnPlayerOfferPress;
		playerRetractButton.Pressed += OnPlayerRetractPress;
		
		favorPanel = GetNode<Panel>("FavorPanel");
		favorProgress = GetNode<ProgressBar>("FavorPanel/FavorProgress");
		offerValueLabel = GetNode<Label>("FavorPanel/OfferValueLabel");
		
		creditsPanel = GetNode<Panel>("CreditsPanel");
		creditsLabel = GetNode<Label>("CreditsPanel/CreditsLabel");
		
		tradeButton = GetNode<Button>("TradeButton");
		dismissButton = GetNode<Button>("DismissButton");
		
		tradeButton.Pressed += OnTradePress;
		dismissButton.Pressed += OnDismissPress;
		
		traderTradeVBox = GetNode<VBoxContainer>("TraderTradeScroll/TraderTradeVBox");
		playerTradeVBox = GetNode<VBoxContainer>("PlayerTradeScroll/PlayerTradeVBox");
		
		activeTrader = null;
		
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
		UpdateReserveResourceValues();
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
		
		traderOffer = new(save.traderOffer);
		playerOffer = new(save.playerOffer);
		
		lastSave = save;
		
		UpdateTraderName();
		UpdateAllLabels();
		
		tradeButton.Disabled = !(traderOffer.Count > 0 || playerOffer.Count > 0);
	}
	
	public void OnReservePress()
	{
		if (!GameData.currentRegion.ContainsTrade() || reserveSpin.Value == 0f)
		{
			return;
		}
		
		Dictionary<string, float> thisInventory = GameData.currentRegion.reserves;
		
		string resID = (string)reserveMenu.GetSelectedMetadata();
		float amount = Math.Min((float)reserveSpin.Value, (GameData.currentRegion.resources.ContainsKey(resID) ? GameData.currentRegion.resources[resID] : 0f));
		
		if (resID == "N/A" || amount <= 0f)
		{
			return;
		}
		
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
		
		UpdateRegionResourceLabels();
		UpdateReserveResourceLabels();
		UpdateResRetMenus();
		UpdatePlayerTradeMenu();
	}
	
	public void OnReturnPress()
	{
		if (!GameData.currentRegion.ContainsTrade() || returnSpin.Value == 0f)
		{
			return;
		}
		
		Dictionary<string, float> thisInventory = GameData.currentRegion.reserves;
		
		string resID = (string)returnMenu.GetSelectedMetadata();
		float amount = Math.Min((float)returnSpin.Value, (thisInventory.ContainsKey(resID) ? thisInventory[resID] : 0f));
		
		if (resID == "N/A" || amount <= 0f)
		{
			return;
		}
		
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
		
		UpdateRegionResourceLabels();
		UpdateReserveResourceLabels();
		UpdateResRetMenus();
		UpdatePlayerTradeMenu();
	}
	
	public void OnPreviousTraderPress()
	{
		if (GameData.currentRegion.landedTraders.Count <= 0)
		{
			return;
		}
		
		int index = GameData.currentRegion.landedTraders.IndexOf(activeTrader);
		
		GD.Print($"TradeControl: Previous Trader - old trader index {index} (Max: {GameData.currentRegion.landedTraders.Count - 1})");
		
		index = Math.Max(index - 1, 0);
		
		activeTrader = GameData.currentRegion.landedTraders[index];
		
		GD.Print($"Trade Control: Previous Trader - new index {index} (Max: {GameData.currentRegion.landedTraders.Count - 1})");
		
		previousTraderButton.Disabled = index <= 0;
		nextTraderButton.Disabled = (index == -1 || index >= GameData.currentRegion.landedTraders.Count - 1);
		
		UpdateTraderName();
		UpdateTraderResourceLabels();
		UpdateTraderTradeMenu();
		UpdateFavorCreditsDisplay();
	}
	
	public void OnNextTraderPress()
	{
		if (GameData.currentRegion.landedTraders.Count <= 0)
		{
			return;
		}
		
		int index = GameData.currentRegion.landedTraders.IndexOf(activeTrader);
		
		GD.Print($"TradeControl: Next Trader - old trader index {index} (Max: {GameData.currentRegion.landedTraders.Count - 1})");
		
		if (index == -1 || index > GameData.currentRegion.landedTraders.Count - 2)
		{
			return;
		}
		
		index = Math.Min(index + 1, GameData.currentRegion.landedTraders.Count - 1);
		
		activeTrader = GameData.currentRegion.landedTraders[index];
		
		GD.Print($"Trade Control: Next Trader - new index {index} (Max: {GameData.currentRegion.landedTraders.Count - 1})");
		
		previousTraderButton.Disabled = index <= 0;
		nextTraderButton.Disabled = (index == -1 || index >= GameData.currentRegion.landedTraders.Count - 1);
		
		UpdateTraderName();
		UpdateTraderResourceLabels();
		UpdateTraderTradeMenu();
		UpdateFavorCreditsDisplay();
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
		
		UpdateTraderResourceLabels();
		UpdateTraderOfferLabels();
		UpdateTraderTradeMenu();
		
		if (activeTrader != null && activeTrader.corporate)
		{
			UpdateCreditValue();
		}
		else
		{
			UpdateFavorProgress();
		}
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
		
		UpdateTraderResourceLabels();
		UpdateTraderOfferLabels();
		UpdateTraderTradeMenu();
		
		if (activeTrader != null && activeTrader.corporate)
		{
			UpdateCreditValue();
		}
		else
		{
			UpdateFavorProgress();
		}
	}
	
	public void OnPlayerOfferPress()
	{
		string resID = (string)playerTradeMenu.GetSelectedMetadata();
		float totalAvailable = GameData.currentRegion.resources.ContainsKey(resID) ? GameData.currentRegion.resources[resID] : 0f;
		
		Dictionary<string, float> thisInventory = null;
		
		if (GameData.currentRegion.ContainsTrade())
		{
			thisInventory = GameData.currentRegion.reserves;
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
			remaining = 0f;
			if (GameData.currentRegion.resources[resID] <= 0f)
			{
				GameData.currentRegion.resources.Remove(resID);
				GameData.resourceControl.UpdateResourcePanels();
			}
		}
		/*else
		{
			GD.Print($"TradeControl: region storage does not contain {GameData.RESOURCES[resID].name}");
			return;
		}*/
		
		if (remaining > 0f)
		{
			amount -= remaining;
		}
		
		if (amount <= 0f)
		{
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
		
		UpdateReserveResourceLabels();
		UpdateRegionResourceLabels();
		UpdatePlayerOfferLabels();
		UpdateResRetMenus();
		UpdatePlayerTradeMenu();
		
		if (activeTrader != null && activeTrader.corporate)
		{
			UpdateCreditValue();
		}
		else
		{
			UpdateFavorProgress();
		}
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
		
		Dictionary<string, float> thisInventory = null;
		
		if (GameData.currentRegion.ContainsTrade())
		{
			thisInventory = GameData.currentRegion.reserves;
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
		
		UpdateReserveResourceLabels();
		UpdateRegionResourceLabels();
		UpdatePlayerOfferLabels();
		UpdateResRetMenus();
		UpdatePlayerTradeMenu();
		
		if (activeTrader != null && activeTrader.corporate)
		{
			UpdateCreditValue();
		}
		else
		{
			UpdateFavorProgress();
		}
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
		float traderValue = activeTrader.CalculateFavor(traderOffer) * activeTrader.greed + activeTrader.data.takeoffFee;
		float totalValue = traderValue > 0f ? playerValue / traderValue: 2f;
		
		if (activeTrader.corporate)
		{
			if (GameData.credits + totalValue >= 0f)
			{
				GD.Print($"TradeControl: Corporate Trade confirmed - new balance {GameData.credits + totalValue}");
				TradeOffers();
			}
			else
			{
				GD.Print($"TradeControl: Corporate Trade failed - insufficient funds");
				tradeButton.Disabled = true;
			}
		}
		else
		{
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
	}
	
	public void OnDismissPress()
	{
		if (activeTrader == null)
		{
			return;
		}
		
		RetractAllOffers();
		
		if (activeTrader.corporate)
		{
			GameData.credits -= activeTrader.data.takeoffFee;
		}
		else
		{
			activeTrader.AdjustFavor(-0.25f);
		}
		
		activeTrader.SetState(Trader.TraderStatus.Departing);
		
		GameData.currentRegion.landedTraders.Remove(activeTrader);
		
		if (GameData.currentRegion.landedTraders.Count > 0)
		{
			activeTrader = GameData.currentRegion.landedTraders[0];
		}
		else
		{
			activeTrader = null;
		}
		
		int traderIndex = GameData.currentRegion.landedTraders.IndexOf(activeTrader);
		
		previousTraderButton.Disabled = (GameData.currentRegion.landedTraders.Count < 2 || traderIndex <= 0);
		nextTraderButton.Disabled = (GameData.currentRegion.landedTraders.Count < 2 || traderIndex == -1 || traderIndex >= GameData.currentRegion.landedTraders.Count - 1);
		
		if (activeTrader != null && activeTrader.corporate)
		{
			UpdateCreditValue();
		}
		else
		{
			UpdateFavorProgress();
		}
		
		UpdateFavorCreditsDisplay();
		
		tradeButton.Disabled = true;
	}
	
	public void OnReserveSelect(long index)
	{
		string resKey = (string)reserveMenu.GetItemMetadata((int)index);
		float value = GameData.currentRegion.resources.ContainsKey(resKey) ? GameData.currentRegion.resources[resKey] : 0f;
		reserveSpin.MaxValue = value;
	}
	
	public void OnReturnSelect(long index)
	{
		if (!GameData.currentRegion.ContainsTrade())
		{
			returnSpin.MaxValue = 0f;
			return;
		}
		
		Dictionary<string, float> thisInventory = GameData.currentRegion.reserves;
		
		string resKey = (string)returnMenu.GetItemMetadata((int)index);
		float value = thisInventory.ContainsKey(resKey) ? thisInventory[resKey] : 0f;
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
		
		if (GameData.currentRegion.ContainsTrade())
		{
			value += GameData.currentRegion.reserves.ContainsKey(resKey) ? GameData.currentRegion.reserves[resKey] : 0f;
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
		float traderValue = activeTrader.CalculateFavor(traderOffer) * activeTrader.greed + activeTrader.data.takeoffFee;
		
		if (activeTrader.corporate)
		{
			float totalValue = playerValue - traderValue;
			GameData.credits += totalValue;
			GameData.androidControl.UpdateCreditDebt();
		}
		else
		{
			float favorRatio = traderValue > 0f ? playerValue / traderValue : 2f;
			float bonusFavor = Mathf.Clamp((favorRatio - 1f) * 0.25f, -0.25f, 0.5f);
			
			activeTrader.AdjustFavor(bonusFavor);
			activeTrader.AdjustProsperity(bonusProsperity);
		}
		
		activeTrader.SetState(Trader.TraderStatus.Departing);
		
		GameData.currentRegion.landedTraders.Remove(activeTrader);
		
		if (GameData.currentRegion.landedTraders.Count > 0)
		{
			activeTrader = GameData.currentRegion.landedTraders[0];
		}
		else
		{
			activeTrader = null;
		}
		
		int traderIndex = GameData.currentRegion.landedTraders.IndexOf(activeTrader);
		
		previousTraderButton.Disabled = (GameData.currentRegion.landedTraders.Count < 2 || traderIndex <= 0);
		nextTraderButton.Disabled = (GameData.currentRegion.landedTraders.Count < 2 || traderIndex == -1 || traderIndex >= GameData.currentRegion.landedTraders.Count - 1);
		
		traderOffer.Clear();
		playerOffer.Clear();
		
		UpdateAllLabels();
		UpdateFavorCreditsDisplay();
		
		if (activeTrader != null && activeTrader.corporate)
		{
			UpdateCreditValue();
		}
		else
		{
			UpdateFavorProgress();
		}
		
		tradeButton.Disabled = true;
	}
	
	public void ResetHubs()
	{
		UpdateHubReserves();
	}
	
	public void UpdateTraderName()
	{
		if (activeTrader == null || GameData.currentRegion.landedTraders.Count == 0)
		{
			traderNameLabel.Text = "No Trader";
			return;
		}
		
		traderNameLabel.Text = activeTrader.name;
	}
	
	public void UpdateHubReserves()
	{
		reserveResLabels.Clear();
		
		foreach (var child in reserveVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		if (!GameData.currentRegion.ContainsTrade())
		{
			return;
		}
		
		Dictionary<string, float> thisInventory = GameData.currentRegion.reserves;
		
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
		int traderIndex = GameData.currentRegion.landedTraders.IndexOf(activeTrader);
		
		previousTraderButton.Disabled = (GameData.currentRegion.landedTraders.Count < 2 || traderIndex <= 0);
		nextTraderButton.Disabled = (GameData.currentRegion.landedTraders.Count < 2 || traderIndex == -1 || traderIndex >= GameData.currentRegion.landedTraders.Count - 1);
		
		UpdateTraderName();
		UpdateAllLabels();
	}
	
	public void UpdateAllLabels()
	{
		UpdateTraderResourceLabels();
		UpdateReserveResourceLabels();
		UpdateRegionResourceLabels();
		UpdateTraderOfferLabels();
		UpdatePlayerOfferLabels();
		UpdateResRetMenus();
		UpdateTraderTradeMenu();
		UpdatePlayerTradeMenu();
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
		
		if (activeTrader == null || activeTrader.inventory.Count == 0)
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
	
	public void UpdateReserveResourceLabels()
	{
		reserveResLabels.Clear();
		
		foreach (var child in reserveVBox.GetChildren())
		{
			child.QueueFree();
		}
		
		Dictionary<string, float> thisInventory = GameData.currentRegion.reserves;
		
		if (thisInventory == null)
		{
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
	
	public void UpdateResRetMenus()
	{
		string selectedResMeta = (string)reserveMenu.GetSelectedMetadata();
		int selectedResIdx = -1;
		
		reserveMenu.Clear();
		
		if (GameData.currentRegion.resources.Count > 0)
		{
			foreach (var res in GameData.currentRegion.resources)
			{
				reserveMenu.AddItem(GameData.RESOURCES[res.Key].name);
				int idx = reserveMenu.ItemCount - 1;
				reserveMenu.SetItemMetadata(idx, res.Key);
				
				if (selectedResIdx < 0 && selectedResMeta == res.Key)
				{
					selectedResIdx = idx;
					reserveMenu.Select(idx);
				}
			}
			reserveMenu.Disabled = false;
			reserveSpin.Editable = true;
			reserveButton.Disabled = false;
		}
		else
		{
			reserveMenu.AddItem("No region resources");
			reserveMenu.SetItemMetadata(0, "N/A");
			reserveMenu.Disabled = true;
			reserveSpin.Editable = false;
			reserveButton.Disabled = true;
		}
		
		if (selectedResIdx < 0)
		{
			reserveMenu.Select(0);
		}
		
		string selectedRetMeta = (string)returnMenu.GetSelectedMetadata();
		int selectedRetIdx = -1;
		
		returnMenu.Clear();
		
		if (GameData.currentRegion.ContainsTrade() && GameData.currentRegion.reserves.Count > 0)
		{
			foreach (var res in GameData.currentRegion.reserves)
			{
				returnMenu.AddItem(GameData.RESOURCES[res.Key].name);
				int idx = returnMenu.ItemCount - 1;
				returnMenu.SetItemMetadata(idx, res.Key);
				
				if (selectedRetIdx < 0 && selectedRetMeta == res.Key)
				{
					selectedRetIdx = idx;
					returnMenu.Select(idx);
				}
			}
			returnMenu.Disabled = false;
			returnSpin.Editable = true;
			returnButton.Disabled = false;
		}
		else
		{
			returnMenu.AddItem("No reserved resources");
			returnMenu.SetItemMetadata(0, "N/A");
			returnMenu.Disabled = true;
			returnSpin.Editable = false;
			returnButton.Disabled = true;
		}
		
		if (selectedRetIdx < 0)
		{
			returnMenu.Select(0);
		}
	}
	
	public void UpdateTraderTradeMenu()
	{
		List<string> resKeys = new();
		
		if (activeTrader != null && activeTrader.inventory.Count > 0)
		{
			foreach (var res in activeTrader.inventory.Keys)
			{
				resKeys.Add(res);
			}
		}
		
		if (activeTrader != null && traderOffer.Count > 0)
		{
			foreach (var res in traderOffer.Keys)
			{
				if (!resKeys.Contains(res))
				{
					resKeys.Add(res);
				}
			}
		}
		
		string selectedMeta = (string)traderTradeMenu.GetSelectedMetadata();
		int selectedIdx = -1;
		traderTradeMenu.Clear();
		
		resKeys.Sort(GameData.CompareResources);
		
		if (resKeys.Count > 0)
		{
			foreach (var res in resKeys)
			{
				traderTradeMenu.AddItem(GameData.RESOURCES[res].name);
				int idx = traderTradeMenu.ItemCount - 1;
				traderTradeMenu.SetItemMetadata(idx, res);
				
				if (selectedIdx < 0 && selectedMeta == res)
				{
					selectedIdx = idx;
					traderTradeMenu.Select(idx);
				}
			}
			
			traderTradeMenu.Disabled = false;
			traderTradeSpin.Editable = true;
			traderOfferButton.Disabled = false;
			traderRetractButton.Disabled = false;
		}
		else
		{
			traderTradeMenu.AddItem("No trader inventory");
			traderTradeMenu.SetItemMetadata(0, "N/A");
			traderTradeMenu.Disabled = true;
			traderTradeSpin.Editable = false;
			traderOfferButton.Disabled = true;
			traderRetractButton.Disabled = true;
		}
		
		if (selectedIdx < 0)
		{
			traderTradeMenu.Select(0);
		}
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
		
		if (GameData.currentRegion.ContainsTrade() && GameData.currentRegion.reserves.Count > 0)
		{
			List<string> hubRes = new(GameData.currentRegion.reserves.Keys);
			
			foreach (var res in hubRes)
			{
				if (!resKeys.Contains(res))
				{
					resKeys.Add(res);
				}
			}
		}
		
		foreach (var res in playerOffer.Keys)
		{
			if (!resKeys.Contains(res))
			{
				resKeys.Add(res);
			}
		}
		
		string selectedMeta = (string)playerTradeMenu.GetSelectedMetadata();
		int selectedIdx = -1;
		playerTradeMenu.Clear();
		
		resKeys.Sort(GameData.CompareResources);
		
		if (resKeys.Count > 0)
		{
			foreach (var res in resKeys)
			{
				string resName = GameData.RESOURCES.ContainsKey(res) ? GameData.RESOURCES[res].name : "invalid res key";
				playerTradeMenu.AddItem(resName);
				int idx = playerTradeMenu.ItemCount - 1;
				playerTradeMenu.SetItemMetadata(idx, res);
				
				if (selectedIdx < 0 && selectedMeta == res)
				{
					selectedIdx = idx;
					playerTradeMenu.Select(idx);
				}
			}
			
			playerTradeMenu.Disabled = false;
			playerTradeSpin.Editable = true;
			playerOfferButton.Disabled = false;
			playerRetractButton.Disabled = false;
		}
		else
		{
			playerTradeMenu.AddItem("No Region Resources");
			playerTradeMenu.SetItemMetadata(0, "N/A");
			playerTradeMenu.Disabled = true;
			playerTradeSpin.Editable = false;
			playerOfferButton.Disabled = true;
			playerRetractButton.Disabled = true;
		}
		
		if (selectedIdx < 0)
		{
			playerTradeMenu.Select(0);
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
		if (!GameData.currentRegion.ContainsTrade())
		{
			return;
		}
		
		Dictionary<string, float> thisInventory = GameData.currentRegion.reserves;
		
		foreach (var res in thisInventory)
		{
			if (!reserveResLabels.ContainsKey(res.Key))
			{
				continue;
			}
			
			reserveResLabels[res.Key].Text = GameData.FormatUnit(res.Value, res.Key);
		}
		
		if (returnMenu.GetSelectedId() < 0 || (string)returnMenu.GetSelectedMetadata() == "N/A")
		{
			returnSpin.MaxValue = 0;
			return;
		}
		
		string resID = (string)returnMenu.GetSelectedMetadata();
		returnSpin.MaxValue = thisInventory.ContainsKey(resID) ? thisInventory[resID] : 0f;
	}
	
	public void UpdateRegionResourceValues()
	{
		foreach (var res in GameData.currentRegion.resources)
		{
			resourceResLabels[res.Key].Text = GameData.FormatUnit(res.Value, res.Key);
		}
		
		string resID = (string)playerTradeMenu.GetSelectedMetadata();
		
		float value = GameData.currentRegion.resources.ContainsKey(resID) ? GameData.currentRegion.resources[resID] : 0f;
		
		Dictionary<string, float> thisInventory = new();
		
		if (GameData.currentRegion.ContainsTrade())
		{
			thisInventory = GameData.currentRegion.reserves;
			value += thisInventory.ContainsKey(resID) ? thisInventory[resID] : 0f;
		}
		
		playerTradeSpin.MaxValue = value;
		
		resID = (string)reserveMenu.GetSelectedMetadata();
		
		reserveSpin.MaxValue = GameData.currentRegion.resources.ContainsKey(resID) ? GameData.currentRegion.resources[resID] : 0f;
		
		resID = (string)returnMenu.GetSelectedMetadata();
		
		returnSpin.MaxValue = thisInventory.ContainsKey(resID) ? thisInventory[resID] : 0f;
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
	
	public void UpdateFavorCreditsDisplay()
	{
		bool corp = activeTrader != null && activeTrader.corporate;
		favorPanel.Visible = !corp;
		creditsPanel.Visible = corp;
	}
	
	public void UpdateFavorProgress()
	{
		float playerValue = activeTrader.CalculateFavor(playerOffer);
		float traderValue = activeTrader.CalculateFavor(traderOffer) * activeTrader.greed + activeTrader.data.takeoffFee;
		
		favorProgress.Value = (playerValue > 0f && traderValue > 0f ? playerValue / traderValue : 0f) * 100f;
		offerValueLabel.Text = $"{playerValue:0.00} / {traderValue:0.00}";
	}
	
	public void UpdateCreditValue()
	{
		float playerValue = activeTrader.CalculateFavor(playerOffer);
		float traderValue = activeTrader.CalculateFavor(traderOffer) * activeTrader.greed + activeTrader.data.takeoffFee;
		float totalValue = playerValue - traderValue;
		
		creditsLabel.Text = $"{totalValue} Credits";
	}
	
	public void AddTradeHub(Infrastructure infra)
	{
		UpdateReserveResourceLabels();
		UpdateResRetMenus();
		UpdatePlayerTradeMenu();
	}
	
	public void RemoveTradeHub(Infrastructure infra)
	{
		Dictionary<string, float> thisInventory = infra.location.reserves;
		
		if (!infra.location.ContainsTrade())
		{
			foreach (var res in thisInventory)
			{
				if (infra.location.resources.ContainsKey(res.Key))
				{
					infra.location.resources[res.Key] += res.Value;
				}
				else
				{
					infra.location.resources[res.Key] = res.Value;
					if (infra.location == GameData.currentRegion)
					{
						GameData.resourceControl.UpdateResourcePanels();
					}
				}
			}
			
			thisInventory.Clear();
		}
		
		UpdateReserveResourceLabels();
	}
}

public class TradeSave
{
	public int activeTrader;
	
	public Dictionary<string, float> traderOffer;
	public Dictionary<string, float> playerOffer;
	
	public TradeSave()
	{
		activeTrader = -1;
		
		traderOffer = new();
		playerOffer = new();
	}
	
	public TradeSave(TradeControl tc)
	{
		activeTrader = tc.activeTrader != null ? tc.activeTrader.idNum : -1;
		
		traderOffer = new(tc.traderOffer);
		playerOffer = new(tc.playerOffer);
	}
}
