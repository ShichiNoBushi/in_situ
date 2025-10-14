import time
import json
import tkinter as tk
from tkinter import ttk

# load from JSON file
def load_json(filename):
    with open(filename) as f:
        return json.load(f)

# load JSON data into reources
HARVEST = load_json("data/harvest.json")
MACHINES = load_json("data/machines.json")
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

resources = {} # list of resource stockpiles

# assign starting values to initial stockpiles
for rid, res in RESOURCES.items():
    starting_amount = res.get("starting amount", 0)
    resources[rid] = float(starting_amount)

machines = [] # list of constructed machines

# create starting machines
for mid, mach in MACHINES.items():
    starting_amount = mach.get("starting amount", 0)
    for i in range(starting_amount):
        machines.append(Machine(mid))

running = True # program is running
frame_rate = 10 # frames per second
last_time = time.time() # time of last update

def run_updates():
    global last_time
    time_delta = time.time() - last_time # time difference from last update
    process_machines(time_delta) # operate all active machines
    update_resources() # update resource displays
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
        if not mach.active or not mach.current_recipe:
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
    if selected in machines[idx].recipes:
        machines[idx].current_recipe = selected

# return formatted value based on unit and factor of 1,000 (M, k, _, m)
def format_unit(resource):
    amount = resources.get(resource, 0.0)
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
        label.config(text = format_unit(r))

machine_recipes_vars = {} # tk.StringVars dictionary keyed by machine index representing selected recipe

# update machine display
def refresh_machine_frames():
    # reset frame
    for widget in frame_machines.winfo_children():
        widget.destroy()
    machine_recipes_vars.clear()
    
    # for each crafted machine
    for i, m in enumerate(machines):
        mach_name = MACHINES[m.id].get("name", m.id) # player visible name of machine

        # frame specific to machine
        subframe = ttk.LabelFrame(frame_machines, text = mach_name, padding = 5)
        subframe.grid(row = i, column = 0)

        # text displaying machine Active/Inactive
        btn_text = "Active" if m.active else "Inactive"

        # button to turn machine on/off
        btn = ttk.Button(subframe, text = btn_text, width = 8, command = lambda idx = i: toggle_machine_active(idx))
        btn.grid(row = 0, column = 0, sticky = "w", padx = 2, pady = 2)

        # variable representing currently selected recipe
        var = tk.StringVar(value = m.current_recipe if m.current_recipe else "")

        # assign StringVar to index in dictionary
        machine_recipes_vars[i] = var

        if m.recipes: # if machine has recipes
            # option menu to select recipe from list available to machine
            option = ttk.OptionMenu(subframe, var, var.get(), *m.recipes, command = lambda selected, idx = i: change_machine_recipe(idx, selected))
            option.grid(row = 0, column = 1, sticky = "e", padx = 2, pady = 2)
        else:
            # label to display no recipes available to machine
            ttk.Label(subframe, text = "No recipes").grid(row = 0, column = 1, sticky = "e", padx = 2, pady = 2)

    update_scroll_region()

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
    for key, data in HARVEST.items():
        if data.get("action") == action:
            harvest_resource(data)
            break

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
    for key, data in MACHINES.items():
        if data.get("name") == machine:
            build_machine(key)

# build ore miner and update
def build_miner():
    if can_build("miner"): # check if sufficient resources
        cost = MACHINES["miner"].get("cost", {}) # total resource cost

        # remove amount of each required resource
        for res, amt in cost.items():
            resources[res] -= amt

        machines.append(Machine("miner")) # add new machine to list

        # update displays
        update_resources()
        refresh_machine_frames()

# end game
def quit_game():
    global running
    running = False
    root.destroy()

# create game window
root = tk.Tk()
root.title("In Situ")

# frames at top and bottom
frame_top = ttk.Frame(root, padding = 5)
frame_bottom = ttk.Frame(root, padding = 5)
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

# scrollbar for machines
scroll_machines = ttk.Scrollbar(frame_machines_super, orient = "vertical", command = canvas_machines.yview)
scroll_machines.pack(side = "right", fill = "y")

canvas_machines.configure(yscrollcommand = scroll_machines.set)

# frame to contain individual frames to display machines
frame_machines = ttk.LabelFrame(canvas_machines, text = "Machines", padding = 5)
canvas_machines.create_window(0, 0, window = frame_machines, anchor = "nw")

def update_scroll_region(event = None):
    canvas_machines.configure(scrollregion = canvas_machines.bbox("all"))

frame_machines.bind("<Configure>", update_scroll_region)

# labels to display each resource
resource_labels = {} # dictionary of labels keyed to resources

# for each resource
for i, r in enumerate(resources):
    # label for player visible name of resource
    ttk.Label(frame_resources, text = RESOURCES[r].get("name", r)).grid(row = i, column = 0, sticky = "w")

    # label displaying formatted quantities of resource
    lbl = ttk.Label(frame_resources, text = format_unit(r), width = 12, anchor = "e")
    lbl.grid(row = i, column = 1, sticky = "e")
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
harvest_var = tk.StringVar(value = list(HARVEST.keys())[0] if HARVEST else "")

# menu to select harvest action
option_harvest = ttk.OptionMenu(
    frame_harvest,
    harvest_var,
    HARVEST[harvest_var.get()]["action"] if harvest_var.get() in HARVEST else "",
    *[data["action"] for data in HARVEST.values()]
)
option_harvest.pack(side = "top")
ttk.Button(frame_harvest, text = "Harvest", command = perform_harvest).pack(side = "bottom", padx = 5) # button to harvest resources

# StringVar for selected machine to build
machine_var = tk.StringVar(value = list(MACHINES.keys())[0] if HARVEST else "")

# menu to select machine to build
option_build = ttk.OptionMenu(
    frame_build,
    machine_var,
    MACHINES[machine_var.get()]["name"] if machine_var.get() in MACHINES else "",
    *[data["name"] for data in MACHINES.values()]
)
option_build.pack(side = "top")
ttk.Button(frame_build, text = "Build", command = perform_build).pack(side = "bottom", padx = 5) # button to build machines

ttk.Button(frame_actions, text = "Build Miner", command = build_miner).pack(side = "left", padx = 5) # button to build miner
ttk.Button(frame_actions, text = "Quit", command = quit_game).pack(side = "right", padx = 5) # button to quit game

refresh_machine_frames() # update machines
last_time = time.time() # update current time
root.after(1000 // frame_rate, run_updates) # begin update loop
root.mainloop()
