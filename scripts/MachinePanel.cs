using Godot;
using System;
using System.Collections.Generic;

public partial class MachinePanel : Control
{
	public Machine machine {get; set;}
	
	private Label nameLabel;
	private CheckButton activeButton;
	private OptionButton recipeMenu;
	private RichTextLabel inputLabel;
	//private RichTextLabel availableLabel;
	private RichTextLabel outputLabel;
	//private RichTextLabel producedLabel;
	private ProgressBar recipeProgress;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("MachinePanel: _Ready() called...");
		nameLabel = GetNode<Label>("Panel/VBoxContainer/MachineName");
		activeButton = GetNode<CheckButton>("Panel/VBoxContainer/ActiveButton");
		recipeMenu = GetNode<OptionButton>("Panel/VBoxContainer/RecipeOption");
		inputLabel = GetNode<RichTextLabel>("Panel/VBoxContainer/HBoxContainer/Inputs");
		//availableLabel = GetNode<RichTextLabel>("Panel/VBoxContainer/HBoxContainer/Available");
		outputLabel = GetNode<RichTextLabel>("Panel/VBoxContainer/HBoxContainer/Outputs");
		//producedLabel = GetNode<RichTextLabel>("Panel/VBoxContainer/HBoxContainer/Produced");
		recipeProgress = GetNode<ProgressBar>("Panel/VBoxContainer/ProgressBar");
		
		activeButton.Toggled += OnActiveToggled;
		recipeMenu.ItemSelected += OnRecipeSelected;
	}
	
	public void Initialize()
	{
		nameLabel.Text = GameData.MACHINES[machine.id].name;
		
		UpdateRecipeMenu();
		
		DisplayRecipeResources();
		recipeProgress.Value = 0;
	}
	
	private void OnActiveToggled(bool pressed)
	{
		if (machine == null) return;
		machine.ToggleActive(pressed);
	}
	
	private void OnRecipeSelected(long index)
	{
		if (machine == null) return;
		
		string recipeID = machine.recipes[(int)index];
		machine.SetRecipe(recipeID);
		
		DisplayRecipeResources();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		DisplayRecipeResources();
	}
	
	public void UpdateRecipeMenu()
	{
		GD.Print($"MachinePanel: Updating menu for {GameData.MACHINES[machine.id].name}");
		recipeMenu.Clear();
			
		bool found = false;
		
		foreach (var rid in machine.recipes)
		{
			RecipeData recipe = GameData.RECIPES[rid];
			
			if (GameData.unlockAllRecipes || recipe.available)
			{
				GD.Print($"Adding recipe {recipe.name} for {GameData.MACHINES[machine.id].name}");
				recipeMenu.AddItem(recipe.name);
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
			GD.Print("MachinePanel: Selecting recipe at index 0");
			recipeMenu.Select(0);
			machine.SetRecipe(GameData.recNameToKey[recipeMenu.GetItemText(0)]);
			GD.Print("MachinePanel: Recipe selected");
		}
	}
	
	private void DisplayRecipeResources()
	{
		if (GameData.RECIPES.ContainsKey(machine.currentRecipe) && GameData.RECIPES[machine.currentRecipe].available)
		{
			RecipeData recipe = GameData.RECIPES[machine.currentRecipe];
			
			Dictionary<string, float> inputs = recipe.inputs;
			Dictionary<string, float> outputs = recipe.outputs;
			
			String inputDisplay = "Input:";
			//String availableDisplay = "";
			
			if (recipe.available && inputs.Count > 0)
			{
				inputDisplay += "\n[table=5]";
				foreach(var res in inputs)
				{
					String resAbbrev = GameData.RESOURCES[res.Key].abbreviation;
					String availResForm = GameData.FormatUnit(machine.location.resources[res.Key], res.Key);
					String inputResForm = GameData.FormatUnit(res.Value, res.Key);
					//inputDisplay += $"\n{resAbbrev}: {availResForm} / {inputResForm}";
					inputDisplay += $"\n[cell]{resAbbrev}[/cell][cell]:[/cell][cell][right]{availResForm}[/right][/cell][cell]/[/cell][cell][right]{inputResForm}[/right][/cell]";
					//availableDisplay += $"\n[code]{availResForm, 8} / {inputResForm, 8}[/code]";
				}
				inputDisplay += "\n[/table]";
			}
			else
			{
				inputDisplay += "\nNo inputs";
			}
			
			inputLabel.Text = inputDisplay;
			//availableLabel.Text = availableDisplay;
			
			String outputDisplay = "Output:";
			//String producedDisplay = "";
			
			if (recipe.available && outputs.Count > 0)
			{
				outputDisplay += "\n[table=3]";
				foreach(var res in outputs)
				{
					String resAbbrev = GameData.RESOURCES[res.Key].abbreviation;
					String outputResForm = GameData.FormatUnit(res.Value, res.Key);
					//outputDisplay += $"\n{resAbbrev}: {outputResForm}";
					outputDisplay += $"\n[cell]{resAbbrev, -15}[/cell][cell]:[/cell][cell][right]{outputResForm}[/right][/cell]";
					//producedDisplay += $"\n[code]{outputResForm, 8}[/code]";
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
}
