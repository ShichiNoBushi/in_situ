using Godot;
using System;
using System.Text;
using System.Collections.Generic;

public partial class DevControl : Control
{
	private LineEdit devLine;
	private Button enterButton;
	private Label devLabel;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("DevControl: _Ready() called...");
		
		devLine = GetNode<LineEdit>("Panel/DevLine");
		enterButton = GetNode<Button>("Panel/EnterButton");
		devLabel = GetNode<Label>("Panel/DevLabel");
		
		devLine.TextSubmitted += SubmitText;
		enterButton.Pressed += SubmitButton;
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	private void SubmitText(String text)
	{
		ExecuteCommand(text);
		devLine.Clear();
	}
	
	private void SubmitButton()
	{
		ExecuteCommand(devLine.Text);
		devLine.Clear();
	}
	
	private void ExecuteCommand(String text)
	{
		GD.Print($"DevControl: Command entered \"{text}\"");
		
		String[] args = SplitQuoted(text);
		
		String cmd = args[0].ToLower();
		
		try
		{
			switch (cmd)
			{
				case "give":
					HandleGive(args);
					break;
				case "unlock":
					HandleUnlock(args);
					break;
				case "complete":
					HandleComplete(args);
					break;
				default:
					devLabel.Text = $"Unknown command {cmd}";
					break;
			}
		}
		catch (Exception e)
		{
			String message = $"DevControl: Error {e.Message}";
			devLabel.Text = message;
			GD.PrintErr(message);
		}
	}
	
	private void HandleGive(String[] args)
	{
		if (args.Length < 3)
		{
			devLabel.Text = "Give format \"give resource <name> <amount>\" or \"give credits <amount>\"";
			return;
		}
		
		String category = args[1].ToLower();
		
		if (category == "resource" && args.Length >= 4)
		{
			String resName = args[2];
			
			if (!float.TryParse(args[3], out float amount))
			{
				devLabel.Text = $"Invalid amount: {args[3]}";
				return;
			}
			
			if (!GameData.RESOURCES.ContainsKey(resName))
			{
				if (GameData.resNameToKey.ContainsKey(resName))
				{
					resName = GameData.resNameToKey[resName];
				}
				else
				{
					devLabel.Text = $"No such resource {resName}";
					return;
				}
			}
			
			if (GameData.currentRegion.resources.ContainsKey(resName))
			{
				GameData.currentRegion.resources[resName] += amount;
			}
			else if (amount > 0f)
			{
				GameData.currentRegion.resources[resName] = amount;
				GameData.SortResources(GameData.currentRegion.resources);
				GameData.resourceControl.UpdateResourcePanels();
				GameData.tradeControl.UpdateRegionResourceLabels();
				GameData.tradeControl.UpdateResRetMenus();
				GameData.tradeControl.UpdatePlayerTradeMenu();
			}
			
			String amountText = GameData.FormatUnit(amount, resName);
			
			devLabel.Text = $"Gave {amountText} of {GameData.RESOURCES[resName].name}.";
		}
		else if (category == "credits" && args.Length >= 3)
		{
			if (!float.TryParse(args[2], out float amount))
			{
				devLabel.Text = $"Invalid amount: {args[2]}";
				return;
			}
			
			if (GameData.credits + amount < 0f)
			{
				devLabel.Text = $"Credit deficit reduces balance into negative: {amount} credits, {GameData.credits} available";
			}
			
			GameData.credits += amount;
			
			GameData.androidControl.UpdateCreditDebt();
			GameData.androidControl.UpdatePayMax();
			
			devLabel.Text = $"Gave {amount} credits. Balance: {GameData.credits}.";
		}
		else
		{
			devLabel.Text = "Give format \"give resource <name> <amount>\" or \"give credits <amount>\"";
		}
	}
	
