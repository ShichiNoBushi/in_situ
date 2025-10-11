import time
import tkinter as tk
from tkinter import ttk

resources = {
    "iron": 0.0,
    "energy": 10.0
}
machines = {
    "miner": 0,
    "srg": 1
}
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

def process_machines(time_delta):
    resources["energy"] += machines["srg"] * 1 * time_delta

    energy_needed = machines["miner"] * .5 * time_delta
    if resources["energy"] >= energy_needed:
        resources["iron"] += machines["miner"] * 1 * time_delta
        resources["energy"] -= energy_needed
    else:
        energy_ratio = resources["energy"] / energy_needed
        resources["iron"] += machines["miner"] * 1 * energy_ratio * time_delta
        resources["energy"] = 0

    resources["energy"] = max(0.0, resources["energy"])

def update_resources():
    for r, label in resource_labels.items():
        label.config(text = f"{resources[r]:.1f}")

def update_machines():
    for m, label in machine_labels.items():
        label.config(text = f"{machines[m]}")

def mine_iron():
    resources["iron"] += 5
    update_resources()

def build_miner():
    if resources["iron"] >= 20:
        resources["iron"] -= 20
        machines["miner"] += 1
        update_resources()
        update_machines()

root = tk.Tk()
root.title("In Situ")

frame_top = ttk.Frame(root, padding = 5)
frame_bottom = ttk.Frame(root, padding = 5)
frame_top.pack(fill = "both", expand = True)
frame_bottom.pack(fill = "x")

frame_resources = ttk.LabelFrame(frame_top, text = "Resources", padding = 5)
frame_machines = ttk.LabelFrame(frame_top, text = "Machines", padding = 5)
frame_resources.grid(row = 0, column = 0, sticky = "nsew", padx = 5, pady = 5)
frame_machines.grid(row = 0, column = 1, sticky = "nsew", padx = 5, pady = 5)

frame_top.columnconfigure(0, weight = 1)
frame_top.columnconfigure(1, weight = 1)

resource_labels = {}
for i, r in enumerate(resources):
    ttk.Label(frame_resources, text = r).grid(row = i, column = 0, sticky = "w")
    lbl = ttk.Label(frame_resources, text = f"{resources[r]:.1f}")
    lbl.grid(row = i, column = 1, sticky = "e")
    resource_labels[r] = lbl

machine_labels = {}
for i, m in enumerate(machines):
    ttk.Label(frame_machines, text = m).grid(row = i, column = 0, sticky = "w")
    lbl = ttk.Label(frame_machines, text = f"{machines[m]}")
    lbl.grid(row = i, column = 1, sticky = "e")
    machine_labels[m] = lbl

frame_actions = ttk.LabelFrame(frame_bottom, text = "Actions", padding = 5)
frame_actions.pack(fill = "x")

ttk.Button(frame_actions, text = "Mine Iron", command = mine_iron).pack(side = "left", padx = 5)
ttk.Button(frame_actions, text = "Build Miner", command = build_miner).pack(side = "left", padx = 5)
ttk.Button(frame_actions, text = "Quit", command = root.destroy).pack(side = "right", padx = 5)

root.after(1000 // frame_rate, run_updates)
root.mainloop()
