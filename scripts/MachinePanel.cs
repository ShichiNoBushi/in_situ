using Godot;
using System;
using System.Collections.Generic;

public partial class MachinePanel : Control
{
	private static readonly Color INACTIVE = Colors.Black;
	private static readonly Color RUNNING = Colors.Green;
	private static readonly Color WARNING = Colors.Yellow;
	private static readonly Color BLOCKED = Colors.Orange;
	private static readonly Color FAILURE = Colors.Red;
	private static readonly Color ERROR = Colors.Purple;
	
	public Machine machine {get; set;}
	
	private Label nameLabel;
	private Button holdButton;
	private CheckButton activeButton;
	private ColorRect statusRect;
	private Label statusLabel;
	private OptionButton recipeMenu;
	private RichTextLabel inputLabel;
	private RichTextLabel outputLabel;
	private ProgressBar recipeProgress;
	private ProgressBar wearProgress;
	private Label wearLabel;
	private Button diagnosticsButton;
	private Button repairButton;
	private Button dismantleButton;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("MachinePanel: _Ready() called...");
		nameLabel = GetNode<Label>("Panel/VBoxMain/MachineName");
		holdButton = GetNode<Button>("Panel/VBoxMain/HBoxContainer/HoldButton");
		activeButton = GetNode<CheckButton>("Panel/VBoxMain/HBoxContainer/ActiveButton");
		statusRect = GetNode<ColorRect>("Panel/VBoxMain/HBoxContainer/StatusPanel/StatusRect");
		statusLabel = GetNode<Label>("Panel/VBoxMain/HBoxContainer/StatusPanel/StatusLabel");
		recipeMenu = GetNode<OptionButton>("Panel/VBoxMain/MachineTab/Production/VBoxProduction/RecipeOption");
		inputLabel = GetNode<RichTextLabel>("Panel/VBoxMain/MachineTab/Production/VBoxProduction/HBoxContainer/Inputs");
		outputLabel = GetNode<RichTextLabel>("Panel/VBoxMain/MachineTab/Production/VBoxProduction/HBoxContainer/Outputs");
		recipeProgress = GetNode<ProgressBar>("Panel/VBoxMain/MachineTab/Production/VBoxProduction/RecipeProgress");
		wearProgress = GetNode<ProgressBar>("Panel/VBoxMain/MachineTab/Maintenance/VBoxMaintenance/WearProgress");
		wearLabel = GetNode<Label>("Panel/VBoxMain/MachineTab/Maintenance/VBoxMaintenance/WearScroll/WearLabel");
		diagnosticsButton = GetNode<Button>("Panel/VBoxMain/MachineTab/Maintenance/VBoxMaintenance/HBoxContainer/DiagnosticsButton");
		repairButton = GetNode<Button>("Panel/VBoxMain/MachineTab/Maintenance/VBoxMaintenance/HBoxContainer/RepairButton");
		dismantleButton = GetNode<Button>("Panel/VBoxMain/MachineTab/Maintenance/VBoxMaintenance/HBoxContainer/DismantleButton");
		
