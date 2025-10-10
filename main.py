import time
import threading

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