import time
import json
import tkinter as tk
from tkinter import ttk

def load_json(filename):
    with open(filename) as f:
        return json.load(f)

MACHINES = load_json("data/machines.json")
RECIPES = load_json("data/recipes.json")
RESOURCES = load_json("data/resources.json")

class Machine:
    def __init__(self, machine_id):
        self.id = machine_id
        self.active = False
        self.recipes = [
            rec_name for rec_name, rec_data in RECIPES.items()
            if self.id in rec_data.get("machines", [])
        ]
        self.current_recipe = self.recipes[0] if self.recipes else None

    def toggle_active(self):
        self.active = not self.active

    def set_recipe(self, index):
        self.current_recipe = self.recipes[index]

resources = {}

for rid, res in RESOURCES.items():
    starting_amount = res.get("starting amount", 0)
    resources[rid] = float(starting_amount)

machines = []

for mid, mach in MACHINES.items():
    starting_amount = mach.get("starting amount", 0)
    for i in range(starting_amount):
        machines.append(Machine(mid))

running = True
frame_rate = 30
last_time = time.time()

def run_updates():
    global last_time
    time_delta = time.time() - last_time
    process_machines(time_delta)
    update_resources()
    last_time = time.time()
    if running:
        root.after(1000 // frame_rate, run_updates)

def can_craft(name):
    if name not in RECIPES:
        return 0.0
    
    recipe = RECIPES[name]
    inputs = recipe.get("inputs", {})

    if not inputs:
        return 1.0
    
    ratios = []
    for res, amt in inputs.items():
        if amt <= 0:
            continue
        available = resources.get(res, 0.0)
        req_amt = amt
        ratios.append(min(available / req_amt, 1.0))

    if not ratios:
        return 1.0
    
    min_ratio = min(ratios)

    if min_ratio <= 0:
        return 0.0
        
    return max(0.0, min(min_ratio, 1.0))

def process_machines(time_delta):
    for mach in machines:
        if not mach.active or not mach.current_recipe:
            continue

        ratio = can_craft(mach.current_recipe)
        if ratio <= 0:
            continue

        rec = RECIPES[mach.current_recipe]
        rec_inputs = rec.get("inputs", {})

        for res, amt in rec_inputs.items():
            resources[res] = max(0.0, resources.get(res, 0.0) - amt * time_delta * ratio)

        rec_outputs = rec.get("outputs", {})

        for res, amt in rec_outputs.items():
            resources[res] = resources.get(res, 0.0) + amt * time_delta * ratio

def toggle_machine_active(idx):
    machines[idx].toggle_active()
    refresh_machine_frames()

def change_machine_recipe(idx, selected):
    if selected in machines[idx].recipes:
        machines[idx].current_recipe = selected

def update_resources():
    for r, label in resource_labels.items():
        label.config(text = f"{resources[r]:.2f}")

machine_recipes_vars = {}

def refresh_machine_frames():
    for widget in frame_machines.winfo_children():
        widget.destroy()
    machine_recipes_vars.clear()
    
    for i, m in enumerate(machines):
        mach_name = MACHINES[m.id].get("name", m.id)
        subframe = ttk.LabelFrame(frame_machines, text = mach_name, padding = 5)
        subframe.grid(row = i, column = 0)
        btn_text = "Active" if m.active else "Inactive"
        btn = ttk.Button(subframe, text = btn_text, width = 8, command = lambda idx = i: toggle_machine_active(idx))
        btn.grid(row = 0, column = 0, sticky = "w", padx = 2, pady = 2)
        var = tk.StringVar(value = m.current_recipe if m.current_recipe else "")
        machine_recipes_vars[i] = var
        if m.recipes:
            option = ttk.OptionMenu(subframe, var, var.get(), *m.recipes, command = lambda selected, idx = i: change_machine_recipe(idx, selected))
            option.grid(row = 0, column = 1, sticky = "e", padx = 2, pady = 2)
        else:
            ttk.Label(subframe, text = "No recipes").grid(row = 0, column = 1, sticky = "e", padx = 2, pady = 2)

    update_scroll_region()

def can_build(name):
    if name not in MACHINES:
        return False
    
    cost = MACHINES[name].get("cost", {})
    for res, amt in cost.items():
        if resources.get(res, 0) < amt:
            return False
        
    return True

def mine_iron():
    resources["iron ore"] += 5
    update_resources()

def build_miner():
    if can_build("miner"):
        cost = MACHINES["miner"].get("cost", {})
        for res, amt in cost.items():
            resources[res] -= amt
        machines.append(Machine("miner"))
        update_resources()
        refresh_machine_frames()

def quit_game():
    global running
    running = False
    root.destroy()

root = tk.Tk()
root.title("In Situ")

frame_top = ttk.Frame(root, padding = 5)
frame_bottom = ttk.Frame(root, padding = 5)
frame_top.pack(fill = "both", expand = True)
frame_bottom.pack(fill = "x")

frame_resources = ttk.LabelFrame(frame_top, text = "Resources", padding = 5)
frame_machines_super = ttk.Frame(frame_top)
frame_resources.grid(row = 0, column = 0, sticky = "nsew", padx = 5, pady = 5)
frame_machines_super.grid(row = 0, column = 1, sticky = "nsew", padx = 5, pady = 5)

frame_top.columnconfigure(0, weight = 1)
frame_top.columnconfigure(1, weight = 1)

canvas_machines = tk.Canvas(frame_machines_super)
canvas_machines.pack(side = "left", fill = "both", expand = True)

scroll_machines = ttk.Scrollbar(frame_machines_super, orient = "vertical", command = canvas_machines.yview)
scroll_machines.pack(side = "right", fill = "y")

canvas_machines.configure(yscrollcommand = scroll_machines.set)

frame_machines = ttk.LabelFrame(canvas_machines, text = "Machines", padding = 5)
canvas_machines.create_window(0, 0, window = frame_machines, anchor = "nw")

def update_scroll_region(event = None):
    canvas_machines.configure(scrollregion = canvas_machines.bbox("all"))

frame_machines.bind("<Configure>", update_scroll_region)

resource_labels = {}
for i, r in enumerate(resources):
    ttk.Label(frame_resources, text = RESOURCES[r].get("name", r)).grid(row = i, column = 0, sticky = "w")
    lbl = ttk.Label(frame_resources, text = f"{resources[r]:.2f}")
    lbl.grid(row = i, column = 1, sticky = "e")
    resource_labels[r] = lbl

frame_actions = ttk.LabelFrame(frame_bottom, text = "Actions", padding = 5)
frame_actions.pack(fill = "x")

ttk.Button(frame_actions, text = "Mine Iron", command = mine_iron).pack(side = "left", padx = 5)
ttk.Button(frame_actions, text = "Build Miner", command = build_miner).pack(side = "left", padx = 5)
ttk.Button(frame_actions, text = "Quit", command = quit_game).pack(side = "right", padx = 5)

refresh_machine_frames()
root.after(1000 // frame_rate, run_updates)
root.mainloop()