		holdButton.ButtonDown += OnHoldDown;
		holdButton.ButtonUp += OnHoldUp;
		activeButton.Toggled += OnActiveToggled;
		recipeMenu.ItemSelected += OnRecipeSelected;
		diagnosticsButton.Pressed += DiagnoseMachine;
		repairButton.Pressed += RepairMachine;
		dismantleButton.Pressed += DismantleMachine;
	}
	
	public void Initialize()
	{
		MachineData data = GameData.MACHINES[machine.id];
		nameLabel.Text = data.name;
		activeButton.SetPressedNoSignal(machine.active);
		
		UpdateRecipeMenu();
		
		DisplayRecipeResources();
		DisplayMaintenance();
		
		recipeProgress.Value = 0;
		
		dismantleButton.Disabled = !data.available;
	}
	
	private void OnHoldDown()
	{
		if (machine == null) return;
		machine.ToggleActive(true);
	}
	
	private void OnHoldUp()
	{
		if (machine == null) return;
		machine.ToggleActive(activeButton.ButtonPressed);
	}
	
	private void OnActiveToggled(bool pressed)
	{
		if (machine == null) return;
		machine.ToggleActive(pressed);
	}
	
	private void OnRecipeSelected(long index)
	{
		if (machine == null) return;
		
		//string recipeID = machine.recipes[(int)index];
		try
		{
			string recipeID = (string)recipeMenu.GetItemMetadata((int)index);
			machine.SetRecipe(recipeID);
			
			DisplayRecipeResources();
		}
		catch (Exception e)
		{
			GD.PrintErr($"MachinePanel: Error displaying recipe - {e.Message}");
		}
	}
	
	private void UpdateStatus()
	{
		if (machine == null)
		{
			SetStatus("Error - null machine", ERROR);
		}
		else if (!GameData.RECIPES.ContainsKey(machine.currentRecipe))
		{
			SetStatus("Error - invalid recipe", ERROR);
		}
		else if (machine.wear >= machine.maxWear)
		{
			SetStatus("Failure - excessive wear", FAILURE);
		}
		else if (!GameData.disableStorage && machine.outputBuffer.Count > 0)
		{
			SetStatus("Blocked - output jammed", BLOCKED);
		}
		else if (machine.active && machine.CanCraft(1.0f) <= 0f)
		{
			SetStatus("Blocked - insufficient input", BLOCKED);
		}
		else if (machine.active && machine.wear > machine.maxWear / 2)
		{
			SetStatus("Warning - high wear", WARNING);
		}
		else if (machine.active)
		{
			SetStatus("Running", RUNNING);
		}
		else
		{
			SetStatus("Inactive", INACTIVE);
		}
	}
	
	private void SetStatus(string text, Color color)
	{
		statusLabel.Text = text;
		statusRect.Color = color;
	}
	
	private void DiagnoseMachine()
	{
		GD.Print($"MachinePanel: Diagnosing machine {GameData.MACHINES[machine.id].name}");
		try
		{
			machine.Diagnose();
		}
		catch (Exception e)
		{
			GD.PrintErr($"MachinePanel: Error diagnosing machine - {e.Message}");
		}
		DisplayMaintenance();
		GD.Print("MachinePanel: Diagnostics complete");
	}
	
	private void RepairMachine()
	{
		GD.Print($"MachinePanel: Repairing machine {GameData.MACHINES[machine.id].name}");
		
		try
		{
			machine.Repair();
		}
		catch (Exception e)
		{
			GD.PrintErr($"MachinePanel: Error repairing machine - {e.Message}");
		}
		
		DisplayMaintenance();
		DisplayRecipeResources();
		GameData.resourceControl.UpdateResourcePanels();
	}
	
	private void DismantleMachine()
	{
		Region loc = machine.location;
		
		GD.Print($"MachinePanel: Dismantling machine {GameData.MACHINES[machine.id].name}");
		
		try
		{
			machine.Dismantle();
		}
		catch (Exception e)
		{
			GD.PrintErr($"MachinePanel: Error dismantling machine - {e.Message}");
			return;
		}
		
		int removeCount = 0;
		
		if (loc != null && loc.machines != null)
		{
			int before = loc.machines.Count;
			loc.machines.RemoveAll(m => ReferenceEquals(m, machine));
			//loc.machines.Remove(machine);
			removeCount += before - loc.machines.Count;
		}
		
		GD.Print($"MachinePanel: Removed {removeCount} instances of {GameData.MACHINES[machine.id].name}");
		
		if (removeCount == 0)
		{
			GD.Print($"MachinePanel: Dismantle completed but no machines removed from location's list");
		}
		
		QueueFree();
		
		if (GameData.machinesControl != null)
		{
			GameData.machinesControl.UpdateMachinePanels();
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		DisplayRecipeResources();
		DisplayMaintenance();
		UpdateStatus();
	}
	
	public void UpdateRecipeMenu()
	{
		GD.Print($"MachinePanel: Updating menu for {GameData.MACHINES[machine.id].name}");
		
		int oldSelectedIdx = recipeMenu.GetSelected();
		string oldSelectedItem = recipeMenu.GetItemText(oldSelectedIdx);
		
		recipeMenu.Clear();
			
		bool found = false;
		
		foreach (var rid in machine.recipes)
		{
			RecipeData recipe = GameData.RECIPES[rid];
			
			bool local = true;
			if (recipe.local == "mining")
			{
				local = false;
				List<string> nodes = machine.location.nodes;
				
				foreach(var res in GameData.RECIPES[rid].outputs.Keys)
				{
					if (nodes.Contains(res))
					{
						local = true;
						break;
					}
				}
			}
			
			if (local && (GameData.unlockAllRecipes || recipe.available))
			{
				GD.Print($"Adding recipe {recipe.name} for {GameData.MACHINES[machine.id].name}");
				recipeMenu.AddItem(recipe.name);
				int idx = recipeMenu.ItemCount - 1;
				recipeMenu.SetItemMetadata(idx, rid);
				found = true;
			}
		}
		
		recipeMenu.Disabled = !found;
		
		if (!found)
		{
			GD.Print($"MachinePanel: No recipes available for {GameData.MACHINES[machine.id].name}");
			recipeMenu.AddItem("No available recipes");
		}
		else
		{
			int selectIdx = -1;
			
			if (!string.IsNullOrEmpty(oldSelectedItem) && oldSelectedItem != "No available recipes")
			{
				for (int i = 0; i < recipeMenu.ItemCount; i++)
				{
					if (recipeMenu.GetItemText(i) == oldSelectedItem)
					{
						selectIdx = i;
						break;
					}
				}
			}
			
			if (selectIdx < 0 && recipeMenu.ItemCount > 0)
			{
				selectIdx = 0;
			}
			
			if (selectIdx >= 0)
			{
				recipeMenu.Select(selectIdx);
			}
			
			GD.Print($"MachinePanel: Selecting recipe at index {selectIdx}");
			//recipeMenu.Select(0);
			//machine.SetRecipe(GameData.recNameToKey[recipeMenu.GetItemText(selectIdx)]);
			string recipeID = (string)recipeMenu.GetItemMetadata(selectIdx);
			machine.SetRecipe(recipeID);
			GD.Print("MachinePanel: Recipe selected");
		}
	}
	
	public void UpdateDismantle()
	{
		MachineData data = GameData.MACHINES[machine.id];
		dismantleButton.Disabled = !data.available;
	}
	
	private void DisplayRecipeResources()
	{
		if (GameData.unlockAllRecipes || GameData.RECIPES.ContainsKey(machine.currentRecipe) && GameData.RECIPES[machine.currentRecipe].available)
		{
			RecipeData recipe = GameData.RECIPES[machine.currentRecipe];
			
			Dictionary<string, float> inputs = recipe.inputs;
			Dictionary<string, float> outputs = recipe.outputs;
			
			float weather = 1.0f;
			
			string inputDisplay = "Input:";
			//string availableDisplay = "";
			
			if ((GameData.unlockAllRecipes || recipe.available) && inputs.Count > 0)
			{
				inputDisplay += "\n[table=5]";
				foreach(var res in inputs)
				{
					if (GameData.RESOURCES.ContainsKey(res.Key))
					{
						string resAbbrev = GameData.RESOURCES[res.Key].abbreviation;
						string availResForm = machine.location.resources.ContainsKey(res.Key) ? GameData.FormatUnit(machine.location.resources[res.Key], res.Key) : GameData.FormatUnit(0f, res.Key);
						string inputResForm = GameData.FormatUnit(res.Value, res.Key);
						//inputDisplay += $"\n{resAbbrev}: {availResForm} / {inputResForm}";
						inputDisplay += $"\n[cell]{resAbbrev}[/cell][cell]:[/cell][cell][right]{availResForm}[/right][/cell][cell]/[/cell][cell][right]{inputResForm}[/right][/cell]";
						//availableDisplay += $"\n[code]{availResForm, 8} / {inputResForm, 8}[/code]";
					}
					else
					{
						inputDisplay += $"\nInvalid key ({res.Key})";
						GD.PrintErr($"MachinePanel: Invalid key - {res.Key}");
					}
				}
				inputDisplay += "\n[/table]";
			}
			else
			{
				inputDisplay += "\nNo inputs";
			}
			
			inputLabel.Text = inputDisplay;
			//availableLabel.Text = availableDisplay;
			
			string outputDisplay = "Output:";
			//string producedDisplay = "";
			
			if ((GameData.unlockAllRecipes || recipe.available) && outputs.Count > 0)
			{
				outputDisplay += "\n[table=3]";
				
				if (recipe.local == "wind")
				{
					weather = machine.location.wind;
				}
				else if (recipe.local == "solar")
				{
					weather = machine.location.solar;
				}
				
				foreach(var res in outputs)
				{
					if (GameData.RESOURCES.ContainsKey(res.Key))
					{
						string resAbbrev = GameData.RESOURCES[res.Key].abbreviation;
						string outputResForm = GameData.FormatUnit(res.Value * weather, res.Key);
						//outputDisplay += $"\n{resAbbrev}: {outputResForm}";
						outputDisplay += $"\n[cell]{resAbbrev, -15}[/cell][cell]:[/cell][cell][right]{outputResForm}[/right][/cell]";
						//producedDisplay += $"\n[code]{outputResForm, 8}[/code]";
					}
					else
					{
						outputDisplay += $"\nInvalid key ({res.Key})";
						GD.PrintErr($"MachinePanel: Invalid key ({res.Key})");
					}
				}
				outputDisplay += "\n[/table]";
			}
			else
			{
				outputDisplay += "\nNo outputs";
			}
			
			outputLabel.Text = outputDisplay;
			//producedLabel.Text = producedDisplay;
		}
		else
		{
			inputLabel.Text = "Recipe Invalid";
			//availableLabel.Text = "";
			outputLabel.Text = "";
			//producedLabel.Text = "";
		}
	}
	
	private void DisplayMaintenance()
	{
		wearProgress.Value = Math.Clamp(machine.wear / machine.maxWear * 100f, 0f, 100f);
		
		string text = "Required Materials:";
		
		if (machine.wear == 0f)
		{
			text += "\nUndamaged";
		}
		else if (machine.diagnosedWear == 0f)
		{
			text += "\nUndiagnosed";
		}
		else
		{
			foreach (var res in machine.repairComponents)
			{
				string available = machine.location.resources.ContainsKey(res.Key) ? GameData.FormatUnit(machine.location.resources[res.Key], res.Key) : GameData.FormatUnit(0f, res.Key);
				string needed = GameData.FormatUnit(res.Value, res.Key);
				text += $"\n{GameData.RESOURCES[res.Key].name} {available} / {needed}";
			}
		}
		
		wearLabel.Text = text;
	}
}