	private void HandleUnlock(String[] args)
	{
		if (args.Length < 3)
		{
			devLabel.Text = "Unlock format \"unlock recipe/machine/quest <name>\"";
			return;
		}
		
		String type = args[1].ToLower();
		String name = args[2];
		
		switch(type)
		{
			case "recipe":
				if (!GameData.RECIPES.ContainsKey(name))
				{
					if(GameData.recNameToKey.ContainsKey(name))
					{
						name = GameData.recNameToKey[name];
					}
					else
					{
						devLabel.Text = $"No such recipe {name}";
						return;
					}
				}
				
				GameData.RECIPES[name].available = true;
				GameData.machinesControl.UpdateMachinePanels();
				devLabel.Text = $"Unlocked recipe {GameData.RECIPES[name].name}";
				break;
			case "machine":
				if (!GameData.MACHINES.ContainsKey(name) && !GameData.INFRASTRUCTURE.ContainsKey(name))
				{
					if(GameData.machNameToKey.ContainsKey(name))
					{
						name = GameData.machNameToKey[name];
					}
					else if (GameData.infraNameToKey.ContainsKey(name))
					{
						name = GameData.infraNameToKey[name];
					}
					else
					{
						devLabel.Text = $"No such machine {name}";
						return;
					}
				}
				
				if (GameData.MACHINES.ContainsKey(name))
				{
					GameData.MACHINES[name].available = true;
					devLabel.Text = $"Unlocked machine {GameData.MACHINES[name].name}";
				}
				else if (GameData.INFRASTRUCTURE.ContainsKey(name))
				{
					GameData.INFRASTRUCTURE[name].available = true;
					devLabel.Text = $"Unlocked infrastructure {GameData.INFRASTRUCTURE[name].name}";
				}
				GameData.buildControl.UpdateBuildMenu();
				break;
			case "quest":
				if (!GameData.QUESTS.ContainsKey(name))
				{
					if (GameData.qstNameToKey.ContainsKey(name))
					{
						name = GameData.qstNameToKey[name];
					}
					else
					{
						devLabel.Text = $"No such quest {name}";
						return;
					}
				}
				
				QuestControl qc = GameData.questControl;
				
				if (qc.completeQuests.ContainsKey(name))
				{
					devLabel.Text = $"Quest {GameData.QUESTS[name].name} already completed";
					return;
				}
				if (qc.activeQuests.ContainsKey(name))
				{
					devLabel.Text = $"Quest {GameData.QUESTS[name].name} already active";
					return;
				}
				
				qc.activeQuests[name] = GameData.QUESTS[name];
				qc.UpdateQuestLists();
				GameData.CheckQuests();
				devLabel.Text = $"Unlocked quest {GameData.QUESTS[name].name}";
				break;
			default:
				devLabel.Text = "Unlock format \"unlock recipe/machine/quest <name>\"";
				break;
		}
	}
	
	private void HandleComplete(String[] args)
	{
		if (args.Length < 3)
		{
			devLabel.Text = "Complete format \"complete quest <name>\"";
			return;
		}
		
		String type = args[1].ToLower();
		String name = args[2];
		
		if (type == "quest")
		{
			if (!GameData.QUESTS.ContainsKey(name))
			{
				if (GameData.qstNameToKey.ContainsKey(name))
				{
					name = GameData.qstNameToKey[name];
				}
				else
				{
					devLabel.Text = $"No such quest {name}";
					return;
				}
			}
			
			QuestControl qc = GameData.questControl;
			
			if (qc.completeQuests.ContainsKey(name))
			{
				devLabel.Text = $"Quest {name} already completed";
				return;
			}
				
			GameData.CompleteQuest(name);
			
			devLabel.Text = $"Quest {GameData.QUESTS[name].name} completed";
		}
		else
		{
			devLabel.Text = "Complete format \"complete quest <name>\"";
		}
	}
	
	private static string[] SplitQuoted(string input)
	{
		List<String> result = new();
		bool inQuotes = false;
		StringBuilder current = new();

		foreach (char c in input)
		{
			if (c == '"')
			{
				inQuotes = !inQuotes;
				continue;
			}

			if (!inQuotes && char.IsWhiteSpace(c))
			{
				if (current.Length > 0)
				{
					result.Add(current.ToString());
					current.Clear();
				}
			}
			else
			{
				current.Append(c);
			}
		}

		if (current.Length > 0)
			result.Add(current.ToString());

		return result.ToArray();
	}
}
