import time
import json
import platform
import shlex
import tkinter as tk
from tkinter import ttk

# load from JSON file
def load_json(filename):
    with open(filename) as f:
        return json.load(f)

# load JSON data into reources
HARVEST = load_json("data/harvest.json")
MACHINES = load_json("data/machines.json")
QUESTS = load_json("data/quests.json")
RECIPES = load_json("data/recipes.json")
RESOURCES = load_json("data/resources.json")

# Machine object
class Machine:
    def __init__(self, machine_id):
        self.id = machine_id # reference to machine data
        self.active = False # if machine is on/off
        self.recipes = [
            rec_name for rec_name, rec_data in RECIPES.items()
            if self.id in rec_data.get("machines", [])
        ] # list of recipes available to machine
        self.current_recipe = self.recipes[0] if self.recipes else None # which recipe the machine is operating

    def toggle_active(self): # turn on/off
        self.active = not self.active

    def set_recipe(self, index): # assign working recipe
        self.current_recipe = self.recipes[index]

# dictionaries to map ids to names
harv_action_to_key = {}
mach_name_to_key = {}
qst_name_to_key = {}
rec_name_to_key = {}
res_name_to_key = {}

for hid, harvest in HARVEST.items():
    harv_action_to_key[harvest["action"]] = hid

for mid, machine in MACHINES.items():
    mach_name_to_key[machine["name"]] = mid

for qid, quest in QUESTS.items():
    qst_name_to_key[quest["name"]] = qid

for rid, recipe in RECIPES.items():
    rec_name_to_key[recipe["name"]] = rid

for rid, resource in RESOURCES.items():
    res_name_to_key[resource["name"]] = rid

resources = {} # list of resource stockpiles
res_types = {"untyped": []} # list of resource types with empty list of untyped resources

# assign starting values to initial stockpiles
for rid, res in RESOURCES.items():
    starting_amount = res.get("starting amount", 0)
    resources[rid] = float(starting_amount)
    rtype = res.get("type", "untyped")
    if rtype not in res_types:
        res_types[rtype] = []

    res_types[rtype].append(rid)

machines = [] # list of constructed machines

# create starting machines
for mid, mach in MACHINES.items():
    starting_amount = mach.get("starting amount", 0)
    for i in range(starting_amount):
        machines.append(Machine(mid))

active_quests = {}
completed_quests = {}

# initialize starting quests
for qid, quest in QUESTS.items():
    if quest.get("start", False):
        active_quests[qid] = quest

running = True # program is running
frame_rate = 10 # frames per second
last_time = time.time() # time of last update

