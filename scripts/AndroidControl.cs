using Godot;
using System;

public partial class AndroidControl : Control
{
	public Android android {get; private set;}
	
	public Label inventoryLabel;
	public Label maximumLabel;
	public RichTextLabel fullInventoryLabel;
	public OptionButton resourceMenu;
	public Label availableLabel;
	public CheckBox toInventoryRadio;
	public CheckBox fromInventoryRadio;
	public SpinBox transferSpin;
	public Button transferButton;
	public Label creditLabel;
	public Label debtLabel;
	public SpinBox paySpin;
	public Button payButton;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		UpdateAndroid();
		
		inventoryLabel = GetNode<Label>("InventoryLabel");
		maximumLabel = GetNode<Label>("MaximumLabel");
		fullInventoryLabel = GetNode<RichTextLabel>("InventoryScroll/FullInventoryLabel");
		resourceMenu = GetNode<OptionButton>("ResourceMenu");
		availableLabel = GetNode<Label>("AvailableLabel");
		toInventoryRadio = GetNode<CheckBox>("ToInventoryRadio");
		fromInventoryRadio = GetNode<CheckBox>("FromInventoryRadio");
		transferSpin = GetNode<SpinBox>("TransferSpin");
		transferButton = GetNode<Button>("TransferButton");
		creditLabel = GetNode<Label>("CreditsLabel");
		debtLabel = GetNode<Label>("DebtLabel");
		paySpin = GetNode<SpinBox>("PaySpin");
		payButton = GetNode<Button>("PayButton");
		
		//fullInventoryLabel.BbcodeEnabled = true;
		
		/*foreach (var res in GameData.RESOURCES)
		{
			if (res.Value.phase == "solid")
			{
				resourceMenu.AddItem(res.Value.name);
				int idx = resourceMenu.ItemCount - 1;
				resourceMenu.SetItemMetadata(idx, res.Key);
			}
		}*/
		UpdateResourceMenu();
		UpdateCreditDebt();
		CallDeferred(nameof(UpdateResourceMenu));
		
