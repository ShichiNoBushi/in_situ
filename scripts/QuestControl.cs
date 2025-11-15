using Godot;
using System;
using System.Collections.Generic;

public partial class QuestControl : Control
{
	public Dictionary<String, QuestData> activeQuests {get; set;} = new();
	public Dictionary<String, QuestData> completeQuests {get; set;} = new();
	
	private ItemList activeList;
	private ItemList completeList;
	private Label displayLabel;
	private Button trackButton;
	private Label questTestLabel;
	
	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		GD.Print("QuestControl: _Ready() called...");
		
		GD.Print("QuestControl: Assigning node references...");
		activeList = GetNode<ItemList>("HBoxContainer/QuestVBox/ActiveScroll/ActiveList");
		completeList = GetNode<ItemList>("HBoxContainer/QuestVBox/CompleteScroll/CompleteList");
		displayLabel = GetNode<Label>("HBoxContainer/DisplayVBox/DisplayScroll/DisplayLabel");
		trackButton = GetNode<Button>("HBoxContainer/DisplayVBox/Panel/TrackButton");
		questTestLabel = GetNode<Label>("QuestTestLabel");
		
		GD.Print("QuestControl: Assigning ItemList and Button actions...");
		
		try
		{
			activeList.ItemSelected += DisplayActiveQuest;
			GD.Print("ActiveList action assigned...");
			completeList.ItemSelected += DisplayCompleteQuest;
			GD.Print("CompleteList action assigned...");
			trackButton.Pressed += SetTrackedQuest;
			GD.Print("TrackButton action assigned...");
		}
		catch (Exception e)
		{
			GD.PrintErr($"Failed assigning actions: {e.Message}");
		}
		GD.Print("QuestControl: Populating active quests and ItemList...");
		foreach (var qst in GameData.QUESTS)
		{
			if (qst.Value.start)
			{
				activeQuests[qst.Key] = qst.Value;
				activeList.AddItem(qst.Value.name);
			}
		}
		
		DisplayQuestsTest();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	private void DisplayQuestsTest()
	{
		GD.Print("QuestControl: Testing loaded quests...");
		
		String text = "Quests:\n";
		
		foreach (var qst in GameData.QUESTS)
		{
			if (qst.Value.start)
			{
				text += $"\n{qst.Value.name} - Starting";
			}
			else
			{
				text += $"\n{qst.Value.name}";
			}
		}
		
		questTestLabel.Text = text;
	}
	
	private void DisplayActiveQuest(long index)
	{
		completeList.DeselectAll();
		
		String questName = activeList.GetItemText((int)index);
		String quest = GameData.qstNameToKey[questName];
		
		displayLabel.Text = DisplayQuestText(quest, true);
	}
	
	private void DisplayCompleteQuest(long index)
	{
		activeList.DeselectAll();
		
		String questName = completeList.GetItemText((int)index);
		String quest = GameData.qstNameToKey[questName];
		
		displayLabel.Text = DisplayQuestText(quest, false);
	}
	
	private String DisplayQuestText(String quest, bool active)
	{
		QuestData selectedQuest = GameData.QUESTS[quest];
		String text = selectedQuest.text;
		
		if (active)
		{
			text += $"\n\n{selectedQuest.hint}";
			
			QuestRequirement requirements = selectedQuest.requirement;
			
			if (requirements.resources.Count > 0)
			{
				text += "\n\nResources:";
				
				foreach (var res in requirements.resources)
				{
					text += $"\n{GameData.RESOURCES[res.Key].name}: {GameData.FormatUnit(res.Value, res.Key)}";
				}
			}
			
			if (requirements.machines.Count > 0)
			{
				text += "\n\nMachines:";
				
				foreach (var mach in requirements.machines)
				{
					text += $"\n{GameData.MACHINES[mach.Key].name}: {mach.Value}";
				}
			}
			
			if (requirements.quests.Count > 0)
			{
				text += "\n\nQuests:";
				
				foreach (var qst in requirements.quests)
				{
					text += $"\n{GameData.QUESTS[qst].name}";
				}
			}
		}
		
		return text;
	}
	
	private void SetTrackedQuest()
	{
		if (activeList.IsAnythingSelected())
		{
			int idx = activeList.GetSelectedItems()[0];
			String questName = activeList.GetItemText(idx);
			String questKey = GameData.qstNameToKey[questName];
			
			GD.Print($"QuestControl: Tracking quest {questName}...");
			
			GameData.trackedQuest = GameData.QUESTS[questKey];
			GD.Print("QuestControl: Assigned quest tracking...");
		}
		else
		{
			GD.Print("QuestControl: No quest selected.");
		}
	}
}
