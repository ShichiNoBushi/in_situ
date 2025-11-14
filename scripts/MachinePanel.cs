using Godot;
using System;
using System.Collections.Generic;

public partial class MachinePanel : Control
{
	public Machine machine {get; set;}
	
	private Label nameLabel;
	private CheckButton activeButton;
	private OptionButton recipeMenu;
	private Label inputLabel;
	private Label outputLabel;
	private ProgressBar recipeProgress;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("MachinePanel: _Ready() called...");
		nameLabel = GetNode<Label>("Panel/VBoxContainer/MachineName");
		activeButton = GetNode<CheckButton>("Panel/VBoxContainer/ActiveButton");
		recipeMenu = GetNode<OptionButton>("Panel/VBoxContainer/RecipeOption");
		inputLabel = GetNode<Label>("Panel/VBoxContainer/HBoxContainer/Inputs");
		outputLabel = GetNode<Label>("Panel/VBoxContainer/HBoxContainer/Outputs");
		recipeProgress = GetNode<ProgressBar>("Panel/VBoxContainer/ProgressBar");
		
		activeButton.Toggled += OnActiveToggled;
		recipeMenu.ItemSelected += OnRecipeSelected;
	}
	
	public void Initialize()
	{
		nameLabel.Text = GameData.MACHINES[machine.id].name;
		
		recipeMenu.Clear();
		foreach (var rid in machine.recipes)
		{
			recipeMenu.AddItem(GameData.RECIPES[rid].name);
		}
		
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
	
	private void DisplayRecipeResources()
	{
		if (GameData.RECIPES.ContainsKey(machine.currentRecipe))
		{
			RecipeData recipe = GameData.RECIPES[machine.currentRecipe];
			
			Dictionary<string, float> inputs = recipe.inputs;
			Dictionary<string, float> outputs = recipe.outputs;
			
			String inputDisplay = "Input:";
			
			if (inputs.Count > 0)
			{
				foreach(var res in inputs)
				{
					String resAbbrev = GameData.RESOURCES[res.Key].abbreviation;
					String availResForm = GameData.FormatUnit(GameData.resources[res.Key], res.Key);
					String inputResForm = GameData.FormatUnit(res.Value, res.Key);
					inputDisplay += $"\n{resAbbrev}: {availResForm} / {inputResForm}";
				}
			}
			else
			{
				inputDisplay += "\nNo inputs";
			}
			
			inputLabel.Text = inputDisplay;
			
			String outputDisplay = "Output:";
			
			if (outputs.Count > 0)
			{
				foreach(var res in outputs)
				{
					String resAbbrev = GameData.RESOURCES[res.Key].abbreviation;
					String outputResForm = GameData.FormatUnit(res.Value, res.Key);
					outputDisplay += $"\n{resAbbrev}: {outputResForm}";
				}
			}
			else
			{
				outputDisplay += "\nNo outputs";
			}
			
			outputLabel.Text = outputDisplay;
		}
		else
		{
			inputLabel.Text = "Recipe Invalid";
			outputLabel.Text = "";
		}
	}
}
