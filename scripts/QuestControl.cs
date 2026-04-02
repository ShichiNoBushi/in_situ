using Godot;
using System;
using System.Collections.Generic;

public partial class QuestControl : Control
{
	public Dictionary<string, QuestData> activeQuests {get; set;} = new();
	public Dictionary<string, QuestData> completeQuests {get; set;} = new();
	
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
		
		DisplayQuestsTest();
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
	
	public void GiveStartingQuests()
	{
		activeQuests.Clear();
		completeQuests.Clear();
		
		GD.Print("QuestControl: Populating active quests and ItemList...");
		foreach (var qst in GameData.QUESTS)
		{
			if (qst.Value.start)
			{
				activeQuests[qst.Key] = qst.Value;
				/*activeList.AddItem(qst.Value.name);
				int idx = activeList.ItemCount - 1;
				activeList.SetItemMetadata(idx, qst.Key);*/
			}
		}
		
		UpdateQuestLists();
	}
	
	public void LoadFromSave(List<string> active, List<string> complete)
	{
		activeQuests.Clear();
		foreach (var a in active)
		{
			if (GameData.QUESTS.ContainsKey(a))
			{
				activeQuests[a] = GameData.QUESTS[a];
			}
			else
			{
				GD.Print($"QuestControl: unknown quest key in active quests {a}");
			}
		}
		
		completeQuests.Clear();
		foreach (var c in complete)
		{
			if (GameData.QUESTS.ContainsKey(c))
			{
				completeQuests[c] = GameData.QUESTS[c];
			}
			else
			{
				GD.Print($"QuestControl: unknown quest key in complete quests{c}");
			}
		}
		
		UpdateQuestLists();
	}
	
	public void UpdateQuestLists()
	{
		activeList.Clear();
		completeList.Clear();
		displayLabel.Text = "No quest selected";
		
		foreach (var quest in activeQuests)
		{
			activeList.AddItem(quest.Value.name);
			int idx = activeList.ItemCount - 1;
			activeList.SetItemMetadata(idx, quest.Key);
		}
		
		if (activeList.ItemCount > 0)
		{
			activeList.Select(0);
			DisplayActiveQuest(0);
		}
		
		foreach (var quest in completeQuests)
		{
			completeList.AddItem(quest.Value.name);
			int idx = completeList.ItemCount - 1;
			completeList.SetItemMetadata(idx, quest.Key);
		}
		
		trackButton.Disabled = activeList.ItemCount == 0;
	}
	
	private void DisplayQuestsTest()
	{
		GD.Print("QuestControl: Testing loaded quests...");
		
		string text = "Quests:\n";
		
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
		
		try
		{
			string questName = activeList.GetItemText((int)index);
			//String quest = GameData.qstNameToKey[questName];
			string quest = (string)activeList.GetItemMetadata((int)index);
			
			if (GameData.QUESTS.ContainsKey(quest))
			{
				displayLabel.Text = DisplayQuestText(quest, true);
				
				trackButton.Disabled = false;
			}
			else
			{
				displayLabel.Text = $"Error displaying quest information\nQuest key not in dictionary: {quest}";
				trackButton.Disabled = true;
			}
		}
		catch (Exception e)
		{
			displayLabel.Text = $"Error displaying quest information\n{e}";
			GD.PrintErr($"QuestControl: Error displaying quest information - {e}");
		}
	}
	
	private void DisplayCompleteQuest(long index)
	{
		activeList.DeselectAll();
		
		try
		{
			string questName = completeList.GetItemText((int)index);
			//String quest = GameData.qstNameToKey[questName];
			string quest = (string)completeList.GetItemMetadata((int)index);
			
			if (GameData.QUESTS.ContainsKey(quest))
			{
				displayLabel.Text = DisplayQuestText(quest, false);
				
				trackButton.Disabled = true;
			}
			else
			{
				displayLabel.Text = $"Error displaying quest information\nQuest key not in dictionary: {quest}";
				trackButton.Disabled = true;
			}
		}
		catch (Exception e)
		{
			displayLabel.Text = $"Error displaying quest information\n{e}";
			GD.PrintErr($"QuestControl: Error displaying quest information - {e}");
		}
	}
	
	private string DisplayQuestText(string quest, bool active)
	{
		QuestData selectedQuest = GameData.QUESTS[quest];
		string text = selectedQuest.text;
		
		string status = active ? "active" : "completed";
		
		GD.Print($"QuestControl: display {status} quest {selectedQuest.name}");
		
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
				text += "\n\nMachines and Infrastructure:";
				
				foreach (var mach in requirements.machines)
				{
					if (GameData.MACHINES.ContainsKey(mach.Key))
					{
						text += $"\n{GameData.MACHINES[mach.Key].name}: {mach.Value}";
					}
					else if (GameData.INFRASTRUCTURE.ContainsKey(mach.Key))
					{
						text += $"\n{GameData.INFRASTRUCTURE[mach.Key].name}: {mach.Value}";
					}
					else
					{
						text += $"\n[Missing Buildable Key: {mach.Key}]: {mach.Value}";
					}
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
			try
			{
				int idx = activeList.GetSelectedItems()[0];
				string questName = activeList.GetItemText(idx);
				//String questKey = GameData.qstNameToKey[questName];
				string questKey = (string)activeList.GetItemMetadata(idx);
				
				GD.Print($"QuestControl: Tracking quest {questName}...");
				
				if (GameData.QUESTS.ContainsKey(questKey))
				{
					GameData.trackedQuest = GameData.QUESTS[questKey];
					GD.Print("QuestControl: Assigned quest tracking...");
				}
				else
				{
					GD.Print($"QuestControl: Quest key not found - {questKey}");
				}
			}
			catch (Exception e)
			{
				GD.Print($"QuestControl: Error assigning tracked quest - {e}");
			}
		}
		else
		{
			GD.Print("QuestControl: No quest selected.");
		}
	}
}
