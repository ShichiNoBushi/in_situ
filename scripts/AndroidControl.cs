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
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		android = GameData.android;
		
		inventoryLabel = GetNode<Label>("InventoryLabel");
		maximumLabel = GetNode<Label>("MaximumLabel");
		fullInventoryLabel = GetNode<RichTextLabel>("InventoryScroll/FullInventoryLabel");
		resourceMenu = GetNode<OptionButton>("ResourceMenu");
		availableLabel = GetNode<Label>("AvailableLabel");
		toInventoryRadio = GetNode<CheckBox>("ToInventoryRadio");
		fromInventoryRadio = GetNode<CheckBox>("FromInventoryRadio");
		transferSpin = GetNode<SpinBox>("TransferSpin");
		transferButton = GetNode<Button>("TransferButton");
		
		fullInventoryLabel.BbcodeEnabled = true;
		
		foreach (var res in GameData.RESOURCES)
		{
			if (res.Value.phase == "solid")
			{
				resourceMenu.AddItem(res.Value.name);
				int idx = resourceMenu.ItemCount - 1;
				resourceMenu.SetItemMetadata(idx, res.Key);
			}
		}
		
		transferButton.Pressed += TransferResource;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		DisplayInventory();
	}
	
	public void DisplayInventory()
	{
		float carried = android.AmountCarried();
		string carriedForm = GameData.FormatUnit2(carried, "g");
		
		float maximum = android.maxInventory;
		string maximumForm = GameData.FormatUnit2(maximum, "g");
		
		float available = 0f;
		int resIdx = resourceMenu.GetSelected();
		string resID = (string)resourceMenu.GetItemMetadata(resIdx);
		if (GameData.currentRegion.resources.ContainsKey(resID))
		{
			available = GameData.currentRegion.resources[resID];
		}
		string availableForm = GameData.FormatUnit2(available, "g");
		
		if (toInventoryRadio.ButtonPressed)
		{
			transferSpin.MaxValue = available;
		}
		else if(fromInventoryRadio.ButtonPressed)
		{
			transferSpin.MaxValue = android.GetResource(resID);
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
		float transferAmount = (float)transferSpin.Value;
		
		if (transferAmount <= 0f)
		{
			return;
		}
		
		int resIdx = resourceMenu.GetSelected();
		
		if (resourceMenu.ItemCount <= 0 || resIdx <= 0)
		{
			return;
		}
		
		string resID = (string)resourceMenu.GetItemMetadata(resIdx);
		//float amount = GameData.currentRegion.resources[resID];
		
		if (toInventoryRadio.ButtonPressed)
		{
			TransferTo(resID, transferAmount);
		}
		else if (fromInventoryRadio.ButtonPressed)
		{
			TransferFrom(resID, transferAmount);
		}
	}
	
	public void TransferTo(string res, float amt)
	{
		float difference = android.GiveResource(res, amt);
		float transfered = amt;
		
		GD.Print($"AndroidControl: Transferring {GameData.FormatUnit(amt, res)} of {GameData.RESOURCES[res].name} to inventory");
		GD.Print($"AndroidControl: difference of {GameData.FormatUnit(difference, res)} cannot be transferred");
		
		if (difference >= 0)
		{
			transfered = amt - difference;
		}
		
		GD.Print($"AndroidControl: {GameData.FormatUnit(GameData.currentRegion.resources[res], res)} before transfer");
		GameData.currentRegion.resources[res] -= transfered;
		GD.Print($"AndroidControl: {GameData.FormatUnit(GameData.currentRegion.resources[res], res)} after transfer");
	}
	
	public void TransferFrom(string res, float amt)
	{
		float taken = android.TakeResource(res, amt);
		
		GD.Print($"AndroidControl: Transferring {GameData.FormatUnit(amt, res)} of {GameData.RESOURCES[res].name} from inventory");
		GD.Print($"AndroidControl: {GameData.FormatUnit(taken, res)} available to be transferred");
		
		GD.Print($"AndroidControl: {GameData.FormatUnit(GameData.currentRegion.resources[res], res)} before transfer");
		GameData.currentRegion.resources[res] += taken;
		GD.Print($"AndroidControl: {GameData.FormatUnit(GameData.currentRegion.resources[res], res)} after transfer");
	}
}