def run_updates():
    global last_time
    time_delta = time.time() - last_time # time difference from last update
    process_machines(time_delta) # operate all active machines
    update_resources() # update resource displays
    check_quests() # check for completed quests
    last_time = time.time() # update time
    if running:
        root.after(1000 // frame_rate, run_updates) # loop

# returns whether can (1) or can't (0) craft and ratio between if insufficient resources
def can_craft(name, time_delta):
    if name not in RECIPES:
        return 0.0 # can't craft if recipe doesn't exist
    
    recipe = RECIPES[name]
    inputs = recipe.get("inputs", {})

    if not inputs:
        return 1.0 # can craft if recipe costs no resources
    
    ratios = []
    for res, amt in inputs.items():
        if amt <= 0:
            continue
        available = resources.get(res, 0.0)
        req_amt = amt * time_delta
        ratios.append(min(available / req_amt, 1.0))

    if not ratios:
        return 1.0 # can craft if all resources fulfilled
    
    min_ratio = min(ratios) # least among ratios

    if min_ratio <= 0:
        return 0.0 # 0 if somehow negative
        
    return max(0.0, min(min_ratio, 1.0)) # 0 if ratio below 0 and 1 if above 1

# run all active machine recipes for amount of time passed
def process_machines(time_delta):
    for mach in machines:
        if not mach.active or not mach.current_recipe or not RECIPES[mach.current_recipe]["available"]:
            continue # ignore if inactive

        ratio = can_craft(mach.current_recipe, time_delta) # ratio based on available resources for recipe
        if ratio <= 0:
            continue # ignore if can't craft

        rec = RECIPES[mach.current_recipe]
        rec_inputs = rec.get("inputs", {}) # input resources for recipe

        # remove all input resources modified by ratio and time passed
        for res, amt in rec_inputs.items():
            resources[res] = max(0.0, resources.get(res, 0.0) - amt * time_delta * ratio)

        rec_outputs = rec.get("outputs", {}) # output resources for recipe

        # increase all ouput resources modified by ratio and time passed
        for res, amt in rec_outputs.items():
            resources[res] = resources.get(res, 0.0) + amt * time_delta * ratio

# toggle machine by index and update
def toggle_machine_active(idx):
    machines[idx].toggle_active()
    refresh_machine_frames()

# set machine's active recipe and update
def change_machine_recipe(idx, selected):
    selected_id = rec_name_to_key[selected]
    if selected_id in machines[idx].recipes:
        machines[idx].current_recipe = selected_id
        update_recipe_labels(idx)

# return formatted value based on unit and factor of 1,000 (M, k, _, m)
def format_unit(amount, resource):
    unit = RESOURCES.get(resource, {}).get("unit", "u")

    if amount == 0:
        return f"0.00 {unit}" # default of 0 amount
    elif amount >= 900000:
        return f"{(amount/1000000):.2f} M{unit}" # 1,234,567 g -> 1.23 Mg
    elif amount >= 900:
        return f"{(amount/1000):.2f} k{unit}" # 1,234 g -> 1.23 kg
    elif amount >= 0.9:
        return f"{amount:.2f} {unit}" # 1.23 g
    elif amount >= 0.0009:
        return f"{(amount*1000):.2f} m{unit}" # .00123 g -> 1.23 mg
    else:
        return "negligible" # if non-zero but insignificantly small amount

# update resource display
def update_resources():
    for r, label in resource_labels.items():
        amt = resources.get(r, 0.0)
        label.config(text = format_unit(amt, r))

    for i in range(len(machines)):
        update_recipe_labels(i)

    refresh_build_menu()

    check_quests()

# check if any quests completed
def check_quests():
    to_complete = []

    for qid, quest in active_quests.items():
        requirements = quest.get("requirement", {})
        res_req = requirements.get("resources", {})
        mach_req = requirements.get("machines", {})
        quest_req = requirements.get("quests", [])

        res_fulfilled = len(res_req) == 0 or all(resources.get(r, 0) >= amt for r, amt in res_req.items())
        mach_fulfilled = len(mach_req) == 0 or all(sum(1 for m in machines if m.id == mid) >= amt for mid, amt in mach_req.items())
        quest_fulfilled = len(quest_req) == 0 or all(req in completed_quests for req in quest_req)

        if res_fulfilled and mach_fulfilled and quest_fulfilled and qid not in to_complete:
            to_complete.append(qid)

    for qid in to_complete:
        complete_quest(qid)

# mark a quest as completed and unlock related machines, recipes, and quests
def complete_quest(qid):
    if qid in completed_quests:
        return

    quest = QUESTS[qid]
    del active_quests[qid] # remove quest from active quests
    completed_quests[qid] = quest # add quest to completed quests

    # add next quest(s) to active quests
    for q in quest["unlocks"]["quests"]:
        if q not in completed_quests and q not in active_quests:
            active_quests[q] = QUESTS[q]

    # unlock recipes
    for rec in quest["unlocks"]["recipes"]:
        RECIPES[rec]["available"] = True

    # unlock machines
    for mach in quest["unlocks"]["machines"]:
        MACHINES[mach]["available"] = True

    update_quests()
    check_quests() # check if new quests are already fulfilled
    refresh_build_menu()
    refresh_machine_frames()

machine_recipes_vars = {} # tk.StringVars dictionary keyed by machine index representing selected recipe
machine_recipes_labels = {} # dictionary of labels to display costs of selected recipe

# update available machines
def refresh_build_menu():
    menu = option_build["menu"]
    menu.delete(0, "end")

    available_machines = [data["name"] for data in MACHINES.values() if data.get("available", False)]

    if not available_machines:
        machine_var.set("No available machines")
        menu.add_command(label = "No available machines", command = lambda: machine_var.set("No available machines"))
        return
    
    for name in available_machines:
        menu.add_command(label = name, command = lambda value = name: machine_var.set(value))

    machine_var.set(available_machines[0])

# update machine display
def refresh_machine_frames():
    # reset frame
    for widget in frame_machines.winfo_children():
        widget.destroy()
    machine_recipes_vars.clear()
    machine_recipes_labels.clear()
    
    # for each crafted machine
    for i, m in enumerate(machines):
        mach_name = MACHINES[m.id].get("name", m.id) # player visible name of machine

        # frame specific to machine
        subframe = ttk.LabelFrame(frame_machines, text = mach_name, width = 500, height = 150, padding = 5)
        subframe.grid(row = i, column = 0)
        subframe.grid_propagate(False)

        # text displaying machine Active/Inactive
        btn_text = "Active" if m.active else "Inactive"

        # button to turn machine on/off
        btn = ttk.Button(subframe, text = btn_text, width = 8, command = lambda idx = i: toggle_machine_active(idx))
        btn.grid(row = 0, column = 0, sticky = "w", padx = 2, pady = 2)

        # variable representing currently selected recipe
        var = tk.StringVar(value = RECIPES[m.current_recipe]["name"] if m.current_recipe else "")

        # assign StringVar to index in dictionary
        machine_recipes_vars[i] = var

        available_recipes = [RECIPES[r]["name"] for r in m.recipes if RECIPES.get(r, {}).get("available", False)]
        if available_recipes: # if machine has available recipes
            # option menu to select recipe from list available to machine
            option = ttk.OptionMenu(
                subframe,
                var,
                var.get() if var.get() in available_recipes else available_recipes[0],
                *available_recipes,
                command = lambda selected, idx = i: change_machine_recipe(idx, selected)
            )
            option.config(width = 15)
            option.grid(row = 0, column = 1, sticky = "e", padx = 2, pady = 2)
        else:
            # label to display no recipes available to machine
            ttk.Label(subframe, text = "No recipes", width = 20).grid(row = 0, column = 1, sticky = "e", padx = 2, pady = 2)

        rec = RECIPES[m.current_recipe]

        label_recipe_cost = ttk.Label(subframe, text = "No recipe selected", wraplength = 220, justify = "left", width = 20)
        label_recipe_cost.grid(row = 1, column = 0)
        label_recipe_output = ttk.Label(subframe, text = "", wraplength = 220, justify = "left", width = 20)
        label_recipe_output.grid(row = 1, column = 1)
        machine_recipes_labels[i] = (label_recipe_cost, label_recipe_output)

        update_recipe_labels(i)

    update_scroll_region()
    update_build_resources()

def update_recipe_labels(idx):
    mach = machines[idx]
    rec_id = mach.current_recipe
    rec = RECIPES.get(rec_id, {})

    labels = machine_recipes_labels.get(idx)
    if not labels:
        return
    
    label_in, label_out = labels
    
    if not rec or not rec.get("available", False):
        label_in.config(text = "No recipe selected")
        label_out.config(text = "")
        return
    
    inputs = rec.get("inputs", {})
    if not inputs:
        label_in.config(text = "No input cost")

    outputs = rec.get("outputs", {})

    cost_str_list = []
    for res, amt in inputs.items():
        res_name = RESOURCES.get(res, {}).get("name", res)
        res_format = format_unit(resources[res], res)
        amt_format = format_unit(amt, res)
        cost_str_list.append(f"{res_name}: {res_format} / {amt_format}")

    output_str_list= []
    for res, amt in outputs.items():
        res_name = RESOURCES.get(res, {}).get("name", res)
        amt_format = format_unit(amt, res)
        output_str_list.append(f"{res_name}: {amt_format}")

    label_in.config(text = "Input:\n" + "\n".join(cost_str_list))
    label_out.config(text = "Output:\n" + "\n".join(output_str_list))

# check if machine can be built
def can_build(name):
    if name not in MACHINES: # if machine does not exist
        return False
    
    cost = MACHINES[name].get("cost", {}) # resource cost of machine
    # check if each required resource is available
    for res, amt in cost.items():
        if resources.get(res, 0) < amt:
            return False # return false if insufficient resources
        
    return True # enough resources

# manually harvest resource and update
def harvest_resource(harvest):
    res = harvest.get("resource", "")

    if not res: # shortcuts if resource doesn't exist
        return

    # update resources based on harvest amount    
    resources[res] += harvest.get("amount", 0)
    update_resources()

# harvests resource based on selected action
def perform_harvest():
    action = harvest_var.get()
    harvest_id = harv_action_to_key[action]
    harvest_resource(HARVEST[harvest_id])

# build machine and update
def build_machine(key):
    if can_build(key):
        cost = MACHINES[key].get("cost", {}) # total resource cost

        # remove amound of each required resource
        for res, amt in cost.items():
            resources[res] -= amt

        machines.append(Machine(key)) # add new machine to list

        # update displays
        update_resources()
        refresh_machine_frames()

def perform_build():
    machine = machine_var.get()
    machine_id = mach_name_to_key[machine]
    build_machine(machine_id)

# dev give resources
def give_resources(res, amount):
    if res not in resources:
        return f"Resource {res} not found"
    
    resources[res] += amount
    
    update_resources()
    check_quests()
    
    return f"Gave {format_unit(amount, res)} of {RESOURCES[res]['name']}"

# dev give machine
def give_machine(mach):
    if mach not in MACHINES:
        return f"Machine {mach} not found"
    
    machines.append(Machine(mach))

    refresh_machine_frames()
    check_quests()

    return f"Gave machine {MACHINES[mach]['name']}"

# dev unlock quest
def unlock_quest(qid):
    if qid not in QUESTS:
        return f"Quest {qid} not found"
    
    if qid in active_quests or qid in completed_quests:
        return f"Quest {QUESTS[qid]['name']} already unlocked or completed"
    
    active_quests[qid] = QUESTS[qid]
    update_quests()
    check_quests()

    return f"Unlocked quest {QUESTS[qid]['name']}"

# dev complete quest
def complete_quest_dev(qid):
    if qid not in QUESTS:
        return f"Quest {qid} not found"
    
    if qid in completed_quests:
        return f"Quest {QUESTS[qid]['name']} already completed"
    
    quest = QUESTS[qid]
    
    if qid in active_quests:
        del active_quests[qid]
    completed_quests[qid] = quest

    # add next quest(s) to active quests
    for q in quest["unlocks"]["quests"]:
        if q not in completed_quests and q not in active_quests:
            active_quests[q] = QUESTS[q]

    # unlock recipes
    for rec in quest["unlocks"]["recipes"]:
        RECIPES[rec]["available"] = True

    # unlock machines
    for mach in quest["unlocks"]["machines"]:
        MACHINES[mach]["available"] = True

    update_quests()
    check_quests()
    refresh_build_menu()
    refresh_machine_frames()

    return f"Quest {QUESTS[qid]['name']} completed"

# dev unlock recipe
def unlock_recipe(rec):
    if rec not in RECIPES:
        return f"Recipe {rec} not found"
    
    RECIPES[rec]["available"] = True

    refresh_machine_frames()

    return f"Recipe {RECIPES[rec]['name']} unlocked"

# dev unlock machine
def unlock_machine(mach):
    if mach not in MACHINES:
        return f"Machine {mach} not found"
    
    MACHINES[mach]["available"] = True

    refresh_build_menu()

    return f"Machine {MACHINES[mach]['name']} unlocked"

# parse dev command and execute function
def execute_dev_command():
    cmd = entry_dev_command.get().strip()

    try:
        tokens = shlex.split(cmd)
    except Exception:
        label_dev_feedback.config(text = "Invalid command syntax")
        entry_dev_command.delete(0, tk.END)
        return
    
    if not tokens:
        label_dev_feedback.config(text = "No command entered")
        entry_dev_command.delete(0, tk.END)
        return
    
    action = tokens[0]
    feedback = ""

    try:
        if action == "give" and len(tokens) >= 3:
            sub = tokens[1].lower()

            if sub == "resource" and len(tokens) >= 4:
                res = tokens[2].lower()

                if res in RESOURCES:
                    try:
                        amt = float(tokens[3])
                    except Exception:
                        feedback = "Invalid amount"
                    else:
                        feedback = give_resources(res, amt)
                else:
                    feedback = f"Resource {res} not found"

            elif sub == "machine" and len(tokens) >= 3:
                mach = tokens[2].lower()

                if mach in MACHINES:
                    feedback = give_machine(mach)
                else:
                    feedback = f"Machine {mach} not found"

            else:
                feedback = "Unknown give command"
        elif action == "unlock" and len(tokens) >= 3:
            sub = tokens[1].lower()

            if sub == "quest":
                qid = tokens[2].lower()

                if qid in QUESTS:
                    feedback = unlock_quest(qid)
                else:
                    feedback = f"Quest {qid} not found"

            elif sub == "recipe":
                rec = tokens[2].lower()

                if rec in RECIPES:
                    feedback = unlock_recipe(rec)
                else:
                    feedback = f"Recipe {rec} not found"

            elif sub == "machine":
                mach = tokens[2].lower()

                if mach in MACHINES:
                    feedback = unlock_machine(mach)
                else:
                    feedback = f"Machine {mach} not found"

            else:
                feedback = "Unknown unlock command"

        elif action == "complete":
            sub = tokens[1].lower()

            if sub == "quest" and len(tokens) >= 3:
                qid = tokens[2].lower()

                if qid in QUESTS:
                    feedback = complete_quest_dev(qid)
                else:
                    feedback = f"Quest {qid} not found"

            else:
                feedback = "Unknown complete command"

        else:
            feedback = "Unknown command"

    except Exception as e:
        feedback = f"Error: {str(e)}"

    label_dev_feedback.config(text = feedback)
    entry_dev_command.delete(0, tk.END)

# end game
def quit_game():
    global running
    running = False
    root.destroy()

# create game window
root = tk.Tk()
root.title("In Situ")

# tabs for game menu
nb_main = ttk.Notebook(root)
nb_main.pack()

# base tab
tab_base = ttk.Frame(nb_main)
nb_main.add(tab_base, text = "Base")

# frames at top and bottom
frame_top = ttk.Frame(tab_base, padding = 5)
frame_bottom = ttk.Frame(tab_base, padding = 5)
frame_top.pack(fill = "both", expand = True)
frame_bottom.pack(fill = "x")

# frames to display resources and machines
frame_resources = ttk.LabelFrame(frame_top, text = "Resources", padding = 5)
frame_machines_super = ttk.Frame(frame_top)
frame_resources.grid(row = 0, column = 0, sticky = "nsew", padx = 5, pady = 5)
frame_machines_super.grid(row = 0, column = 1, sticky = "nsew", padx = 5, pady = 5)

frame_top.columnconfigure(0, weight = 1)
frame_top.columnconfigure(1, weight = 1)

# scrollable canvas for machines
canvas_machines = tk.Canvas(frame_machines_super)
canvas_machines.pack(side = "left", fill = "both", expand = True)

def _on_mousewheel(event):
    canvas_machines.yview_scroll(int(-1 * (event.delta / 120)), "units")

def _on_mac_mousewheel(event):
    canvas_machines.yview_scroll(int(-1 * event.delta), "units")

def _bind_to_mousewheel(event):
    canvas_machines.bind_all("<MouseWheel>", mousewheel_func)
    canvas_machines.bind_all("<Button-4>", lambda e: canvas_machines.yview_scroll(-1, "units"))
    canvas_machines.bind_all("<Button-5>", lambda e: canvas_machines.yview_scroll(1, "units"))

def _unbind_from_mousewheel(event):
    canvas_machines.unbind_all("<MouseWheel>")
    canvas_machines.unbind_all("Button-4")
    canvas_machines.unbind_all("Button-5")

# scrollbar for machines
scroll_machines = ttk.Scrollbar(frame_machines_super, orient = "vertical", command = canvas_machines.yview)
scroll_machines.pack(side = "right", fill = "y")

canvas_machines.configure(yscrollcommand = scroll_machines.set)

mousewheel_func = _on_mousewheel

if platform.system() == "Darwin":
    mousewheel_func = _on_mac_mousewheel

# frame to contain individual frames to display machines
frame_machines = ttk.LabelFrame(canvas_machines, text = "Machines", padding = 5)
canvas_machines.create_window(0, 0, window = frame_machines, anchor = "nw")

def update_scroll_region(event = None):
    canvas_machines.configure(scrollregion = canvas_machines.bbox("all"))

frame_machines.bind("<Configure>", update_scroll_region)
frame_machines.bind("<Enter>", _bind_to_mousewheel)
frame_machines.bind("<Leave>", _unbind_from_mousewheel)

# labels to display each resource
type_frames = {}
resource_labels = {} # dictionary of labels keyed to resources

# create subframes for resource types
for rtype in res_types.keys():
    if rtype == "untyped": # skip untyped resources
        continue

    type_frames[rtype] = ttk.LabelFrame(frame_resources, text = rtype.title(), padding = 5)
    type_frames[rtype].pack()

if res_types["untyped"]: # if there are any untyped resources, create a subframe for them; ignore otherwise
    type_frames["untyped"] = ttk.LabelFrame(frame_resources, text = "Untyped", padding = 5)

type_counts = {}
for t in res_types.keys():
    type_counts[t] = 0

# for each resource
for i, r in enumerate(resources):
    rtype = RESOURCES[r].get("type", "untyped") # assign to resource type
    subframe = type_frames[rtype] # select subframe for type
    rownum = type_counts[rtype] # get index for type
    type_counts[rtype] += 1 # and increment

    # label for player visible name of resource
    ttk.Label(subframe, text = RESOURCES[r].get("name", r), width = 20).grid(row = rownum, column = 0, sticky = "w")

    # label displaying formatted quantities of resource
    amt = resources.get(r, 0.0)
    lbl = ttk.Label(subframe, text = format_unit(amt, r), width = 12, anchor = "e")
    lbl.grid(row = rownum, column = 1, sticky = "e")
    resource_labels[r] = lbl # add display label to dictionary

# frame for player actions
frame_actions = ttk.LabelFrame(frame_bottom, text = "Actions", padding = 5)
frame_actions.pack(fill = "x")

# frame for harvesting resources
frame_harvest = ttk.Frame(frame_actions)
frame_harvest.pack(side = "left", padx = 5)

# frame for building machines
frame_build = ttk.Frame(frame_actions)
frame_build.pack(side = "left", padx = 5)

# StringVar for selected harvest actions
harvest_var = tk.StringVar(value = list(HARVEST.values())[0]["action"] if HARVEST else "")

# menu to select harvest action
option_harvest = ttk.OptionMenu(
    frame_harvest,
    harvest_var,
    HARVEST[harvest_var.get()]["action"] if harvest_var.get() in HARVEST else "",
    *[data["action"] for data in HARVEST.values()]
)
option_harvest.config(width = 15)
option_harvest.pack(side = "top")
ttk.Button(frame_harvest, text = "Harvest", command = perform_harvest).pack(side = "bottom", padx = 5) # button to harvest resources

# StringVar for selected machine to build
available_machines = [m["name"] for m in MACHINES.values() if m.get("available", False)]
machine_var = tk.StringVar(value = available_machines[0] if available_machines else "")

def update_build_resources():
    mach = MACHINES.get(mach_name_to_key.get(machine_var.get(), ""), {})

    if not mach:
        label_build_resources.config(text = "No machine selected")
        return
    
    build_resources = mach.get("cost", {})
    res_str_list = []

    if not build_resources:
        label_build_resources.config(text = "Error: No Resource Cost")
        return

    for res, amt in build_resources.items():
        res_str_list.append(f"{RESOURCES.get(res, {}).get('name', 'No Name')}: {format_unit(resources[res], res)} / {format_unit(amt, res)}")

    label_build_resources.config(text = "\n".join(res_str_list))

machine_var.trace_add("write", lambda *args: update_build_resources())

# menu to select machine to build
option_build = ttk.OptionMenu(
    frame_build,
    machine_var,
    MACHINES[machine_var.get()]["name"] if machine_var.get() in MACHINES else "",
    *[data["name"] for data in MACHINES.values()],
)
option_build.config(width = 15)
option_build.pack(side = "top")
ttk.Button(frame_build, text = "Build", command = perform_build).pack(side = "bottom", padx = 5) # button to build machines

label_build_resources = ttk.Label(frame_build, text = "No machine selected", wraplength = 200, justify = "left", width = 25)
label_build_resources.pack(side = "bottom")

update_build_resources()

def update_quests():
    txt_active_quests.configure(state = "normal")
    txt_completed_quests.configure(state = "normal")

    txt_active_quests.delete("1.0", tk.END)
    txt_completed_quests.delete("1.0", tk.END)

    if len(active_quests.values()) > 0:
        for quest in active_quests.values():
            txt_active_quests.insert("end", f"{quest['name']}\n")
            txt_active_quests.insert("end", f"  {quest.get('text', '')}\n\n")
            if quest.get("hint", "") != "":
                txt_active_quests.insert("end", f"Hint:\n  {quest['hint']}\n\n")
    else:
        txt_active_quests.insert("end", "No active quests.\n\n")

    if len(completed_quests.values()) > 0:
        for quest in completed_quests.values():
            txt_completed_quests.insert("end", f"{quest['name']}\n")
            txt_completed_quests.insert("end", f"  {quest.get('text', '')}\n\n")
    else:
        txt_completed_quests.insert("end", "No completed quests.\n\n")

    txt_active_quests.configure(state = "disabled")
    txt_completed_quests.configure(state = "disabled")

    refresh_build_menu()

# quests tab
tab_quests = ttk.Frame(nb_main)
nb_main.add(tab_quests, text = "Quests")

# frame to display quests
frame_aquests = ttk.LabelFrame(tab_quests, text = "Active Quests")
frame_cquests = ttk.LabelFrame(tab_quests, text = "Completed Quests")
frame_aquests.grid(row = 0, column = 0)
frame_cquests.grid(row = 1, column = 0)

txt_active_quests = tk.Text(frame_aquests, wrap = "word", height = 20, width = 60, state = "disabled")
txt_active_quests.pack(side = "left", fill = "both", expand = True)

scroll_active_quests = ttk.Scrollbar(frame_aquests, command = txt_active_quests.yview)
scroll_active_quests.pack(side = "right", fill = "y")
txt_active_quests.configure(yscrollcommand = scroll_active_quests.set)

txt_completed_quests = tk.Text(frame_cquests, wrap = "word", height = 20, width = 60, state = "disabled")
txt_completed_quests.pack(side = "left", fill = "both", expand = True)

scroll_completed_quests = ttk.Scrollbar(frame_cquests, command = txt_completed_quests.yview)
scroll_completed_quests.pack(side = "right", fill = "y")
txt_completed_quests.configure(yscrollcommand = scroll_completed_quests.set)

# options tab
tab_options = ttk.Frame(nb_main)
nb_main.add(tab_options, text = "Options")

ttk.Button(tab_options, text = "Quit", command = quit_game).pack(side = "top", padx = 5) # button to quit game

frame_dev_commands = tk.Frame(tab_options)
frame_dev_commands.pack(side = "bottom", fill = "x", padx = 10, pady = 10)

entry_dev_command = tk.Entry(frame_dev_commands, width = 40)
entry_dev_command.bind("<Return>", lambda event: execute_dev_command())
entry_dev_command.grid(row = 0, column = 0, padx = 2, pady = 2)

ttk.Button(frame_dev_commands, text = "Execute", command = execute_dev_command).grid(row = 0, column = 1, padx = 2, pady = 2)

label_dev_feedback = ttk.Label(frame_dev_commands, text = "")
label_dev_feedback.grid(row = 1, column = 0, padx = 2, pady = 2)

update_quests()

refresh_build_menu() # update buildable machines
refresh_machine_frames() # update machines
last_time = time.time() # update current time
root.after(1000 // frame_rate, run_updates) # begin update loop
root.mainloop()
