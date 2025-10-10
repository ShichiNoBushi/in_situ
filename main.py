import time
import threading
import tkinter as tk

resources = {
    "iron": 0.0
}
machines = {
    "miner": 0
}
running = True

def background_loop():
    global resources
    while running:
        resources["iron"] += machines["miner"] * 1
        time.sleep(1)

def update_resources():
    resources["iron"] += machines["miner"] * 1
    label_resource.config(text = f"Iron: {resources['iron']:.1f}")
    if running:
        root.after(1000, update_resources)

def mine_iron():
    resources["iron"] += 5
    label_resource.config(text = f"Iron: {resources['iron']:.1f}")

def build_miner():
    if resources["iron"] >= 20:
        resources["iron"] -= 20
        machines["miner"] += 1
        label_resource.config(text = f"Iron: {resources['iron']:.1f}")
        label_machines.config(text = f"Miner: {machines['miner']}")

root = tk.Tk()
label_resource = tk.Label(root, text = f"Iron: {resources['iron']:.1f}", font = ("Courier", 16))
label_resource.pack()
label_machines = tk.Label(root, text = f"Miner: {machines['miner']}", font = ("Courier", 16))
label_machines.pack()
button_mine = tk.Button(root, text = "Mine Iron", command = mine_iron)
button_mine.pack()
button_build = tk.Button(root, text = "Build Miner", command = build_miner)
button_build.pack()

root.after(1000, update_resources)
root.mainloop()

'''
def handle_input():
    global running
    while running:
        cmd = input("\n> ").strip().lower()

        if cmd == "mine iron":
            resources["iron"] += 5
            print(f"You mined 5 iron manually. Iron: {resources['iron']}")
        elif cmd == "build miner":
            if resources["iron"] >= 20:
                machines["miner"] += 1
                resources["iron"] -= 20
                print("Built 1 miner. It will now produce iron automatically.")
            else:
                print("Not enough iron")
        elif cmd == "status":
            print(f"Resources: {resources}")
            print(f"Machines: {machines}")
        elif cmd == "quit":
            running = False
        else:
            print("Commands: mine iron, build miner, status, quit")

threading.Thread(target = background_loop, daemon = True).start()
handle_input()
'''