		transferButton.Pressed += TransferResource;
		payButton.Pressed += PayDebt;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		DisplayInventory();
	}
	
	public void UpdateResourceMenu()
	{
		if (resourceMenu == null)
		{
			GD.PrintErr("AndroidControl: resourceMenu null");
			return;
		}
		
		resourceMenu.Clear();
		
		foreach (var res in GameData.RESOURCES)
		{
			if (res.Value.phase == "solid")
			{
				resourceMenu.AddItem(res.Value.name);
				int idx = resourceMenu.ItemCount - 1;
				resourceMenu.SetItemMetadata(idx, res.Key);
			}
		}
		
		if (resourceMenu.ItemCount == 0)
		{
			resourceMenu.AddItem("No solid resources");
			resourceMenu.Disabled = true;
		}
		else
		{
			resourceMenu.Disabled = false;
		}
		
		resourceMenu.Select(0);
	}
	
	public void UpdateAndroid()
	{
		android = GameData.android;
	}
	
	public void UpdateCreditDebt()
	{
		if (creditLabel == null || debtLabel == null)
		{
			GD.PrintErr($"AndroidControl: Error updating credit and debt, no credit or debt label");
			return;
		}
		
		creditLabel.Text = $"{GameData.credits:0.0000}";
		debtLabel.Text = $"{GameData.debt:0.0000}";
	}
	
	public void UpdatePayMax()
	{
		if (paySpin == null)
		{
			GD.PrintErr($"AndroidControl: Error updating pay SpinBox max, null value");
			return;
		}
		
		paySpin.MaxValue = Math.Min(GameData.credits, GameData.debt);
	}
	
	public void DisplayInventory()
	{
		float carried = android.AmountCarried();
		string carriedForm = GameData.FormatUnit2(carried, "g");
		
		float maximum = android.maxInventory;
		string maximumForm = GameData.FormatUnit2(maximum, "g");
		
		float available = 0f;
		int resIdx = resourceMenu.GetSelected();
		string resID = "";
		string availableForm = "";
		
		if (resourceMenu.ItemCount > 0 && resIdx >= 0 && !resourceMenu.Disabled)
		{
			resID = (string)resourceMenu.GetItemMetadata(resIdx);
			if (GameData.currentRegion.resources.ContainsKey(resID))
			{
				available = GameData.currentRegion.resources[resID];
			}
			availableForm = GameData.FormatUnit2(available, "g");
			
			if (toInventoryRadio.ButtonPressed)
			{
				transferSpin.MaxValue = available;
			}
			else if(fromInventoryRadio.ButtonPressed)
			{
				transferSpin.MaxValue = android.GetResource(resID);
			}
		}
		else
		{
			transferSpin.MaxValue = 0f;
		}
		
		inventoryLabel.Text = carriedForm;
		maximumLabel.Text = maximumForm;
		availableLabel.Text = availableForm;
		
		string androidInv = "[table=2]";
		
		foreach (var res in android.inventory)
		{
			string name = GameData.RESOURCES[res.Key].name;
			string amtForm = GameData.FormatUnit(res.Value, res.Key);
			
			androidInv += $"[cell]{name}[/cell][cell][right]{amtForm}[/right][/cell]";
		}
		
		androidInv += "[/table]";
		
		fullInventoryLabel.Text = androidInv;
	}
	
	public void TransferResource()
	{
		GD.Print("AndroidControl: Transferring resources 1");
		float transferAmount = (float)transferSpin.Value;
		
		if (transferAmount <= 0f)
		{
			GD.Print("AndroidControl: insufficient transfered resources");
			return;
		}
		
		GD.Print("AndroidControl: 2");
		
		int resIdx = resourceMenu.GetSelected();
		
		if (resourceMenu.ItemCount <= 0 || resIdx < 0)
		{
			GD.Print($"AndroidControl: error with resourceMenu - Count {resourceMenu.ItemCount} index {resIdx}");
			return;
		}
		
		GD.Print("AndroidControl: 3");
		
		string resID = (string)resourceMenu.GetItemMetadata(resIdx);
		//float amount = GameData.currentRegion.resources[resID];
		
		GD.Print("AndroidControl: 4");
		
		if (toInventoryRadio.ButtonPressed)
		{
			TransferTo(resID, transferAmount);
		}
		else if (fromInventoryRadio.ButtonPressed)
		{
			TransferFrom(resID, transferAmount);
		}
		else
		{
			GD.Print("AndroidControl: Error determining selected radio button");
		}
	}
	
	public void TransferTo(string res, float amt)
	{
		float difference = 0f;
		float transfered = 0f;
		
		try
		{
			difference = android.GiveResource(res, amt);
			transfered = amt;
		}
		catch (Exception e)
		{
			GD.PrintErr($"AndroidControl: Error giving android resources - {e.Message}");
			return;
		}
		
		GD.Print($"AndroidControl: Transferring {GameData.FormatUnit(amt, res)} of {GameData.RESOURCES[res].name} to inventory");
		GD.Print($"AndroidControl: difference of {GameData.FormatUnit(difference, res)} cannot be transferred");
		
		if (difference >= 0)
		{
			transfered = amt - difference;
		}
		
		float before = GameData.currentRegion.resources.ContainsKey(res) ? GameData.currentRegion.resources[res] : 0f;
		GD.Print($"AndroidControl: {GameData.FormatUnit(before, res)} before transfer");
		
		if (GameData.currentRegion.resources.ContainsKey(res))
		{
			GameData.currentRegion.resources[res] -= transfered;
			if (GameData.currentRegion.resources[res] <= 0f)
			{
				GameData.currentRegion.resources.Remove(res);
			}
		}
		
		float after = GameData.currentRegion.resources.ContainsKey(res) ? GameData.currentRegion.resources[res] : 0f;
		GD.Print($"AndroidControl: {GameData.FormatUnit(after, res)} after transfer");
	}
	
	public void TransferFrom(string res, float amt)
	{
		float taken = 0f;
		
		try
		{
			taken = android.TakeResource(res, amt);
		}
		catch (Exception e)
		{
			GD.PrintErr($"AndroidControl: Error taking resources from android - {e.Message}");
			return;
		}
		
		GD.Print($"AndroidControl: Transferring {GameData.FormatUnit(amt, res)} of {GameData.RESOURCES[res].name} from inventory");
		GD.Print($"AndroidControl: {GameData.FormatUnit(taken, res)} available to be transferred");
		
		float before = GameData.currentRegion.resources.ContainsKey(res) ? GameData.currentRegion.resources[res] : 0f;
		GD.Print($"AndroidControl: {GameData.FormatUnit(before, res)} before transfer");
		
		if (GameData.currentRegion.resources.ContainsKey(res))
		{
			GameData.currentRegion.resources[res] += taken;
		}
		else
		{
			GameData.currentRegion.resources[res] = taken;
		}
		
		float after = GameData.currentRegion.resources.ContainsKey(res) ? GameData.currentRegion.resources[res] : 0f;
		GD.Print($"AndroidControl: {GameData.FormatUnit(after, res)} after transfer");
	}
	
	public void PayDebt()
	{
		if (paySpin.Value <= 0f)
		{
			paySpin.Value = 0f;
			return;
		}
		
		float maxValue = Math.Min(GameData.credits, GameData.debt);
		float payValue = Math.Min((float)paySpin.Value, maxValue);
		
		GameData.credits -= payValue;
		GameData.debt -= payValue;
		
		UpdateCreditDebt();
		UpdatePayMax();
	}
}
