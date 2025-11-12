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
		inputLabel = GetNode<Label>("Panel/VBoxContainer/HBoxContainer/Input");
		outputLabel = GetNode<Label>("Panel/VBoxContainer/HBoxContainer/Output");
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
		
		inputLabel.Text = "";
		outputLabel.Text = "";
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
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
